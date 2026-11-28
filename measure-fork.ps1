param(
    [string] $Label = 'fork',
    [int] $WorldSeconds = 30,
    [string] $Extra = ''
)
# Same protocol as measure-coldvi-max.ps1, but runs the Belobug fork build with
# the settings its own scripts\play.ps1 uses, plus the five renderer ports
# (VICTORY_PORT_CONSTANTS/DESCRIPTORS/CBV/GUARD/JOURNAL) its README measures.
# Layout: this repository sits in the game folder; the fork is cloned beside it
# as VIctory-Belobug-AMD and built with
#   cargo build --locked --profile diag -p victory --features diagnostics
# Logs and measure-*.txt are written to the game folder, never into the repository.
$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $repo
$dir  = Join-Path $root 'VIctory-Belobug-AMD'
$exe  = Join-Path $dir 'target\diag\victory.exe'
$game = Join-Path $root 'game_scarlett_master.exe'
$out  = Join-Path $root "measure-$Label.txt"
$log  = Join-Path $root "log-$Label.txt"

Get-ChildItem env: | Where-Object Name -like 'VICTORY_*' | Where-Object Name -ne 'VICTORY_GAME_DIR' |
    ForEach-Object { Remove-Item "env:$($_.Name)" }

# scripts\play.ps1 defaults.
$env:VICTORY_TEXTURE_NO_WAIT = '1'; $env:VICTORY_RELAY = '2'; $env:VICTORY_WIDE_BACK = '1'
$env:VICTORY_CARD_REHASH_RUNS = '1'; $env:VICTORY_JOIN_COPIES = '1'
$env:VICTORY_INDIRECT_WINDOW = '1'; $env:VICTORY_RELAY_COPY = '1'; $env:VICTORY_CARD_STEPS = '2'
$env:VICTORY_INDIRECT_BATCH = '2'; $env:VICTORY_MAP_THREADS = '1'
$env:VICTORY_TITLE_PRIORITIES = '0'; $env:VICTORY_CARD_AWAKE = '1'
$env:VICTORY_SHOW_AFTER = '1'; $env:VICTORY_AMBIENT = '0.8'
$env:VICTORY_THIN = '1'; $env:VICTORY_JOIN_PARTS = '8'; $env:VICTORY_UPLOAD_AHEAD = '1'
$env:VICTORY_D3D12_DRED = 'light'
# Renderer ports (README table: patches 1, 2, 5, 6 and 7).
$env:VICTORY_PORT_CONSTANTS = '1'; $env:VICTORY_PORT_DESCRIPTORS = '1'
$env:VICTORY_PORT_CBV = '1'; $env:VICTORY_PORT_GUARD = '1'; $env:VICTORY_PORT_JOURNAL = '1'
# Same cache and save data as the main build, so the run starts from the same state.
$env:VICTORY_CACHE_DIR = "$env:LOCALAPPDATA\coldvi\cache"
$env:VICTORY_DATA_DIR  = "$env:LOCALAPPDATA\coldvi\data"
if ($Extra) { foreach ($kv in $Extra.Split(',')) { $p = $kv.Split('='); Set-Item "env:$($p[0])" $p[1] } }

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class K {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void keybd_event(byte k, byte s, uint f, UIntPtr e);
}
"@

"run=$Label fork started=$(Get-Date -Format o)" | Out-File $out -Encoding utf8
$p = Start-Process -FilePath $exe -WorkingDirectory $dir -PassThru `
    -ArgumentList @('run', "`"$game`"", '--console', 'series-s', '--execute') `
    -RedirectStandardOutput $log -RedirectStandardError "$log.err"

$t0 = Get-Date; $world = $null; $menuSince = $null
while ($true) {
    Start-Sleep 5
    $p.Refresh()
    if ($p.HasExited) { "exited code=$($p.ExitCode)" | Out-File $out -Append -Encoding utf8; break }
    $t = $p.MainWindowTitle
    $draws = 0; if ($t -match '(\d+) draws/frame') { $draws = [int]$Matches[1] }
    $el = [int]((Get-Date) - $t0).TotalSeconds
    if (-not $world) {
        if ($draws -gt 1000) { $world = Get-Date; "world at ${el}s" | Out-File $out -Append -Encoding utf8 }
        elseif ($draws -gt 0 -and $draws -lt 30 -and $t -match ' 30(\.0)? fps') {
            if (-not $menuSince) { $menuSince = Get-Date }
            if (((Get-Date) - $menuSince).TotalSeconds -ge 30) {
                [K]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
                Start-Sleep -Milliseconds 300
                [K]::keybd_event(0x0D,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 120
                [K]::keybd_event(0x0D,0,2,[UIntPtr]::Zero)
                $menuSince = Get-Date
            }
        } else { $menuSince = $null }
    } else {
        $m = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory
        "{0}s avail={1}MB | {2}" -f $el, $m.AvailableMBytes, $t | Out-File $out -Append -Encoding utf8
        if (((Get-Date) - $world).TotalSeconds -ge $WorldSeconds) { break }
    }
    if ($el -gt ($WorldSeconds + 480)) { break }
}
$kids = Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $p.Id }
foreach ($k in $kids) { Stop-Process -Id $k.ProcessId -Force -ErrorAction SilentlyContinue }
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
