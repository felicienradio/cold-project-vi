// VI hardware database: one way in, nothing out.
//
// Two routes take data in, none gives it out: POST /v1/hw (hardware, below) and POST /v1/report (a
// bug report's zip the player chose to send from the launcher, see report()).
//
// POST /v1/hw It takes the launcher's hardware report (the exact fields below,
// nothing else), keeps one row per ID, and answers with a bare status code. There is no route that
// reads anything: the database is read only by edupode, in the Cloudflare dashboard or with the
// separate admin Worker behind Cloudflare Access (admin.js).
//
// A report for an ID that exists is taken only with the secret its first report came with (the
// database keeps the secret's SHA-256), so knowing someone's ID is not enough to change their row.
// No IP address is stored; the rate limit keeps per-IP counts as a salted hash for one hour only.

import { zipFile } from './zip.js';

const FIELDS = {
  schema: 'number', id: 'string', gpu: 'string', gpu_vendor: 'string', gpu_driver: 'string',
  vram_mb: 'number', rebar_bios: 'boolean', rebar_d3d12: 'nullable-boolean', cpu: 'string',
  cpu_cores: 'number', cpu_threads: 'number', ram_gb: 'number', board_maker: 'string', board: 'string',
  os: 'string', disk: 'string', laptop: 'boolean',
};
// Optional (launcher 0.3.21 and later, with sharing on): the last game of 3 minutes or more on this PC, as the
// launcher measured it from its own log: median FPS and the slowest 5 %, the preset, engine and launcher version,
// and how long it was played. Absent from older launchers' reports.
const OPTIONAL = {
  fps_median: 'number', fps_low: 'number', fps_minutes: 'number',
  fps_preset: 'string', fps_engine: 'string', fps_version: 'string',
};
const ID = /^VI-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$/;
const MAX_BODY = 2048;
const PER_HOUR = 20;

async function sha256(text) {
  const d = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
  return [...new Uint8Array(d)].map(b => b.toString(16).padStart(2, '0')).join('');
}

function valid(r) {
  if (typeof r !== 'object' || r === null || Array.isArray(r)) return false;
  for (const k of Object.keys(r)) if (!(k in FIELDS) && !(k in OPTIONAL)) return false;
  for (const [k, t] of Object.entries(OPTIONAL)) {
    if (!(k in r)) continue;
    const v = r[k];
    if (t === 'string' && (typeof v !== 'string' || v.length > 40)) return false;
    if (t === 'number' && (typeof v !== 'number' || !Number.isFinite(v) || v < 0 || v > 1000)) return false;
  }
  for (const [k, t] of Object.entries(FIELDS)) {
    const v = r[k];
    if (t === 'string' && (typeof v !== 'string' || v.length > 200)) return false;
    if (t === 'number' && (typeof v !== 'number' || !Number.isFinite(v) || v < 0 || v > 1e7)) return false;
    if (t === 'boolean' && typeof v !== 'boolean') return false;
    if (t === 'nullable-boolean' && !(v === null || typeof v === 'boolean')) return false;
  }
  return r.schema === 1 && ID.test(r.id);
}

// Same length and every byte compared: how long it takes says nothing about the token.
function sameToken(a, b) {
  if (typeof a !== 'string' || typeof b !== 'string' || a.length !== b.length || a.length < 32) return false;
  let d = 0;
  for (let i = 0; i < a.length; i++) d |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return d === 0;
}

