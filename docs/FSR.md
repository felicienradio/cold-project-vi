# FSR upscaling and frame generation

Two switches, both off by default, both needing AMD's own files **that you fetch yourself**:

| Switch | What it does |
|---|---|
| `VICTORY_FSR=1` | AMD's FSR upscaler does the upscaling the game asks its FidelityFX API for, from the same colour, depth, motion vectors, exposure and composition masks, into the same output. |
| `VICTORY_FSR_FRAMEGEN=1` | AMD's FidelityFX frame generation between the game's frames. On its own it needs an upscaling that is not the game's own: `VICTORY_FSR=1` gives it one on a card without DLSS (`VICTORY_DLSS=1` was the only way before). |
| `VICTORY_FSR_MODE` | The render scale: `nativeaa` (`dlaa`), `quality`, `balanced`, `performance`, `ultraperformance`. Without it, `VICTORY_DLSS_MODE`'s name is used. |

## The files you have to bring

This repository carries none of them. They are AMD's, signed, and you get them from AMD's own
repository:

```powershell
.\scripts\get-amd-fsr.ps1
```

That fetches — into `amd-sdk\FidelityFX\bin` (git-ignored), or wherever `-To` says:

| File | Size | For |
|---|---|---|
| `amd_fidelityfx_loader_dx12.dll` | 39 KB | both |
| `amd_fidelityfx_upscaler_dx12.dll` | 58 MB | `VICTORY_FSR` — FSR Upscaling 4's ML model, and the analytical upscaler the API falls back to on cards that cannot run it |
| `amd_fidelityfx_framegeneration_dx12.dll` | 30 MB | `VICTORY_FSR_FRAMEGEN` |

Each is checked against the SHA-256 of the copy this tree was tested with (the script refuses a
different one) and against AMD's signature when the game loads it. **Check AMD's licence before
you use them**: `docs/license.md` of the FidelityFX SDK, saved beside the files as
`amd-fidelityfx-license.md`. In short: binaries only, no reverse engineering, keep the notice.

If you would rather keep them somewhere else, point the game at the folder:

```powershell
$env:VICTORY_FIDELITYFX_DIR = "D:\somewhere\fidelityfx"
```

Without `VICTORY_FIDELITYFX_DIR` the folder is `fidelityfx` beside the executable.

## Which model runs

`VICTORY_FSR=1` creates an upscaler context on the card the game runs on and asks it which
provider it loaded; the start's log says so:

```
FSR upscaling: AMD's upscaler context made render=[1280, 720] output=[1920, 1080] model=4.1.2
```

`4.1.2` is FSR Upscaling 4, the ML model (RDNA 4, e.g. RX 9070 XT). A card that cannot run it
gets the analytical upscaler of the same API instead, and the same line names that one. Nothing
here fakes the ML path: what the API says it loaded is what runs.

The same line for frame generation, once per context:

```
FSR frame generation: our swap chain is FidelityFX's frame interpolation swap chain
```

## What it costs and what it needs

- The upscaler is a compute pass where the game's own upscaler would have run, so it is neither
  free nor a second full-screen pass on top: it replaces the game's, at the same place.
- Frame generation needs depth and motion vectors — the game gives both to its upscaler, which is
  the same dispatch we take over — and it doubles the frames shown while adding half a frame of
  latency. At 22-26 fps rendered the hand feels the latency more than the eye enjoys the
  smoothness; it is worth trying at 60 (menus, cutscenes) and is a matter of taste in the open
  world.
- The interface is kept out of interpolation by the same hudless copy DLSS Frame Generation uses
  (`VICTORY_FRAMEGEN_HUDLESS=0` turns it off, and the UI then smears).
- Frame generation needs the line of submissions (`VICTORY_THIN=1`, which the launcher sets).
