//! Frame generation that needs nothing of the card's maker (`VICTORY_VI_FRAMEGEN=1`): for cards
//! without NVIDIA Smooth Motion (GTX 10, RTX 20/30, AMD, Intel). Between two frames the title
//! presents, a third is made from the two and shown halfway between them.
//!
//! It works on the swap chain's buffer, after the frame is composed into it (at the window's
//! size, the interface in it), so it needs nothing of the title's (no motion vectors, no depth):
//! - a compute pass finds, for each 8x8 block of the newer frame, where it was in the older one
//!   (block matching on luma over ±28 pixels, coarse then fine, small motion preferred);
//! - a pixel pass draws each pixel from both frames moved half way along that motion, and takes
//!   the newer frame where the two disagree (things that appear, the interface's edges).
//!
//! The generated frame is presented at once, in place of the newer one; the newer one is kept
//! and presented half a frame interval later by a thread of its own ([`Pacer`], through
//! [`crate::host::Host::show_held`]), so the title's thread never waits for it. The cost: half a
//! frame of delay, a copy of the frame and two short passes on the card. It does not make more
//! of the title's own frames; motion looks smoother.

use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::{Arc, Condvar, Mutex, OnceLock};
use std::time::{Duration, Instant};

use windows::Win32::Graphics::Direct3D::Fxc::D3DCompile;
use windows::Win32::Graphics::Direct3D::ID3DBlob;
use windows::Win32::Graphics::Direct3D12::*;
use windows::Win32::Graphics::Dxgi::Common::*;
use windows::core::{PCSTR, s};

use crate::host::{Host, HostList, transition};

/// `VICTORY_VI_FRAMEGEN=1`.
pub(crate) fn on() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    *ON.get_or_init(|| {
        let on = std::env::var_os("VICTORY_VI_FRAMEGEN").is_some_and(|v| v == "1");
        if on {
            tracing::info!("frame generation on (VICTORY_VI_FRAMEGEN): a frame made between each two of the title's");
        }
        on
    })
}

const SOURCE: &str = r#"
#define RS "DescriptorTable(SRV(t0, numDescriptors=3), UAV(u0)), StaticSampler(s0, filter=FILTER_MIN_MAG_MIP_LINEAR, addressU=TEXTURE_ADDRESS_CLAMP, addressV=TEXTURE_ADDRESS_CLAMP)"
Texture2D<float4> older : register(t0);
Texture2D<float4> newer : register(t1);
Texture2D<float2> moved : register(t2);
RWTexture2D<float2> motion : register(u0);
SamplerState lin : register(s0);

float luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

// The block's 16 samples (every other pixel of 8x8) against `older` shifted by d.
float cost(float b[16], int2 origin, int2 d, int2 last) {
    float s = 0;
    [unroll] for (int i = 0; i < 16; i++) {
        int2 p = clamp(origin + int2((i & 3) * 2, (i >> 2) * 2) + d, int2(0, 0), last);
        s += abs(luma(older.Load(int3(p, 0)).rgb) - b[i]);
    }
    return s + 0.004 * (abs(d.x) + abs(d.y));
}

// One thread per 8x8 block of `newer`: the shift (pixels) to where it was in `older`.
[RootSignature(RS)]
[numthreads(8, 8, 1)]
void match_blocks(uint3 id : SV_DispatchThreadID) {
    uint w, h;
    newer.GetDimensions(w, h);
    int2 origin = int2(id.xy) * 8;
    if (origin.x >= (int)w || origin.y >= (int)h) return;
    int2 last = int2(w - 1, h - 1);
    float b[16];
    [unroll] for (int i = 0; i < 16; i++)
        b[i] = luma(newer.Load(int3(min(origin + int2((i & 3) * 2, (i >> 2) * 2), last), 0)).rgb);
    float best = cost(b, origin, int2(0, 0), last);
    int2 found = int2(0, 0);
    for (int y = -28; y <= 28; y += 4)
        for (int x = -28; x <= 28; x += 4) {
            float c = cost(b, origin, int2(x, y), last);
            if (c < best) { best = c; found = int2(x, y); }
        }
    int2 around = found;
    for (int y2 = -3; y2 <= 3; y2++)
        for (int x2 = -3; x2 <= 3; x2++) {
            int2 d = around + int2(x2, y2);
            float c = cost(b, origin, d, last);
            if (c < best) { best = c; found = d; }
        }
    motion[id.xy] = float2(found);
}

