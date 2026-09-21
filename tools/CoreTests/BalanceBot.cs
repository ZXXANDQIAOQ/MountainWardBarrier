using System;
using System.Collections.Generic;
using MountainWardBarrier.Core;

namespace MountainWardBarrier.Tools
{
    /// <summary>
    /// 一个「会玩这个游戏」的脚本机器人。
    ///
    /// 用途不是做 AI，而是做平衡性回归测试：让机器人用一套固定的、
    /// 合理但不完美的策略去打三个难度，看它能不能赢。
    /// 如果某个难度连机器人都稳输，说明那一档数值太狠了。
    /// </summary>
    public class BalanceBot
    {
        private readonly BattleSimulation _sim;
        private readonly List<GridPos> _candidates = new List<GridPos>();
        private readonly List<int> _attackTowerIds = new List<int>();
        private float _thinkTimer;

        /// <summary>机器人是否在备战阶段立刻开波（true = 不拖时间）。</summary>
        public bool RushWaves = true;
        /// <summary>在攒够这么多座攻击法阵之前，机器人会老老实实等备战倒计时，攒灵气。</summary>
        public int ComfortableAttackTowers = 8;

        public BalanceBot(BattleSimulation sim)
        {
            _sim = sim;
            BuildCandidateOrder();
        }

        private void BuildCandidateOrder()
        {
            GridMap map = _sim.Map;
            List<KeyValuePair<float, GridPos>> scored = new List<KeyValuePair<float, GridPos>>();
            for (int y = 0; y < map.Rows; y++)
            {
                for (int x = 0; x < map.Columns; x++)
                {
                    if (!map.IsBuildable(x, y))
                    {
                        continue;
                    }
                    GridPos cell = new GridPos(x, y);
                    float d = DistanceToNearestPath(map, cell);
                    scored.Add(new KeyValuePair<float, GridPos>(d, cell));
                }
            }
            scored.Sort(delegate (KeyValuePair<float, GridPos> a, KeyValuePair<float, GridPos> b)
            {
                int c = a.Key.CompareTo(b.Key);
                if (c != 0)
                {
                    return c;
                }
                int cy = a.Value.y.CompareTo(b.Value.y);
                if (cy != 0)
                {
                    return cy;
                }
                return a.Value.x.CompareTo(b.Value.x);
            });
            for (int i = 0; i < scored.Count; i++)
            {
                _candidates.Add(scored[i].Value);
            }
        }

        private static float DistanceToNearestPath(GridMap map, GridPos cell)
        {
            float best = float.MaxValue;
            for (int p = 0; p < 2; p++)
            {
                PathData path = map.GetPath(p);
                for (int i = 0; i < path.Cells.Count; i++)
                {
                    float d = Float2.Distance(GridMap.CellCenter(cell), GridMap.CellCenter(path.Cells[i]));
                    if (d < best)
                    {
                        best = d;
                    }
                }
            }
            return best;
        }

        public void Update(float dt)
        {
            if (_sim.Finished)
            {
                return;
            }

            if (_sim.Phase == BattlePhase.Preparing && RushWaves)
            {
                // 阵线还没成型时先别急着开波，用备战时间攒灵气 —— 这是正常人都会做的事。
                bool ready = CountTowers(TowerKind.AttackArray) >= ComfortableAttackTowers
                    || _sim.PrepTimer <= 0.6f
                    || _sim.CurrentWaveNumber <= 2;
                if (ready)
                {
                    _sim.LaunchWave();
                }
            }

            _thinkTimer -= dt;
            if (_thinkTimer > 0f)
            {
                return;
            }
            _thinkTimer = 0.4f;
            Think();
        }