// The averages, never a row: GET /v1/stats with `Authorization: Bearer <STATS_TOKEN>` (a secret only
// the GitHub workflow that publishes them holds, .github/workflows/hardware-stats.yml). No ID, no
// single PC: groups of fewer than 3 PCs are folded into "other".
async function stats(env) {
  const rows = (await env.DB.prepare('SELECT report, last_seen FROM hardware').all()).results.map(r => ({ ...JSON.parse(r.report), last_seen: r.last_seen }));
  const n = rows.length;
  const avg = f => n ? Math.round(rows.reduce((s, r) => s + (Number(r[f]) || 0), 0) / n * 10) / 10 : 0;
  const share = f => n ? Math.round(rows.filter(f).length / n * 1000) / 10 : 0;
  const top = (key, extra) => {
    const groups = new Map();
    for (const r of rows) {
      const k = key(r) || 'unknown';
      if (!groups.has(k)) groups.set(k, []);
      groups.get(k).push(r);
    }
    const out = [];
    let other = 0;
    for (const [k, list] of [...groups.entries()].sort((a, b) => b[1].length - a[1].length)) {
      if (list.length < 3) { other += list.length; continue; }
      out.push({ name: k, pcs: list.length, ...(extra ? extra(list) : {}) });
    }
    if (other) out.push({ name: 'other', pcs: other });
    return out;
  };
  const mean = (list, f) => Math.round(list.reduce((s, r) => s + (Number(r[f]) || 0), 0) / list.length);
  // FPS: only PCs whose launcher measured a game (fps_median), a median of their medians; none for fewer than 3.
  const measured = list => list.filter(r => typeof r.fps_median === 'number' && r.fps_median > 0);
  const median = xs => { const a = [...xs].sort((x, y) => x - y); const m = a.length >> 1; return a.length % 2 ? a[m] : (a[m - 1] + a[m]) / 2; };
  const fps = list => {
    const m = measured(list);
    if (m.length < 3) return {};
    return { fps_pcs: m.length, fps_median: Math.round(median(m.map(r => r.fps_median)) * 10) / 10, fps_low: Math.round(median(m.map(r => Number(r.fps_low) || 0)) * 10) / 10 };
  };
  const month = Date.now() - 30 * 86400000;
  return {
    updated: new Date().toISOString(),
    pcs: n,
    active_30_days: rows.filter(r => Date.parse(r.last_seen + 'Z') > month).length,
    average: { ram_gb: avg('ram_gb'), vram_gb: Math.round(avg('vram_mb') / 1024 * 10) / 10, cpu_threads: avg('cpu_threads') },
    percent: {
      rebar_in_bios: share(r => r.rebar_bios),
      rebar_for_the_game: share(r => r.rebar_d3d12 === true),
      laptops: share(r => r.laptop),
      ssd: share(r => r.disk === 'SSD'),
      ram_16_gb_or_less: share(r => r.ram_gb <= 16),
      vram_8_gb_or_less: share(r => r.vram_mb <= 8 * 1024),
    },
    gpu_vendors: top(r => r.gpu_vendor),
    gpus: top(r => r.gpu, list => ({ vram_gb: Math.round(mean(list, 'vram_mb') / 1024), rebar_percent: Math.round(list.filter(r => r.rebar_d3d12 === true).length / list.length * 100), ...fps(list) })),
    fps: fps(rows),
    fps_presets: top(r => measured([r]).length ? r.fps_preset : null, fps).filter(g => g.name !== 'unknown' && g.name !== 'other'),
    cpus: top(r => r.cpu, list => ({ threads: mean(list, 'cpu_threads') })),
    ram_gb: top(r => String(r.ram_gb)),
    windows: top(r => { const m = (r.os || '').match(/Windows (10|11)/); return m ? 'Windows ' + m[1] : 'other'; }),
  };
}

// A bug report: the zip the launcher made (summary, settings and log tails, the user name taken out of
// every path), sent only when the player presses "Send to edupode". Kept whole in `reports`, read only by
// edupode (scripts/get-reports.ps1 or the dashboard). Headers: X-VI-Id (the launcher's ID), X-VI-Kind
// (game or launcher); the body is the zip itself, at most MAX_REPORT bytes (a D1 row holds 2 MB).
const MAX_REPORT = 1800 * 1024;
const REPORTS_PER_HOUR = 5;
const MAX_SESSION = 400 * 1024;
const SESSIONS_PER_HOUR = 12;