struct V { float4 at : SV_Position; float2 uv : TEXCOORD0; };
[RootSignature(RS)]
V vs(uint id : SV_VertexID) {
    V v;
    v.uv = float2((id << 1) & 2, id & 2);
    v.at = float4(v.uv * float2(2, -2) + float2(-1, 1), 0, 1);
    return v;
}

// The frame halfway: each pixel from both frames moved half way along the motion found.
[RootSignature(RS)]
float4 halfway(V v) : SV_Target {
    uint w, h;
    newer.GetDimensions(w, h);
    float2 d = moved.SampleLevel(lin, v.uv, 0) * 0.5 / float2(w, h);
    float3 a = older.SampleLevel(lin, v.uv + d, 0).rgb;
    float3 b = newer.SampleLevel(lin, v.uv - d, 0).rgb;
    float3 n = newer.SampleLevel(lin, v.uv, 0).rgb;
    float apart = abs(luma(a) - luma(b));
    return float4(lerp((a + b) * 0.5, n, saturate((apart - 0.06) * 10)), 1);
}
"#;

fn compile(entry: PCSTR, target: PCSTR) -> windows::core::Result<ID3DBlob> {
    let mut code: Option<ID3DBlob> = None;
    let mut errors: Option<ID3DBlob> = None;
    // SAFETY: the source and names live through the call; the out pointers are ours.
    let made = unsafe {
        D3DCompile(SOURCE.as_ptr().cast(), SOURCE.len(), s!("framegen"), None, None, entry, target, 0, 0, &mut code, Some(&mut errors))
    };
    if let Err(e) = made {
        if let Some(errors) = errors {
            // SAFETY: the blob's own bytes.
            let text = unsafe { std::slice::from_raw_parts(errors.GetBufferPointer().cast::<u8>(), errors.GetBufferSize()) };
            tracing::warn!(errors = %String::from_utf8_lossy(text), "frame generation shaders did not compile");
        }
        return Err(e);
    }
    code.ok_or_else(windows::core::Error::empty)
}

/// The frames the generator keeps, and its passes.
pub(crate) struct FrameGen {
    root: ID3D12RootSignature,
    blocks: ID3D12PipelineState,
    halfway: ID3D12PipelineState,
    format: DXGI_FORMAT,
    size: (u32, u32),
    /// The last two frames (the swap chain's buffer as composed), by turns.
    frames: [ID3D12Resource; 2],
    /// Which of `frames` holds the newest, and whether the other holds one.
    newest: usize,
    have_older: bool,
    motion: ID3D12Resource,
    /// A heap of 8 views for each presenter list slot (as [`crate::compose::Composer`]).
    views: Vec<ID3D12DescriptorHeap>,
    step: u32,
    /// The list the pacer shows the held frame with.
    pub(crate) held_list: Option<HostList>,
    /// The number of the frame held for the pacer (the newest real one).
    pub(crate) held: u64,
    last_real: Option<Instant>,
    /// Half the title's frame interval, smoothed.
    half: Duration,
    pacer: Arc<Pacer>,
}
// SAFETY: D3D12 objects are free-threaded; the generator is used under the presenter's lock.
unsafe impl Send for FrameGen {}
// SAFETY: as above.
unsafe impl Sync for FrameGen {}

/// Frames generated and held frames shown, for the log line.
pub(crate) static MADE: [AtomicU64; 2] = [AtomicU64::new(0), AtomicU64::new(0)];

