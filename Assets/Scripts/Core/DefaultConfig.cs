using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    /// <summary>
    /// 内置默认配置表。
    ///
    /// 它是游戏的「出厂数值」：当 Resources/Config/game_config.json 不存在、
    /// 或者解析失败时，游戏会退回使用这里的数值，保证永远不会因为配置问题而无法启动。
    /// 想要调数值又不想重新编译，改 JSON 即可。
    /// </summary>
    public static class DefaultConfig
    {
        public const int ConfigVersion = 1;

        // ------------------------------------------------------------ 棋盘布局

        /// <summary>棋盘列数。</summary>
        public const int Columns = 6;
        /// <summary>棋盘行数。</summary>
        public const int Rows = 9;

        public static List<GridPos> BuildPathA()
        {
            List<GridPos> p = new List<GridPos>();
            p.Add(new GridPos(0, 0));
            p.Add(new GridPos(0, 3));
            p.Add(new GridPos(2, 3));
            p.Add(new GridPos(2, 7));
            return p;
        }

        public static List<GridPos> BuildPathB()
        {
            List<GridPos> p = new List<GridPos>();
            p.Add(new GridPos(5, 0));
            p.Add(new GridPos(5, 3));
            p.Add(new GridPos(3, 3));
            p.Add(new GridPos(3, 7));
            return p;
        }

        public static List<GridPos> BuildCoreCells()
        {
            List<GridPos> p = new List<GridPos>();
            p.Add(new GridPos(2, 8));
            p.Add(new GridPos(3, 8));
            return p;
        }

        // ------------------------------------------------------------ 主入口

        public static GameDatabase Build()
        {
            GameDatabase db = new GameDatabase();
            db.version = ConfigVersion;
            db.gameName = "仙侠·护山大阵";
            db.columns = Columns;
            db.rows = Rows;
            db.pathA = BuildPathA();
            db.pathB = BuildPathB();
            db.coreCells = BuildCoreCells();

            db.towers = BuildTowers();
            db.spirits = BuildSpirits();
            db.traps = BuildTraps();
            db.skills = BuildSkills();
            db.enemies = BuildEnemies();
            db.waves = BuildWaves();
            db.difficulties = BuildDifficulties();
            return db;
        }

        // ------------------------------------------------------------ 阵法

        private static List<TowerConfig> BuildTowers()
        {
            List<TowerConfig> list = new List<TowerConfig>();

            TowerConfig gather = new TowerConfig();
            gather.kind = TowerKind.SpiritGather;
            gather.id = "spirit_gather";
            gather.displayName = "聚灵阵";
            gather.description = "牵引山川灵脉，持续产出灵气。\n是全部战术的经济基础，建议尽早铺开。";
            gather.sprite = "tower_spirit_gather";
            gather.cost = 50;
            gather.upgradeCostBase = 40;
            gather.maxLevel = 5;
            gather.hp = 120f;
            gather.damage = 0f;
            gather.attackInterval = 0f;
            gather.range = 0f;
            gather.spiritPerSecond = 2.0f;
            gather.projectileSpeed = 0f;
            list.Add(gather);

            TowerConfig attack = new TowerConfig();
            attack.kind = TowerKind.AttackArray;
            attack.id = "attack_array";
            attack.displayName = "攻击法阵";
            attack.description = "凝聚剑气远程攻击妖魔。\n升到 3 级后附带小范围溅射。";
            attack.sprite = "tower_attack";
            attack.cost = 90;
            attack.upgradeCostBase = 70;
            attack.maxLevel = 5;
            attack.hp = 140f;
            attack.damage = 30f;
            attack.attackInterval = 0.70f;
            attack.range = 2.6f;
            attack.projectileSpeed = 9f;
            attack.splashRadius = 0.9f;
            list.Add(attack);

            TowerConfig bind = new TowerConfig();
            bind.kind = TowerKind.BindArray;
            bind.id = "bind_array";
            bind.displayName = "困阵";
            bind.description = "以符链缠住阵中妖魔，大幅降低其移动速度。\n是拖延时间、给输出阵争取空间的核心。";
            bind.sprite = "tower_bind";
            bind.cost = 70;
            bind.upgradeCostBase = 60;
            bind.maxLevel = 5;
            bind.hp = 110f;
            bind.range = 2.2f;
            bind.slowFactor = 0.50f;
            list.Add(bind);

            TowerConfig illusion = new TowerConfig();
            illusion.kind = TowerKind.IllusionArray;
            illusion.id = "illusion_array";
            illusion.displayName = "幻阵";
            illusion.description = "幻化重重虚影，使范围内的阵法与仙灵有概率闪避妖魔的攻击。";
            illusion.sprite = "tower_illusion";
            illusion.cost = 85;
            illusion.upgradeCostBase = 70;
            illusion.maxLevel = 5;
            illusion.hp = 100f;
            illusion.range = 2.2f;
            illusion.dodgeChance = 0.25f;
            list.Add(illusion);

            TowerConfig shield = new TowerConfig();
            shield.kind = TowerKind.ShieldArray;
            shield.id = "shield_array";
            shield.displayName = "护盾阵";
            shield.description = "张开灵光护罩，为范围内的阵法提供可再生护盾，优先替它们承受伤害。";
            shield.sprite = "tower_shield";
            shield.cost = 110;
            shield.upgradeCostBase = 90;
            shield.maxLevel = 5;
            shield.hp = 160f;
            shield.range = 2.2f;
            shield.shieldPool = 200f;
            shield.shieldRegen = 18f;
            list.Add(shield);

            return list;
        }

        // ------------------------------------------------------------ 仙灵

        private static List<SpiritConfig> BuildSpirits()
        {
            List<SpiritConfig> list = new List<SpiritConfig>();

            SpiritConfig sword = new SpiritConfig();
            sword.kind = SpiritKind.SwordSpirit;
            sword.id = "sword_spirit";
            sword.displayName = "剑灵";
            sword.description = "以身化剑的近战仙灵，单体伤害极高，血量厚。\n召唤在道路上阻截妖魔，会替你分担攻击。";
            sword.sprite = "spirit_sword";
            sword.cost = 60;
            sword.hp = 240f;
            sword.damage = 34f;
            sword.attackInterval = 0.9f;
            sword.range = 1.2f;
            sword.duration = 30f;
            list.Add(sword);

            SpiritConfig talisman = new SpiritConfig();
            talisman.kind = SpiritKind.TalismanSpirit;
            talisman.id = "talisman_spirit";
            talisman.displayName = "符灵";
            talisman.description = "御符遥击的远程仙灵，伤害带小范围溅射。\n脆，但输出位置安全。";
            talisman.sprite = "spirit_talisman";
            talisman.cost = 80;
            talisman.hp = 140f;
            talisman.damage = 26f;
            talisman.attackInterval = 1.3f;
            talisman.range = 3.2f;
            talisman.duration = 25f;
            talisman.splash = true;
            talisman.splashRadius = 1.0f;
            talisman.projectileSpeed = 7f;
            list.Add(talisman);

            SpiritConfig pill = new SpiritConfig();
            pill.kind = SpiritKind.PillSpirit;
            pill.id = "pill_spirit";
            pill.displayName = "丹灵";
            pill.description = "不参与厮杀，持续炼制灵气并缓缓修复山门核心。\n是打持久战的关键。";
            pill.sprite = "spirit_pill";
            pill.cost = 70;
            pill.hp = 160f;
            pill.damage = 0f;
            pill.attackInterval = 0f;
            pill.range = 0f;
            pill.duration = 25f;
            pill.spiritPerSecond = 3.0f;
            pill.coreRepairPerSecond = 2.0f;
            list.Add(pill);

            return list;
        }

        // ------------------------------------------------------------ 符箓

        private static List<TrapConfig> BuildTraps()
        {
            List<TrapConfig> list = new List<TrapConfig>();

            TrapConfig thunder = new TrapConfig();
            thunder.kind = TrapKind.ThunderTalisman;
            thunder.id = "thunder_talisman";
            thunder.displayName = "雷符";
            thunder.description = "妖魔踏上即引下天雷，对单一目标造成极高伤害。\n适合用来点杀妖将、大妖。";
            thunder.sprite = "trap_thunder";
            thunder.cost = 40;
            thunder.damage = 160f;
            thunder.radius = 0.9f;
            list.Add(thunder);

            TrapConfig ice = new TrapConfig();
            ice.kind = TrapKind.IceTalisman;
            ice.id = "ice_talisman";
            ice.displayName = "冰符";
            ice.description = "触发后冻结一片区域，范围内的妖魔大幅减速。\n伤害不高，但控场价值极高。";
            ice.sprite = "trap_ice";
            ice.cost = 35;
            ice.damage = 40f;
            ice.radius = 1.4f;
            ice.slowFactor = 0.35f;
            ice.slowDuration = 5f;
            list.Add(ice);

            TrapConfig blast = new TrapConfig();
            blast.kind = TrapKind.BlastTalisman;
            blast.id = "blast_talisman";
            blast.displayName = "爆符";
            blast.description = "引爆灵气，对大范围内的全部妖魔造成爆发伤害。\n对付密集的小妖潮最划算。";
            blast.sprite = "trap_blast";
            blast.cost = 55;
            blast.damage = 240f;
            blast.radius = 1.6f;
            list.Add(blast);

            return list;
        }

        // ------------------------------------------------------------ 终极技能

        private static List<SkillConfig> BuildSkills()
        {
            List<SkillConfig> list = new List<SkillConfig>();

            SkillConfig thunder = new SkillConfig();
            thunder.kind = SkillKind.HeavenlyThunder;
            thunder.id = "heavenly_thunder";
            thunder.displayName = "天雷咒";
            thunder.description = "召九霄天雷加身，对全场妖魔造成巨额伤害。\n耗尽家底的一击，留到最危急的时刻。";
            thunder.sprite = "skill_thunder";
            thunder.cost = 180;
            thunder.cooldown = 45f;
            thunder.damage = 380f;
            list.Add(thunder);

            SkillConfig frost = new SkillConfig();
            frost.kind = SkillKind.FrostSeal;
            frost.id = "frost_seal";
            frost.displayName = "冰封";
            frost.description = "以玄冰之力冻结全场妖魔，使其在一段时间内完全无法行动。\n冻结期间它们仍然会被打。";
            frost.sprite = "skill_frost";
            frost.cost = 150;
            frost.cooldown = 50f;
            frost.duration = 5f;
            list.Add(frost);

            SkillConfig rain = new SkillConfig();
            rain.kind = SkillKind.SpiritRain;
            rain.id = "spirit_rain";
            rain.displayName = "灵雨";
            rain.description = "降下甘霖，修复山门核心的耐久，并立刻回复一笔灵气。";
            rain.sprite = "skill_rain";
            rain.cost = 120;
            rain.cooldown = 40f;
            rain.coreRepairPercent = 0.25f;
            rain.damage = 120f;
            list.Add(rain);

            return list;
        }

        // ------------------------------------------------------------ 妖魔

        private static List<EnemyConfig> BuildEnemies()
        {
            List<EnemyConfig> list = new List<EnemyConfig>();

            EnemyConfig minion = new EnemyConfig();
            minion.kind = EnemyKind.Minion;
            minion.id = "minion";
            minion.displayName = "小妖";
            minion.sprite = "enemy_minion";
            minion.hp = 90f;
            minion.speed = 1.05f;
            minion.armor = 0f;
            minion.reward = 8;
            minion.coreDamage = 6f;
            minion.attackDamage = 14f;
            minion.attackInterval = 1.0f;
            minion.attackRange = 0.72f;
            minion.ability = EnemyAbility.None;
            minion.visualScale = 0.62f;
            list.Add(minion);

            EnemyConfig lieutenant = new EnemyConfig();
            lieutenant.kind = EnemyKind.Lieutenant;
            lieutenant.id = "lieutenant";
            lieutenant.displayName = "妖将";
            lieutenant.sprite = "enemy_lieutenant";
            lieutenant.hp = 260f;
            lieutenant.speed = 0.85f;
            lieutenant.armor = 0.15f;
            lieutenant.reward = 20;
            lieutenant.coreDamage = 12f;
            lieutenant.attackDamage = 26f;
            lieutenant.attackInterval = 1.1f;
            lieutenant.attackRange = 0.80f;
            lieutenant.ability = EnemyAbility.Split;
            lieutenant.visualScale = 0.74f;
            list.Add(lieutenant);

            EnemyConfig greater = new EnemyConfig();
            greater.kind = EnemyKind.Greater;
            greater.id = "greater";
            greater.displayName = "大妖";
            greater.sprite = "enemy_greater";
            greater.hp = 520f;
            greater.speed = 0.70f;
            greater.armor = 0.20f;
            greater.reward = 40;
            greater.coreDamage = 20f;
            greater.attackDamage = 40f;
            greater.attackInterval = 1.4f;
            greater.attackRange = 0.85f;
            greater.ability = EnemyAbility.Shield;
            greater.shieldAmount = 240f;
            greater.shieldRegenInterval = 8f;
            greater.visualScale = 0.86f;
            list.Add(greater);

            EnemyConfig boss = new EnemyConfig();
            boss.kind = EnemyKind.Boss;
            boss.id = "boss";
            boss.displayName = "魔君";
            boss.sprite = "enemy_boss";
            boss.hp = 2200f;
            boss.speed = 0.55f;
            boss.armor = 0.30f;
            boss.reward = 150;
            boss.coreDamage = 45f;
            boss.attackDamage = 80f;
            boss.attackInterval = 1.6f;
            boss.attackRange = 1.0f;
            boss.ability = EnemyAbility.Frenzy;
            boss.frenzyThreshold = 0.5f;
            boss.frenzySpeedMultiplier = 1.6f;
            boss.frenzyAttackMultiplier = 1.5f;
            boss.visualScale = 1.15f;
            list.Add(boss);

            return list;
        }

        // ------------------------------------------------------------ 波次表

        private static WaveGroup G(EnemyKind kind, int count, float interval, int pathIndex, float startDelay)
        {
            WaveGroup g = new WaveGroup();
            g.kind = kind;
            g.count = count;
            g.interval = interval;
            g.pathIndex = pathIndex;
            g.startDelay = startDelay;
            return g;
        }

        private static WaveConfig W(int index, float prepTime, float hpMul, float speedMul, int clearBonus, params WaveGroup[] groups)
        {
            WaveConfig w = new WaveConfig();
            w.index = index;
            w.label = index + " / 20 波";
            w.prepTime = prepTime;
            w.hpMultiplier = hpMul;
            w.speedMultiplier = speedMul;
            w.clearBonus = clearBonus;
            for (int i = 0; i < groups.Length; i++)
            {
                w.groups.Add(groups[i]);
            }
            return w;
        }

        /// <summary>
        /// 20 波标准波表。不同难度只是从这张表里截取前 N 波（简单 10 / 普通 15 / 困难 20），
        /// 再叠加难度倍率。第 5、10、15、20 波为 Boss 波，因此三个难度的收官战都是 Boss。
        /// </summary>
        private static List<WaveConfig> BuildWaves()
        {
            List<WaveConfig> list = new List<WaveConfig>();

            list.Add(W(1, 12f, 1.00f, 1.00f, 15,
                G(EnemyKind.Minion, 5, 2.0f, 0, 0f)));

            list.Add(W(2, 7f, 1.10f, 1.00f, 20,
                G(EnemyKind.Minion, 4, 1.5f, 0, 0f),
                G(EnemyKind.Minion, 4, 1.5f, 1, 0f)));

            list.Add(W(3, 7f, 1.25f, 1.00f, 25,
                G(EnemyKind.Lieutenant, 2, 3.0f, 0, 0f)));

            list.Add(W(4, 7f, 1.40f, 1.02f, 30,
                G(EnemyKind.Minion, 10, 1.2f, 0, 0f),
                G(EnemyKind.Lieutenant, 2, 3.0f, 1, 3f)));

            list.Add(W(5, 10f, 1.60f, 1.00f, 70,
                G(EnemyKind.Boss, 1, 1.0f, 0, 0f)));

            list.Add(W(6, 7f, 1.75f, 1.03f, 35,
                G(EnemyKind.Minion, 6, 1.0f, 0, 0f),
                G(EnemyKind.Minion, 6, 1.0f, 1, 0f)));

            list.Add(W(7, 7f, 1.90f, 1.05f, 40,
                G(EnemyKind.Lieutenant, 2, 2.0f, 0, 0f),
                G(EnemyKind.Lieutenant, 2, 2.0f, 1, 0f)));

            list.Add(W(8, 7f, 2.05f, 1.06f, 50,
                G(EnemyKind.Greater, 1, 2.2f, 0, 0f),
                G(EnemyKind.Greater, 2, 2.2f, 1, 1.5f)));

            list.Add(W(9, 8f, 2.25f, 1.08f, 55,
                G(EnemyKind.Minion, 8, 0.9f, 0, 0f),
                G(EnemyKind.Minion, 8, 0.9f, 1, 0f),
                G(EnemyKind.Lieutenant, 2, 2.5f, 0, 4f)));

            list.Add(W(10, 10f, 2.50f, 1.09f, 90,
                G(EnemyKind.Boss, 1, 1.0f, 0, 0f),
                G(EnemyKind.Lieutenant, 2, 2.5f, 1, 2f)));

            list.Add(W(11, 8f, 2.70f, 1.10f, 60,
                G(EnemyKind.Greater, 2, 1.8f, 0, 0f),
                G(EnemyKind.Greater, 3, 1.8f, 1, 1f)));

            list.Add(W(12, 8f, 2.90f, 1.11f, 65,
                G(EnemyKind.Minion, 10, 0.8f, 0, 0f),
                G(EnemyKind.Minion, 10, 0.8f, 1, 0f),
                G(EnemyKind.Greater, 1, 2.5f, 0, 6f)));

            list.Add(W(13, 8f, 3.10f, 1.12f, 70,
                G(EnemyKind.Lieutenant, 4, 1.4f, 0, 0f),
                G(EnemyKind.Lieutenant, 4, 1.4f, 1, 0f)));

            list.Add(W(14, 8f, 3.35f, 1.13f, 80,
                G(EnemyKind.Greater, 3, 1.2f, 0, 0f),
                G(EnemyKind.Minion, 12, 1.2f, 1, 0f)));

            list.Add(W(15, 12f, 3.60f, 1.15f, 140,
                G(EnemyKind.Boss, 1, 1.0f, 0, 0f),
                G(EnemyKind.Boss, 1, 1.0f, 1, 4f)));

            list.Add(W(16, 8f, 3.85f, 1.16f, 90,
                G(EnemyKind.Minion, 12, 0.7f, 0, 0f),
                G(EnemyKind.Minion, 12, 0.7f, 1, 0f),
                G(EnemyKind.Greater, 2, 2.5f, 0, 7f)));

            list.Add(W(17, 9f, 4.10f, 1.17f, 100,
                G(EnemyKind.Lieutenant, 5, 1.1f, 0, 0f),
                G(EnemyKind.Greater, 5, 1.1f, 1, 0f),
                G(EnemyKind.Lieutenant, 5, 1.5f, 1, 6f)));

            list.Add(W(18, 9f, 4.35f, 1.18f, 110,
                G(EnemyKind.Greater, 4, 1.3f, 0, 0f),
                G(EnemyKind.Greater, 4, 1.3f, 1, 0f)));

            list.Add(W(19, 10f, 4.65f, 1.20f, 140,
                G(EnemyKind.Minion, 15, 0.6f, 0, 0f),
                G(EnemyKind.Minion, 15, 0.6f, 1, 0f),
                G(EnemyKind.Lieutenant, 8, 1.2f, 0, 8f),
                G(EnemyKind.Greater, 3, 2.0f, 1, 8f)));

            list.Add(W(20, 14f, 5.00f, 1.22f, 260,
                G(EnemyKind.Boss, 1, 1.0f, 0, 0f),
                G(EnemyKind.Boss, 1, 1.0f, 1, 5f),
                G(EnemyKind.Greater, 2, 2.0f, 0, 3f),
                G(EnemyKind.Greater, 2, 2.0f, 1, 3f),
                G(EnemyKind.Lieutenant, 4, 0.9f, 0, 10f),
                G(EnemyKind.Lieutenant, 4, 0.9f, 1, 10f)));

            return list;
        }

        // ------------------------------------------------------------ 难度

        private static List<DifficultyConfig> BuildDifficulties()
        {
            List<DifficultyConfig> list = new List<DifficultyConfig>();

            DifficultyConfig easy = new DifficultyConfig();
            easy.difficulty = Difficulty.Easy;
            easy.displayName = "简单";
            easy.description = "10 波。妖魔虚弱、灵气充裕，适合熟悉玩法。";
            easy.waveCount = 10;
            easy.enemyHpMultiplier = 0.8f;
            easy.enemySpeedMultiplier = 0.95f;
            easy.spiritGainMultiplier = 1.2f;
            easy.startSpirit = 120f;
            easy.spiritRegenPerSecond = 1.2f;
            easy.coreHp = 700f;
            list.Add(easy);

            DifficultyConfig normal = new DifficultyConfig();
            normal.difficulty = Difficulty.Normal;
            normal.displayName = "普通";
            normal.description = "15 波。标准节奏，数值按设计基准校准。";
            normal.waveCount = 15;
            normal.enemyHpMultiplier = 1.0f;
            normal.enemySpeedMultiplier = 1.0f;
            normal.spiritGainMultiplier = 1.0f;
            normal.startSpirit = 100f;
            normal.spiritRegenPerSecond = 1.2f;
            normal.coreHp = 600f;
            list.Add(normal);

            DifficultyConfig hard = new DifficultyConfig();
            hard.difficulty = Difficulty.Hard;
            hard.displayName = "困难";
            hard.description = "20 波。妖魔凶悍、灵气紧张，Boss 双线齐出。";
            hard.waveCount = 20;
            hard.enemyHpMultiplier = 1.25f;
            hard.enemySpeedMultiplier = 1.08f;
            hard.spiritGainMultiplier = 0.9f;
            hard.startSpirit = 100f;
            hard.spiritRegenPerSecond = 1.2f;
            hard.coreHp = 450f;
            list.Add(hard);

            return list;
        }
    }
}
