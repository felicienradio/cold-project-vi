# Proves the launcher's engine-freshness rule without the game: `Julia's Launcher.exe --stale-check`
# runs the same walk the Setup step and PLAY use, over a made-up source tree whose times we move.
# Run it after building the launcher:
#   launcher\build-launcher.cmd
#   powershell -ExecutionPolicy Bypass -File scripts\test-launcher-stale.ps1
param([string]$Launcher = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $Launcher) { $Launcher = Join-Path $root "Julia's Launcher.exe" }
if (-not (Test-Path $Launcher)) {
    $mine = Join-Path $root 'build\launcher-check.exe'
    if (Test-Path $mine) { $Launcher = $mine } else { throw "no launcher to test: build it first (launcher\build-launcher.cmd)" }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("stale-check-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force -Path $work | Out-Null
$src = Join-Path $work 'tree'
$engine = Join-Path $work 'victory.exe'
# The launcher's own verdict: 0 when the engine is at least as new as every input, 1 when the source
# is newer, 2 when it could not look.
function Verdict {
    # (A windowless program: Start-Process waits for it and keeps its exit code.)
    $p = Start-Process -FilePath $Launcher -ArgumentList '--stale-check', $src, $engine -Wait -PassThru
    switch ($p.ExitCode) { 0 { 'fresh' } 1 { 'stale' } default { throw "the launcher could not check (exit $($p.ExitCode))" } }
}
function Stamp([string]$path, [datetime]$when) {
    if (-not (Test-Path $path)) { New-Item -ItemType File -Force -Path $path | Out-Null }
    (Get-Item $path).LastWriteTimeUtc = $when
}
$fail = 0
try {
    New-Item -ItemType Directory -Force (Join-Path $src 'crates\victory-gpu\src') | Out-Null
    New-Item -ItemType Directory -Force (Join-Path $src 'target\diag') | Out-Null
    New-Item -ItemType Directory -Force (Join-Path $src '.git\objects') | Out-Null
    New-Item -ItemType Directory -Force $work | Out-Null
    Stamp (Join-Path $src 'Cargo.toml') ([datetime]::UtcNow.AddHours(-3))
    Stamp (Join-Path $src 'crates\victory-gpu\src\lib.rs') ([datetime]::UtcNow.AddHours(-2))

    # 1. The engine is newer than every input: fresh.
    Stamp $engine ([datetime]::UtcNow.AddHours(-1))
    $v = Verdict
    if ($v -eq 'fresh') { Write-Host "  ok    fresh, the engine newer than the source" }
    else { Write-Host "  FAIL  wanted fresh, got: $v"; $fail++ }

    # 2. A source file moves in front of the engine: stale.
    Stamp (Join-Path $src 'crates\victory-gpu\src\lib.rs') ([datetime]::UtcNow.AddMinutes(-1))
    $v = Verdict
    if ($v -eq 'stale') { Write-Host "  ok    stale, the source newer than the engine" }
    else { Write-Host "  FAIL  wanted stale, got: $v"; $fail++ }

    # 3. Build output and git's own files are not inputs, however new they are.
    Stamp (Join-Path $src 'target\diag\victory.exe') ([datetime]::UtcNow.AddHours(1))
    Stamp (Join-Path $src 'target\diag\stamp.toml') ([datetime]::UtcNow.AddHours(1))
    Stamp (Join-Path $src '.git\objects\new.rs') ([datetime]::UtcNow.AddHours(1))
    $v = Verdict
    if ($v -eq 'stale') { Write-Host "  ok    target\ and .git\ do not count as inputs" }
    else { Write-Host "  FAIL  target\ or .git\ counted as an input: $v"; $fail++ }

    # 4. The engine rebuilt after them: fresh again.
    Stamp $engine ([datetime]::UtcNow.AddHours(2))
    $v = Verdict
    if ($v -eq 'fresh') { Write-Host "  ok    fresh again once the engine is rebuilt" }
    else { Write-Host "  FAIL  wanted fresh after the rebuild, got: $v"; $fail++ }
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
if ($fail -gt 0) { Write-Host "$fail check(s) failed"; exit 1 }
Write-Host "engine freshness: all checks passed"