async function report(request, env) {
  const id = request.headers.get('X-VI-Id') || '';
  const kind = request.headers.get('X-VI-Kind') || '';
  if (!ID.test(id) || !['game', 'launcher', 'session'].includes(kind)) return new Response(null, { status: 400 });
  const length = Number(request.headers.get('content-length') || '0');
  // A game's log (kind session, sent when each game closes for players who said yes) is smaller and counted apart.
  const session = kind === 'session';
  if (length > (session ? MAX_SESSION : MAX_REPORT)) return new Response(null, { status: 413 });
  const hour = Math.floor(Date.now() / 3600000);
  const who = await sha256(`${env.RATE_SALT}|${session ? 'session' : 'report'}|${request.headers.get('CF-Connecting-IP') || ''}|${hour}`);
  await env.DB.prepare('DELETE FROM rate WHERE hour < ?').bind(hour).run();
  const seen = await env.DB.prepare('INSERT INTO rate (who, hour, n) VALUES (?, ?, 1) ON CONFLICT(who) DO UPDATE SET n = n + 1 RETURNING n')
    .bind(who, hour).first('n');
  if (seen > (session ? SESSIONS_PER_HOUR : REPORTS_PER_HOUR)) return new Response(null, { status: 429 });
  const zip = new Uint8Array(await request.arrayBuffer());
  // A zip and nothing else: its first bytes are PK\x03\x04.
  if (zip.length < 22 || zip.length > MAX_REPORT || zip[0] !== 0x50 || zip[1] !== 0x4b || zip[2] !== 3 || zip[3] !== 4)
    return new Response(null, { status: 400 });
  const n = await env.DB.prepare(
    "INSERT INTO reports (created, vi_id, kind, bytes, zip) VALUES (datetime('now'), ?, ?, ?, ?) RETURNING n"
  ).bind(id, kind, zip.length, zip).first('n');
  // The player chose a public report (X-VI-Public: 1): a GitHub issue with the summary only; the zip stays here.
  let issue = null;
  if (!session && request.headers.get('X-VI-Public') === '1') {
    try { issue = await openIssue(env, n, id, kind, zip); } catch { }
  }
  return new Response(JSON.stringify(issue ? { n, issue } : { n }), { status: 201, headers: { 'content-type': 'application/json' } });
}

// A public GitHub issue for report n on ISSUES_REPO (wrangler.toml), made with GITHUB_TOKEN (a fine-grained token
// for that repository only, Issues: read and write; wrangler secret put GITHUB_TOKEN). It holds the summary.txt the
// launcher wrote (PC parts, versions, preset, settings, what the player ticked and wrote; the user name is already
// taken out), without the motherboard line, with @ and ``` made harmless so it pings nobody and stays one block.
// The logs stay in the report box. Returns the issue's address, or null.
async function openIssue(env, n, id, kind, zip) {
  if (!env.GITHUB_TOKEN || !/^[\w.-]+\/[\w.-]+$/.test(env.ISSUES_REPO || '')) return null;
  let summary = await zipFile(zip, 'summary.txt');
  if (!summary) return null;
  summary = summary.slice(0, 12000)
    .replace(/(CPU: .*?RAM: [^\r\n]*?)\s+Board: [^\r\n]*/, '$1')
    .replace(/```/g, "'''")
    .replace(/@/g, '@​');
  const line = (label) => { const m = summary.match(new RegExp(`^${label}: (.+)$`, 'm')); return m ? m[1].trim() : ''; };
  let first = line('Ticked').split(';')[0] || line('In their words') || (kind === 'game' ? 'Game problem' : 'Launcher problem');
  if (first.length > 80) first = first.slice(0, 80) + '...';
  const body = `Report #${n} from \`${id}\`, sent from Julia's Launcher. The logs are kept private in the report box.\n\n\`\`\`text\n${summary.trim()}\n\`\`\``;
  const r = await fetch(`https://api.github.com/repos/${env.ISSUES_REPO}/issues`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28', 'User-Agent': 'vi-hardware-worker', 'content-type': 'application/json',
    },
    body: JSON.stringify({ title: `[${kind}] ${first}`, body, labels: ['bug', kind] }),
  });
  if (!r.ok) return null;
  return (await r.json()).html_url || null;
}

// The save gallery. A player shares a save from the launcher (POST /v1/saves: headers X-VI-Id, X-VI-Title and
// X-VI-Note, URL-encoded; the body is the launcher's save zip). It waits as "pending" until edupode approves it
// in the admin panel (admin.js): titles are the players' own words and the gallery is public. Approved saves are
// listed at GET /saves (a page) and GET /v1/saves (JSON), and downloaded at GET /v1/saves/<n>.zip.
const MAX_SAVE = 1024 * 1024;
const SAVES_PER_HOUR = 3;
const esc = s => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const clean = (s, max) => {
  let t = '';
  try { t = decodeURIComponent(s || ''); } catch { return null; }
  t = t.replace(/[\u0000-\u001f\u007f]/g, ' ').replace(/\s+/g, ' ').trim();
  return t.length <= max ? t : null;
};