fn texture(device: &ID3D12Device, format: DXGI_FORMAT, (w, h): (u32, u32), uav: bool, state: D3D12_RESOURCE_STATES) -> windows::core::Result<ID3D12Resource> {
    let mut made: Option<ID3D12Resource> = None;
    // SAFETY: plain creation; the descriptions live through the call.
    unsafe {
        device.CreateCommittedResource(
            &D3D12_HEAP_PROPERTIES { Type: D3D12_HEAP_TYPE_DEFAULT, ..Default::default() },
            D3D12_HEAP_FLAG_NONE,
            &D3D12_RESOURCE_DESC {
                Dimension: D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                Width: u64::from(w),
                Height: h,
                DepthOrArraySize: 1,
                MipLevels: 1,
                Format: format,
                SampleDesc: DXGI_SAMPLE_DESC { Count: 1, Quality: 0 },
                Flags: if uav { D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS } else { D3D12_RESOURCE_FLAG_NONE },
                ..Default::default()
            },
            state,
            None,
            &mut made,
        )?;
    }
    made.ok_or_else(windows::core::Error::empty)
}

const READ: D3D12_RESOURCE_STATES = D3D12_RESOURCE_STATES(
    D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE.0 | D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE.0,
);

impl FrameGen {
    /// A generator for swap chain buffers of `format` and `size`.
    pub(crate) fn new(host: &Host, format: DXGI_FORMAT, size: (u32, u32)) -> windows::core::Result<Self> {
        let (blocks, vs, halfway) = (compile(s!("match_blocks"), s!("cs_5_1"))?, compile(s!("vs"), s!("vs_5_1"))?, compile(s!("halfway"), s!("ps_5_1"))?);
        let device = &host.device;
        // SAFETY: the blobs' own bytes, alive through the calls.
        unsafe {
            let bytes = |b: &ID3DBlob| std::slice::from_raw_parts(b.GetBufferPointer().cast::<u8>(), b.GetBufferSize());
            let code = |b: &ID3DBlob| D3D12_SHADER_BYTECODE { pShaderBytecode: b.GetBufferPointer(), BytecodeLength: b.GetBufferSize() };
            let root: ID3D12RootSignature = device.CreateRootSignature(0, bytes(&blocks))?;
            let blocks_pso: ID3D12PipelineState = device.CreateComputePipelineState(&D3D12_COMPUTE_PIPELINE_STATE_DESC {
                pRootSignature: std::mem::transmute_copy(&root),
                CS: code(&blocks),
                ..Default::default()
            })?;
            let mut desc = D3D12_GRAPHICS_PIPELINE_STATE_DESC {
                pRootSignature: std::mem::transmute_copy(&root),
                VS: code(&vs),
                PS: code(&halfway),
                SampleMask: u32::MAX,
                RasterizerState: D3D12_RASTERIZER_DESC { FillMode: D3D12_FILL_MODE_SOLID, CullMode: D3D12_CULL_MODE_NONE, ..Default::default() },
                PrimitiveTopologyType: D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                NumRenderTargets: 1,
                SampleDesc: DXGI_SAMPLE_DESC { Count: 1, Quality: 0 },
                ..Default::default()
            };
            desc.RTVFormats[0] = format;
            desc.BlendState.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL.0 as u8;
            let halfway_pso: ID3D12PipelineState = device.CreateGraphicsPipelineState(&desc)?;
            let frames = [texture(device, format, size, false, READ)?, texture(device, format, size, false, READ)?];
            let motion = texture(device, DXGI_FORMAT_R16G16_FLOAT, (size.0.div_ceil(8).max(1), size.1.div_ceil(8).max(1)), true, D3D12_RESOURCE_STATE_UNORDERED_ACCESS)?;
            tracing::info!(width = size.0, height = size.1, format = format.0, "frame generation ready");
            Ok(FrameGen {
                root,
                blocks: blocks_pso,
                halfway: halfway_pso,
                format,
                size,
                frames,
                newest: 0,
                have_older: false,
                motion,
                views: Vec::new(),
                step: device.GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV),
                held_list: None,
                held: 0,
                last_real: None,
                half: Duration::from_millis(16),
                pacer: Pacer::start(),
            })
        }
    }

    /// Whether this generator fits buffers of `format` and `size`.
    pub(crate) fn fits(&self, format: DXGI_FORMAT, size: (u32, u32)) -> bool {
        self.format == format && self.size == size
    }

    /// Records, into `list` (a presenter list of `slot`), after the frame is composed into `back`
    /// (in COPY_DEST, and left so): the frame kept, and when an older one is kept too, the frame
    /// halfway between them drawn over `back`. True when `back` now holds a generated frame (the
    /// real one is then shown by the pacer, [`Self::after_present`]).
    pub(crate) fn record(&mut self, host: &Host, slot: usize, list: &ID3D12GraphicsCommandList, back: &ID3D12Resource) -> windows::core::Result<bool> {
        let new = 1 - self.newest;
        // SAFETY: an open list; our live resources and the live back buffer in the states named.
        unsafe {
            list.ResourceBarrier(&[
                transition(back, D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_COPY_SOURCE),
                transition(&self.frames[new], READ, D3D12_RESOURCE_STATE_COPY_DEST),
            ]);
            list.CopyResource(&self.frames[new], back);
            list.ResourceBarrier(&[transition(&self.frames[new], D3D12_RESOURCE_STATE_COPY_DEST, READ)]);
            let older = self.have_older.then_some(self.newest);
            self.newest = new;
            self.have_older = true;
            let Some(older) = older else {
                list.ResourceBarrier(&[transition(back, D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_COPY_DEST)]);
                return Ok(false);
            };
            while self.views.len() <= slot {
                self.views.push(host.device.CreateDescriptorHeap(&D3D12_DESCRIPTOR_HEAP_DESC {
                    Type: D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
                    NumDescriptors: 8,
                    Flags: D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
                    NodeMask: 0,
                })?);
            }
            let heap = &self.views[slot];
            let (cpu, gpu) = (heap.GetCPUDescriptorHandleForHeapStart(), heap.GetGPUDescriptorHandleForHeapStart());
            let at = |i: usize| D3D12_CPU_DESCRIPTOR_HANDLE { ptr: cpu.ptr + i * self.step as usize };
            let srv = |format: DXGI_FORMAT| D3D12_SHADER_RESOURCE_VIEW_DESC {
                Format: format,
                ViewDimension: D3D12_SRV_DIMENSION_TEXTURE2D,
                Shader4ComponentMapping: D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
                Anonymous: D3D12_SHADER_RESOURCE_VIEW_DESC_0 { Texture2D: D3D12_TEX2D_SRV { MipLevels: 1, ..Default::default() } },
            };
            let uav = D3D12_UNORDERED_ACCESS_VIEW_DESC {
                Format: DXGI_FORMAT_R16G16_FLOAT,
                ViewDimension: D3D12_UAV_DIMENSION_TEXTURE2D,
                ..Default::default()
            };
            // Table 0 (the matching): older, newer, no motion to read, the motion written.
            // Table 1 (the frame halfway): older, newer, the motion read, no motion written.
            for table in 0..2 {
                let base = table * 4;
                host.device.CreateShaderResourceView(Some(&self.frames[older]), Some(&srv(self.format)), at(base));
                host.device.CreateShaderResourceView(Some(&self.frames[new]), Some(&srv(self.format)), at(base + 1));
                if table == 0 {
                    host.device.CreateShaderResourceView(None, Some(&srv(DXGI_FORMAT_R16G16_FLOAT)), at(base + 2));
                    host.device.CreateUnorderedAccessView(Some(&self.motion), None, Some(&uav), at(base + 3));
                } else {
                    host.device.CreateShaderResourceView(Some(&self.motion), Some(&srv(DXGI_FORMAT_R16G16_FLOAT)), at(base + 2));
                    host.device.CreateUnorderedAccessView(None, None, Some(&uav), at(base + 3));
                }
            }
            let table = |i: u64| D3D12_GPU_DESCRIPTOR_HANDLE { ptr: gpu.ptr + i * 4 * u64::from(self.step) };
            list.SetDescriptorHeaps(&[Some(heap.clone())]);
            list.SetComputeRootSignature(&self.root);
            list.SetPipelineState(&self.blocks);
            list.SetComputeRootDescriptorTable(0, table(0));
            let blocks = (self.size.0.div_ceil(8), self.size.1.div_ceil(8));
            list.Dispatch(blocks.0.div_ceil(8), blocks.1.div_ceil(8), 1);
            list.ResourceBarrier(&[
                transition(&self.motion, D3D12_RESOURCE_STATE_UNORDERED_ACCESS, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE),
                transition(back, D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_RENDER_TARGET),
            ]);
            let rtv = host.rtv(Some(back), None);
            list.OMSetRenderTargets(1, Some(&rtv), false, None);
            list.SetGraphicsRootSignature(&self.root);
            list.SetPipelineState(&self.halfway);
            list.SetGraphicsRootDescriptorTable(0, table(1));
            list.IASetPrimitiveTopology(windows::Win32::Graphics::Direct3D::D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
            list.RSSetViewports(&[D3D12_VIEWPORT { Width: self.size.0 as f32, Height: self.size.1 as f32, MaxDepth: 1.0, ..Default::default() }]);
            list.RSSetScissorRects(&[windows::Win32::Foundation::RECT { left: 0, top: 0, right: self.size.0 as i32, bottom: self.size.1 as i32 }]);
            list.DrawInstanced(3, 1, 0, 0);
            list.ResourceBarrier(&[
                transition(&self.motion, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, D3D12_RESOURCE_STATE_UNORDERED_ACCESS),
                transition(back, D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_COPY_DEST),
            ]);
        }
        MADE[0].fetch_add(1, Ordering::Relaxed);
        Ok(true)
    }

    /// After the present of frame `number`: the title's frame interval measured, and with a
    /// generated frame shown, the real one handed to the pacer for half an interval later.
    pub(crate) fn after_present(&mut self, number: u64, generated: bool) {
        let now = Instant::now();
        if let Some(last) = self.last_real {
            let half = (now - last) / 2;
            // Smoothed; between 4 ms and 50 ms (a pause or a load screen is not a frame interval).
            let half = half.clamp(Duration::from_millis(4), Duration::from_millis(50));
            self.half = (self.half * 3 + half) / 4;
        }
        self.last_real = Some(now);
        self.held = number;
        if generated {
            self.pacer.hand(number, now + self.half);
        }
    }

    /// Records, into `list`, the held real frame copied into `back` (in PRESENT, and left so).
    pub(crate) fn record_held(&self, list: &ID3D12GraphicsCommandList, back: &ID3D12Resource) {
        let frame = &self.frames[self.newest];
        // SAFETY: an open list; our live frame and the live back buffer in the states named.
        unsafe {
            list.ResourceBarrier(&[
                transition(back, D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_COPY_DEST),
                transition(frame, READ, D3D12_RESOURCE_STATE_COPY_SOURCE),
            ]);
            list.CopyResource(back, frame);
            list.ResourceBarrier(&[
                transition(back, D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PRESENT),
                transition(frame, D3D12_RESOURCE_STATE_COPY_SOURCE, READ),
            ]);
        }
    }

}

/// The thread that shows each held real frame when its time comes.
pub(crate) struct Pacer {
    job: Mutex<Option<(u64, Instant)>>,
    wake: Condvar,
}

impl Pacer {
    fn start() -> Arc<Self> {
        let pacer = Arc::new(Pacer { job: Mutex::new(None), wake: Condvar::new() });
        let me = Arc::clone(&pacer);
        let _ = std::thread::Builder::new().name("victory-framegen-pacer".into()).spawn(move || me.run());
        pacer
    }

    fn hand(&self, number: u64, due: Instant) {
        *self.job.lock().unwrap_or_else(|e| e.into_inner()) = Some((number, due));
        self.wake.notify_one();
    }

    fn run(&self) {
        loop {
            let (number, due) = {
                let mut job = self.job.lock().unwrap_or_else(|e| e.into_inner());
                while job.is_none() {
                    job = self.wake.wait(job).unwrap_or_else(|e| e.into_inner());
                }
                job.take().unwrap_or((0, Instant::now()))
            };
            let now = Instant::now();
            if due > now {
                std::thread::sleep(due - now);
            }
            if let Some(host) = crate::host::host() {
                host.show_held(number);
            }
        }
    }
}
