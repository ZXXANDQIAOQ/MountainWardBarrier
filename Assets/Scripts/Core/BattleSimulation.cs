using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    /// <summary>对局阶段。</summary>
    public enum BattlePhase
    {
        /// <summary>备战：可以布置阵法 / 升级，倒计时结束后自动开波，也可以手动提前开。</summary>
        Preparing = 0,
        /// <summary>交战：妖魔正在进攻。</summary>
        Fighting = 1,
        Victory = 2,
        Defeat = 3
    }

    /// <summary>一波之内某一组妖魔的出怪队列。</summary>
    public class SpawnTicket
    {
        public EnemyKind kind;
        public int remaining;
        public float interval;
        public int pathIndex;
        public float timer;
        public float hpMultiplier;
        public float speedMultiplier;
    }

    /// <summary>
    /// 战斗模拟器：整个游戏唯一的「真相来源」。
    ///
    /// 它是不依赖 UnityEngine 的纯 C#，因此可以在 Unity 之外被单元测试覆盖
    /// （见 tools/CoreTests）。表现层只负责读它的状态、画出来、播声音。
    /// </summary>
    public class BattleSimulation
    {
        // ------------------------------------------------------------ 依赖注入

        public readonly GameDatabase Db;
        public readonly GridMap Map;
        public readonly Difficulty Difficulty;
        public readonly DifficultyConfig DiffCfg;
        public readonly SimRandom Rng;

        public readonly int Seed;

        // ------------------------------------------------------------ 对局状态

        public BattlePhase Phase = BattlePhase.Preparing;
        public bool Started;
        public bool Finished;
        public bool Victory;
        public bool Paused;
        /// <summary>倍速（1 或 2）。</summary>
        public float GameSpeed = 1f;

        public float Spirit;
        public float CoreHp;
        public float CoreMaxHp;
        public float ElapsedSeconds;
        /// <summary>备战倒计时。</summary>
        public float PrepTimer;

        public readonly List<EnemyUnit> Enemies = new List<EnemyUnit>();
        public readonly List<TowerUnit> Towers = new List<TowerUnit>();
        public readonly List<SpiritUnit> Spirits = new List<SpiritUnit>();
        public readonly List<TrapUnit> Traps = new List<TrapUnit>();
        public readonly List<ProjectileUnit> Projectiles = new List<ProjectileUnit>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        /// <summary>本地埋点收集器（纯单机，写本地文件，见 TelemetryCollector）。</summary>
        public readonly TelemetryCollector Telemetry = new TelemetryCollector();

        // ------------------------------------------------------------ 统计（埋点 / 结算用）

        public int TotalKills;
        public float TotalSpiritEarned;
        public float TotalSpiritSpent;
        public int TowersBuiltCount;
        public int TowersSoldCount;
        public int SpiritsSummonedCount;
        public int TrapsPlacedCount;
        public int LeakedCount;
        public float DamageDealt;
        public int MaxWaveReached;
        /// <summary>本局布置过哪些阵法（按 TowerKind 的位掩码），用于成就判定。</summary>
        public int TowerKindMask;
        public int SkillCastCount;
        public int ThunderCastCount;

        // ------------------------------------------------------------ 内部

        private readonly List<WaveConfig> _waves;
        private readonly List<SpawnTicket> _tickets = new List<SpawnTicket>();
        private readonly Dictionary<int, EnemyUnit> _enemyById = new Dictionary<int, EnemyUnit>();
        private readonly Dictionary<int, TowerUnit> _towerById = new Dictionary<int, TowerUnit>();
        private readonly Dictionary<int, SpiritUnit> _spiritById = new Dictionary<int, SpiritUnit>();
        private readonly Dictionary<int, TowerUnit> _towerAt = new Dictionary<int, TowerUnit>();
        private readonly Dictionary<int, SpiritUnit> _spiritAt = new Dictionary<int, SpiritUnit>();
        private readonly Dictionary<int, TrapUnit> _trapAt = new Dictionary<int, TrapUnit>();

        private readonly float[] _skillCooldown;

        private int _nextEntityId = 1;
        /// <summary>下一波要开的是第几波（0 基）。</summary>
        private int _waveCursor;
        /// <summary>正在打的波（0 基），没在打时为 -1。</summary>
        private int _activeWaveIndex = -1;
        private float _slowFieldTimer;
        private float _coreShakeCooldown;

        // ------------------------------------------------------------ 构造

        public BattleSimulation(GameDatabase db, Difficulty difficulty, int seed)
        {
            Db = db != null ? db : DefaultConfig.Build();
            Map = new GridMap(Db);
            Difficulty = difficulty;
            DiffCfg = Db.GetDifficulty(difficulty);
            if (DiffCfg == null)
            {
                DiffCfg = new DifficultyConfig();
                DiffCfg.waveCount = Db.waves.Count;
                DiffCfg.startSpirit = 100f;
                DiffCfg.coreHp = 500f;
                DiffCfg.spiritRegenPerSecond = 1f;
                DiffCfg.enemyHpMultiplier = 1f;
                DiffCfg.enemySpeedMultiplier = 1f;
                DiffCfg.spiritGainMultiplier = 1f;
            }
            Seed = seed;
            Rng = new SimRandom(seed);
            _waves = Db.GetWavesFor(difficulty);
            _skillCooldown = new float[8];
        }

        // ------------------------------------------------------------ 只读查询（给 UI 用）

        public int TotalWaves
        {
            get { return _waves.Count; }
        }

        /// <summary>当前（或即将）第几波，1 基。</summary>
        public int CurrentWaveNumber
        {
            get
            {
                if (_activeWaveIndex >= 0 && Phase == BattlePhase.Fighting)
                {
                    return _activeWaveIndex + 1;
                }
                return SimMath.ClampInt(_waveCursor + 1, 1, Math.Max(1, TotalWaves));
            }
        }

        public WaveConfig UpcomingWave
        {
            get
            {
                if (_waveCursor < _waves.Count)
                {
                    return _waves[_waveCursor];
                }
                return null;
            }
        }

        public WaveConfig ActiveWave
        {
            get
            {
                if (_activeWaveIndex >= 0 && _activeWaveIndex < _waves.Count)
                {
                    return _waves[_activeWaveIndex];
                }
                return null;
            }
        }

        /// <summary>本波还没解决的妖魔数（场上 + 待出场）。</summary>
        public int WaveEnemiesRemaining
        {
            get
            {
                int n = Enemies.Count;
                for (int i = 0; i < _tickets.Count; i++)
                {
                    n += _tickets[i].remaining;
                }
                return n;
            }
        }

        public float CoreHpRatio
        {
            get
            {
                if (CoreMaxHp <= 0f)
                {
                    return 0f;
                }
                return SimMath.Clamp01(CoreHp / CoreMaxHp);
            }
        }

        /// <summary>当前灵气每秒收入（基础 + 聚灵阵 + 丹灵）。</summary>
        public float SpiritIncomePerSecond
        {
            get
            {
                float income = DiffCfg.spiritRegenPerSecond * DiffCfg.spiritGainMultiplier;
                for (int i = 0; i < Towers.Count; i++)
                {
                    TowerUnit t = Towers[i];
                    if (t.kind == TowerKind.SpiritGather && t.hp > 0f)
                    {
                        income += t.config.SpiritPerSecondAtLevel(t.level) * DiffCfg.spiritGainMultiplier;
                    }
                }
                for (int i = 0; i < Spirits.Count; i++)
                {
                    SpiritUnit s = Spirits[i];
                    if (s.kind == SpiritKind.PillSpirit && s.hp > 0f)
                    {
                        income += s.config.spiritPerSecond * DiffCfg.spiritGainMultiplier;
                    }
                }
                return income;
            }
        }

        public float GetSkillCooldown(SkillKind kind)
        {
            int idx = (int)kind;
            if (idx < 0 || idx >= _skillCooldown.Length)
            {
                return 0f;
            }
            return _skillCooldown[idx];
        }

        public float GetSkillCooldownRatio(SkillKind kind)
        {
            SkillConfig cfg = Db.GetSkill(kind);
            if (cfg == null || cfg.cooldown <= 0f)
            {
                return 0f;
            }
            return SimMath.Clamp01(GetSkillCooldown(kind) / cfg.cooldown);
        }

        public TowerUnit GetTowerAt(GridPos cell)
        {
            TowerUnit t;
            if (_towerAt.TryGetValue(CellKey(cell), out t))
            {
                return t;
            }
            return null;
        }

        public SpiritUnit GetSpiritAt(GridPos cell)
        {
            SpiritUnit s;
            if (_spiritAt.TryGetValue(CellKey(cell), out s))
            {
                return s;
            }
            return null;
        }

        public TrapUnit GetTrapAt(GridPos cell)
        {
            TrapUnit t;
            if (_trapAt.TryGetValue(CellKey(cell), out t))
            {
                return t;
            }
            return null;
        }

        public bool IsCellOccupied(GridPos cell)
        {
            int key = CellKey(cell);
            return _towerAt.ContainsKey(key) || _spiritAt.ContainsKey(key) || _trapAt.ContainsKey(key);
        }

        public void ClearEvents()
        {
            Events.Clear();
        }

        // ------------------------------------------------------------ 开局

        /// <summary>进入对局：进入第一波的备战阶段。</summary>
        public void Begin()
        {
            Started = true;
            Finished = false;
            Victory = false;
            Phase = BattlePhase.Preparing;
            Spirit = DiffCfg.startSpirit;
            CoreMaxHp = DiffCfg.coreHp;
            CoreHp = CoreMaxHp;
            TotalSpiritEarned = DiffCfg.startSpirit;
            _waveCursor = 0;
            _activeWaveIndex = -1;
            PrepTimer = _waves.Count > 0 ? _waves[0].prepTime : 0f;
            Telemetry.LogStart(Difficulty, Seed);
            Emit(SimEvent.Make(SimEventType.PrepPhaseStarted, 0, 0, PrepTimer));
        }

        /// <summary>手动开始当前这一波（跳过剩余备战时间）。</summary>
        public ActionResult LaunchWave()
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            if (!Started)
            {
                return ActionResult.Fail(ActionRejectReason.BattleNotRunning);
            }
            if (Phase != BattlePhase.Preparing)
            {
                return ActionResult.Fail(ActionRejectReason.BattleNotRunning);
            }
            ForceLaunchWave();
            return ActionResult.Success(_waveCursor - 1);
        }

        private void ForceLaunchWave()
        {
            if (_waveCursor >= _waves.Count)
            {
                // 没有可打的波次（配置为空），直接判定守山成功，避免卡死在备战阶段。
                Victory = true;
                Finished = true;
                Phase = BattlePhase.Victory;
                Emit(SimEvent.Make(SimEventType.Victory, 0, 0));
                return;
            }
            WaveConfig w = _waves[_waveCursor];
            _activeWaveIndex = _waveCursor;
            _waveCursor++;
            Phase = BattlePhase.Fighting;
            PrepTimer = 0f;
            if (w.index > MaxWaveReached)
            {
                MaxWaveReached = w.index;
            }

            _tickets.Clear();
            for (int i = 0; i < w.groups.Count; i++)
            {
                WaveGroup g = w.groups[i];
                SpawnTicket ticket = new SpawnTicket();
                ticket.kind = g.kind;
                ticket.remaining = g.count;
                ticket.interval = g.interval > 0.01f ? g.interval : 0.5f;
                ticket.pathIndex = g.pathIndex;
                ticket.timer = g.startDelay;
                ticket.hpMultiplier = w.hpMultiplier;
                ticket.speedMultiplier = w.speedMultiplier;
                _tickets.Add(ticket);
            }

            Emit(SimEvent.Make(SimEventType.WaveStarted, w.index, w.TotalEnemies));
            Telemetry.Log("wave_started", ElapsedSeconds, "wave" + w.index, w.TotalEnemies);
        }

        // ------------------------------------------------------------ 主循环

        public void Tick(float deltaTime)
        {
            if (!Started || Finished || Paused)
            {
                return;
            }
            float dt = deltaTime * GameSpeed;
            if (dt <= 0f)
            {
                return;
            }

            ElapsedSeconds += dt;
            if (_coreShakeCooldown > 0f)
            {
                _coreShakeCooldown -= dt;
            }

            TickSkillCooldowns(dt);
            TickEconomy(dt);
            TickTowers(dt);
            TickSpirits(dt);
            TickSpawners(dt);
            TickEnemies(dt);
            TickProjectiles(dt);
            TickTraps();
            ReapDead();
            TickWaveFlow(dt);
            CheckEndConditions();
        }

        private void TickSkillCooldowns(float dt)
        {
            for (int i = 0; i < _skillCooldown.Length; i++)
            {
                if (_skillCooldown[i] > 0f)
                {
                    _skillCooldown[i] -= dt;
                    if (_skillCooldown[i] < 0f)
                    {
                        _skillCooldown[i] = 0f;
                    }
                }
            }
        }

        private void TickEconomy(float dt)
        {
            float gain = DiffCfg.spiritRegenPerSecond * DiffCfg.spiritGainMultiplier;
            for (int i = 0; i < Towers.Count; i++)
            {
                TowerUnit t = Towers[i];
                if (t.kind == TowerKind.SpiritGather && t.hp > 0f)
                {
                    gain += t.config.SpiritPerSecondAtLevel(t.level) * DiffCfg.spiritGainMultiplier;
                }
            }
            for (int i = 0; i < Spirits.Count; i++)
            {
                SpiritUnit s = Spirits[i];
                if (s.kind == SpiritKind.PillSpirit && s.hp > 0f)
                {
                    gain += s.config.spiritPerSecond * DiffCfg.spiritGainMultiplier;
                    if (s.config.coreRepairPerSecond > 0f && CoreHp < CoreMaxHp)
                    {
                        CoreHp += s.config.coreRepairPerSecond * dt;
                        if (CoreHp > CoreMaxHp)
                        {
                            CoreHp = CoreMaxHp;
                        }
                    }
                }
            }
            if (gain > 0f)
            {
                Spirit += gain * dt;
                TotalSpiritEarned += gain * dt;
            }
        }

        // ------------------------------------------------------------ 布阵 / 召唤 / 符箓

        public ActionResult TryBuildTower(GridPos cell, TowerKind kind)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            if (!Map.InBounds(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.OutOfBounds);
            }
            if (!Map.IsBuildable(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.CellNotBuildable);
            }
            if (IsCellOccupied(cell))
            {
                return ActionResult.Fail(ActionRejectReason.CellOccupied);
            }
            TowerConfig cfg = Db.GetTower(kind);
            if (cfg == null)
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            if (Spirit < cfg.cost)
            {
                return ActionResult.Fail(ActionRejectReason.NotEnoughSpirit);
            }

            TowerUnit t = new TowerUnit();
            t.id = _nextEntityId++;
            t.kind = kind;
            t.config = cfg;
            t.cell = cell;
            t.level = 1;
            t.hp = cfg.HpAtLevel(1);
            t.maxHp = t.hp;
            t.shieldHp = cfg.shieldPool > 0f ? cfg.ShieldPoolAtLevel(1) : 0f;
            t.cooldown = 0f;

            Towers.Add(t);
            _towerById[t.id] = t;
            _towerAt[CellKey(cell)] = t;

            SpendSpirit(cfg.cost);
            TowersBuiltCount++;
            TowerKindMask |= 1 << (int)kind;
            Telemetry.Log("tower_placed", ElapsedSeconds, kind.ToString(), cfg.cost);
            Emit(SimEvent.Make(SimEventType.TowerBuilt, t.id, cell.x, cell.y, (float)kind));
            return ActionResult.Success(t.id);
        }

        public ActionResult TryUpgradeTower(int towerId)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            TowerUnit t;
            if (!_towerById.TryGetValue(towerId, out t) || t.hp <= 0f)
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            if (t.level >= t.config.maxLevel)
            {
                return ActionResult.Fail(ActionRejectReason.MaxLevelReached);
            }
            int cost = t.config.UpgradeCostAtLevel(t.level);
            if (Spirit < cost)
            {
                return ActionResult.Fail(ActionRejectReason.NotEnoughSpirit);
            }

            float hpRatio = t.HpRatio;
            t.level++;
            t.maxHp = t.config.HpAtLevel(t.level);
            t.hp = t.maxHp * Math.Max(hpRatio, 0.6f);
            if (t.config.shieldPool > 0f)
            {
                float newMax = t.config.ShieldPoolAtLevel(t.level);
                t.shieldHp += newMax - t.config.ShieldPoolAtLevel(t.level - 1);
                if (t.shieldHp > newMax)
                {
                    t.shieldHp = newMax;
                }
            }

            SpendSpirit(cost);
            Telemetry.Log("tower_upgraded", ElapsedSeconds, t.kind.ToString() + "_" + (t.level - 1) + "to" + t.level, cost);
            Emit(SimEvent.Make(SimEventType.TowerUpgraded, t.id, t.cell.x, t.cell.y, t.level));
            return ActionResult.Success(t.id);
        }

        public ActionResult TrySellTower(int towerId)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            TowerUnit t;
            if (!_towerById.TryGetValue(towerId, out t))
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            int refund = t.RefundValue;
            Spirit += refund;
            TotalSpiritEarned += refund;
            RemoveTower(t);
            TowersSoldCount++;
            Emit(SimEvent.Make(SimEventType.TowerSold, towerId, t.cell.x, t.cell.y, refund));
            return ActionResult.Success(towerId);
        }

        public ActionResult TryPlaceTrap(GridPos cell, TrapKind kind)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            if (!Map.InBounds(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.OutOfBounds);
            }
            if (!Map.IsPath(cell.x, cell.y) || Map.IsCore(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.CellNotPath);
            }
            if (IsCellOccupied(cell))
            {
                return ActionResult.Fail(ActionRejectReason.CellOccupied);
            }
            TrapConfig cfg = Db.GetTrap(kind);
            if (cfg == null)
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            if (Spirit < cfg.cost)
            {
                return ActionResult.Fail(ActionRejectReason.NotEnoughSpirit);
            }

            TrapUnit t = new TrapUnit();
            t.id = _nextEntityId++;
            t.kind = kind;
            t.config = cfg;
            t.cell = cell;

            Traps.Add(t);
            _trapAt[CellKey(cell)] = t;
            SpendSpirit(cfg.cost);
            TrapsPlacedCount++;
            Emit(SimEvent.Make(SimEventType.TrapTriggered, t.id, cell.x, cell.y, (float)kind + 100f));
            return ActionResult.Success(t.id);
        }

        public ActionResult TrySummonSpirit(GridPos cell, SpiritKind kind)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            if (!Map.InBounds(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.OutOfBounds);
            }
            if (!Map.IsPath(cell.x, cell.y) || Map.IsCore(cell.x, cell.y))
            {
                return ActionResult.Fail(ActionRejectReason.CellNotPath);
            }
            if (IsCellOccupied(cell))
            {
                return ActionResult.Fail(ActionRejectReason.CellOccupied);
            }
            SpiritConfig cfg = Db.GetSpirit(kind);
            if (cfg == null)
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            if (Spirit < cfg.cost)
            {
                return ActionResult.Fail(ActionRejectReason.NotEnoughSpirit);
            }

            SpiritUnit s = new SpiritUnit();
            s.id = _nextEntityId++;
            s.kind = kind;
            s.config = cfg;
            s.cell = cell;
            s.hp = cfg.hp;
            s.maxHp = cfg.hp;
            s.life = cfg.duration;
            s.cooldown = 0.4f;

            Spirits.Add(s);
            _spiritById[s.id] = s;
            _spiritAt[CellKey(cell)] = s;
            SpendSpirit(cfg.cost);
            SpiritsSummonedCount++;
            Emit(SimEvent.Make(SimEventType.SpiritSummoned, s.id, cell.x, cell.y, (float)kind));
            return ActionResult.Success(s.id);
        }

        public ActionResult CastSkill(SkillKind kind)
        {
            if (Finished)
            {
                return ActionResult.Fail(ActionRejectReason.BattleFinished);
            }
            if (!Started)
            {
                return ActionResult.Fail(ActionRejectReason.BattleNotRunning);
            }
            SkillConfig cfg = Db.GetSkill(kind);
            if (cfg == null)
            {
                return ActionResult.Fail(ActionRejectReason.Unknown);
            }
            int idx = (int)kind;
            if (idx >= 0 && idx < _skillCooldown.Length && _skillCooldown[idx] > 0f)
            {
                return ActionResult.Fail(ActionRejectReason.SkillOnCooldown);
            }
            if (Spirit < cfg.cost)
            {
                return ActionResult.Fail(ActionRejectReason.NotEnoughSpirit);
            }

            SpendSpirit(cfg.cost);
            if (idx >= 0 && idx < _skillCooldown.Length)
            {
                _skillCooldown[idx] = cfg.cooldown;
            }

            switch (kind)
            {
                case SkillKind.HeavenlyThunder:
                    {
                        for (int i = 0; i < Enemies.Count; i++)
                        {
                            EnemyUnit e = Enemies[i];
                            if (!e.alive)
                            {
                                continue;
                            }
                            float dealt = e.TakeDamage(cfg.damage);
                            DamageDealt += dealt;
                            Emit(SimEvent.Make(SimEventType.EnemyDamaged, e.id, e.position.x, e.position.y, dealt));
                        }
                        break;
                    }
                case SkillKind.FrostSeal:
                    {
                        for (int i = 0; i < Enemies.Count; i++)
                        {
                            if (Enemies[i].alive)
                            {
                                Enemies[i].ApplyFreeze(cfg.duration);
                            }
                        }
                        break;
                    }
                case SkillKind.SpiritRain:
                    {
                        float heal = CoreMaxHp * cfg.coreRepairPercent;
                        CoreHp += heal;
                        if (CoreHp > CoreMaxHp)
                        {
                            CoreHp = CoreMaxHp;
                        }
                        Spirit += cfg.damage;
                        TotalSpiritEarned += cfg.damage;
                        Emit(SimEvent.Make(SimEventType.SpiritGained, Map.CoreCenter.x, Map.CoreCenter.y, cfg.damage));
                        break;
                    }
            }

            Emit(SimEvent.Make(SimEventType.SkillCast, 0, 0, (float)kind));
            SkillCastCount++;
            if (kind == SkillKind.HeavenlyThunder)
            {
                ThunderCastCount++;
            }
            Telemetry.Log("skill_cast", ElapsedSeconds, kind.ToString(), cfg.cost);
            return ActionResult.Success((int)kind);
        }

        // ------------------------------------------------------------ 阵法逻辑

        private void TickTowers(float dt)
        {
            for (int i = 0; i < Towers.Count; i++)
            {
                TowerUnit t = Towers[i];
                if (t.fireFlash > 0f)
                {
                    t.fireFlash -= dt;
                }
                if (t.hitFlash > 0f)
                {
                    t.hitFlash -= dt;
                }
                if (t.hp <= 0f)
                {
                    continue;
                }

                // 护盾阵的护盾池自然回复
                if (t.kind == TowerKind.ShieldArray && t.config.shieldPool > 0f)
                {
                    float max = t.config.ShieldPoolAtLevel(t.level);
                    if (t.shieldHp < max)
                    {
                        t.shieldHp += t.config.shieldRegen * dt;
                        if (t.shieldHp > max)
                        {
                            t.shieldHp = max;
                        }
                    }
                }

                if (t.kind != TowerKind.AttackArray)
                {
                    continue;
                }

                t.cooldown -= dt;
                if (t.cooldown > 0f)
                {
                    continue;
                }

                EnemyUnit target = FindTarget(t.Center, t.Range);
                if (target == null)
                {
                    t.cooldown = 0f;
                    continue;
                }

                t.cooldown = t.config.attackInterval;
                t.fireFlash = 0.12f;

                if (t.config.projectileSpeed > 0f)
                {
                    ProjectileUnit p = new ProjectileUnit();
                    p.id = _nextEntityId++;
                    p.position = t.Center;
                    p.targetEnemyId = target.id;
                    p.targetPosition = target.position;
                    p.speed = t.config.projectileSpeed;
                    p.damage = t.Damage;
                    p.splashRadius = t.level >= 3 ? t.config.splashRadius : 0f;
                    p.fromTower = true;
                    p.towerKind = t.kind;
                    p.angle = AngleTo(t.Center, target.position);
                    Projectiles.Add(p);
                    Emit(SimEvent.Make(SimEventType.ProjectileFired, t.cell.x, t.cell.y));
                }
                else
                {
                    float dealt = target.TakeDamage(t.Damage);
                    DamageDealt += dealt;
                    Emit(SimEvent.Make(SimEventType.ProjectileHit, target.id, target.position.x, target.position.y, dealt));
                }
            }

            // 困阵：周期性对范围内的妖魔施加减速（按 0.3 秒一次，省性能）
            _slowFieldTimer -= dt;
            if (_slowFieldTimer <= 0f)
            {
                _slowFieldTimer = 0.3f;
                for (int i = 0; i < Towers.Count; i++)
                {
                    TowerUnit t = Towers[i];
                    if (t.kind != TowerKind.BindArray || t.hp <= 0f)
                    {
                        continue;
                    }
                    float r = t.Range;
                    float r2 = r * r;
                    Float2 c = t.Center;
                    for (int j = 0; j < Enemies.Count; j++)
                    {
                        EnemyUnit e = Enemies[j];
                        if (!e.alive)
                        {
                            continue;
                        }
                        if (Float2.SqrDistance(e.position, c) <= r2)
                        {
                            e.ApplySlow(t.config.slowFactor, 0.5f);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------ 仙灵逻辑

        private void TickSpirits(float dt)
        {
            for (int i = Spirits.Count - 1; i >= 0; i--)
            {
                SpiritUnit s = Spirits[i];
                if (s.fireFlash > 0f)
                {
                    s.fireFlash -= dt;
                }
                if (s.hitFlash > 0f)
                {
                    s.hitFlash -= dt;
                }
                if (s.hp <= 0f)
                {
                    continue;
                }

                s.life -= dt;
                if (s.life <= 0f)
                {
                    RemoveSpirit(s);
                    Emit(SimEvent.Make(SimEventType.SpiritExpired, s.id, s.cell.x, s.cell.y));
                    continue;
                }

                if (s.config.damage <= 0f || s.config.attackInterval <= 0f)
                {
                    continue;
                }

                s.cooldown -= dt;
                if (s.cooldown > 0f)
                {
                    continue;
                }

                EnemyUnit target = FindTarget(s.Center, s.config.range);
                if (target == null)
                {
                    s.cooldown = 0f;
                    continue;
                }

                s.cooldown = s.config.attackInterval;
                s.fireFlash = 0.12f;

                if (s.config.projectileSpeed > 0f)
                {
                    ProjectileUnit p = new ProjectileUnit();
                    p.id = _nextEntityId++;
                    p.position = s.Center;
                    p.targetEnemyId = target.id;
                    p.targetPosition = target.position;
                    p.speed = s.config.projectileSpeed;
                    p.damage = s.config.damage;
                    p.splashRadius = s.config.splash ? s.config.splashRadius : 0f;
                    p.fromTower = false;
                    p.spiritKind = s.kind;
                    p.angle = AngleTo(s.Center, target.position);
                    Projectiles.Add(p);
                    Emit(SimEvent.Make(SimEventType.ProjectileFired, s.cell.x, s.cell.y));
                }
                else
                {
                    if (s.config.splash && s.config.splashRadius > 0f)
                    {
                        ApplyAreaDamage(target.position, s.config.splashRadius, s.config.damage);
                    }
                    else
                    {
                        float dealt = target.TakeDamage(s.config.damage);
                        DamageDealt += dealt;
                        Emit(SimEvent.Make(SimEventType.ProjectileHit, target.id, target.position.x, target.position.y, dealt));
                    }
                }
            }
        }

        // ------------------------------------------------------------ 出怪

        private void TickSpawners(float dt)
        {
            if (Phase != BattlePhase.Fighting)
            {
                return;
            }
            for (int i = 0; i < _tickets.Count; i++)
            {
                SpawnTicket ticket = _tickets[i];
                if (ticket.remaining <= 0)
                {
                    continue;
                }
                ticket.timer -= dt;
                int guard = 0;
                while (ticket.timer <= 0f && ticket.remaining > 0 && guard < 32)
                {
                    guard++;
                    SpawnEnemy(ticket.kind, ticket.pathIndex, 0f, ticket.hpMultiplier, ticket.speedMultiplier, 0, 1f);
                    ticket.remaining--;
                    ticket.timer += ticket.interval;
                }
            }
        }

        private EnemyUnit SpawnEnemy(EnemyKind kind, int pathIndex, float travelled, float hpMul, float speedMul, int splitGeneration, float splitHpRatio)
        {
            EnemyConfig cfg = Db.GetEnemy(kind);
            if (cfg == null)
            {
                return null;
            }
            PathData path = Map.GetPath(pathIndex);

            EnemyUnit e = new EnemyUnit();
            e.id = _nextEntityId++;
            e.kind = kind;
            e.config = cfg;
            e.pathIndex = pathIndex;
            e.baseSpeed = cfg.speed;
            e.difficultySpeedMultiplier = DiffCfg.enemySpeedMultiplier;
            e.hpScaleFactor = hpMul;
            e.speedScaleFactor = speedMul;
            e.maxHp = cfg.hp * hpMul * splitHpRatio;
            e.hp = e.maxHp;
            e.armor = cfg.armor;
            e.splitGeneration = splitGeneration;
            e.visualScale = cfg.visualScale;
            e.travelled = SimMath.Clamp(travelled, 0f, Math.Max(0f, path.Length - 0.01f));
            e.position = path.SampleAt(e.travelled);
            e.progress = path.Length > 0f ? SimMath.Clamp01(e.travelled / path.Length) : 0f;

            if (cfg.ability == EnemyAbility.Shield && cfg.shieldAmount > 0f)
            {
                e.shieldMax = cfg.shieldAmount * hpMul;
                e.shieldHp = e.shieldMax;
            }

            Enemies.Add(e);
            _enemyById[e.id] = e;
            Emit(SimEvent.Make(SimEventType.EnemySpawned, e.id, e.position.x, e.position.y, (float)kind));
            return e;
        }

        // ------------------------------------------------------------ 妖魔逻辑

        private void TickEnemies(float dt)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyUnit e = Enemies[i];
                if (!e.alive)
                {
                    continue;
                }

                e.UpdateStatus(dt);

                if (e.retargetTimer > 0f)
                {
                    e.retargetTimer -= dt;
                }

                // 1) 判断当前目标是否还值得打
                TowerUnit tower = e.targetTowerId >= 0 ? LookupTower(e.targetTowerId) : null;
                SpiritUnit spirit = e.targetSpiritId >= 0 ? LookupSpirit(e.targetSpiritId) : null;
                if ((tower == null || tower.hp <= 0f) && (spirit == null || spirit.hp <= 0f))
                {
                    e.targetTowerId = -1;
                    e.targetSpiritId = -1;
                }

                // 2) 目标失效时重新搜索
                if (e.targetTowerId < 0 && e.targetSpiritId < 0 && e.retargetTimer <= 0f)
                {
                    e.retargetTimer = 0.2f;
                    AcquireTarget(e);
                }

                // 3) 有目标就停下攻击，否则继续前进
                if (e.targetTowerId >= 0 || e.targetSpiritId >= 0)
                {
                    e.state = EnemyState.Attacking;
                    e.attackTimer -= dt;
                    if (e.attackTimer <= 0f)
                    {
                        e.attackTimer = e.config.attackInterval;
                        PerformAttack(e);
                    }
                    continue;
                }

                // 4) 前进
                e.state = EnemyState.Moving;
                PathData path = Map.GetPath(e.pathIndex);
                float speed = e.CurrentSpeed;
                if (speed <= 0f)
                {
                    continue;
                }
                e.travelled += speed * dt;
                if (path.Length > 0f && e.travelled >= path.Length)
                {
                    e.travelled = path.Length;
                    e.position = path.SampleAt(e.travelled);
                    e.progress = 1f;
                    OnEnemyReachedCore(e);
                    continue;
                }
                e.position = path.SampleAt(e.travelled);
                e.progress = path.Length > 0f ? SimMath.Clamp01(e.travelled / path.Length) : 0f;
            }
        }

        private void AcquireTarget(EnemyUnit e)
        {
            float range = e.config.attackRange;
            float bestDist = float.MaxValue;
            TowerUnit bestTower = null;
            SpiritUnit bestSpirit = null;

            for (int i = 0; i < Towers.Count; i++)
            {
                TowerUnit t = Towers[i];
                if (t.hp <= 0f)
                {
                    continue;
                }
                float d = Float2.Distance(e.position, t.Center);
                if (d <= range && d < bestDist)
                {
                    bestDist = d;
                    bestTower = t;
                    bestSpirit = null;
                }
            }
            for (int i = 0; i < Spirits.Count; i++)
            {
                SpiritUnit s = Spirits[i];
                if (s.hp <= 0f)
                {
                    continue;
                }
                float d = Float2.Distance(e.position, s.Center);
                if (d <= range && d < bestDist)
                {
                    bestDist = d;
                    bestSpirit = s;
                    bestTower = null;
                }
            }

            e.targetTowerId = bestTower != null ? bestTower.id : -1;
            e.targetSpiritId = bestSpirit != null ? bestSpirit.id : -1;
        }

        private void PerformAttack(EnemyUnit e)
        {
            float damage = e.config.attackDamage;
            if (e.frenzyActive)
            {
                damage *= e.frenzyAttackMultiplier;
            }

            if (e.targetTowerId >= 0)
            {
                TowerUnit t = LookupTower(e.targetTowerId);
                if (t == null || t.hp <= 0f)
                {
                    e.targetTowerId = -1;
                    return;
                }
                // 幻阵闪避
                if (Rng.Chance(DodgeChanceAt(t.Center)))
                {
                    Emit(SimEvent.Make(SimEventType.EnemyAttackDodged, t.cell.x, t.cell.y));
                    return;
                }
                // 护盾阵代挡
                damage = AbsorbByShieldArray(t.Center, damage);
                if (damage <= 0f)
                {
                    Emit(SimEvent.Make(SimEventType.TowerDamaged, t.id, t.cell.x, t.cell.y, 0f));
                    return;
                }
                float dealt = t.TakeDamage(damage);
                Emit(SimEvent.Make(SimEventType.TowerDamaged, t.id, t.cell.x, t.cell.y, dealt));
                if (t.hp <= 0f)
                {
                    Emit(SimEvent.Make(SimEventType.TowerDestroyed, t.id, t.cell.x, t.cell.y));
                }
                return;
            }

            if (e.targetSpiritId >= 0)
            {
                SpiritUnit s = LookupSpirit(e.targetSpiritId);
                if (s == null || s.hp <= 0f)
                {
                    e.targetSpiritId = -1;
                    return;
                }
                if (Rng.Chance(DodgeChanceAt(s.Center)))
                {
                    Emit(SimEvent.Make(SimEventType.EnemyAttackDodged, s.cell.x, s.cell.y));
                    return;
                }
                s.TakeDamage(damage);
                Emit(SimEvent.Make(SimEventType.SpiritDamaged, s.id, s.cell.x, s.cell.y, damage));
            }
        }

        private void OnEnemyReachedCore(EnemyUnit e)
        {
            e.alive = false;
            LeakedCount++;
            float dmg = e.config.coreDamage;
            CoreHp -= dmg;
            if (CoreHp < 0f)
            {
                CoreHp = 0f;
            }
            Emit(SimEvent.Make(SimEventType.EnemyReachedCore, e.id, e.position.x, e.position.y, dmg));
            if (_coreShakeCooldown <= 0f)
            {
                _coreShakeCooldown = 0.25f;
                Emit(SimEvent.Make(SimEventType.CoreDamaged, Map.CoreCenter.x, Map.CoreCenter.y, dmg));
            }
        }

        // ------------------------------------------------------------ 弹道

        private void TickProjectiles(float dt)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileUnit p = Projectiles[i];
                if (!p.alive)
                {
                    Projectiles.RemoveAt(i);
                    continue;
                }

                EnemyUnit target = p.targetEnemyId >= 0 ? LookupEnemy(p.targetEnemyId) : null;
                if (target != null && target.alive)
                {
                    p.targetPosition = target.position;
                }

                Float2 delta = p.targetPosition - p.position;
                float dist = delta.Magnitude;
                float step = p.speed * dt;

                if (dist <= step || dist < 0.0001f)
                {
                    p.position = p.targetPosition;
                    ResolveProjectileHit(p, target);
                    Projectiles.RemoveAt(i);
                    continue;
                }

                Float2 dir = delta / dist;
                p.position = new Float2(p.position.x + dir.x * step, p.position.y + dir.y * step);
                p.angle = AngleTo(Float2.Zero, dir);
            }
        }

        private void ResolveProjectileHit(ProjectileUnit p, EnemyUnit target)
        {
            if (p.splashRadius > 0f)
            {
                ApplyAreaDamage(p.targetPosition, p.splashRadius, p.damage);
                Emit(SimEvent.Make(SimEventType.ProjectileHit, p.targetPosition.x, p.targetPosition.y, p.damage));
                return;
            }
            if (target != null && target.alive)
            {
                float dealt = target.TakeDamage(p.damage);
                DamageDealt += dealt;
                Emit(SimEvent.Make(SimEventType.ProjectileHit, target.id, target.position.x, target.position.y, dealt));
            }
        }

        private void ApplyAreaDamage(Float2 center, float radius, float damage)
        {
            float r2 = radius * radius;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyUnit e = Enemies[i];
                if (!e.alive)
                {
                    continue;
                }
                if (Float2.SqrDistance(e.position, center) <= r2)
                {
                    float dealt = e.TakeDamage(damage);
                    DamageDealt += dealt;
                    Emit(SimEvent.Make(SimEventType.EnemyDamaged, e.id, e.position.x, e.position.y, dealt));
                }
            }
        }

        // ------------------------------------------------------------ 符箓

        private void TickTraps()
        {
            if (Traps.Count == 0 || Enemies.Count == 0)
            {
                return;
            }
            for (int i = Traps.Count - 1; i >= 0; i--)
            {
                TrapUnit trap = Traps[i];
                bool triggered = false;

                if (trap.kind == TrapKind.ThunderTalisman)
                {
                    EnemyUnit victim = null;
                    for (int j = 0; j < Enemies.Count; j++)
                    {
                        EnemyUnit e = Enemies[j];
                        if (!e.alive)
                        {
                            continue;
                        }
                        if (Map.WorldToCell(e.position) == trap.cell)
                        {
                            if (victim == null || e.hp > victim.hp)
                            {
                                victim = e;
                            }
                        }
                    }
                    if (victim != null)
                    {
                        float dealt = victim.TakeDamage(trap.config.damage);
                        DamageDealt += dealt;
                        Emit(SimEvent.Make(SimEventType.EnemyDamaged, victim.id, victim.position.x, victim.position.y, dealt));
                        triggered = true;
                    }
                }
                else
                {
                    Float2 center = trap.Center;
                    float r2 = trap.config.radius * trap.config.radius;
                    bool any = false;
                    for (int j = 0; j < Enemies.Count; j++)
                    {
                        EnemyUnit e = Enemies[j];
                        if (!e.alive)
                        {
                            continue;
                        }
                        if (Float2.SqrDistance(e.position, center) > r2)
                        {
                            continue;
                        }
                        any = true;
                        float dealt = e.TakeDamage(trap.config.damage);
                        DamageDealt += dealt;
                        if (trap.kind == TrapKind.IceTalisman)
                        {
                            e.ApplySlow(trap.config.slowFactor, trap.config.slowDuration);
                        }
                        Emit(SimEvent.Make(SimEventType.EnemyDamaged, e.id, e.position.x, e.position.y, dealt));
                    }
                    if (any)
                    {
                        triggered = true;
                    }
                }

                if (triggered)
                {
                    _trapAt.Remove(CellKey(trap.cell));
                    Traps.RemoveAt(i);
                    Emit(SimEvent.Make(SimEventType.TrapTriggered, trap.id, trap.cell.x, trap.cell.y, (float)trap.kind));
                }
            }
        }

        // ------------------------------------------------------------ 清理与波次流转

        private void ReapDead()
        {
            bool anyDead = false;
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (!Enemies[i].alive)
                {
                    anyDead = true;
                    break;
                }
            }
            if (!anyDead)
            {
                return;
            }

            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                EnemyUnit e = Enemies[i];
                if (e.alive)
                {
                    continue;
                }

                Enemies.RemoveAt(i);
                _enemyById.Remove(e.id);

                // 抵达山门的妖魔不计入击杀
                if (e.travelled >= Map.GetPath(e.pathIndex).Length - 0.001f)
                {
                    continue;
                }

                TotalKills++;
                float reward = e.config.reward * DiffCfg.spiritGainMultiplier;
                Spirit += reward;
                TotalSpiritEarned += reward;
                Emit(SimEvent.Make(SimEventType.EnemyDied, e.id, e.position.x, e.position.y, reward));

                // 分裂
                if (e.config.ability == EnemyAbility.Split && e.splitGeneration == 0)
                {
                    const int childCount = 2;
                    for (int k = 0; k < childCount; k++)
                    {
                        float offset = e.travelled - 0.25f * (k + 1);
                        if (offset < 0f)
                        {
                            offset = 0f;
                        }
                        SpawnEnemy(EnemyKind.Minion, e.pathIndex, offset, e.hpScaleFactor, e.speedScaleFactor, 1, 0.4f);
                    }
                    Emit(SimEvent.Make(SimEventType.EnemySplit, e.id, e.position.x, e.position.y, childCount));
                }
            }

            // 仙灵死亡清理
            for (int i = Spirits.Count - 1; i >= 0; i--)
            {
                SpiritUnit s = Spirits[i];
                if (s.hp > 0f)
                {
                    continue;
                }
                RemoveSpirit(s);
                Emit(SimEvent.Make(SimEventType.SpiritDied, s.id, s.cell.x, s.cell.y, 0f));
            }

            // 阵法被摧毁
            for (int i = Towers.Count - 1; i >= 0; i--)
            {
                TowerUnit t = Towers[i];
                if (t.hp > 0f)
                {
                    continue;
                }
                RemoveTower(t);
                Emit(SimEvent.Make(SimEventType.TowerDestroyed, t.id, t.cell.x, t.cell.y, 0f));
            }
        }

        private void TickWaveFlow(float dt)
        {
            if (Phase == BattlePhase.Preparing)
            {
                PrepTimer -= dt;
                if (PrepTimer <= 0f)
                {
                    PrepTimer = 0f;
                    ForceLaunchWave();
                }
                return;
            }

            if (Phase != BattlePhase.Fighting)
            {
                return;
            }

            bool spawnedAll = true;
            for (int i = 0; i < _tickets.Count; i++)
            {
                if (_tickets[i].remaining > 0)
                {
                    spawnedAll = false;
                    break;
                }
            }
            if (!spawnedAll || Enemies.Count > 0)
            {
                return;
            }

            WaveConfig cleared = ActiveWave;
            int bonus = cleared != null ? cleared.clearBonus : 0;
            if (bonus > 0)
            {
                Spirit += bonus;
                TotalSpiritEarned += bonus;
                Emit(SimEvent.Make(SimEventType.SpiritGained, Map.CoreCenter.x, Map.CoreCenter.y, bonus));
            }
            Emit(SimEvent.Make(SimEventType.WaveCleared, cleared != null ? cleared.index : 0, bonus));
            Telemetry.Log("wave_completed", ElapsedSeconds, "wave" + (cleared != null ? cleared.index : 0), CoreHpRatio);

            _tickets.Clear();
            _activeWaveIndex = -1;

            if (_waveCursor >= _waves.Count)
            {
                Victory = true;
                Finished = true;
                Phase = BattlePhase.Victory;
                Emit(SimEvent.Make(SimEventType.Victory, 0, 0));
                return;
            }

            Phase = BattlePhase.Preparing;
            PrepTimer = _waves[_waveCursor].prepTime;
            Emit(SimEvent.Make(SimEventType.PrepPhaseStarted, 0, 0, PrepTimer));
        }

        private void CheckEndConditions()
        {
            if (Finished)
            {
                return;
            }
            if (CoreHp <= 0f)
            {
                CoreHp = 0f;
                Victory = false;
                Finished = true;
                Phase = BattlePhase.Defeat;
                Emit(SimEvent.Make(SimEventType.Defeat, 0, 0));
            }
        }

        // ------------------------------------------------------------ 工具

        private void SpendSpirit(int amount)
        {
            Spirit -= amount;
            TotalSpiritSpent += amount;
            if (Spirit < 0f)
            {
                Spirit = 0f;
            }
        }

        private void RemoveTower(TowerUnit t)
        {
            for (int i = 0; i < Towers.Count; i++)
            {
                if (Towers[i].id == t.id)
                {
                    Towers.RemoveAt(i);
                    break;
                }
            }
            _towerById.Remove(t.id);
            int key = CellKey(t.cell);
            TowerUnit cur;
            if (_towerAt.TryGetValue(key, out cur) && cur != null && cur.id == t.id)
            {
                _towerAt.Remove(key);
            }
        }

        private void RemoveSpirit(SpiritUnit s)
        {
            for (int i = 0; i < Spirits.Count; i++)
            {
                if (Spirits[i].id == s.id)
                {
                    Spirits.RemoveAt(i);
                    break;
                }
            }
            _spiritById.Remove(s.id);
            int key = CellKey(s.cell);
            SpiritUnit cur;
            if (_spiritAt.TryGetValue(key, out cur) && cur != null && cur.id == s.id)
            {
                _spiritAt.Remove(key);
            }
        }

        private EnemyUnit LookupEnemy(int id)
        {
            EnemyUnit e;
            if (_enemyById.TryGetValue(id, out e))
            {
                return e;
            }
            return null;
        }

        private TowerUnit LookupTower(int id)
        {
            TowerUnit t;
            if (_towerById.TryGetValue(id, out t))
            {
                return t;
            }
            return null;
        }

        private SpiritUnit LookupSpirit(int id)
        {
            SpiritUnit s;
            if (_spiritById.TryGetValue(id, out s))
            {
                return s;
            }
            return null;
        }

        /// <summary>找范围内「离山门最近」的妖魔作为攻击目标。</summary>
        private EnemyUnit FindTarget(Float2 center, float range)
        {
            if (range <= 0f)
            {
                return null;
            }
            float r2 = range * range;
            EnemyUnit best = null;
            float bestProgress = -1f;
            for (int i = 0; i < Enemies.Count; i++)
            {
                EnemyUnit e = Enemies[i];
                if (!e.alive)
                {
                    continue;
                }
                if (Float2.SqrDistance(e.position, center) > r2)
                {
                    continue;
                }
                if (e.progress > bestProgress)
                {
                    bestProgress = e.progress;
                    best = e;
                }
            }
            return best;
        }

        private float DodgeChanceAt(Float2 point)
        {
            float best = 0f;
            for (int i = 0; i < Towers.Count; i++)
            {
                TowerUnit t = Towers[i];
                if (t.kind != TowerKind.IllusionArray || t.hp <= 0f || t.config.dodgeChance <= 0f)
                {
                    continue;
                }
                float r = t.Range;
                if (Float2.SqrDistance(point, t.Center) <= r * r)
                {
                    float chance = t.config.dodgeChance * (1f + 0.15f * (t.level - 1));
                    if (chance > best)
                    {
                        best = chance;
                    }
                }
            }
            if (best > 0.75f)
            {
                best = 0.75f;
            }
            return best;
        }

        /// <summary>护盾阵替范围内的阵法承受伤害，返回扣除护盾后剩下的伤害。</summary>
        private float AbsorbByShieldArray(Float2 targetCenter, float damage)
        {
            for (int i = 0; i < Towers.Count; i++)
            {
                TowerUnit t = Towers[i];
                if (t.kind != TowerKind.ShieldArray || t.hp <= 0f || t.shieldHp <= 0f)
                {
                    continue;
                }
                float r = t.Range;
                if (Float2.SqrDistance(targetCenter, t.Center) > r * r)
                {
                    continue;
                }
                float absorbed = damage;
                if (absorbed > t.shieldHp)
                {
                    absorbed = t.shieldHp;
                }
                t.shieldHp -= absorbed;
                return damage - absorbed;
            }
            return damage;
        }

        private static float AngleTo(Float2 from, Float2 to)
        {
            return (float)Math.Atan2((double)(to.y - from.y), (double)(to.x - from.x));
        }

        private int CellKey(GridPos p)
        {
            return p.y * Map.Columns + p.x;
        }

        private void Emit(SimEvent e)
        {
            Events.Add(e);
        }
    }
}