        private void Think()
        {
            float spirit = _sim.Spirit;

            int gatherCount = CountTowers(TowerKind.SpiritGather);
            int attackCount = CountTowers(TowerKind.AttackArray);
            int bindCount = CountTowers(TowerKind.BindArray);
            int shieldCount = CountTowers(TowerKind.ShieldArray);
            int illusionCount = CountTowers(TowerKind.IllusionArray);

            // 1) 先铺 4 座聚灵阵打底
            if (gatherCount < 4 && TryBuild(TowerKind.SpiritGather))
            {
                return;
            }
            // 2) 沿路要点位堆攻击法阵
            if (attackCount < 6 && TryBuild(TowerKind.AttackArray))
            {
                return;
            }
            // 3) 补一座困阵、一座幻阵、一座护盾阵
            if (bindCount < 1 && TryBuild(TowerKind.BindArray))
            {
                return;
            }
            if (illusionCount < 1 && TryBuild(TowerKind.IllusionArray))
            {
                return;
            }
            if (shieldCount < 1 && TryBuild(TowerKind.ShieldArray))
            {
                return;
            }
            // 4) 继续补攻击法阵
            if (attackCount < 12 && TryBuild(TowerKind.AttackArray))
            {
                return;
            }
            // 5) 钱多了就升级（优先升攻击法阵）
            if (spirit > 260f && TryUpgradeAny())
            {
                return;
            }
            // 6) 富余灵气拿来丢符箓
            if (spirit > 400f && TryPlaceTrap())
            {
                return;
            }
            // 7) 再富余就叫仙灵
            if (spirit > 520f && TrySummonSpirit())
            {
                return;
            }
            // 8) 危急时刻开大
            if (_sim.CoreHpRatio < 0.55f)
            {
                _sim.CastSkill(SkillKind.FrostSeal);
                _sim.CastSkill(SkillKind.HeavenlyThunder);
            }
            if (_sim.CoreHpRatio < 0.8f && spirit > 300f)
            {
                _sim.CastSkill(SkillKind.SpiritRain);
            }
        }

        private int CountTowers(TowerKind kind)
        {
            int n = 0;
            for (int i = 0; i < _sim.Towers.Count; i++)
            {
                if (_sim.Towers[i].kind == kind)
                {
                    n++;
                }
            }
            return n;
        }

        private bool TryBuild(TowerKind kind)
        {
            TowerConfig cfg = _sim.Db.GetTower(kind);
            if (cfg == null || _sim.Spirit < cfg.cost)
            {
                return false;
            }
            for (int i = 0; i < _candidates.Count; i++)
            {
                GridPos cell = _candidates[i];
                if (_sim.IsCellOccupied(cell))
                {
                    continue;
                }
                if (_sim.TryBuildTower(cell, kind).ok)
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryUpgradeAny()
        {
            int bestId = -1;
            int bestCost = int.MaxValue;
            for (int i = 0; i < _sim.Towers.Count; i++)
            {
                TowerUnit t = _sim.Towers[i];
                if (t.kind != TowerKind.AttackArray && t.kind != TowerKind.SpiritGather)
                {
                    continue;
                }
                int cost = t.NextUpgradeCost;
                if (cost < 0 || _sim.Spirit < cost)
                {
                    continue;
                }
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestId = t.id;
                }
            }
            if (bestId < 0)
            {
                return false;
            }
            return _sim.TryUpgradeTower(bestId).ok;
        }

        private bool TryPlaceTrap()
        {
            TrapKind[] kinds = new TrapKind[] { TrapKind.BlastTalisman, TrapKind.IceTalisman, TrapKind.ThunderTalisman };
            for (int k = 0; k < kinds.Length; k++)
            {
                TrapConfig cfg = _sim.Db.GetTrap(kinds[k]);
                if (cfg == null || _sim.Spirit < cfg.cost)
                {
                    continue;
                }
                for (int p = 0; p < 2; p++)
                {
                    PathData path = _sim.Map.GetPath(p);
                    for (int i = path.Cells.Count / 3; i < path.Cells.Count; i++)
                    {
                        GridPos cell = path.Cells[i];
                        if (_sim.IsCellOccupied(cell))
                        {
                            continue;
                        }
                        if (_sim.TryPlaceTrap(cell, kinds[k]).ok)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool TrySummonSpirit()
        {
            SpiritKind[] kinds = new SpiritKind[] { SpiritKind.PillSpirit, SpiritKind.SwordSpirit };
            for (int k = 0; k < kinds.Length; k++)
            {
                SpiritConfig cfg = _sim.Db.GetSpirit(kinds[k]);
                if (cfg == null || _sim.Spirit < cfg.cost)
                {
                    continue;
                }
                for (int p = 0; p < 2; p++)
                {
                    PathData path = _sim.Map.GetPath(p);
                    for (int i = path.Cells.Count / 2; i < path.Cells.Count; i++)
                    {
                        GridPos cell = path.Cells[i];
                        if (_sim.IsCellOccupied(cell))
                        {
                            continue;
                        }
                        if (_sim.TrySummonSpirit(cell, kinds[k]).ok)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
