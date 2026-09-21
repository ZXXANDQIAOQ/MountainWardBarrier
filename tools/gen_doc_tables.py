# -*- coding: utf-8 -*-
"""
把 Resources/Config/game_config.json 里的数值导成 Markdown 表格。

用途：docs/数值与平衡.md 里的表格全部由它生成，所以文档里的数字和游戏里跑的
一定是同一份，不会出现「改了配置忘了改文档」。

用法（在仓库根目录）：
    python tools/gen_doc_tables.py > docs/_tables.md
或先看差多少：
    python tools/gen_doc_tables.py
"""

import io
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(ROOT, "Assets", "Resources", "Config", "game_config.json")


def num(v, digits=0):
    """数字格式化：整数不带小数点，小数按需保留位数。"""
    if v is None:
        return "-"
    if isinstance(v, float):
        if v == int(v):
            return str(int(v))
        return ("%." + str(digits) + "f") % v
    return str(v)


def effect_text(item):
    """把一行配置里"真正起作用的那几个字段"压缩成一句话，方便快速对照。"""
    parts = []
    return "、".join(parts)


def main():
    with io.open(CONFIG, encoding="utf-8") as fp:
        db = json.load(fp)

    out = []
    w = out.append

    # 妖魔用中文名显示，配置里存的是枚举名
    enemy_name = {c["kind"]: c["displayName"] for c in db["enemies"]}

    w("<!-- 本文件由 tools/gen_doc_tables.py 生成，请勿手工编辑 -->")
    w("")

    w("## 阵法")
    w("")
    w("| 阵法 | 造价 | 生命 | 伤害 | 攻击间隔 | 射程 | 产灵气/秒 | 减速 | 闪避 | 护盾 | 满级 | 升级基数 |")
    w("|---|---|---|---|---|---|---|---|---|---|---|---|")
    for c in db["towers"]:
        w("| %s | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
            c["displayName"], c["cost"], num(c["hp"]), num(c["damage"]),
            num(c["attackInterval"], 2), num(c["range"], 1), num(c["spiritPerSecond"], 1),
            num(c["slowFactor"], 2), num(c["dodgeChance"], 2), num(c["shieldPool"]),
            c["maxLevel"], c["upgradeCostBase"]))
    w("")

    w("## 仙灵")
    w("")
    w("| 仙灵 | 造价 | 生命 | 伤害 | 攻击间隔 | 射程 | 存在时间 | 修复核心/秒 | 溅射 |")
    w("|---|---|---|---|---|---|---|---|---|")
    for c in db["spirits"]:
        splash = ("半径 " + num(c["splashRadius"], 1)) if c["splash"] else "否"
        w("| %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
            c["displayName"], c["cost"], num(c["hp"]), num(c["damage"]),
            num(c["attackInterval"], 2), num(c["range"], 1), num(c["duration"]),
            num(c["coreRepairPerSecond"], 1), splash))
    w("")

    w("## 符箓")
    w("")
    w("| 符箓 | 造价 | 伤害 | 范围 | 减速 | 减速时长 |")
    w("|---|---|---|---|---|---|")
    for c in db["traps"]:
        w("| %s | %s | %s | %s | %s | %s |" % (
            c["displayName"], c["cost"], num(c["damage"]), num(c["radius"], 1),
            num(c["slowFactor"], 2), num(c["slowDuration"], 1)))
    w("")

    w("## 终极技")
    w("")
    w("| 终极技 | 造价 | 冷却 | 伤害 | 持续 | 修复核心 |")
    w("|---|---|---|---|---|---|")
    for c in db["skills"]:
        repair = ("%s%%" % num(c["coreRepairPercent"] * 100)) if c["coreRepairPercent"] else "-"
        w("| %s | %s | %ss | %s | %s | %s |" % (
            c["displayName"], c["cost"], num(c["cooldown"]), num(c["damage"]),
            num(c["duration"], 1), repair))
    w("")

    w("## 妖魔")
    w("")
    w("| 妖魔 | 生命 | 护甲 | 速度 | 赏金 | 破门伤害 | 拆阵伤害 | 攻击间隔 | 攻击射程 | 能力 | 护盾 | 狂暴阈值 | 体型 |")
    w("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for c in db["enemies"]:
        w("| %s | %s | %s%% | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
            c["displayName"], num(c["hp"]), num(c["armor"] * 100), num(c["speed"], 2),
            c["reward"], num(c["coreDamage"]), num(c["attackDamage"]),
            num(c["attackInterval"], 2), num(c["attackRange"], 2), c["ability"],
            num(c["shieldAmount"]), num(c["frenzyThreshold"], 2), num(c["visualScale"], 2)))
    w("")

    w("## 难度基准")
    w("")
    w("| 难度 | 波数 | 妖魔生命× | 妖魔速度× | 灵气获取× | 起始灵气 | 灵气回复/秒 | 山门耐久 |")
    w("|---|---|---|---|---|---|---|---|")
    for c in db["difficulties"]:
        w("| %s | %s | %s | %s | %s | %s | %s | %s |" % (
            c["displayName"], c["waveCount"], num(c["enemyHpMultiplier"], 2),
            num(c["enemySpeedMultiplier"], 2), num(c["spiritGainMultiplier"], 2),
            num(c["startSpirit"]), num(c["spiritRegenPerSecond"], 1), num(c["coreHp"])))
    w("")

    w("## 波次表（共 %d 波，下表为未乘难度系数的基准值）" % len(db["waves"]))
    w("")
    w("| 波次 | 备战 | 生命倍率 | 速度倍率 | 清波奖励 | 阵容 | 总数 | BOSS |")
    w("|---|---|---|---|---|---|---|---|")
    for wv in db["waves"]:
        comp = " + ".join("%s×%d" % (enemy_name.get(g["kind"], g["kind"]), g["count"])
                          for g in wv["groups"])
        boss = "★" if wv["index"] % 5 == 0 else ""
        total = sum(g["count"] for g in wv["groups"])
        w("| %d | %ss | %s | %s | %s | %s | %d | %s |" % (
            wv["index"], num(wv["prepTime"]), num(wv["hpMultiplier"], 2),
            num(wv["speedMultiplier"], 2), wv["clearBonus"], comp, total, boss))
    w("")

    w("## 棋盘")
    w("")
    w("| 项目 | 值 |")
    w("|---|---|")
    w("| 棋盘尺寸 | %d 列 × %d 行 |" % (db["columns"], db["rows"]))
    w("| 甲路 | %s |" % " → ".join("(%d,%d)" % (p[0], p[1]) for p in db["pathA"]))
    w("| 乙路 | %s |" % " → ".join("(%d,%d)" % (p[0], p[1]) for p in db["pathB"]))
    w("| 山门 | %s |" % "、".join("(%d,%d)" % (p[0], p[1]) for p in db["coreCells"]))
    w("")

    sys.stdout.write("\n".join(out))


if __name__ == "__main__":
    main()
