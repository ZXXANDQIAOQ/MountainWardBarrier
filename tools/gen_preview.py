# -*- coding: utf-8 -*-
"""
把 Assets/Resources/Sprites 下的精灵图拼成两张预览图，方便在 README 里一眼看完全部美术。

生成：
    tools/_preview_units.png     单位与地形（12 张单位 + 8 张地形）
    tools/_preview_sprites.png   全部精灵图（含 UI 图标与特效）

用法（在仓库根目录）：
    python tools/gen_preview.py

这是纯粹的"看一眼"工具，不参与游戏构建；改了 gen_sprites.py 之后顺手跑一次即可。
"""

import io
import os
import sys

from PIL import Image, ImageDraw

sys.stdout.reconfigure(encoding="utf-8")

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "Assets", "Resources", "Sprites")

BG = (26, 30, 38)
LABEL = (150, 162, 176)
PAD = 18
COLS = 8


def collect(folders):
    items = []
    for folder in folders:
        directory = os.path.join(SPRITES, folder)
        if not os.path.isdir(directory):
            continue
        for name in sorted(os.listdir(directory)):
            if name.lower().endswith(".png") and not name.startswith("bg_"):
                items.append((folder, name[:-4], os.path.join(directory, name)))
    return items


def build_sheet(items, out_path, title):
    """按固定网格排布：每格上方 128x128 放图，下方写文件名。"""
    cell_w, cell_h = 160, 176
    rows = (len(items) + COLS - 1) // COLS
    width = COLS * cell_w + PAD
    height = rows * cell_h + PAD + 44

    sheet = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(sheet)
    draw.text((PAD, 14), title, fill=(232, 226, 214))
    draw.line([(PAD, 36), (width - PAD, 36)], fill=(70, 78, 92), width=1)

    for index, (folder, name, path) in enumerate(items):
        col = index % COLS
        row = index // COLS
        x = PAD + col * cell_w
        y = 48 + row * cell_h

        sprite = Image.open(path).convert("RGBA")
        sprite.thumbnail((128, 128), Image.LANCZOS)
        sheet.paste(sprite, (x + (cell_w - sprite.width) // 2, y), sprite)

        label = name if len(name) <= 20 else name[:19] + "…"
        draw.text((x + 6, y + 132), label, fill=LABEL)

    # 4 倍缩小再放大，得到近似抗锯齿的文字与线条观感（与美术生成保持同一套处理）
    sheet.save(out_path, "PNG")
    print("  %-34s %5d x %-5d %6d B" % (
        os.path.relpath(out_path, ROOT), sheet.width, sheet.height, os.path.getsize(out_path)))


def main():
    units = collect(["Units", "Terrain"])
    every = collect(["Units", "Terrain", "UI"])

    print("生成预览图：")
    build_sheet(units, os.path.join(ROOT, "tools", "_preview_units.png"),
                "Units & Terrain  (%d)" % len(units))
    build_sheet(every, os.path.join(ROOT, "tools", "_preview_sprites.png"),
                "All Sprites  (%d)" % len(every))
    print("\n提示：这两张图只用于预览，游戏本身不读取它们。")


if __name__ == "__main__":
    main()
