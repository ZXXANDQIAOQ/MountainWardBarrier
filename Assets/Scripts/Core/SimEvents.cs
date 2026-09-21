using System;
using System.Collections.Generic;

namespace MountainWardBarrier.Core
{
    /// <summary>模拟层向表现层广播的事件类型。</summary>
    public enum SimEventType
    {
        EnemySpawned = 0,
        EnemyDamaged = 1,
        EnemyDied = 2,
        EnemyReachedCore = 3,
        EnemySplit = 4,
        TowerBuilt = 5,
        TowerUpgraded = 6,
        TowerSold = 7,
        TowerDamaged = 8,
        TowerDestroyed = 9,
        SpiritSummoned = 10,
        SpiritDied = 11,
        SpiritExpired = 12,
        TrapTriggered = 13,
        ProjectileFired = 14,
        ProjectileHit = 15,
        SkillCast = 16,
        WaveStarted = 17,
        WaveCleared = 18,
        PrepPhaseStarted = 19,
        CoreDamaged = 20,
        Victory = 21,
        Defeat = 22,
        ActionRejected = 23,
        SpiritGained = 24,
        EnemyAttacked = 25,
        EnemyAttackDodged = 26,
        SpiritDamaged = 27,
        TrapPlaced = 28
    }

    /// <summary>
    /// 一次模拟事件。表现层根据它播放音效 / 特效 / 飘字。
    /// 保持成扁平的结构体，避免每帧产生大量小对象。
    /// </summary>
    public struct SimEvent
    {
        public SimEventType type;
        public int a;
        public int b;
        public float x;
        public float y;
        public float amount;

        public static SimEvent Make(SimEventType type, float x, float y)
        {
            SimEvent e = new SimEvent();
            e.type = type;
            e.a = 0;
            e.b = 0;
            e.x = x;
            e.y = y;
            e.amount = 0f;
            return e;
        }

        public static SimEvent Make(SimEventType type, float x, float y, float amount)
        {
            SimEvent e = new SimEvent();
            e.type = type;
            e.a = 0;
            e.b = 0;
            e.x = x;
            e.y = y;
            e.amount = amount;
            return e;
        }

        public static SimEvent Make(SimEventType type, int a, int b)
        {
            SimEvent e = new SimEvent();
            e.type = type;
            e.a = a;
            e.b = b;
            e.x = 0f;
            e.y = 0f;
            e.amount = 0f;
            return e;
        }

        public static SimEvent Make(SimEventType type, int a, float x, float y, float amount)
        {
            SimEvent e = new SimEvent();
            e.type = type;
            e.a = a;
            e.b = 0;
            e.x = x;
            e.y = y;
            e.amount = amount;
            return e;
        }
    }

    /// <summary>操作被拒绝的原因，供 UI 提示用。</summary>
    public enum ActionRejectReason
    {
        None = 0,
        NotEnoughSpirit = 1,
        CellOccupied = 2,
        CellNotBuildable = 3,
        CellNotPath = 4,
        OutOfBounds = 5,
        MaxLevelReached = 6,
        SkillOnCooldown = 7,
        BattleNotRunning = 8,
        BattleFinished = 9,
        Unknown = 10
    }

    /// <summary>数值语义化的操作结果。</summary>
    public struct ActionResult
    {
        public bool ok;
        public ActionRejectReason reason;
        public int id;

        public static ActionResult Fail(ActionRejectReason reason)
        {
            ActionResult r = new ActionResult();
            r.ok = false;
            r.reason = reason;
            r.id = -1;
            return r;
        }

        public static ActionResult Success(int id)
        {
            ActionResult r = new ActionResult();
            r.ok = true;
            r.reason = ActionRejectReason.None;
            r.id = id;
            return r;
        }

        public string Message()
        {
            if (ok)
            {
                return "ok";
            }
            switch (reason)
            {
                case ActionRejectReason.NotEnoughSpirit: return "灵气不足";
                case ActionRejectReason.CellOccupied: return "此处已有阵法";
                case ActionRejectReason.CellNotBuildable: return "这里不能布置";
                case ActionRejectReason.CellNotPath: return "符箓只能布在道路上";
                case ActionRejectReason.OutOfBounds: return "超出棋盘范围";
                case ActionRejectReason.MaxLevelReached: return "已是最高等级";
                case ActionRejectReason.SkillOnCooldown: return "技能冷却中";
                case ActionRejectReason.BattleNotRunning: return "尚未开始";
                case ActionRejectReason.BattleFinished: return "对局已结束";
                default: return "无法执行";
            }
        }
    }
}
