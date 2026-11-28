"""docs/HARDWARE-STATS.md from the averages the hardware database gives (hardware-stats.yml).

    python3 scripts/hardware-stats.py docs/hardware-stats.json docs/HARDWARE-STATS.md
"""
import json
import sys


def table(rows, columns):
    if not rows:
        return "_none yet_\n"
    head = "| " + " | ".join(title for title, _ in columns) + " |\n"
    line = "|" + "|".join("---" for _ in columns) + "|\n"
    body = "".join("| " + " | ".join(str(r.get(key, "")) for _, key in columns) + " |\n" for r in rows)
    return head + line + body


def fps_line(f):
    if not f or not f.get("fps_median"):
        return "_Not enough measured games yet._\n"
    return f"**{f['fps_pcs']} PCs measured**: {f['fps_median']} FPS median, {f['fps_low']} in the slowest 5 %.\n"


def main(source, target):
    with open(source, encoding="utf-8") as f:
        s = json.load(f)
    a, p = s["average"], s["percent"]
    text = f"""# Players' hardware

The hardware of the PCs whose players turned **Hardware sharing** on in Julia's Launcher: averages only
(groups of fewer than 3 PCs are folded into "other"). Updated every 6 hours by
[hardware-stats.yml](../.github/workflows/hardware-stats.yml); last update {s["updated"]}.

**{s["pcs"]} PCs** ({s["active_30_days"]} seen in the last 30 days).

| Average | |
|---|---|
| RAM | {a["ram_gb"]} GB |
| Video memory | {a["vram_gb"]} GB |
| Processor threads | {a["cpu_threads"]} |

| Share of PCs | |
|---|---|
| Resizable BAR on in the BIOS | {p["rebar_in_bios"]}% |
| Resizable BAR given to the game (D3D12) | {p["rebar_for_the_game"]}% |
| Laptops | {p["laptops"]}% |
| System disk is an SSD | {p["ssd"]}% |
| 16 GB of RAM or less | {p["ram_16_gb_or_less"]}% |
| 8 GB of video memory or less | {p["vram_8_gb_or_less"]}% |

## Graphics card makers

{table(s["gpu_vendors"], [("Maker", "name"), ("PCs", "pcs")])}
## Graphics cards

{table(s["gpus"], [("Card", "name"), ("PCs", "pcs"), ("Video memory (GB)", "vram_gb"), ("FPS median", "fps_median"), ("FPS slowest 5 %", "fps_low"), ("PCs measured", "fps_pcs"), ("Resizable BAR for the game (%)", "rebar_percent")])}
## FPS

Measured by the players' own launchers (0.3.21 and later, with sharing on): each PC's last game of 3 minutes or more,
the first 90 seconds left out; a median of the PCs' medians, for groups of 3 PCs or more.
{fps_line(s.get("fps"))}
{table(s.get("fps_presets", []), [("Preset", "name"), ("PCs measured", "fps_pcs"), ("FPS median", "fps_median"), ("FPS slowest 5 %", "fps_low")])}
## Processors

{table(s["cpus"], [("Processor", "name"), ("PCs", "pcs"), ("Threads", "threads")])}
## RAM

{table(s["ram_gb"], [("GB", "name"), ("PCs", "pcs")])}
## Windows

{table(s["windows"], [("Version", "name"), ("PCs", "pcs")])}"""
    with open(target, "w", encoding="utf-8") as f:
        f.write(text)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
