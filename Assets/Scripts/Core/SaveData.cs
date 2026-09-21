using System;
using System.Collections.Generic;
using System.Globalization;

namespace MountainWardBarrier.Core
{
    // ============================================================== 存档

    /// <summary>
    /// 本地存档。用 MiniJson 手工读写，好处是它属于纯逻辑层，可以脱离 Unity 做往返测试。
    /// 在 Unity 里最终被存成 PlayerPrefs 的一个字符串（见 SaveSystem）。
    /// </summary>
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>各难度的最高波次记录，下标同 Difficulty。</summary>
        public int[] bestWave = new int[3];
        /// <summary>各难度的最快通关时间（秒），0 表示尚未通关。</summary>
        public float[] bestTime = new float[3];
        /// <summary>已解锁的成就下标。</summary>
        public List<int> unlocked = new List<int>();

        public int totalBattles;
        public int totalVictories;
        public int totalDefeats;
        public int totalKills;
        public float totalSpiritEarned;
        public float totalPlayTime;

        public bool tutorialCompleted;

        public bool musicEnabled = true;
        public bool sfxEnabled = true;
        public float musicVolume = 0.55f;
        public float sfxVolume = 0.80f;
        public bool damageNumbers = true;
        /// <summary>上次选择的难度（Difficulty 的整数值）。</summary>
        public int preferredDifficulty = 1;
        public bool showFps;

        public void Normalize()
        {
            if (bestWave == null || bestWave.Length < 3)
            {
                int[] copy = new int[3];
                if (bestWave != null)
                {
                    for (int i = 0; i < bestWave.Length && i < 3; i++)
                    {
                        copy[i] = bestWave[i];
                    }
                }
                bestWave = copy;
            }
            if (bestTime == null || bestTime.Length < 3)
            {
                float[] copy = new float[3];
                if (bestTime != null)
                {
                    for (int i = 0; i < bestTime.Length && i < 3; i++)
                    {
                        copy[i] = bestTime[i];
                    }
                }
                bestTime = copy;
            }
            if (unlocked == null)
            {
                unlocked = new List<int>();
            }
            musicVolume = SimMath.Clamp(musicVolume, 0f, 1f);
            sfxVolume = SimMath.Clamp(sfxVolume, 0f, 1f);
            preferredDifficulty = SimMath.ClampInt(preferredDifficulty, 0, 2);
        }

        public bool IsUnlocked(int index)
        {
            return unlocked != null && unlocked.Contains(index);
        }

        public bool Unlock(int index)
        {
            if (unlocked == null)
            {
                unlocked = new List<int>();
            }
            if (unlocked.Contains(index))
            {
                return false;
            }
            unlocked.Add(index);
            return true;
        }

        public int BestWave(Difficulty d)
        {
            int i = (int)d;
            if (bestWave == null || i < 0 || i >= bestWave.Length)
            {
                return 0;
            }
            return bestWave[i];
        }

        public float BestTime(Difficulty d)
        {
            int i = (int)d;
            if (bestTime == null || i < 0 || i >= bestTime.Length)
            {
                return 0f;
            }
            return bestTime[i];
        }

        /// <summary>把一局的结果合并进存档，返回这次新解锁的成就下标。</summary>
        public List<int> ApplyReport(BattleReport r)
        {
            Normalize();
            totalBattles++;
            totalKills += r.kills;
            totalSpiritEarned += r.spiritEarned;
            totalPlayTime += r.duration;
            if (r.victory)
            {
                totalVictories++;
            }
            else
            {
                totalDefeats++;
            }

            int di = (int)r.difficulty;
            if (di >= 0 && di < 3)
            {
                if (r.waveReached > bestWave[di])
                {
                    bestWave[di] = r.waveReached;
                }
                if (r.victory)
                {
                    if (bestTime[di] <= 0f || r.duration < bestTime[di])
                    {
                        bestTime[di] = r.duration;
                    }
                }
            }

            return Achievements.Evaluate(this, r);
        }

        // ------------------------------------------------------------ 序列化

