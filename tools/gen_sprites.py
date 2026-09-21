#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
《仙侠·护山大阵》美术资源生成器
=====================================

纯程序化绘制，不依赖任何外部素材，也没有版权问题。

用法：
    python tools/gen_sprites.py

输出到 Assets/Resources/Sprites/{Units,Terrain,UI}/
重新运行会覆盖生成结果 —— 想改美术风格直接改这里的代码，比拿 Photoshop 重画省事。

风格约定：
    我方（阵法 / 仙灵）  →  玉青 + 鎏金
    敌方（妖魔）        →  暗紫 + 妖红
    统一风格：墨黑描边 + 中心辉光，靠发光营造"仙气"
"""

import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

# --------------------------------------------------------------------------- 调色板

INK       = (16, 18, 26, 255)
INK_SOFT  = (42, 48, 64, 255)

JADE      = (96, 214, 178, 255)
JADE_D    = (26, 100, 94, 255)
JADE_L    = (186, 244, 220, 255)

GOLD      = (242, 198, 96, 255)
GOLD_D    = (160, 110, 30, 255)
GOLD_L    = (255, 234, 174, 255)

CINNABAR  = (206, 62, 58, 255)
CINNABAR_D= (128, 34, 34, 255)

PURPLE    = (152, 110, 228, 255)
PURPLE_D  = (72, 44, 110, 255)

ICE       = (138, 218, 248, 255)
ICE_D     = (44, 106, 150, 255)

BONE      = (238, 232, 214, 255)
BONE_D    = (172, 164, 144, 255)

STONE     = (126, 134, 148, 255)
STONE_D   = (72, 80, 94, 255)

DEMON     = (178, 62, 84, 255)
DEMON_D   = (88, 32, 54, 255)
DEMON_P   = (130, 86, 178, 255)

A = 255


def lerp(c1, c2, t):
    return tuple(int(round(c1[i] + (c2[i] - c1[i]) * t)) for i in range(4))


def shade(c, t):
    """t>0 提亮，t<0 压暗。"""
    if t >= 0:
        return lerp(c, (255, 255, 255, c[3]), t)
    return lerp(c, (0, 0, 0, c[3]), -t)


# --------------------------------------------------------------------------- 画布封装

class Art(object):
    """带超采样的绘图画布：逻辑坐标作图，最后统一缩放得到抗锯齿边缘。"""

    def __init__(self, w, h, ss=4):
        self.w = w
        self.h = h
        self.ss = ss
        self.img = Image.new("RGBA", (w * ss, h * ss), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    # --- 坐标换算

    def s(self, v):
        return v * self.ss

    def box(self, x0, y0, x1, y1):
        return [self.s(x0), self.s(y0), self.s(x1), self.s(y1)]

    # --- 基础图形

    def line(self, pts, color, width=2.0):
        p = [(self.s(x), self.s(y)) for x, y in pts]
        self.d.line(p, fill=color, width=max(1, int(round(self.s(width)))), joint="curve")

    def ellipse(self, cx, cy, rx, ry, fill=None, outline=None, width=2.0):
        w = max(1, int(round(self.s(width)))) if outline else 0
        self.d.ellipse(self.box(cx - rx, cy - ry, cx + rx, cy + ry),
                       fill=fill, outline=outline, width=w)

    def circle(self, cx, cy, r, fill=None, outline=None, width=2.0):
        self.ellipse(cx, cy, r, r, fill, outline, width)

    def arc(self, cx, cy, r, a0, a1, color, width=3.0):
        self.d.arc(self.box(cx - r, cy - r, cx + r, cy + r), a0, a1,
                   fill=color, width=max(1, int(round(self.s(width)))))

    def polygon(self, pts, fill=None, outline=None, width=2.0):
        p = [(self.s(x), self.s(y)) for x, y in pts]
        w = max(1, int(round(self.s(width)))) if outline else 0
        self.d.polygon(p, fill=fill, outline=outline, width=w)

    def rrect(self, x0, y0, x1, y1, radius, fill=None, outline=None, width=2.0):
        w = max(1, int(round(self.s(width)))) if outline else 0
        self.d.rounded_rectangle(self.box(x0, y0, x1, y1),
                                 radius=self.s(radius), fill=fill, outline=outline, width=w)

    def rect(self, x0, y0, x1, y1, fill=None, outline=None, width=2.0):
        w = max(1, int(round(self.s(width)))) if outline else 0
        self.d.rectangle(self.box(x0, y0, x1, y1), fill=fill, outline=outline, width=w)

    # --- 效果

    def glow(self, cx, cy, radius, color, strength=0.9):
        """径向辉光。在 96x96 小图上算好再放大，比逐像素画整图快得多。"""
        n = 96
        small = Image.new("RGBA", (n, n), (0, 0, 0, 0))
        px = small.load()
        for y in range(n):
            for x in range(n):
                dx = (x + 0.5) / n * 2.0 - 1.0
                dy = (y + 0.5) / n * 2.0 - 1.0
                dist = math.sqrt(dx * dx + dy * dy)
                if dist >= 1.0:
                    continue
                f = 1.0 - dist
                f = f * f * f
                px[x, y] = (color[0], color[1], color[2], int(255 * strength * f))

        size = int(radius * 2 * self.ss)
        if size < 2:
            return
        layer = small.resize((size, size), Image.BILINEAR)
        full = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        full.paste(layer, (int(self.s(cx) - size / 2), int(self.s(cy) - size / 2)), layer)
        self.img = Image.alpha_composite(self.img, full)
        self.d = ImageDraw.Draw(self.img)

    def soft_blur(self, radius):
        self.img = self.img.filter(ImageFilter.GaussianBlur(self.s(radius)))
        self.d = ImageDraw.Draw(self.img)

    # --- 阵法常用的"符文环"

    def rune_ring(self, cx, cy, r, color, ticks=8, width=5.0, inner=None,
                  tick_len=0.13, tick_width=None):
        self.circle(cx, cy, r, outline=INK, width=width + 4.0)
        self.circle(cx, cy, r, outline=color, width=width)
        if inner:
            self.circle(cx, cy, inner, outline=INK, width=3.0)
            self.circle(cx, cy, inner, outline=shade(color, -0.25), width=1.8)
        tw = tick_width if tick_width else width * 0.9
        for i in range(ticks):
            ang = (360.0 / ticks) * i - 90.0
            rad = math.radians(ang)
            r0 = r + width * 0.55
            r1 = r + width * 0.55 + tick_len * r
            self.line([(cx + math.cos(rad) * r0, cy + math.sin(rad) * r0),
                       (cx + math.cos(rad) * r1, cy + math.sin(rad) * r1)],
                      INK, tw + 3.0)
            self.line([(cx + math.cos(rad) * r0, cy + math.sin(rad) * r0),
                       (cx + math.cos(rad) * r1, cy + math.sin(rad) * r1)],
                      color, tw)

    def save(self, path, resize=True):
        out = self.img.resize((self.w, self.h), Image.LANCZOS) if resize else self.img
        folder = os.path.dirname(path)
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)
        out.save(path, "PNG")
        return path


# --------------------------------------------------------------------------- 阵法图标

def tower_spirit_gather(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 108, JADE, 0.55)
    a.rune_ring(c, c, 84, JADE, ticks=8, width=6.0, inner=60)
    # 中央：上窜的灵气
    for i, (dx, h, w) in enumerate([(0, 62, 5), (-16, 44, 4), (16, 44, 4)]):
        col = GOLD if i == 0 else JADE_L
        a.line([(c + dx - w * 0.7, c + 16), (c + dx - w * 0.3, c - h * 0.2),
                (c + dx + w * 0.7, c - h * 0.6)], INK, w + 4)
        a.line([(c + dx - w * 0.7, c + 16), (c + dx - w * 0.3, c - h * 0.2),
                (c + dx + w * 0.7, c - h * 0.6)], col, w)
    a.circle(c, c + 26, 11, fill=GOLD, outline=INK, width=3)
    a.circle(c, c + 26, 4.5, fill=GOLD_L)
    # 四角小菱形
    for ang in (45, 135, 225, 315):
        rad = math.radians(ang)
        px, py = c + math.cos(rad) * 84, c + math.sin(rad) * 84
        a.polygon([(px, py - 11), (px + 11, py), (px, py + 11), (px - 11, py)],
                  fill=GOLD, outline=INK, width=2)
    return a.save(path)


def tower_attack(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 108, GOLD, 0.5)
    a.rune_ring(c, c, 84, GOLD, ticks=8, width=6.0, inner=60)
    # 中央：一柄向上的剑
    a.polygon([(c, c - 58), (c + 13, c - 14), (c, c + 6), (c - 13, c - 14)],
              fill=BONE, outline=INK, width=3)
    a.polygon([(c, c - 50), (c + 4, c - 16), (c, c + 2), (c - 4, c - 16)],
              fill=JADE_L)
    a.rect(c - 30, c + 6, c + 30, c + 16, fill=GOLD, outline=INK, width=3)
    a.rect(c - 7, c + 16, c + 7, c + 40, fill=GOLD_D, outline=INK, width=3)
    a.circle(c, c + 46, 8, fill=GOLD, outline=INK, width=3)
    return a.save(path)


def tower_bind(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 108, JADE, 0.45)
    a.rune_ring(c, c, 84, JADE, ticks=8, width=6.0, inner=60)
    # 四条锁链：链环连成菱形
    for ang in (45, 135, 225, 315):
        rad = math.radians(ang)
        px, py = c + math.cos(rad) * 46, c + math.sin(rad) * 46
        a.ellipse(px, py, 15, 9, outline=INK, width=7.0)
        a.ellipse(px, py, 15, 9, outline=shade(GOLD, -0.1), width=3.0)
    # 中间的锁扣
    a.rrect(c - 22, c - 22, c + 22, c + 22, 6, fill=JADE_D, outline=INK, width=3)
    a.circle(c, c - 4, 8, fill=INK, outline=None)
    a.rect(c - 3, c - 4, c + 3, c + 12, fill=INK)
    a.circle(c, c - 4, 4, fill=JADE_L)
    return a.save(path)


def tower_illusion(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 112, PURPLE, 0.45)
    # 重影圆环
    a.circle(c - 12, c - 10, 84, outline=(150, 200, 226, 90), width=4.0)
    a.circle(c + 12, c + 10, 84, outline=(150, 200, 226, 70), width=4.0)
    a.rune_ring(c, c, 84, PURPLE, ticks=8, width=6.0, inner=60)
    # 中央：一只眼
    a.polygon([(c - 44, c), (c, c - 26), (c + 44, c), (c, c + 26)],
              fill=BONE, outline=INK, width=3)
    a.circle(c, c, 15, fill=JADE, outline=INK, width=3)
    a.circle(c, c, 7, fill=INK)
    a.circle(c + 4, c - 4, 2.6, fill=BONE)
    return a.save(path)


def tower_shield(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 108, GOLD, 0.45)
    a.rune_ring(c, c, 84, GOLD, ticks=8, width=6.0, inner=60)
    # 中央：六边形盾
    r = 46
    pts = [(c + math.cos(math.radians(-90 + 60 * i)) * r,
            c + math.sin(math.radians(-90 + 60 * i)) * r) for i in range(6)]
    a.polygon(pts, fill=JADE_D, outline=INK, width=4)
    inner = [(c + math.cos(math.radians(-90 + 60 * i)) * (r - 8),
              c + math.sin(math.radians(-90 + 60 * i)) * (r - 8)) for i in range(6)]
    a.polygon(inner, fill=shade(JADE, -0.15), outline=GOLD, width=3)
    # 盾面上的"人"字
    a.line([(c - 18, c - 6), (c, c + 16), (c + 18, c - 6)], INK, 11)
    a.line([(c - 18, c - 6), (c, c + 16), (c + 18, c - 6)], JADE_L, 6)
    return a.save(path)


# --------------------------------------------------------------------------- 仙灵图标

def _spirit_body(a, c, w, h, color, ink=INK):
    a.rrect(c - w / 2, c - h / 2, c + w / 2, c + h / 2, w * 0.42,
            fill=color, outline=ink, width=3)


def spirit_sword(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 104, JADE, 0.5)
    # 剑灵本体（先画身体，剑压在身前）
    _spirit_body(a, c, 76, 96, JADE)
    a.circle(c, c - 66, 31, fill=BONE, outline=INK, width=3)
    # 发髻
    a.circle(c, c - 96, 12, fill=JADE_D, outline=INK, width=3)
    # 眼睛
    a.ellipse(c - 13, c - 70, 5.5, 8, fill=INK)
    a.ellipse(c + 13, c - 70, 5.5, 8, fill=INK)
    # 手中的剑：斜握在身前，剑尖朝上
    a.line([(c - 44, c + 74), (c + 46, c - 88)], INK, 18)
    a.line([(c - 44, c + 74), (c + 46, c - 88)], BONE, 11)
    a.line([(c - 30, c + 50), (c + 34, c - 60)], JADE_L, 4)
    # 剑格 + 剑柄
    a.line([(c - 30, c + 12), (c + 2, c + 44)], GOLD, 12)
    a.line([(c - 30, c + 12), (c + 2, c + 44)], INK, 4)
    a.line([(c - 44, c + 74), (c - 26, c + 54)], GOLD_D, 13)
    # 剑气
    a.line([(c + 58, c - 104), (c + 76, c - 78)], (200, 244, 226, 180), 5)
    a.line([(c + 40, c - 116), (c + 50, c - 96)], (200, 244, 226, 140), 4)
    return a.save(path)


def spirit_talisman(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 100, GOLD, 0.45)
    # 符纸
    a.polygon([(c - 40, c - 78), (c + 40, c - 78), (c + 40, c + 62), (c, c + 92),
               (c - 40, c + 62)], fill=BONE, outline=INK, width=4)
    a.polygon([(c - 31, c - 68), (c + 31, c - 68), (c + 31, c + 56), (c, c + 80),
               (c - 31, c + 56)], outline=shade(GOLD, -0.1), width=2.5)
    # 朱砂符文
    a.line([(c, c - 60), (c, c - 30)], CINNABAR, 6)
    a.line([(c - 18, c - 46), (c + 18, c - 46)], CINNABAR, 6)
    a.line([(c - 18, c - 22), (c + 18, c - 22)], CINNABAR, 6)
    a.line([(c - 14, c - 22), (c - 14, c + 10)], CINNABAR, 6)
    a.line([(c + 14, c - 22), (c + 14, c + 10)], CINNABAR, 6)
    a.line([(c - 14, c + 10), (c + 14, c + 10)], CINNABAR, 6)
    a.line([(c, c + 10), (c, c + 42)], CINNABAR, 6)
    a.circle(c, c + 52, 7, fill=CINNABAR)
    return a.save(path)


def spirit_pill(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 104, GOLD, 0.55)
    # 丹炉（葫芦形）
    a.circle(c, c + 30, 48, fill=JADE_D, outline=INK, width=4)
    a.circle(c, c - 32, 34, fill=shade(JADE_D, 0.1), outline=INK, width=4)
    a.rect(c - 14, c - 10, c + 14, c + 12, fill=JADE_D, outline=INK, width=3)
    a.rrect(c - 22, c - 74, c + 22, c - 54, 6, fill=GOLD, outline=INK, width=3)
    # 高光
    a.arc(c, c + 30, 32, 200, 320, JADE_L, 4)
    a.arc(c, c - 32, 22, 200, 320, JADE_L, 3)
    # 升起的丹药与灵气
    a.circle(c, c - 104, 15, fill=GOLD_L, outline=INK, width=3)
    a.circle(c - 4, c - 109, 4, fill=BONE)
    for i, dx in enumerate((-30, 30)):
        a.line([(c + dx, c - 90), (c + dx * 0.6, c - 118)], INK, 8)
        a.line([(c + dx, c - 90), (c + dx * 0.6, c - 118)], GOLD, 4)
    return a.save(path)


# --------------------------------------------------------------------------- 妖魔图标

def _demon_body(a, c, w, h, color, outline=INK):
    a.rrect(c - w / 2, c - h / 2, c + w / 2, c + h / 2, w * 0.4,
            fill=color, outline=outline, width=4)


def _horns(a, c, spread, base_y, tip_y, color, width=9):
    """两段式弯角：先向两侧张开，再往上翘 —— 比直线更像妖角。"""
    mid_y = base_y + (tip_y - base_y) * 0.45
    for sgn in (-1, 1):
        p0 = (c + sgn * spread * 0.42, base_y)
        p1 = (c + sgn * spread * 1.05, mid_y)
        p2 = (c + sgn * spread * 0.78, tip_y)
        a.line([p0, p1, p2], INK, width * 1.9)
        a.line([p0, p1, p2], color, width)


def _feet(a, c, half, y, color):
    """两只脚，让妖魔的剪影不那么像方块。"""
    for sgn in (-1, 1):
        a.rrect(c + sgn * half - 17, y - 6, c + sgn * half + 17, y + 20, 8,
                fill=color, outline=INK, width=3)


def _claws(a, c, half, y, color, spread=30):
    for sgn in (-1, 1):
        a.line([(c + sgn * half, y), (c + sgn * (half + spread), y + 24)], INK, 14)
        a.line([(c + sgn * half, y), (c + sgn * (half + spread), y + 24)], color, 8)
        for k in range(3):
            a.line([(c + sgn * (half + spread), y + 24),
                    (c + sgn * (half + spread + 12), y + 38 - k * 9)], BONE, 3.5)


def _eyes(a, c, dx, y, r, color):
    for sgn in (-1, 1):
        a.ellipse(c + sgn * dx, y, r * 1.15, r, fill=INK)
        a.ellipse(c + sgn * dx, y, r * 0.75, r * 0.66, fill=color)


def enemy_minion(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c + 10, 78, DEMON_P, 0.4)
    _feet(a, c, 30, c + 54, DEMON_P)
    _demon_body(a, c, 104, 112, DEMON_P)
    _horns(a, c, 58, c - 46, c - 92, BONE, width=8)
    _eyes(a, c, 22, c - 8, 11, (255, 120, 110, A))
    a.line([(c - 22, c + 32), (c - 4, c + 42), (c + 4, c + 32), (c + 22, c + 42)], INK, 9)
    a.line([(c - 22, c + 32), (c - 4, c + 42), (c + 4, c + 32), (c + 22, c + 42)], BONE, 4)
    _claws(a, c, 52, c + 4, DEMON_P, spread=26)
    return a.save(path)


def enemy_lieutenant(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c + 8, 84, DEMON, 0.4)
    _feet(a, c, 32, c + 60, DEMON_D)
    # 大刀（斜挎在身后）
    a.line([(c + 30, c + 76), (c + 88, c - 50)], INK, 20)
    a.line([(c + 30, c + 76), (c + 88, c - 50)], STONE_D, 12)
    a.polygon([(c + 60, c - 30), (c + 106, c - 88), (c + 78, c - 16)],
              fill=BONE, outline=INK, width=3)
    _demon_body(a, c, 118, 126, DEMON)
    # 甲片
    a.rrect(c - 46, c + 6, c + 46, c + 60, 14, fill=DEMON_D, outline=INK, width=3)
    for dx in (-22, 0, 22):
        a.circle(c + dx, c + 32, 8, fill=GOLD_D, outline=INK, width=2)
    _horns(a, c, 66, c - 52, c - 106, BONE, width=9)
    # 头盔
    a.rrect(c - 38, c - 78, c + 38, c - 26, 12, fill=DEMON_D, outline=INK, width=4)
    a.polygon([(c - 30, c - 68), (c, c - 100), (c + 30, c - 68)],
              fill=GOLD_D, outline=INK, width=3)
    _eyes(a, c, 20, c - 44, 10, (255, 140, 90, A))
    return a.save(path)


def enemy_greater(path):
    S = 256
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c + 6, 96, DEMON_D, 0.5)
    _feet(a, c, 40, c + 62, shade(DEMON_D, 0.05))
    # 肩甲尖刺
    for sgn in (-1, 1):
        for k in range(3):
            x = c + sgn * (54 + k * 10)
            a.polygon([(x, c - 20), (x + sgn * 20, c - 58 - k * 10), (x + sgn * 2, c - 14)],
                      fill=shade(STONE_D, -0.1), outline=INK, width=3)
    _demon_body(a, c, 142, 140, shade(DEMON_D, 0.14))
    a.rrect(c - 63, c - 64, c + 63, c + 62, 40, fill=None, outline=INK, width=4)
    # 胸甲
    a.polygon([(c - 40, c - 26), (c + 40, c - 26), (c + 26, c + 40), (c - 26, c + 40)],
              fill=DEMON_D, outline=INK, width=4)
    a.circle(c, c + 4, 16, fill=CINNABAR, outline=INK, width=3)
    a.circle(c, c + 4, 7, fill=GOLD_L)
    # 头
    a.circle(c, c - 74, 30, fill=shade(DEMON_D, 0.22), outline=INK, width=4)
    _horns(a, c, 50, c - 88, c - 124, BONE, width=8)
    _eyes(a, c, 15, c - 74, 9, (255, 130, 90, A))
    return a.save(path)


def enemy_boss(path):
    S = 320
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c + 6, 132, CINNABAR, 0.5)
    # 披风：左右两片，让剪影更宽更有压迫感
    a.polygon([(c - 60, c - 60), (c - 150, c - 10), (c - 128, c + 118),
               (c - 40, c + 96)], fill=CINNABAR_D, outline=INK, width=5)
    a.polygon([(c + 60, c - 60), (c + 150, c - 10), (c + 128, c + 118),
               (c + 40, c + 96)], fill=CINNABAR_D, outline=INK, width=5)
    _feet(a, c, 40, c + 76, DEMON_D)
    # 本体
    a.rrect(c - 56, c - 76, c + 56, c + 80, 24, fill=shade(DEMON_D, 0.08), outline=INK, width=5)
    # 胸口魔纹
    a.circle(c, c + 8, 26, fill=CINNABAR, outline=INK, width=4)
    a.circle(c, c + 8, 12, fill=GOLD_L)
    a.line([(c - 40, c - 44), (c + 40, c - 44)], GOLD, 5)
    # 头
    a.circle(c, c - 98, 40, fill=shade(DEMON_D, 0.18), outline=INK, width=5)
    _horns(a, c, 60, c - 112, c - 168, BONE, width=10)
    _eyes(a, c, 20, c - 98, 12, (255, 150, 80, A))
    # 王冠
    for k, dx in enumerate((-30, 0, 30)):
        h = 44 if k == 1 else 30
        a.polygon([(c + dx - 11, c - 128), (c + dx, c - 128 - h), (c + dx + 11, c - 128)],
                  fill=GOLD_D, outline=INK, width=3)
    # 獠牙
    a.line([(c - 22, c - 78), (c - 27, c - 58)], BONE, 6)
    a.line([(c + 22, c - 78), (c + 27, c - 58)], BONE, 6)
    _claws(a, c, 56, c + 24, DEMON_D, spread=42)
    return a.save(path)


# --------------------------------------------------------------------------- 地形

def tile_buildable(path, alt=False):
    S = 256
    base = (44, 54, 62, 255) if not alt else (50, 60, 68, 255)
    a = Art(S, S, ss=2)
    a.rect(0, 0, S, S, fill=base)
    # 石纹
    for i in range(0, S, 32):
        a.line([(i, 0), (i, S)], (58, 70, 80, 120), 1.0)
        a.line([(0, i), (S, i)], (58, 70, 80, 120), 1.0)
    # 内框
    a.rrect(6, 6, S - 6, S - 6, 10, outline=(96, 132, 132, 110), width=2.0)
    # 中心淡淡符文（要压得住，别抢阵法本身的视觉）
    a.circle(S / 2, S / 2, 22, outline=(110, 190, 170, 52), width=2.0)
    a.circle(S / 2, S / 2, 5, fill=(110, 190, 170, 70))
    for ang in (0, 90, 180, 270):
        rad = math.radians(ang)
        a.line([(S / 2 + math.cos(rad) * 26, S / 2 + math.sin(rad) * 26),
                (S / 2 + math.cos(rad) * 36, S / 2 + math.sin(rad) * 36)],
               (110, 190, 170, 52), 2.0)
    return a.save(path)


def tile_path(path):
    S = 256
    a = Art(S, S, ss=2)
    a.rect(0, 0, S, S, fill=(38, 34, 44, 255))
    import random
    rnd = random.Random(7)
    for _ in range(160):
        x = rnd.uniform(0, S)
        y = rnd.uniform(0, S)
        r = rnd.uniform(1.5, 5.0)
        v = rnd.randint(-12, 14)
        col = (38 + v, 34 + v, 44 + v, 255)
        a.circle(x, y, r, fill=col)
    # 路沿
    a.line([(0, 3), (S, 3)], (86, 76, 96, 200), 3.0)
    a.line([(0, S - 3), (S, S - 3)], (86, 76, 96, 200), 3.0)
    return a.save(path)


def core_gate(path):
    W, H = 512, 256
    a = Art(W, H)
    cx = W / 2.0
    a.glow(cx, H * 0.55, 190, JADE, 0.45)
    # 底座
    a.rrect(40, H - 46, W - 40, H - 12, 10, fill=STONE_D, outline=INK, width=4)
    # 两根柱子
    for sgn in (-1, 1):
        x = cx + sgn * 132
        a.rrect(x - 22, 84, x + 22, H - 38, 8, fill=CINNABAR, outline=INK, width=4)
        a.rect(x - 8, 96, x + 8, H - 46, fill=shade(CINNABAR, 0.2))
        a.rrect(x - 28, 78, x + 28, 96, 6, fill=GOLD_D, outline=INK, width=3)
    # 横梁
    a.rrect(cx - 176, 58, cx + 176, 86, 8, fill=CINNABAR, outline=INK, width=4)
    # 屋檐
    a.polygon([(cx - 196, 60), (cx + 196, 60), (cx + 150, 26), (cx - 150, 26)],
              fill=GOLD_D, outline=INK, width=4)
    a.polygon([(cx - 120, 28), (cx + 120, 28), (cx + 84, 6), (cx - 84, 6)],
              fill=shade(GOLD_D, -0.2), outline=INK, width=4)
    # 匾额
    a.rrect(cx - 74, 96, cx + 74, 150, 8, fill=GOLD, outline=INK, width=4)
    a.line([(cx - 42, 112), (cx - 42, 136)], INK, 7)
    a.line([(cx - 42, 124), (cx - 20, 112)], INK, 7)
    a.line([(cx - 20, 112), (cx - 20, 136)], INK, 7)
    a.line([(cx + 8, 112), (cx + 8, 136)], INK, 7)
    a.line([(cx + 8, 124), (cx + 34, 112)], INK, 7)
    a.line([(cx + 34, 112), (cx + 34, 136)], INK, 7)
    # 灵光柱
    for sgn in (-1, 1):
        x = cx + sgn * 132
        a.line([(x, 78), (x, 34)], (140, 240, 200, 90), 8.0)
    return a.save(path)


def portal(path, color):
    W, H = 256, 160
    a = Art(W, H)
    cx, cy = W / 2.0, H / 2.0
    a.glow(cx, cy, 120, color, 0.6)
    a.ellipse(cx, cy, 86, 52, fill=INK, outline=INK, width=4)
    for k, (rx, ry, w) in enumerate([(78, 46, 5), (62, 34, 4), (46, 22, 3)]):
        a.ellipse(cx, cy, rx, ry, outline=shade(color, -0.1 + k * 0.2), width=w)
    a.ellipse(cx, cy, 26, 12, fill=shade(color, 0.3))
    return a.save(path)


# --------------------------------------------------------------------------- 符箓与技能

def _talisman_strip(a, W, H, paper, ink_color, glyph):
    cx = W / 2.0
    a.polygon([(cx - 62, 40), (cx + 62, 40), (cx + 62, H - 96), (cx, H - 44),
               (cx - 62, H - 96)], fill=INK, outline=None)
    a.polygon([(cx - 54, 48), (cx + 54, 48), (cx + 54, H - 100), (cx, H - 56),
               (cx - 54, H - 100)], fill=paper, outline=None)
    a.rrect(cx - 46, 58, cx + 46, H - 112, 6, outline=ink_color, width=3.0)
    glyph(a, cx, H / 2.0, ink_color)


def trap_thunder(path):
    W = H = 224
    a = Art(W, H)
    a.glow(W / 2.0, H / 2.0, 100, GOLD, 0.5)

    def glyph(a, cx, cy, col):
        a.polygon([(cx - 6, cy - 44), (cx + 16, cy - 44), (cx + 2, cy - 8),
                   (cx + 22, cy - 8), (cx - 12, cy + 46), (cx - 2, cy + 4),
                   (cx - 22, cy + 4)], fill=col)

    _talisman_strip(a, W, H, BONE, GOLD_D, glyph)
    return a.save(path)


def trap_ice(path):
    W = H = 224
    a = Art(W, H)
    a.glow(W / 2.0, H / 2.0, 100, ICE, 0.55)

    def glyph(a, cx, cy, col):
        for ang in range(0, 360, 60):
            rad = math.radians(ang)
            a.line([(cx, cy), (cx + math.cos(rad) * 44, cy + math.sin(rad) * 44)], col, 5)
            ex, ey = cx + math.cos(rad) * 44, cy + math.sin(rad) * 44
            for da in (-32, 32):
                r2 = math.radians(ang + da)
                a.line([(ex, ey), (ex + math.cos(r2) * 14, ey + math.sin(r2) * 14)], col, 4)

    _talisman_strip(a, W, H, (226, 244, 252, 255), ICE_D, glyph)
    return a.save(path)


def trap_blast(path):
    W = H = 224
    a = Art(W, H)
    a.glow(W / 2.0, H / 2.0, 100, CINNABAR, 0.55)

    def glyph(a, cx, cy, col):
        for ang in range(0, 360, 45):
            rad = math.radians(ang)
            r = 46 if ang % 90 == 0 else 32
            a.line([(cx, cy), (cx + math.cos(rad) * r, cy + math.sin(rad) * r)], col, 5)
        a.circle(cx, cy, 12, fill=col)

    _talisman_strip(a, W, H, (255, 226, 218, 255), CINNABAR_D, glyph)
    return a.save(path)


def _skill_ring(a, S, color, glow=0.6):
    c = S / 2.0
    a.glow(c, c, S * 0.46, color, glow)
    a.rune_ring(c, c, S * 0.35, color, ticks=12, width=S * 0.026, inner=S * 0.26,
                tick_len=0.10)
    return c


def skill_thunder(path):
    S = 288
    a = Art(S, S)
    c = _skill_ring(a, S, GOLD)
    a.polygon([(c - 16, c - 82), (c + 34, c - 82), (c + 2, c - 16),
               (c + 40, c - 16), (c - 30, c + 86), (c - 4, c + 8), (c - 44, c + 8)],
              fill=GOLD_L, outline=INK, width=4)
    return a.save(path)


def skill_frost(path):
    S = 288
    a = Art(S, S)
    c = _skill_ring(a, S, ICE)
    for ang in range(0, 360, 60):
        rad = math.radians(ang)
        a.line([(c, c), (c + math.cos(rad) * 80, c + math.sin(rad) * 80)], INK, 11)
        a.line([(c, c), (c + math.cos(rad) * 80, c + math.sin(rad) * 80)], ICE, 6)
        ex, ey = c + math.cos(rad) * 80, c + math.sin(rad) * 80
        for da in (-34, 34):
            r2 = math.radians(ang + da)
            a.line([(ex, ey), (ex + math.cos(r2) * 26, ey + math.sin(r2) * 26)], ICE, 5)
    a.circle(c, c, 18, fill=BONE, outline=INK, width=4)
    return a.save(path)


def skill_rain(path):
    S = 288
    a = Art(S, S)
    c = _skill_ring(a, S, JADE)
    for dx in (-44, 0, 44):
        a.line([(c + dx, c - 54), (c + dx - 12, c + 26)], INK, 13)
        a.line([(c + dx, c - 54), (c + dx - 12, c + 26)], ICE, 7)
        a.circle(c + dx - 14, c + 40, 8, fill=ICE, outline=INK, width=3)
    return a.save(path)


# --------------------------------------------------------------------------- UI 与特效

def ui_spirit(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 56, JADE, 0.7)
    a.circle(c, c, 30, fill=JADE_D, outline=INK, width=4)
    a.circle(c, c + 2, 22, fill=JADE, outline=None)
    a.circle(c - 8, c - 8, 8, fill=JADE_L)
    a.circle(c + 9, c + 6, 5, fill=shade(JADE, 0.4))
    return a.save(path)


def ui_core(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.polygon([(c - 40, c + 34), (c + 40, c + 34), (c + 40, c + 16), (c - 40, c + 16)],
              fill=STONE_D, outline=INK, width=4)
    a.rrect(c - 22, c - 22, c + 22, c + 18, 6, fill=CINNABAR, outline=INK, width=4)
    a.polygon([(c - 40, c - 22), (c + 40, c - 22), (c + 28, c - 40), (c - 28, c - 40)],
              fill=GOLD_D, outline=INK, width=4)
    a.rrect(c - 16, c - 10, c + 16, c + 8, 4, fill=GOLD, outline=INK, width=3)
    return a.save(path)


def ui_clock(path):
    """计时图标（结算面板的"耗时"行）。"""
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.circle(c, c, 44, fill=JADE_D, outline=INK, width=5)
    a.circle(c, c, 34, fill=shade(JADE_D, -0.25), outline=None)
    # 指针：短时针 + 长分针
    a.line([(c, c), (c, c - 20)], GOLD, 7.0)
    a.line([(c, c), (c + 17, c + 9)], BONE, 5.5)
    a.circle(c, c, 6, fill=GOLD, outline=INK, width=3)
    # 顶部提环
    a.rrect(c - 9, c - 56, c + 9, c - 44, 3, fill=GOLD_D, outline=INK, width=3)
    return a.save(path)


def ui_wave(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    for k, (r, w) in enumerate([(20, 6), (32, 6), (44, 6)]):
        col = lerp(BONE, JADE, k / 2.0)
        a.arc(c, c + 10, r, 200, 340, INK, w + 4)
        a.arc(c, c + 10, r, 200, 340, col, w)
    return a.save(path)


def ui_pause(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.rrect(c - 30, c - 38, c - 10, c + 38, 5, fill=BONE, outline=INK, width=4)
    a.rrect(c + 10, c - 38, c + 30, c + 38, 5, fill=BONE, outline=INK, width=4)
    return a.save(path)


def ui_play(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.polygon([(c - 26, c - 40), (c + 40, c), (c - 26, c + 40)],
              fill=BONE, outline=INK, width=4)
    return a.save(path)


def ui_speed(path, double=False):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    offs = (-26, 6) if double else (-6, 6)
    for k, ox in enumerate(offs if double else (-6,)):
        a.polygon([(c + ox, c - 34), (c + ox + 34, c), (c + ox, c + 34)],
                  fill=JADE, outline=INK, width=4)
    if not double:
        a.polygon([(c - 34, c - 34), (c, c), (c - 34, c + 34)],
                  fill=JADE_D, outline=INK, width=4)
    return a.save(path)


def ui_close(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    for ang in (45, 135):
        rad = math.radians(ang)
        a.line([(c - math.cos(rad) * 36, c - math.sin(rad) * 36),
                (c + math.cos(rad) * 36, c + math.sin(rad) * 36)], INK, 18)
        a.line([(c - math.cos(rad) * 36, c - math.sin(rad) * 36),
                (c + math.cos(rad) * 36, c + math.sin(rad) * 36)], BONE, 10)
    return a.save(path)


def ui_gear(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    for i in range(8):
        rad = math.radians(i * 45.0)
        a.rrect(c + math.cos(rad) * 34 - 9, c + math.sin(rad) * 34 - 9,
                c + math.cos(rad) * 34 + 9, c + math.sin(rad) * 34 + 9, 3,
                fill=BONE, outline=INK, width=3)
    a.circle(c, c, 30, fill=BONE, outline=INK, width=4)
    a.circle(c, c, 13, fill=INK)
    return a.save(path)


def ui_trophy(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 52, GOLD, 0.45)
    a.rrect(c - 30, c - 42, c + 30, c + 6, 8, fill=GOLD, outline=INK, width=4)
    a.arc(c - 34, c - 24, 20, 90, 270, INK, 9)
    a.arc(c + 34, c - 24, 20, -90, 90, INK, 9)
    a.rect(c - 8, c + 6, c + 8, c + 28, fill=GOLD_D, outline=INK, width=3)
    a.rrect(c - 24, c + 28, c + 24, c + 42, 4, fill=GOLD_D, outline=INK, width=3)
    a.circle(c, c - 16, 10, fill=GOLD_L)
    return a.save(path)


def ui_book(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.rrect(c - 36, c - 40, c + 36, c + 40, 6, fill=BONE, outline=INK, width=4)
    a.line([(c, c - 40), (c, c + 40)], INK, 5)
    for k in range(4):
        y = c - 24 + k * 14
        a.line([(c - 28, y), (c - 8, y)], STONE, 4)
        a.line([(c + 8, y), (c + 28, y)], STONE, 4)
    return a.save(path)


def ui_info(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.circle(c, c, 38, fill=JADE_D, outline=INK, width=4)
    a.circle(c, c - 20, 6, fill=BONE)
    a.rrect(c - 6, c - 8, c + 6, c + 24, 4, fill=BONE)
    return a.save(path)


def ui_sound(path, on=True):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.polygon([(c - 34, c - 14), (c - 14, c - 14), (c + 4, c - 34),
               (c + 4, c + 34), (c - 14, c + 14), (c - 34, c + 14)],
              fill=BONE, outline=INK, width=4)
    if on:
        for k, r in enumerate((18, 30)):
            a.arc(c + 6, c, r, -60, 60, JADE, 5)
    else:
        for ang in (45, 135):
            rad = math.radians(ang)
            a.line([(c + 16 - math.cos(rad) * 18, c - math.sin(rad) * 18),
                    (c + 16 + math.cos(rad) * 18, c + math.sin(rad) * 18)], CINNABAR, 8)
    return a.save(path)


def ui_sell(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    # 铲子
    a.line([(c - 26, c - 34), (c + 18, c + 18)], INK, 13)
    a.line([(c - 26, c - 34), (c + 18, c + 18)], BONE, 7)
    a.polygon([(c + 6, c + 6), (c + 38, c + 38), (c + 18, c + 46), (c - 2, c + 24)],
              fill=STONE, outline=INK, width=4)
    return a.save(path)


def ui_upgrade(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.polygon([(c, c - 40), (c + 34, c - 4), (c - 34, c - 4)],
              fill=JADE, outline=INK, width=4)
    a.rrect(c - 12, c - 4, c + 12, c + 34, 4, fill=JADE_D, outline=INK, width=4)
    return a.save(path)


def ui_lock(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.arc(c, c - 14, 22, 180, 360, BONE, 8)
    a.rrect(c - 32, c - 12, c + 32, c + 34, 8, fill=STONE_D, outline=INK, width=4)
    a.circle(c, c + 8, 8, fill=BONE)
    a.rect(c - 3, c + 8, c + 3, c + 22, fill=BONE)
    return a.save(path)


def ui_check(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.circle(c, c, 38, fill=JADE_D, outline=INK, width=4)
    a.line([(c - 18, c + 2), (c - 4, c + 18), (c + 20, c - 18)], INK, 16)
    a.line([(c - 18, c + 2), (c - 4, c + 18), (c + 20, c - 18)], JADE_L, 8)
    return a.save(path)


def ui_star(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 52, GOLD, 0.5)
    pts = []
    for i in range(10):
        r = 40 if i % 2 == 0 else 17
        rad = math.radians(-90 + i * 36)
        pts.append((c + math.cos(rad) * r, c + math.sin(rad) * r))
    a.polygon(pts, fill=GOLD, outline=INK, width=4)
    return a.save(path)


def ui_skull(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.rrect(c - 30, c - 36, c + 30, c + 16, 18, fill=BONE, outline=INK, width=4)
    a.ellipse(c - 14, c - 18, 9, 11, fill=INK)
    a.ellipse(c + 14, c - 18, 9, 11, fill=INK)
    a.polygon([(c, c - 4), (c + 7, c + 10), (c - 7, c + 10)], fill=INK)
    for dx in (-16, 0, 16):
        a.rect(c + dx - 5, c + 16, c + dx + 5, c + 34, fill=BONE, outline=INK, width=3)
    return a.save(path)


def ui_arrow_up(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.polygon([(c, c - 36), (c + 30, c + 2), (c - 30, c + 2)], fill=BONE, outline=INK, width=4)
    a.rrect(c - 10, c + 2, c + 10, c + 36, 3, fill=BONE, outline=INK, width=4)
    return a.save(path)


def ui_panel(path, base=(255, 255, 255, 232), edge=(255, 255, 255, 255)):
    """
    九宫格面板底图。

    刻意画成**白色**：这样代码里可以用 Image.color 随便染色，
    面板/按钮的底色全部由代码决定，不用为每种颜色各生成一张图。
    编辑器脚本会把它设成 sprite border，UI 里就能任意拉伸不变形。
    """
    S = 96
    r = 26
    a = Art(S, S, ss=4)
    a.rrect(0, 0, S, S, r, fill=base)
    a.rrect(2, 2, S - 2, S - 2, r - 2, outline=edge, width=2.0)
    a.line([(r, 3.5), (S - r, 3.5)], (255, 255, 255, 255), 1.4)
    return a.save(path)


def ui_bar(path, fill=False):
    """进度条底/填充，同样是白色，靠代码染色。"""
    W, H = 64, 24
    a = Art(W, H, ss=4)
    alpha = 255 if fill else 220
    a.rrect(0, 0, W, H, H / 2, fill=(255, 255, 255, alpha))
    return a.save(path)


def fx_bolt(path):
    S = 128
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 60, GOLD_L, 0.9)
    a.circle(c, c, 26, fill=(255, 244, 208, 220))
    a.circle(c, c, 12, fill=(255, 255, 255, 255))
    return a.save(path)


def fx_impact(path):
    S = 192
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 90, BONE, 0.75)
    a.polygon([(c, c - 74), (c + 18, c - 18), (c + 74, c), (c + 18, c + 18),
               (c, c + 74), (c - 18, c + 18), (c - 74, c), (c - 18, c - 18)],
              fill=(255, 252, 236, 235))
    return a.save(path)


def fx_ring(path):
    S = 192
    a = Art(S, S)
    c = S / 2.0
    for k, (r, w, al) in enumerate([(80, 8, 200), (62, 6, 150), (44, 4, 100)]):
        a.circle(c, c, r, outline=(200, 244, 226, al), width=w)
    return a.save(path)


def fx_frost(path, color=ICE):
    S = 192
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 86, color, 0.7)
    for ang in range(0, 360, 60):
        rad = math.radians(ang)
        a.line([(c, c), (c + math.cos(rad) * 64, c + math.sin(rad) * 64)], color, 7)
    a.circle(c, c, 16, fill=BONE)
    return a.save(path)


def fx_slash(path):
    S = 160
    a = Art(S, S)
    c = S / 2.0
    a.arc(c, c, 56, 20, 160, (255, 250, 232, 235), 12)
    a.arc(c, c, 44, 30, 150, (255, 255, 255, 200), 6)
    return a.save(path)


def fx_shield(path):
    S = 192
    a = Art(S, S)
    c = S / 2.0
    a.glow(c, c, 84, GOLD, 0.6)
    r = 66
    pts = [(c + math.cos(math.radians(-90 + 60 * i)) * r,
            c + math.sin(math.radians(-90 + 60 * i)) * r) for i in range(6)]
    a.polygon(pts, outline=(255, 232, 176, 200), width=7)
    a.polygon(pts, outline=(255, 255, 240, 140), width=3)
    return a.save(path)


# --------------------------------------------------------------------------- 背景

def _mountains(d, W, H, base_y, color, seed, amp, step=90):
    import random
    rnd = random.Random(seed)
    pts = [(0, H)]
    x = 0
    while x <= W + step:
        y = base_y + math.sin(x * 0.0032 + seed) * amp * 0.5 + rnd.uniform(-amp * 0.35, amp * 0.3)
        pts.append((x, y))
        x += step
    pts.append((W, H))
    d.polygon(pts, fill=color)


def bg_menu(path):
    W, H = 1080, 1920
    img = Image.new("RGB", (W, H), (14, 18, 30))
    d = ImageDraw.Draw(img)
    # 天空渐变
    for y in range(H):
        t = y / float(H)
        if t < 0.55:
            k = t / 0.55
            col = lerp((26, 40, 74, 255), (78, 62, 108, 255), k)
        else:
            k = (t - 0.55) / 0.45
            col = lerp((78, 62, 108, 255), (16, 20, 32, 255), k)
        d.line([(0, y), (W, y)], fill=(col[0], col[1], col[2]))
    # 月亮
    mx, my = W * 0.72, H * 0.20
    for r in range(150, 0, -6):
        k = 1.0 - r / 150.0
        a = int(70 * k * k)
        d.ellipse([mx - r, my - r, mx + r, my + r], fill=(240, 236, 200))
    d.ellipse([mx - 96, my - 96, mx + 96, my + 96], fill=(246, 242, 214))
    # 云带
    for i, (cy, al, hh) in enumerate([(H * 0.28, 26, 26), (H * 0.36, 20, 18), (H * 0.46, 14, 22)]):
        d.ellipse([-200, cy, W + 200, cy + hh], fill=(206, 216, 226))
    # 远山 / 近山
    _mountains(d, W, H, H * 0.60, (44, 52, 84), 1, 130, 120)
    _mountains(d, W, H, H * 0.68, (32, 40, 66), 2, 110, 90)
    _mountains(d, W, H, H * 0.78, (22, 28, 46), 3, 90, 70)
    # 前景：山门前的台阶与护山大阵的光环
    d.ellipse([-260, H * 0.80, W + 260, H * 0.98], fill=(18, 24, 38))
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ga = Art(W, H, ss=1)
    ga.glow(W * 0.5, H * 0.86, 460, (96, 214, 178, 255), 0.30)
    ga.glow(W * 0.5, H * 0.86, 240, (242, 198, 96, 255), 0.22)
    img = Image.alpha_composite(img.convert("RGBA"), ga.img).convert("RGB")
    d = ImageDraw.Draw(img)
    for k, r in enumerate((330, 250, 170)):
        d.ellipse([W * 0.5 - r, H * 0.86 - r * 0.28, W * 0.5 + r, H * 0.86 + r * 0.28],
                  outline=(120, 216, 186), width=4)
    # 星点
    import random
    rnd = random.Random(11)
    for _ in range(220):
        x = rnd.uniform(0, W)
        y = rnd.uniform(0, H * 0.5)
        r = rnd.uniform(0.8, 2.4)
        v = rnd.randint(180, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(v, v, min(255, v + 20)))
    img.save(path, "PNG")
    return path


def bg_battle(path):
    W, H = 1080, 1920
    img = Image.new("RGB", (W, H), (16, 20, 30))
    d = ImageDraw.Draw(img)
    for y in range(H):
        t = y / float(H)
        col = lerp((24, 30, 46, 255), (12, 16, 24, 255), t)
        d.line([(0, y), (W, y)], fill=(col[0], col[1], col[2]))
    # 远处的山影
    _mountains(d, W, H, H * 0.10, (30, 38, 56), 5, 60, 140)
    _mountains(d, W, H, H * 0.20, (22, 28, 42), 6, 50, 110)
    # 中央的淡光（阵法所在）
    ga = Art(W, H, ss=1)
    ga.glow(W * 0.5, H * 0.55, 620, (70, 140, 140, 255), 0.16)
    img = Image.alpha_composite(img.convert("RGBA"), ga.img).convert("RGB")
    # 四周压暗，把注意力聚到中间
    vign = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vign)
    for i in range(90):
        al = int(150 * (1 - i / 90.0) ** 2)
        vd.rectangle([i * 6, i * 6, W - i * 6, H - i * 6], outline=(0, 0, 0, al))
    img = Image.alpha_composite(img.convert("RGBA"), vign).convert("RGB")
    img.save(path, "PNG")
    return path


# --------------------------------------------------------------------------- 主流程

def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    if len(sys.argv) > 1:
        root = sys.argv[1]
    units = os.path.join(root, "Assets", "Resources", "Sprites", "Units")
    terrain = os.path.join(root, "Assets", "Resources", "Sprites", "Terrain")
    ui = os.path.join(root, "Assets", "Resources", "Sprites", "UI")

    jobs = []
    # 阵法
    jobs.append((os.path.join(units, "tower_spirit_gather.png"), tower_spirit_gather))
    jobs.append((os.path.join(units, "tower_attack.png"), tower_attack))
    jobs.append((os.path.join(units, "tower_bind.png"), tower_bind))
    jobs.append((os.path.join(units, "tower_illusion.png"), tower_illusion))
    jobs.append((os.path.join(units, "tower_shield.png"), tower_shield))
    # 仙灵
    jobs.append((os.path.join(units, "spirit_sword.png"), spirit_sword))
    jobs.append((os.path.join(units, "spirit_talisman.png"), spirit_talisman))
    jobs.append((os.path.join(units, "spirit_pill.png"), spirit_pill))
    # 妖魔
    jobs.append((os.path.join(units, "enemy_minion.png"), enemy_minion))
    jobs.append((os.path.join(units, "enemy_lieutenant.png"), enemy_lieutenant))
    jobs.append((os.path.join(units, "enemy_greater.png"), enemy_greater))
    jobs.append((os.path.join(units, "enemy_boss.png"), enemy_boss))
    # 地形
    jobs.append((os.path.join(terrain, "tile_buildable.png"), tile_buildable))
    jobs.append((os.path.join(terrain, "tile_buildable_alt.png"), lambda p: tile_buildable(p, True)))
    jobs.append((os.path.join(terrain, "tile_path.png"), tile_path))
    jobs.append((os.path.join(terrain, "core_gate.png"), core_gate))
    jobs.append((os.path.join(terrain, "portal_a.png"), lambda p: portal(p, JADE)))
    jobs.append((os.path.join(terrain, "portal_b.png"), lambda p: portal(p, CINNABAR)))
    # 符箓与技能
    jobs.append((os.path.join(ui, "trap_thunder.png"), trap_thunder))
    jobs.append((os.path.join(ui, "trap_ice.png"), trap_ice))
    jobs.append((os.path.join(ui, "trap_blast.png"), trap_blast))
    jobs.append((os.path.join(ui, "skill_thunder.png"), skill_thunder))
    jobs.append((os.path.join(ui, "skill_frost.png"), skill_frost))
    jobs.append((os.path.join(ui, "skill_rain.png"), skill_rain))
    # UI 图标
    for name, fn in [
        ("ui_spirit", ui_spirit), ("ui_core", ui_core), ("ui_wave", ui_wave),
        ("ui_pause", ui_pause), ("ui_play", ui_play), ("ui_close", ui_close),
        ("ui_gear", ui_gear), ("ui_trophy", ui_trophy), ("ui_book", ui_book),
        ("ui_info", ui_info), ("ui_sell", ui_sell), ("ui_upgrade", ui_upgrade),
        ("ui_lock", ui_lock), ("ui_check", ui_check), ("ui_star", ui_star),
        ("ui_skull", ui_skull), ("ui_arrow_up", ui_arrow_up), ("ui_clock", ui_clock),
    ]:
        jobs.append((os.path.join(ui, name + ".png"), fn))
    jobs.append((os.path.join(ui, "ui_speed1.png"), lambda p: ui_speed(p, False)))
    jobs.append((os.path.join(ui, "ui_speed2.png"), lambda p: ui_speed(p, True)))
    jobs.append((os.path.join(ui, "ui_sound_on.png"), lambda p: ui_sound(p, True)))
    jobs.append((os.path.join(ui, "ui_sound_off.png"), lambda p: ui_sound(p, False)))
    # 九宫格面板 / 进度条
    jobs.append((os.path.join(ui, "ui_panel.png"), lambda p: ui_panel(p)))
    jobs.append((os.path.join(ui, "ui_panel_dark.png"), lambda p: ui_panel(
        p, base=(255, 255, 255, 248), edge=(255, 255, 255, 255))))
    jobs.append((os.path.join(ui, "ui_bar_bg.png"), lambda p: ui_bar(p, False)))
    jobs.append((os.path.join(ui, "ui_bar_fill.png"), lambda p: ui_bar(p, True)))
    # 特效
    jobs.append((os.path.join(ui, "fx_bolt.png"), fx_bolt))
    jobs.append((os.path.join(ui, "fx_impact.png"), fx_impact))
    jobs.append((os.path.join(ui, "fx_ring.png"), fx_ring))
    jobs.append((os.path.join(ui, "fx_frost.png"), fx_frost))
    jobs.append((os.path.join(ui, "fx_slash.png"), fx_slash))
    jobs.append((os.path.join(ui, "fx_shield.png"), fx_shield))
    # 背景
    jobs.append((os.path.join(terrain, "bg_menu.png"), bg_menu))
    jobs.append((os.path.join(terrain, "bg_battle.png"), bg_battle))

    for path, fn in jobs:
        fn(path)
        size = os.path.getsize(path)
        print("  %-46s %6d B" % (os.path.relpath(path, root), size))

    print("")
    print("共生成 %d 个精灵图。" % len(jobs))


if __name__ == "__main__":
    main()
