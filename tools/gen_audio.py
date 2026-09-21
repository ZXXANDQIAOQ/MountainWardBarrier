#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
《仙侠·护山大阵》音效 / BGM 生成器
=====================================

同样是纯程序化合成，不依赖任何外部音频素材。

用法：
    python tools/gen_audio.py

输出到 Assets/Resources/Audio/{BGM,SFX}/，格式为 22050Hz 单声道 16bit WAV。

音色思路：
    · 拨弦音（古筝感）用 Karplus-Strong 算法合出来
    · 旋律走五声音阶（宫商角徵羽），一听就是"中式"
    · 氛围垫底用极缓慢的正弦叠加 + 反馈延迟做"空谷回声"
    · 打击音全部用噪声 + 低通/高通塑形
"""

import math
import os
import random
import struct
import sys

SR = 22050


# --------------------------------------------------------------------------- 基础工具

def silence(dur):
    return [0.0] * int(dur * SR)


def pad_to(tracks):
    n = max(len(t) for t in tracks)
    out = []
    for t in tracks:
        out.append(t + [0.0] * (n - len(t)))
    return out


def mix(*tracks):
    tracks = pad_to([t for t in tracks if t is not None])
    if not tracks:
        return []
    out = list(tracks[0])
    for t in tracks[1:]:
        for i in range(len(out)):
            out[i] += t[i]
    return out


def gain(sig, g):
    return [v * g for v in sig]


def sine(freq, dur, amp=0.5, phase=0.0, freq_end=None):
    n = int(dur * SR)
    out = [0.0] * n
    ph = phase
    for i in range(n):
        if freq_end is None:
            f = freq
        else:
            t = i / float(n) if n else 0.0
            f = freq + (freq_end - freq) * t
        ph += 2.0 * math.pi * f / SR
        out[i] = math.sin(ph) * amp
    return out


def noise(dur, amp=0.5, seed=0):
    rnd = random.Random(seed)
    return [rnd.uniform(-amp, amp) for _ in range(int(dur * SR))]


def lowpass(sig, cutoff):
    """一阶低通。cutoff 用归一化频率（0~1，1 表示奈奎斯特）。"""
    a = math.exp(-2.0 * math.pi * cutoff)
    out = [0.0] * len(sig)
    y = 0.0
    for i, v in enumerate(sig):
        y = (1.0 - a) * v + a * y
        out[i] = y
    return out


def highpass(sig, cutoff):
    a = math.exp(-2.0 * math.pi * cutoff)
    out = [0.0] * len(sig)
    y = 0.0
    prev = 0.0
    for i, v in enumerate(sig):
        y = (1.0 - a) * v + a * y
        out[i] = v - y
        prev = v
    return out


def env(sig, attack=0.005, power=2.5, hold=0.0):
    """起音 + 指数衰减包络。"""
    n = len(sig)
    a = max(1, int(attack * SR))
    h = int(hold * SR)
    out = [0.0] * n
    for i in range(n):
        if i < a:
            e = i / float(a)
        elif i < a + h:
            e = 1.0
        else:
            t = (i - a - h) / float(max(1, n - a - h))
            e = math.pow(1.0 - t, power)
        out[i] = sig[i] * e
    return out


def delay(sig, time, feedback=0.35, wet=0.4, taps=4):
    """廉价回声，用来做空谷回响。"""
    d = int(time * SR)
    out = list(sig) + [0.0] * (d * taps)
    for k in range(1, taps + 1):
        g = wet * math.pow(feedback, k)
        off = d * k
        for i in range(len(sig)):
            out[i + off] += sig[i] * g
    return out


def pluck(freq, dur, amp=0.5, decay=0.996, damp=0.5, seed=0):
    """Karplus-Strong 拨弦：古筝 / 琵琶的近似音色。"""
    n = max(2, int(SR / freq))
    rnd = random.Random(seed)
    buf = [rnd.uniform(-1.0, 1.0) for _ in range(n)]
    total = int(dur * SR)
    out = [0.0] * total
    idx = 0
    prev = 0.0
    for i in range(total):
        cur = buf[idx]
        out[i] = cur * amp
        nxt = buf[idx + 1 if idx + 1 < n else 0]
        buf[idx] = (cur * (1.0 - damp) + nxt * damp) * decay
        idx += 1
        if idx >= n:
            idx = 0
    return out


def bell(freq, dur, amp=0.4, mod=1.5):
    """钟磬音：正弦 + 轻微非谐泛音。"""
    t = sine(freq, dur, amp * 0.7)
    t2 = sine(freq * 2.01, dur, amp * 0.22)
    t3 = sine(freq * mod, dur, amp * 0.16)
    return env(mix(t, t2, t3), attack=0.002, power=3.2)


def soft_clip(sig, drive=1.4):
    out = [0.0] * len(sig)
    for i, v in enumerate(sig):
        x = v * drive
        out[i] = math.tanh(x * 1.2) * 0.85
    return out


def normalize(sig, peak=0.92):
    m = 0.0
    for v in sig:
        if abs(v) > m:
            m = abs(v)
    if m < 1e-9:
        return sig
    k = peak / m
    return [v * k for v in sig]


def fade(sig, fin=0.01, fout=0.05):
    n = len(sig)
    a = max(1, int(fin * SR))
    b = max(1, int(fout * SR))
    out = list(sig)
    for i in range(min(a, n)):
        out[i] *= i / float(a)
    for i in range(min(b, n)):
        out[n - 1 - i] *= i / float(b)
    return out


def write_wav(path, sig, peak=0.92):
    sig = normalize(soft_clip(sig, 1.15), peak)
    folder = os.path.dirname(path)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    data = b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32000)) for v in sig)
    with open(path, "wb") as f:
        f.write(b"RIFF")
        f.write(struct.pack("<I", 36 + len(data)))
        f.write(b"WAVEfmt ")
        f.write(struct.pack("<IHHIIHH", 16, 1, 1, SR, SR * 2, 2, 16))
        f.write(b"data")
        f.write(struct.pack("<I", len(data)))
        f.write(data)
    return path


# --------------------------------------------------------------------------- 五声音阶

# D 宫五声音阶（D E F# A B），仙侠味最稳的一组
SCALE = [0, 2, 4, 7, 9]
BASE = 293.66  # D4


def semi_freq(steps):
    return BASE * math.pow(2.0, steps / 12.0)


def deg_freq(degree, octave=0):
    """degree 可以任意大，自动跨八度。"""
    oct_shift, idx = divmod(degree, len(SCALE))
    steps = SCALE[idx] + 12 * (oct_shift + octave)
    return semi_freq(steps)


# --------------------------------------------------------------------------- BGM

def bgm_menu(path):
    """主菜单：慢速、留白多、有回声，像在山门前打坐。"""
    beat = 0.62
    # (音节 degree, 起始拍, 时值拍, 力度)
    melody = [
        (5, 0.0, 1.6, 0.42), (4, 1.6, 0.9, 0.30), (2, 2.5, 1.4, 0.36),
        (0, 4.0, 1.8, 0.40), (2, 6.0, 1.0, 0.28), (4, 7.0, 2.2, 0.34),
        (5, 9.5, 1.2, 0.36), (7, 10.7, 1.6, 0.32), (5, 12.5, 2.4, 0.40),
        (4, 15.0, 1.0, 0.28), (2, 16.0, 1.6, 0.34), (0, 18.0, 3.0, 0.42),
    ]
    total = 22.0
    track = silence(total)
    for i, (deg, start, dur, amp) in enumerate(melody):
        f = deg_freq(deg)
        note = pluck(f, dur * beat + 1.4, amp=amp, decay=0.9975, damp=0.42, seed=i * 13 + 3)
        note = env(note, attack=0.004, power=2.0)
        off = int(start * beat * SR)
        for k in range(min(len(note), len(track) - off)):
            track[off + k] += note[k]

    # 低音持续音（箫/埙的感觉）
    drone = []
    for f, a in ((deg_freq(0, -2), 0.16), (deg_freq(4, -3), 0.10)):
        drone.append(env(sine(f, total, a), attack=1.2, power=1.2))
    drone_track = mix(*drone)

    # 每两小节一声清磬
    bell_track = silence(total)
    for k, t0 in enumerate((2.8, 8.6, 14.4, 19.5)):
        b = bell(deg_freq(7, 0), 3.2, amp=0.16)
        off = int(t0 * SR)
        for i in range(min(len(b), len(bell_track) - off)):
            bell_track[off + i] += b[i]

    body = mix(track, drone_track, bell_track)
    body = delay(body, 0.34, feedback=0.34, wet=0.30, taps=4)
    # 尾巴只留一点点淡出：BGM 是循环播放的，淡出太长每圈都会听到一次"掉音"
    return write_wav(path, fade(body, 0.02, 0.22), peak=0.72)


def bgm_battle(path):
    """战斗：更快的拨弦节奏 + 鼓点 + 紧张的旋律线。"""
    beat = 0.34
    total = 26.0
    n_beats = int(total / beat)

    # 1) 拨弦织体：固定五声分解和弦，营造"法术连发"的紧迫感
    arp = [0, 2, 4, 7, 4, 2]
    ostinato = silence(total)
    for b in range(n_beats):
        deg = arp[b % len(arp)] + (5 if (b // len(arp)) % 4 == 2 else 0)
        f = deg_freq(deg)
        note = pluck(f, 0.5, amp=0.20, decay=0.9955, damp=0.5, seed=b * 7 + 1)
        note = env(note, attack=0.002, power=2.6)
        off = int(b * beat * SR)
        for k in range(min(len(note), len(ostinato) - off)):
            ostinato[off + k] += note[k]

    # 2) 鼓：每拍一记低鼓，弱拍加沙锤
    drums = silence(total)
    rnd = random.Random(5)
    for b in range(n_beats):
        off = int(b * beat * SR)
        kick = lowpass(env(mix(sine(96, 0.24, 0.6, freq_end=52),
                              noise(0.24, 0.16, seed=b + 40)), 0.004, 2.4), 0.10)
        for k in range(min(len(kick), len(drums) - off)):
            drums[off + k] += kick[k]
        if b % 2 == 1:
            off2 = int((b + 0.5) * beat * SR)
            hat = highpass(env(noise(0.08, 0.14, seed=b + 90), 0.001, 3.0), 0.35)
            for k in range(min(len(hat), len(drums) - off2)):
                drums[off2 + k] += hat[k]

    # 3) 旋律：每 8 拍换一句
    melody_phrases = [
        [(7, 0, 1.5, 0.34), (5, 1.5, 1.0, 0.26), (4, 2.5, 2.0, 0.32)],
        [(5, 0, 1.0, 0.30), (4, 1.0, 1.0, 0.26), (2, 2.0, 2.5, 0.34)],
        [(9, 0, 1.5, 0.34), (7, 1.5, 1.5, 0.30), (5, 3.0, 1.6, 0.30)],
        [(4, 0, 1.0, 0.28), (7, 1.0, 1.0, 0.30), (9, 2.0, 2.4, 0.36)],
    ]
    lead = silence(total)
    phrase_beats = 4
    for p, phrase in enumerate(melody_phrases):
        start_beat = p * phrase_beats * 2
        if start_beat * beat >= total:
            break
        for (deg, sb, sd, amp) in phrase:
            f = deg_freq(deg)
            note = mix(pluck(f, sd * beat + 1.0, amp=amp, decay=0.997, damp=0.4,
                             seed=p * 31 + int(sb * 10)),
                       sine(f, sd * beat, amp * 0.22))
            note = env(note, attack=0.006, power=2.2)
            off = int((start_beat + sb) * beat * SR)
            if off >= len(lead):
                continue
            for k in range(min(len(note), len(lead) - off)):
                lead[off + k] += note[k]

    body = mix(ostinato, drums, lead, gain(drone_pad(total, (deg_freq(0, -2), deg_freq(7, -3)), 0.09), 1.0))
    body = delay(body, 0.22, feedback=0.26, wet=0.20, taps=3)
    return write_wav(path, fade(body, 0.02, 0.22), peak=0.80)


def drone_pad(total, freqs, amp):
    parts = []
    for f in freqs:
        parts.append(env(sine(f, total, amp), attack=1.8, power=1.1))
    return mix(*parts)


# --------------------------------------------------------------------------- 音效

def sfx_click(path):
    s = mix(sine(1180, 0.07, 0.5, freq_end=1560),
            env(noise(0.03, 0.10, seed=1), 0.001, 4.0))
    return write_wav(path, env(s, 0.001, 3.4), peak=0.6)


def sfx_place(path):
    s = mix(env(sine(220, 0.35, 0.55, freq_end=150), 0.002, 2.6),
            env(noise(0.09, 0.22, seed=2), 0.001, 3.0),
            env(pluck(deg_freq(0), 0.5, 0.30, decay=0.996, damp=0.45, seed=4), 0.002, 2.0))
    return write_wav(path, s, peak=0.78)


def sfx_upgrade(path):
    s = silence(1.0)
    for i, deg in enumerate((0, 2, 4, 7)):
        note = bell(deg_freq(deg), 0.9, amp=0.34)
        off = int(i * 0.055 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    return write_wav(path, delay(s, 0.13, 0.28, 0.22, 3), peak=0.8)


def sfx_sell(path):
    s = silence(0.7)
    for i, deg in enumerate((4, 2)):
        note = pluck(deg_freq(deg), 0.5, amp=0.4, decay=0.996, damp=0.45, seed=i + 8)
        off = int(i * 0.07 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    coin = env(mix(sine(2100, 0.12, 0.25), sine(3100, 0.12, 0.18)), 0.001, 3.2)
    off = int(0.10 * SR)
    for k in range(min(len(coin), len(s) - off)):
        s[off + k] += coin[k]
    return write_wav(path, s, peak=0.72)


def sfx_deny(path):
    s = mix(sine(160, 0.16, 0.5, freq_end=120),
            sine(242, 0.16, 0.25, freq_end=180))
    s = lowpass(env(s, 0.003, 2.4), 0.12)
    return write_wav(path, soft_clip(s, 2.0), peak=0.6)


def sfx_shoot(path):
    body = mix(env(sine(760, 0.16, 0.30, freq_end=1500), 0.002, 3.0),
               env(highpass(noise(0.16, 0.18, seed=3), 0.20), 0.002, 3.0))
    return write_wav(path, body, peak=0.52)


def sfx_hit(path):
    s = mix(env(noise(0.09, 0.35, seed=6), 0.001, 3.0),
            env(sine(520, 0.09, 0.28, freq_end=300), 0.001, 3.0))
    s = highpass(s, 0.06)
    return write_wav(path, s, peak=0.55)


def sfx_enemy_die(path):
    s = mix(env(sine(430, 0.30, 0.42, freq_end=90), 0.002, 2.2),
            env(noise(0.22, 0.20, seed=9), 0.002, 2.6),
            env(bell(deg_freq(0), 0.35, 0.14), 0.002, 2.6))
    return write_wav(path, s, peak=0.7)


def sfx_enemy_spawn(path):
    s = silence(0.55)
    for i, d in enumerate((0.0, 0.16)):
        growl = lowpass(env(mix(sine(120, 0.2, 0.45, freq_end=86),
                                noise(0.2, 0.22, seed=11 + i)), 0.004, 2.0), 0.08)
        off = int(d * SR)
        for k in range(min(len(growl), len(s) - off)):
            s[off + k] += growl[k]
    return write_wav(path, s, peak=0.62)


def sfx_core_hit(path):
    s = mix(env(sine(84, 0.5, 0.75, freq_end=44), 0.003, 2.0),
            lowpass(env(noise(0.3, 0.30, seed=12), 0.002, 2.4), 0.05),
            env(sine(240, 0.18, 0.18, freq_end=120), 0.002, 2.4))
    return write_wav(path, delay(s, 0.17, 0.3, 0.22, 3), peak=0.9)


def sfx_thunder(path):
    crack = env(mix(noise(0.35, 0.75, seed=13),
                    sine(3200, 0.12, 0.4, freq_end=600)), 0.001, 1.6)
    rumble = lowpass(env(noise(1.1, 0.55, seed=14), 0.03, 1.2), 0.035)
    boom = env(sine(70, 0.9, 0.6, freq_end=38), 0.004, 1.4)
    return write_wav(path, mix(crack, rumble, boom), peak=0.95)


def sfx_freeze(path):
    s = silence(1.2)
    for i, f in enumerate((2200, 2900, 3600, 4400)):
        note = env(sine(f, 0.5, 0.14), 0.004, 2.6)
        off = int(i * 0.05 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    ice = highpass(env(noise(0.7, 0.22, seed=15), 0.01, 2.0), 0.30)
    s = mix(s, ice, env(sine(180, 0.6, 0.20, freq_end=260), 0.01, 2.0))
    return write_wav(path, delay(s, 0.19, 0.32, 0.26, 4), peak=0.78)


def sfx_summon(path):
    s = silence(1.1)
    for i, deg in enumerate((0, 4, 7)):
        note = bell(deg_freq(deg), 1.0, amp=0.34)
        off = int(i * 0.06 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    return write_wav(path, delay(s, 0.16, 0.34, 0.28, 4), peak=0.8)


def sfx_trap(path):
    s = mix(env(noise(0.14, 0.42, seed=16), 0.001, 2.6),
            env(sine(880, 0.12, 0.32, freq_end=240), 0.001, 2.6))
    return write_wav(path, s, peak=0.66)


def sfx_wave_start(path):
    gong = bell(110, 1.6, amp=0.45, mod=2.4)
    horn = env(mix(sine(165, 1.2, 0.22), sine(247, 1.2, 0.14)), 0.15, 1.6)
    s = mix(gong, horn, env(noise(0.5, 0.12, seed=17), 0.01, 2.0))
    return write_wav(path, delay(s, 0.26, 0.34, 0.26, 4), peak=0.85)


def sfx_victory(path):
    s = silence(2.4)
    seq = [(0, 0.0, 0.45), (2, 0.16, 0.45), (4, 0.32, 0.55),
           (7, 0.50, 0.9), (9, 0.78, 1.4)]
    for i, (deg, t0, dur) in enumerate(seq):
        note = mix(bell(deg_freq(deg), dur + 0.6, amp=0.34),
                   env(pluck(deg_freq(deg), dur + 0.4, 0.26, decay=0.998, damp=0.4,
                             seed=20 + i), 0.004, 1.8))
        off = int(t0 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    return write_wav(path, delay(s, 0.30, 0.36, 0.30, 4), peak=0.88)


def sfx_defeat(path):
    s = silence(2.4)
    seq = [(4, 0.0, 0.7), (2, 0.35, 0.7), (0, 0.75, 1.4)]
    for i, (deg, t0, dur) in enumerate(seq):
        # 用小三度色彩压暗情绪
        f = deg_freq(deg) * (0.94 if i >= 1 else 1.0)
        note = mix(env(sine(f, dur, 0.30), 0.05, 1.6),
                   env(sine(f * 0.5, dur, 0.22), 0.05, 1.6))
        off = int(t0 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    low = env(sine(55, 2.2, 0.35, freq_end=40), 0.2, 1.2)
    s = mix(s, low, env(lowpass(noise(1.6, 0.12, seed=18), 0.03), 0.3, 1.4))
    return write_wav(path, delay(s, 0.36, 0.32, 0.24, 3), peak=0.82)


def sfx_unlock(path):
    s = silence(1.3)
    for i, deg in enumerate((4, 7, 9, 11 % 5 + 5)):
        note = bell(deg_freq(deg), 1.1, amp=0.32)
        off = int(i * 0.075 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    return write_wav(path, delay(s, 0.14, 0.4, 0.32, 4), peak=0.82)


def sfx_split(path):
    s = silence(0.45)
    for i, deg in enumerate((2, 0)):
        note = env(pluck(deg_freq(deg), 0.3, 0.4, decay=0.994, damp=0.5, seed=25 + i), 0.002, 2.4)
        off = int(i * 0.09 * SR)
        for k in range(min(len(note), len(s) - off)):
            s[off + k] += note[k]
    return write_wav(path, s, peak=0.68)


def sfx_shield(path):
    s = mix(env(sine(300, 0.4, 0.35, freq_end=520), 0.05, 2.0),
            env(highpass(noise(0.26, 0.16, seed=19), 0.25), 0.01, 2.4))
    return write_wav(path, s, peak=0.62)


# --------------------------------------------------------------------------- 主流程

SFX = [
    ("sfx_click", sfx_click),
    ("sfx_place", sfx_place),
    ("sfx_upgrade", sfx_upgrade),
    ("sfx_sell", sfx_sell),
    ("sfx_deny", sfx_deny),
    ("sfx_shoot", sfx_shoot),
    ("sfx_hit", sfx_hit),
    ("sfx_enemy_die", sfx_enemy_die),
    ("sfx_enemy_spawn", sfx_enemy_spawn),
    ("sfx_core_hit", sfx_core_hit),
    ("sfx_thunder", sfx_thunder),
    ("sfx_freeze", sfx_freeze),
    ("sfx_summon", sfx_summon),
    ("sfx_trap", sfx_trap),
    ("sfx_wave_start", sfx_wave_start),
    ("sfx_victory", sfx_victory),
    ("sfx_defeat", sfx_defeat),
    ("sfx_unlock", sfx_unlock),
    ("sfx_split", sfx_split),
    ("sfx_shield", sfx_shield),
]


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    if len(sys.argv) > 1:
        root = sys.argv[1]
    bgm_dir = os.path.join(root, "Assets", "Resources", "Audio", "BGM")
    sfx_dir = os.path.join(root, "Assets", "Resources", "Audio", "SFX")

    count = 0
    for name, fn in (("bgm_menu", bgm_menu), ("bgm_battle", bgm_battle)):
        p = os.path.join(bgm_dir, name + ".wav")
        fn(p)
        print("  %-40s %8d B" % (os.path.relpath(p, root), os.path.getsize(p)))
        count += 1

    for name, fn in SFX:
        p = os.path.join(sfx_dir, name + ".wav")
        fn(p)
        print("  %-40s %8d B" % (os.path.relpath(p, root), os.path.getsize(p)))
        count += 1

    print("")
    print("共生成 %d 个音频文件。" % count)


if __name__ == "__main__":
    main()