        public string ToJson()
        {
            Normalize();
            Dictionary<string, object> root = new Dictionary<string, object>();
            root["version"] = version;
            root["bestWave"] = IntList(bestWave);
            root["bestTime"] = FloatList(bestTime);
            root["unlocked"] = IntList(unlocked.ToArray());
            root["totalBattles"] = totalBattles;
            root["totalVictories"] = totalVictories;
            root["totalDefeats"] = totalDefeats;
            root["totalKills"] = totalKills;
            root["totalSpiritEarned"] = totalSpiritEarned;
            root["totalPlayTime"] = totalPlayTime;
            root["tutorialCompleted"] = tutorialCompleted;
            root["musicEnabled"] = musicEnabled;
            root["sfxEnabled"] = sfxEnabled;
            root["musicVolume"] = musicVolume;
            root["sfxVolume"] = sfxVolume;
            root["damageNumbers"] = damageNumbers;
            root["preferredDifficulty"] = preferredDifficulty;
            root["showFps"] = showFps;
            return MiniJson.Serialize(root);
        }

        public static SaveData FromJson(string json)
        {
            SaveData s = new SaveData();
            if (string.IsNullOrEmpty(json))
            {
                return s;
            }
            Dictionary<string, object> root = MiniJson.DeserializeObject(json);
            if (root == null)
            {
                return s;
            }

            s.version = MiniJson.GetInt(root, "version", CurrentVersion);
            s.bestWave = ReadIntArray(MiniJson.GetList(root, "bestWave"), 3);
            s.bestTime = ReadFloatArray(MiniJson.GetList(root, "bestTime"), 3);
            s.unlocked = new List<int>();
            List<object> unlockedRaw = MiniJson.GetList(root, "unlocked");
            if (unlockedRaw != null)
            {
                for (int i = 0; i < unlockedRaw.Count; i++)
                {
                    s.unlocked.Add(ToInt(unlockedRaw[i]));
                }
            }
            s.totalBattles = MiniJson.GetInt(root, "totalBattles", 0);
            s.totalVictories = MiniJson.GetInt(root, "totalVictories", 0);
            s.totalDefeats = MiniJson.GetInt(root, "totalDefeats", 0);
            s.totalKills = MiniJson.GetInt(root, "totalKills", 0);
            s.totalSpiritEarned = MiniJson.GetFloat(root, "totalSpiritEarned", 0f);
            s.totalPlayTime = MiniJson.GetFloat(root, "totalPlayTime", 0f);
            s.tutorialCompleted = MiniJson.GetBool(root, "tutorialCompleted", false);
            s.musicEnabled = MiniJson.GetBool(root, "musicEnabled", true);
            s.sfxEnabled = MiniJson.GetBool(root, "sfxEnabled", true);
            s.musicVolume = MiniJson.GetFloat(root, "musicVolume", 0.55f);
            s.sfxVolume = MiniJson.GetFloat(root, "sfxVolume", 0.80f);
            s.damageNumbers = MiniJson.GetBool(root, "damageNumbers", true);
            s.preferredDifficulty = MiniJson.GetInt(root, "preferredDifficulty", 1);
            s.showFps = MiniJson.GetBool(root, "showFps", false);
            s.Normalize();
            return s;
        }

