using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    /// <summary>妖魔状态。</summary>
    public enum EnemyState
    {
        Moving = 0,
        Attacking = 1
    }

    /// <summary>
    /// 妖魔实例。逻辑层持有全部状态；表现层只读取位置 / 血量比例来摆图。
    /// </summary>
    public class EnemyUnit
    {
        public int id;
        public EnemyKind kind;
        public EnemyConfig config;

        /// <summary>0 = 甲路（左），1 = 乙路（右）。</summary>
        public int pathIndex;
        public Float2 position;
        public float travelled;
        /// <summary>沿路径的进度 [0,1]，用来判断「谁更靠近山门」。</summary>
        public float progress;

        public float hp;
        public float maxHp;
        public float armor;
        public float baseSpeed;
        public float difficultySpeedMultiplier = 1f;
        /// <summary>出场时使用过的血量倍率（难度 × 波次），分裂出的小妖按它继承强度。</summary>
        public float hpScaleFactor = 1f;
        /// <summary>出场时使用过的速度倍率。</summary>
        public float speedScaleFactor = 1f;

        // 负面状态
        public float slowFactor = 1f;
        public float slowTimer;
        public float freezeTimer;

        // 护盾能力
        public float shieldHp;
        public float shieldMax;
        public float shieldTimer;

        // 狂暴能力
        public bool frenzyActive;
        public float frenzySpeedMultiplier = 1f;
        public float frenzyAttackMultiplier = 1f;

        // 分裂能力
        public int splitGeneration;

        public bool alive = true;
        public EnemyState state = EnemyState.Moving;
        public float attackTimer;
        /// <summary>重新寻找攻击目标的冷却，避免每帧遍历全场。</summary>
        public float retargetTimer;
        /// <summary>当前攻击目标：阵法 id。</summary>
        public int targetTowerId = -1;
        /// <summary>当前攻击目标：仙灵 id。</summary>
        public int targetSpiritId = -1;

        /// <summary>表现层用：受击闪白剩余时间。</summary>
        public float hitFlash;
        /// <summary>表现层用：渲染缩放。</summary>
        public float visualScale = 1f;

        public bool IsFrozen
        {
            get { return freezeTimer > 0f; }
        }

        public float CurrentSpeed
        {
            get
            {
                if (freezeTimer > 0f)
                {
                    return 0f;
                }
                float speed = baseSpeed * difficultySpeedMultiplier * slowFactor;
                if (frenzyActive)
                {
                    speed *= frenzySpeedMultiplier;
                }
                return speed;
            }
        }

        public float HpRatio
        {
            get
            {
                if (maxHp <= 0f)
                {
                    return 0f;
                }
                return SimMath.Clamp01(hp / maxHp);
            }
        }

        public void ApplySlow(float factor, float duration)
        {
            if (factor <= 0f)
            {
                factor = 0.01f;
            }
            // 取更强的减速；减速时长取更久的那个。
            if (factor < slowFactor)
            {
                slowFactor = factor;
                slowTimer = duration;
            }
            else if (Math.Abs(factor - slowFactor) < 0.001f && duration > slowTimer)
            {
                slowTimer = duration;
            }
        }

        public void ApplyFreeze(float duration)
        {
            if (duration > freezeTimer)
            {
                freezeTimer = duration;
            }
        }

        /// <summary>结算一次伤害，返回实际掉血量（已计入护甲与护盾）。</summary>
        public float TakeDamage(float raw)
        {
            if (!alive || raw <= 0f)
            {
                return 0f;
            }

            float afterArmor = raw * (1f - SimMath.Clamp(armor, 0f, 0.9f));
            if (afterArmor < 1f)
            {
                afterArmor = 1f;
            }

            float actual = afterArmor;

            if (shieldHp > 0f)
            {
                float absorbed = afterArmor;
                if (absorbed > shieldHp)
                {
                    absorbed = shieldHp;
                }
                shieldHp -= absorbed;
                actual = afterArmor - absorbed;
            }

            hp -= actual;
            hitFlash = 0.16f;

            if (hp <= 0f)
            {
                hp = 0f;
                alive = false;
            }
            return actual;
        }

        /// <summary>推进状态计时器。返回本次是否刚好恢复了护盾。</summary>
        public bool UpdateStatus(float dt)
        {
            bool shieldRestored = false;

            if (hitFlash > 0f)
            {
                hitFlash -= dt;
            }

            if (freezeTimer > 0f)
            {
                freezeTimer -= dt;
                if (freezeTimer < 0f)
                {
                    freezeTimer = 0f;
                }
            }

            if (slowTimer > 0f)
            {
                slowTimer -= dt;
                if (slowTimer <= 0f)
                {
                    slowTimer = 0f;
                    slowFactor = 1f;
                }
            }

            if (config != null && config.ability == EnemyAbility.Shield && shieldMax > 0f)
            {
                if (shieldHp <= 0f)
                {
                    shieldTimer += dt;
                    if (shieldTimer >= config.shieldRegenInterval)
                    {
                        shieldTimer = 0f;
                        shieldHp = shieldMax;
                        shieldRestored = true;
                    }
                }
                else
                {
                    shieldTimer = 0f;
                }
            }

            if (config != null && config.ability == EnemyAbility.Frenzy && !frenzyActive)
            {
                if (HpRatio <= config.frenzyThreshold)
                {
                    frenzyActive = true;
                    frenzySpeedMultiplier = config.frenzySpeedMultiplier;
                    frenzyAttackMultiplier = config.frenzyAttackMultiplier;
                }
            }

            return shieldRestored;
        }
    }

    /// <summary>阵法实例。</summary>
    public class TowerUnit
    {
        public int id;
        public TowerKind kind;
        public TowerConfig config;
        public GridPos cell;
        public int level = 1;
        public float hp;
        public float maxHp;
        public float cooldown;
        /// <summary>护盾阵赋予的护盾值。</summary>
        public float shieldHp;
        /// <summary>表现层：受击闪白剩余时间。</summary>
        public float hitFlash;
        /// <summary>表现层：开火高亮剩余时间。</summary>
        public float fireFlash;

        public float Damage
        {
            get { return config != null ? config.DamageAtLevel(level) : 0f; }
        }

        public float Range
        {
            get { return config != null ? config.RangeAtLevel(level) : 0f; }
        }

        public float HpRatio
        {
            get
            {
                if (maxHp <= 0f)
                {
                    return 0f;
                }
                return SimMath.Clamp01(hp / maxHp);
            }
        }

        public Float2 Center
        {
            get { return GridMap.CellCenter(cell); }
        }

        public int NextUpgradeCost
        {
            get
            {
                if (config == null || level >= config.maxLevel)
                {
                    return -1;
                }
                return config.UpgradeCostAtLevel(level);
            }
        }

        /// <summary>铲除返还的灵气（约为总投入的 60%）。</summary>
        public int RefundValue
        {
            get
            {
                if (config == null)
                {
                    return 0;
                }
                int invested = config.cost;
                for (int lv = 1; lv < level; lv++)
                {
                    invested += config.UpgradeCostAtLevel(lv);
                }
                return (int)Math.Round(invested * 0.6f);
            }
        }

        public float TakeDamage(float raw)
        {
            if (raw <= 0f || hp <= 0f)
            {
                return 0f;
            }
            float actual = raw;
            if (shieldHp > 0f)
            {
                float absorbed = raw;
                if (absorbed > shieldHp)
                {
                    absorbed = shieldHp;
                }
                shieldHp -= absorbed;
                actual = raw - absorbed;
            }
            hp -= actual;
            hitFlash = 0.16f;
            if (hp < 0f)
            {
                hp = 0f;
            }
            return actual;
        }
    }

    /// <summary>仙灵实例（召唤单位，有存在时长与血量）。</summary>
    public class SpiritUnit
    {
        public int id;
        public SpiritKind kind;
        public SpiritConfig config;
        public GridPos cell;
        public float hp;
        public float maxHp;
        public float cooldown;
        public float life;
        public float hitFlash;
        public float fireFlash;

        public Float2 Center
        {
            get { return GridMap.CellCenter(cell); }
        }

        public float HpRatio
        {
            get
            {
                if (maxHp <= 0f)
                {
                    return 0f;
                }
                return SimMath.Clamp01(hp / maxHp);
            }
        }

        public float LifeRatio
        {
            get
            {
                if (config == null || config.duration <= 0f)
                {
                    return 1f;
                }
                return SimMath.Clamp01(life / config.duration);
            }
        }

        public float TakeDamage(float raw)
        {
            if (raw <= 0f || hp <= 0f)
            {
                return 0f;
            }
            hp -= raw;
            hitFlash = 0.16f;
            if (hp < 0f)
            {
                hp = 0f;
            }
            return raw;
        }
    }

    /// <summary>符箓（一次性陷阱）。</summary>
    public class TrapUnit
    {
        public int id;
        public TrapKind kind;
        public TrapConfig config;
        public GridPos cell;

        public Float2 Center
        {
            get { return GridMap.CellCenter(cell); }
        }
    }

    /// <summary>飞行中的法术 / 弹道。</summary>
    public class ProjectileUnit
    {
        public int id;
        public Float2 position;
        public int targetEnemyId = -1;
        public Float2 targetPosition;
        public float speed = 9f;
        public float damage;
        public float splashRadius;
        public bool fromTower;
        public TowerKind towerKind;
        public SpiritKind spiritKind;
        /// <summary>表现层：朝向（弧度）。</summary>
        public float angle;
        /// <summary>是否已经结算完毕，等待回收。</summary>
        public bool alive = true;
    }
}
