using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    // ==================================================================== 枚举

    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>妖魔种类。</summary>
    public enum EnemyKind
    {
        /// <summary>小妖：数量多、脆、奖励低。</summary>
        Minion = 0,
        /// <summary>妖将：中等强度，死亡后分裂出小妖。</summary>
        Lieutenant = 1,
        /// <summary>大妖：高血量高护甲，自带护盾。</summary>
        Greater = 2,
        /// <summary>魔君：Boss，血量极高，半血后狂暴。</summary>
        Boss = 3
    }

    /// <summary>妖魔特殊能力。</summary>
    public enum EnemyAbility
    {
        None = 0,
        /// <summary>护盾：周期恢复一层护盾值。</summary>
        Shield = 1,
        /// <summary>狂暴：血量低于一半后攻速与移速提升。</summary>
        Frenzy = 2,
        /// <summary>分裂：死亡时分裂出两只小妖。</summary>
        Split = 3
    }

    /// <summary>阵法种类。</summary>
    public enum TowerKind
    {
        /// <summary>聚灵阵：加速灵气产出。</summary>
        SpiritGather = 0,
        /// <summary>攻击法阵：远程单体攻击。</summary>
        AttackArray = 1,
        /// <summary>困阵：范围内妖魔减速。</summary>
        BindArray = 2,
        /// <summary>幻阵：范围内阵法与仙灵获得闪避。</summary>
        IllusionArray = 3,
        /// <summary>护盾阵：为范围内的阵法提供可再生的护盾池。</summary>
        ShieldArray = 4
    }

    /// <summary>仙灵种类。</summary>
    public enum SpiritKind
    {
        /// <summary>剑灵：高单体近战。</summary>
        SwordSpirit = 0,
        /// <summary>符灵：远程范围法术。</summary>
        TalismanSpirit = 1,
        /// <summary>丹灵：不攻击，持续产出灵气并修复山门核心。</summary>
        PillSpirit = 2
    }

    /// <summary>符箓种类（一次性陷阱）。</summary>
    public enum TrapKind
    {
        /// <summary>雷符：高额单体伤害。</summary>
        ThunderTalisman = 0,
        /// <summary>冰符：范围减速。</summary>
        IceTalisman = 1,
        /// <summary>爆符：大范围爆发伤害。</summary>
        BlastTalisman = 2
    }

    /// <summary>终极技能。</summary>
    public enum SkillKind
    {
        /// <summary>天雷咒：全屏伤害。</summary>
        HeavenlyThunder = 0,
        /// <summary>冰封：全场冻结。</summary>
        FrostSeal = 1,
        /// <summary>灵雨：修复山门核心并回复灵气。</summary>
        SpiritRain = 2
    }

    /// <summary>格子类型（供渲染层决定画什么）。</summary>
    public enum CellType
    {
        Buildable = 0,
        Path = 1,
        Core = 2
    }

    // ============================================================== 配置数据模型

    /// <summary>阵法配置。所有数值都可以在 Resources/Config/game_config.json 中调整。</summary>
    [Serializable]
    public class TowerConfig
    {
        public TowerKind kind;
        public string id;
        public string displayName;
        public string description;
        public string sprite;

        public int cost;
        public int upgradeCostBase;
        public int maxLevel;

        public float hp;
        public float damage;
        public float attackInterval;
        public float range;

        /// <summary>聚灵阵专用：每秒额外产出的灵气。</summary>
        public float spiritPerSecond;
        /// <summary>困阵专用：范围内妖魔的速度乘数（0.55 表示降到 55%）。</summary>
        public float slowFactor;
        /// <summary>幻阵专用：范围内阵法 / 仙灵的闪避概率 [0,1]。</summary>
        public float dodgeChance;
        /// <summary>护盾阵专用：护盾池上限。</summary>
        public float shieldPool;
        /// <summary>护盾阵专用：护盾池每秒回复量。</summary>
        public float shieldRegen;

        /// <summary>弹道飞行速度（格/秒）；为 0 表示瞬发法术。</summary>
        public float projectileSpeed;
        public float splashRadius;

        public float DamageAtLevel(int level)
        {
            return damage * (1f + 0.45f * (level - 1));
        }

        public float RangeAtLevel(int level)
        {
            return range + 0.15f * (level - 1);
        }

        public float HpAtLevel(int level)
        {
            return hp * Pow(1.35f, level - 1);
        }

        public float SpiritPerSecondAtLevel(int level)
        {
            return spiritPerSecond * (1f + 0.5f * (level - 1));
        }

        public float ShieldPoolAtLevel(int level)
        {
            return shieldPool * (1f + 0.6f * (level - 1));
        }

        public int UpgradeCostAtLevel(int currentLevel)
        {
            return (int)Math.Round(upgradeCostBase * Pow(1.6f, currentLevel - 1));
        }

        private static float Pow(float b, int e)
        {
            float r = 1f;
            for (int i = 0; i < e; i++)
            {
                r *= b;
            }
            return r;
        }
    }

    /// <summary>仙灵配置。</summary>
    [Serializable]
    public class SpiritConfig
    {
        public SpiritKind kind;
        public string id;
        public string displayName;
        public string description;
        public string sprite;

        public int cost;
        public float hp;
        public float damage;
        public float attackInterval;
        public float range;
        /// <summary>存在时长（秒），到时自动消失。</summary>
        public float duration;
        /// <summary>丹灵专用：每秒额外灵气。</summary>
        public float spiritPerSecond;
        /// <summary>丹灵专用：每秒修复山门核心的耐久。</summary>
        public float coreRepairPerSecond;
        /// <summary>攻击是否为范围伤害。</summary>
        public bool splash;
        public float splashRadius;
        public float projectileSpeed;
    }

    /// <summary>符箓（一次性陷阱）配置。</summary>
    [Serializable]
    public class TrapConfig
    {
        public TrapKind kind;
        public string id;
        public string displayName;
        public string description;
        public string sprite;

        public int cost;
        public float damage;
        public float radius;
        public float slowFactor;
        public float slowDuration;
    }

    /// <summary>终极技能配置。</summary>
    [Serializable]
    public class SkillConfig
    {
        public SkillKind kind;
        public string id;
        public string displayName;
        public string description;
        public string sprite;

        public int cost;
        public float cooldown;
        public float damage;
        public float duration;
        /// <summary>灵雨专用：修复核心耐久的百分比 [0,1]。</summary>
        public float coreRepairPercent;
    }

    /// <summary>妖魔配置。</summary>
    [Serializable]
    public class EnemyConfig
    {
        public EnemyKind kind;
        public string id;
        public string displayName;
        public string sprite;

        public float hp;
        /// <summary>移动速度，单位：格/秒。</summary>
        public float speed;
        /// <summary>伤害减免百分比 [0,1)。</summary>
        public float armor;
        /// <summary>击杀获得的灵气。</summary>
        public int reward;
        /// <summary>抵达山门核心时造成的耐久伤害。</summary>
        public float coreDamage;

        public float attackDamage;
        public float attackInterval;
        /// <summary>攻击半径（格）。</summary>
        public float attackRange;

        public EnemyAbility ability;
        /// <summary>护盾能力：护盾上限。</summary>
        public float shieldAmount;
        /// <summary>护盾能力：护盾完全破碎后重新生成的间隔（秒）。</summary>
        public float shieldRegenInterval;
        /// <summary>狂暴能力：血量低于该比例后进入狂暴。</summary>
        public float frenzyThreshold;
        public float frenzySpeedMultiplier;
        public float frenzyAttackMultiplier;

        /// <summary>渲染半径（格），只影响表现层。</summary>
        public float visualScale;
    }

    /// <summary>一波之内的一组妖魔。</summary>
    [Serializable]
    public class WaveGroup
    {
        public EnemyKind kind;
        public int count;
        /// <summary>组内每只妖魔之间的出场间隔（秒）。</summary>
        public float interval;
        /// <summary>路径索引：0 = 甲路（左），1 = 乙路（右）。</summary>
        public int pathIndex;
        /// <summary>相对本波开始的出场延迟（秒）。</summary>
        public float startDelay;
    }

    /// <summary>单波配置。</summary>
    [Serializable]
    public class WaveConfig
    {
        /// <summary>1 起算的波次编号。</summary>
        public int index;
        public string label;
        /// <summary>本波开始前的准备时间（秒）。</summary>
        public float prepTime;
        public float hpMultiplier;
        public float speedMultiplier;
        /// <summary>清空本波后奖励的灵气。</summary>
        public int clearBonus;
        public List<WaveGroup> groups = new List<WaveGroup>();

        public bool IsBossWave
        {
            get
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].kind == EnemyKind.Boss)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>本波妖魔总数，用于波次预告。</summary>
        public int TotalEnemies
        {
            get
            {
                int total = 0;
                for (int i = 0; i < groups.Count; i++)
                {
                    total += groups[i].count;
                }
                return total;
            }
        }

        /// <summary>本波出场总时长（秒），用于估算波次间隔。</summary>
        public float SpawnDuration
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < groups.Count; i++)
                {
                    float end = groups[i].startDelay + groups[i].interval * groups[i].count;
                    if (end > max)
                    {
                        max = end;
                    }
                }
                return max;
            }
        }
    }

    /// <summary>难度配置。</summary>
    [Serializable]
    public class DifficultyConfig
    {
        public Difficulty difficulty;
        public string displayName;
        public string description;
        /// <summary>本难度打多少波（从总波表里截取）。</summary>
        public int waveCount;
        public float enemyHpMultiplier;
        public float enemySpeedMultiplier;
        public float spiritGainMultiplier;
        public float startSpirit;
        /// <summary>灵气每秒自动增长的基础值。</summary>
        public float spiritRegenPerSecond;
        public float coreHp;
    }

    // ============================================================== 全局配置库

    /// <summary>
    /// 全部可调数值的集合。运行时由 ConfigLoader 从 JSON 载入，
    /// 若 JSON 缺失或解析失败则回退到 DefaultConfig 内置的一套默认值。
    /// </summary>
    public class GameDatabase
    {
        public int version = 1;
        public string gameName = "仙侠·护山大阵";

        /// <summary>棋盘列数。</summary>
        public int columns = 6;
        /// <summary>棋盘行数。</summary>
        public int rows = 9;

        /// <summary>甲路（左）的路径拐点，按从入口到山门的顺序排列。</summary>
        public List<GridPos> pathA = new List<GridPos>();
        /// <summary>乙路（右）的路径拐点。</summary>
        public List<GridPos> pathB = new List<GridPos>();
        /// <summary>山门核心占据的格子。</summary>
        public List<GridPos> coreCells = new List<GridPos>();

        public List<TowerConfig> towers = new List<TowerConfig>();
        public List<SpiritConfig> spirits = new List<SpiritConfig>();
        public List<TrapConfig> traps = new List<TrapConfig>();
        public List<SkillConfig> skills = new List<SkillConfig>();
        public List<EnemyConfig> enemies = new List<EnemyConfig>();
        public List<WaveConfig> waves = new List<WaveConfig>();
        public List<DifficultyConfig> difficulties = new List<DifficultyConfig>();

        public TowerConfig GetTower(TowerKind kind)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                if (towers[i].kind == kind)
                {
                    return towers[i];
                }
            }
            return null;
        }

        public SpiritConfig GetSpirit(SpiritKind kind)
        {
            for (int i = 0; i < spirits.Count; i++)
            {
                if (spirits[i].kind == kind)
                {
                    return spirits[i];
                }
            }
            return null;
        }

        public TrapConfig GetTrap(TrapKind kind)
        {
            for (int i = 0; i < traps.Count; i++)
            {
                if (traps[i].kind == kind)
                {
                    return traps[i];
                }
            }
            return null;
        }

        public SkillConfig GetSkill(SkillKind kind)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].kind == kind)
                {
                    return skills[i];
                }
            }
            return null;
        }

        public EnemyConfig GetEnemy(EnemyKind kind)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].kind == kind)
                {
                    return enemies[i];
                }
            }
            return null;
        }

        public DifficultyConfig GetDifficulty(Difficulty difficulty)
        {
            for (int i = 0; i < difficulties.Count; i++)
            {
                if (difficulties[i].difficulty == difficulty)
                {
                    return difficulties[i];
                }
            }
            return difficulties.Count > 0 ? difficulties[0] : null;
        }

        /// <summary>本难度实际会出场的波表。</summary>
        public List<WaveConfig> GetWavesFor(Difficulty difficulty)
        {
            DifficultyConfig d = GetDifficulty(difficulty);
            int limit = d != null ? d.waveCount : waves.Count;
            List<WaveConfig> result = new List<WaveConfig>();
            for (int i = 0; i < waves.Count && result.Count < limit; i++)
            {
                result.Add(waves[i]);
            }
            return result;
        }
    }
}