        private static List<object> IntList(int[] arr)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < arr.Length; i++)
            {
                list.Add(arr[i]);
            }
            return list;
        }

        private static List<object> FloatList(float[] arr)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < arr.Length; i++)
            {
                list.Add(arr[i]);
            }
            return list;
        }

        private static int[] ReadIntArray(List<object> src, int size)
        {
            int[] arr = new int[size];
            if (src == null)
            {
                return arr;
            }
            for (int i = 0; i < src.Count && i < size; i++)
            {
                arr[i] = ToInt(src[i]);
            }
            return arr;
        }

        private static float[] ReadFloatArray(List<object> src, int size)
        {
            float[] arr = new float[size];
            if (src == null)
            {
                return arr;
            }
            for (int i = 0; i < src.Count && i < size; i++)
            {
                arr[i] = ToFloat(src[i]);
            }
            return arr;
        }

        private static int ToInt(object o)
        {
            if (o is double)
            {
                return (int)Math.Round((double)o);
            }
            if (o is string)
            {
                int v;
                if (int.TryParse((string)o, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                {
                    return v;
                }
            }
            return 0;
        }

        private static float ToFloat(object o)
        {
            if (o is double)
            {
                return (float)(double)o;
            }
            if (o is string)
            {
                float v;
                if (float.TryParse((string)o, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    return v;
                }
            }
            return 0f;
        }
    }

    // ============================================================== 对局结算

    /// <summary>一局结束后的统计快照，用于结算界面、排行、成就与埋点。</summary>
    public class BattleReport
    {
        public Difficulty difficulty;
        public bool victory;
        public int waveReached;
        public int totalWaves;
        public float duration;
        public float coreHpLeft;
        public float coreHpMax;

        public int kills;
        public int leaked;
        public int towersBuilt;
        public int towersSold;
        public int spiritsSummoned;
        public int trapsPlaced;

        public float spiritEarned;
        public float spiritSpent;
        public float damageDealt;

        public int towerKindMask;
        public int skillCasts;
        public int thunderCasts;
        public int seed;

        /// <summary>
        /// 这一局是否刷新了该难度的纪录（最高波次或最快通关）。
        /// 由表现层在并入存档**之前**和旧纪录比较后写入，用于结算界面的"新纪录"标记。
        /// </summary>
        public bool newRecord;

        public float CoreHpRatio
        {
            get
            {
                if (coreHpMax <= 0f)
                {
                    return 0f;
                }
                return SimMath.Clamp01(coreHpLeft / coreHpMax);
            }
        }

        public static BattleReport FromSimulation(BattleSimulation sim)
        {
            BattleReport r = new BattleReport();
            r.difficulty = sim.Difficulty;
            r.victory = sim.Victory;
            r.waveReached = sim.MaxWaveReached;
            r.totalWaves = sim.TotalWaves;
            r.duration = sim.ElapsedSeconds;
            r.coreHpLeft = sim.CoreHp;
            r.coreHpMax = sim.CoreMaxHp;
            r.kills = sim.TotalKills;
            r.leaked = sim.LeakedCount;
            r.towersBuilt = sim.TowersBuiltCount;
            r.towersSold = sim.TowersSoldCount;
            r.spiritsSummoned = sim.SpiritsSummonedCount;
            r.trapsPlaced = sim.TrapsPlacedCount;
            r.spiritEarned = sim.TotalSpiritEarned;
            r.spiritSpent = sim.TotalSpiritSpent;
            r.damageDealt = sim.DamageDealt;
            r.towerKindMask = sim.TowerKindMask;
            r.skillCasts = sim.SkillCastCount;
            r.thunderCasts = sim.ThunderCastCount;
            r.seed = sim.Seed;
            return r;
        }
    }

    // ============================================================== 成就

    public class AchievementDef
    {
        public string id;
        public string name;
        public string description;

        public AchievementDef(string id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }
    }

    /// <summary>成就系统。全部条件都是拿「存档 + 本局战报」纯函数判断的，方便测试。</summary>
    public static class Achievements
    {
        public static readonly AchievementDef[] All = new AchievementDef[]
        {
            new AchievementDef("first_battle", "初入山门", "完成第一场对局。"),
            new AchievementDef("clear_easy", "守山有功", "通关简单难度。"),
            new AchievementDef("clear_normal", "中流砥柱", "通关普通难度。"),
            new AchievementDef("clear_hard", "力挽狂澜", "通关困难难度。"),
            new AchievementDef("iron_core", "固若金汤", "以 90% 以上的山门耐久通关。"),
            new AchievementDef("spirit_tycoon", "灵气大亨", "单局累计获得 6000 点灵气。"),
            new AchievementDef("five_arrays", "五行俱全", "单局里布置齐全部 5 种阵法。"),
            new AchievementDef("immaculate", "一尘不染", "通关且没有任何妖魔抵达山门。"),
            new AchievementDef("thunder_lord", "雷动九天", "单局使用天雷咒 3 次。"),
            new AchievementDef("veteran", "百战之身", "累计完成 30 场对局。")
        };

        public static int Count
        {
            get { return All.Length; }
        }

        public static bool Check(int index, SaveData save, BattleReport r)
        {
            switch (index)
            {
                case 0: return save.totalBattles >= 1;
                case 1: return r.victory && r.difficulty == Difficulty.Easy;
                case 2: return r.victory && r.difficulty == Difficulty.Normal;
                case 3: return r.victory && r.difficulty == Difficulty.Hard;
                case 4: return r.victory && r.CoreHpRatio >= 0.9f;
                case 5: return r.spiritEarned >= 6000f;
                case 6: return r.towerKindMask == 31;
                case 7: return r.victory && r.leaked == 0;
                case 8: return r.thunderCasts >= 3;
                case 9: return save.totalBattles >= 30;
                default: return false;
            }
        }

        /// <summary>检查全部成就，把新解锁的写进存档并返回下标列表。</summary>
        public static List<int> Evaluate(SaveData save, BattleReport r)
        {
            List<int> fresh = new List<int>();
            for (int i = 0; i < All.Length; i++)
            {
                if (save.IsUnlocked(i))
                {
                    continue;
                }
                if (Check(i, save, r))
                {
                    save.Unlock(i);
                    fresh.Add(i);
                }
            }
            return fresh;
        }

        public static int UnlockedCount(SaveData save)
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (save.IsUnlocked(i))
                {
                    n++;
                }
            }
            return n;
        }
    }

    // ============================================================== 数据埋点

    /// <summary>一条埋点记录。</summary>
    public struct TelemetryRecord
    {
        public string name;
        public float time;
        public string key;
        public float value;
    }

    /// <summary>
    /// 本地埋点收集器。游戏是纯单机、没有服务器，所以埋点只写成本地 JSONL 文件，
    /// 供开发者自己回看「哪一波劝退」「哪个阵法没人用」。
    /// </summary>
    public class TelemetryCollector
    {
        public readonly List<TelemetryRecord> Records = new List<TelemetryRecord>();

        public int Count
        {
            get { return Records.Count; }
        }

        public void Clear()
        {
            Records.Clear();
        }

        public void Log(string name, float time, string key, float value)
        {
            TelemetryRecord r = new TelemetryRecord();
            r.name = name;
            r.time = time;
            r.key = key;
            r.value = value;
            Records.Add(r);
        }

        public void LogStart(Difficulty difficulty, int seed)
        {
            Log("battle_start", 0f, "difficulty", (float)difficulty);
            Log("battle_start", 0f, "seed", seed);
        }

        public void LogTowerPlaced(TowerKind kind, GridPos cell, int cost)
        {
            Log("tower_placed", 0f, kind.ToString(), cost);
        }

        public void LogTowerUpgraded(TowerKind kind, int fromLevel, int toLevel, int cost)
        {
            Log("tower_upgraded", 0f, kind.ToString() + "_" + fromLevel + "to" + toLevel, cost);
        }

        public void LogWaveCompleted(int waveIndex, float coreHpRatio, float elapsed)
        {
            Log("wave_completed", elapsed, "wave" + waveIndex, coreHpRatio);
        }

        public void LogBattleEnd(BattleReport r)
        {
            Log("battle_end", r.duration, r.victory ? "victory" : "defeat", r.waveReached);
            Log("battle_end", r.duration, "kills", r.kills);
            Log("battle_end", r.duration, "spirit_spent", r.spiritSpent);
            Log("battle_end", r.duration, "core_hp_ratio", r.CoreHpRatio);
        }

        /// <summary>输出成 JSONL（每行一个事件），便于用文本工具或表格软件分析。</summary>
        public string ToJsonLines()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < Records.Count; i++)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["event"] = Records[i].name;
                d["t"] = Records[i].time;
                d["key"] = Records[i].key;
                d["value"] = Records[i].value;
                sb.Append(MiniJson.Serialize(d));
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