// The zip's entries, from its central directory: every name must be a save file (or the export's hash list),
// at most 400 of them, none larger than 32 MB once unpacked.
const SAVE_PATH = /^[0-9A-Fa-f]{16}\/[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\/[A-Za-z0-9_]{1,64}~[0-9A-Fa-f]{16}\/v[0-9]{1,5}\/([0-9]{1,4}|manifest)$/;
function onlySaveFiles(zip) {
  const v = new DataView(zip.buffer, zip.byteOffset, zip.byteLength);
  let end = -1;
  for (let i = zip.length - 22; i >= Math.max(0, zip.length - 22 - 65535); i--) if (v.getUint32(i, true) === 0x06054b50) { end = i; break; }
  if (end < 0) return false;
  const count = v.getUint16(end + 10, true), size = v.getUint32(end + 12, true), at0 = v.getUint32(end + 16, true);
  if (count === 0 || count > 401 || at0 + size > end) return false;
  let at = at0, files = 0;
  for (let k = 0; k < count; k++) {
    if (at + 46 > end || v.getUint32(at, true) !== 0x02014b50) return false;
    const unpacked = v.getUint32(at + 24, true), nameLen = v.getUint16(at + 28, true), extra = v.getUint16(at + 30, true), comment = v.getUint16(at + 32, true);
    const name = new TextDecoder().decode(zip.subarray(at + 46, at + 46 + nameLen)).replace(/\\/g, '/');
    at += 46 + nameLen + extra + comment;
    if (name.endsWith('/')) continue;
    if (name !== 'vi-save.sha256' && !SAVE_PATH.test(name)) return false;
    if (unpacked > 32 * 1024 * 1024) return false;
    files++;
  }
  return files > 0;
}

async function shareSave(request, env) {
  const id = request.headers.get('X-VI-Id') || '';
  const title = clean(request.headers.get('X-VI-Title'), 80);
  const note = clean(request.headers.get('X-VI-Note'), 300);
  if (!ID.test(id) || title === null || title.length < 3 || note === null) return new Response(null, { status: 400 });
  if (Number(request.headers.get('content-length') || '0') > MAX_SAVE) return new Response(null, { status: 413 });
  const hour = Math.floor(Date.now() / 3600000);
  const who = await sha256(`${env.RATE_SALT}|save|${request.headers.get('CF-Connecting-IP') || ''}|${hour}`);
  await env.DB.prepare('DELETE FROM rate WHERE hour < ?').bind(hour).run();
  const seen = await env.DB.prepare('INSERT INTO rate (who, hour, n) VALUES (?, ?, 1) ON CONFLICT(who) DO UPDATE SET n = n + 1 RETURNING n')
    .bind(who, hour).first('n');
  if (seen > SAVES_PER_HOUR) return new Response(null, { status: 429 });
  const zip = new Uint8Array(await request.arrayBuffer());
  if (zip.length < 22 || zip.length > MAX_SAVE || zip[0] !== 0x50 || zip[1] !== 0x4b || zip[2] !== 3 || zip[3] !== 4)
    return new Response(null, { status: 400 });
  // Only the save module's files, as the launcher's import takes them (launcher\Saves.cs, SavePath): nothing else
  // reaches the gallery, so a shared save cannot carry a program along.
  if (!onlySaveFiles(zip)) return new Response(null, { status: 422 });
  const n = await env.DB.prepare(
    "INSERT INTO saves (created, vi_id, title, note, bytes, zip) VALUES (datetime('now'), ?, ?, ?, ?, ?) RETURNING n"
  ).bind(id, title, note, zip.length, zip).first('n');
  return new Response(JSON.stringify({ n, state: 'pending' }), { status: 201, headers: { 'content-type': 'application/json' } });
}

async function approvedSaves(env) {
  return (await env.DB.prepare("SELECT n, created, title, note, bytes, downloads FROM saves WHERE state = 'approved' ORDER BY n DESC LIMIT 300").all()).results;
}

async function galleryPage(env) {
  const rows = await approvedSaves(env);
  const cards = rows.map(r => `<div class=card><h2>${esc(r.title)}</h2><p>${esc(r.note)}</p>` +
    `<p class=dim>${esc(r.created.slice(0, 10))} · ${Math.max(1, Math.round(r.bytes / 1024))} KB · ${r.downloads} downloads</p>` +
    `<a class=btn href="/v1/saves/${r.n}.zip">Download</a></div>`).join('') || '<p class=dim>No save shared yet.</p>';
  return new Response(`<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content="width=device-width,initial-scale=1">
<title>Julia's Launcher · Save gallery</title>
<style>body{font:15px system-ui;background:#13171f;color:#e2e8f0;margin:0;padding:24px;max-width:1000px;margin:auto}
h1{font-size:24px}.dim{color:#8b95a7;font-size:13px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:14px}
.card{background:#1c2230;border-radius:12px;padding:14px 16px}.card h2{font-size:17px;margin:0 0 6px}.card p{margin:4px 0;overflow-wrap:anywhere}
.btn{display:inline-block;margin-top:8px;background:#ff5fa2;color:#fff;text-decoration:none;padding:7px 14px;border-radius:18px}</style>
<h1>Save gallery</h1>
<p class=dim>Saves shared by players. Download one, then in Julia's Launcher: Hardware &amp; ID &gt; Game saves &gt; Import a save... (yours are backed up first).
Share yours from the same place. EN · <span lang=fr>Sauvegardes partagées par les joueurs : télécharge, puis Matériel et ID &gt; Sauvegardes du jeu &gt; Importer.</span>
· <span lang=pt>Saves partilhados pelos jogadores: descarrega, depois Hardware e ID &gt; Saves do jogo &gt; Importar.</span></p>
<div class=grid>${cards}</div></html>`, { headers: { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'public, max-age=60' } });
}

async function downloadSave(env, n) {
  const row = await env.DB.prepare("SELECT title, zip FROM saves WHERE n = ? AND state = 'approved'").bind(n).first();
  if (!row) return new Response(null, { status: 404 });
  await env.DB.prepare('UPDATE saves SET downloads = downloads + 1 WHERE n = ?').bind(n).run();
  return new Response(row.zip, { headers: { 'content-type': 'application/zip', 'content-disposition': `attachment; filename="VI-save-${n}.zip"`, 'cache-control': 'no-store' } });
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'POST' && url.pathname === '/v1/report') return report(request, env);
    if (request.method === 'POST' && url.pathname === '/v1/saves') return shareSave(request, env);
    if (request.method === 'GET' && (url.pathname === '/saves' || url.pathname === '/')) return galleryPage(env);
    if (request.method === 'GET' && url.pathname === '/v1/saves')
      return new Response(JSON.stringify(await approvedSaves(env)), { headers: { 'content-type': 'application/json', 'cache-control': 'public, max-age=60' } });
    const dl = request.method === 'GET' && url.pathname.match(/^\/v1\/saves\/(\d{1,9})\.zip$/);
    if (dl) return downloadSave(env, Number(dl[1]));
    if (request.method === 'GET' && url.pathname === '/v1/stats') {
      const auth = request.headers.get('Authorization') || '';
      if (!env.STATS_TOKEN || !sameToken(auth.replace(/^Bearer /, ''), env.STATS_TOKEN)) return new Response(null, { status: 404 });
      return new Response(JSON.stringify(await stats(env)), { headers: { 'content-type': 'application/json', 'cache-control': 'no-store' } });
    }
    if (request.method !== 'POST' || url.pathname !== '/v1/hw') return new Response(null, { status: 404 });
    const secret = request.headers.get('X-VI-Secret') || '';
    if (!/^[0-9a-f]{64}$/.test(secret)) return new Response(null, { status: 400 });
    const length = Number(request.headers.get('content-length') || '0');
    if (length > MAX_BODY) return new Response(null, { status: 413 });
    const text = await request.text();
    if (text.length > MAX_BODY) return new Response(null, { status: 413 });

    // Rate limit: a salted hash of the IP and the hour, never the IP itself.
    const hour = Math.floor(Date.now() / 3600000);
    const who = await sha256(`${env.RATE_SALT}|${request.headers.get('CF-Connecting-IP') || ''}|${hour}`);
    await env.DB.prepare('DELETE FROM rate WHERE hour < ?').bind(hour).run();
    const seen = await env.DB.prepare('INSERT INTO rate (who, hour, n) VALUES (?, ?, 1) ON CONFLICT(who) DO UPDATE SET n = n + 1 RETURNING n')
      .bind(who, hour).first('n');
    if (seen > PER_HOUR) return new Response(null, { status: 429 });

    let r;
    try { r = JSON.parse(text); } catch { return new Response(null, { status: 400 }); }
    if (!valid(r)) return new Response(null, { status: 400 });

    const secretHash = await sha256(secret);
    const row = await env.DB.prepare('SELECT secret_hash FROM hardware WHERE id = ?').bind(r.id).first();
    if (row && row.secret_hash !== secretHash) return new Response(null, { status: 403 });
    await env.DB.prepare(
      `INSERT INTO hardware (id, secret_hash, report, first_seen, last_seen) VALUES (?, ?, ?, datetime('now'), datetime('now'))
       ON CONFLICT(id) DO UPDATE SET report = excluded.report, last_seen = excluded.last_seen`
    ).bind(r.id, secretHash, JSON.stringify(r)).run();
    return new Response(null, { status: 204 });
  },
};
