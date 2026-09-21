using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MountainWardBarrier.Core;

namespace MountainWardBarrier.Tools
{
    /// <summary>
    /// 核心逻辑的自测入口。
    ///
    ///   dotnet run                 —— 跑全部测试
    ///   dotnet run -- export-config &lt;路径&gt;  —— 从内置默认数值导出 game_config.json
    ///
    /// 它把 Assets/Scripts/Core 下的纯逻辑源码直接编进来，所以任何让游戏跑不起来的
    /// 语法 / 逻辑错误都会在这里被挡住。
    /// </summary>
    public static class Program
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (Exception)
            {
                // 某些终端不支持切换编码，忽略即可。
            }

            if (args.Length >= 2 && args[0] == "export-config")
            {
                return ExportConfig(args[1]);
            }

            Console.WriteLine("=== 《仙侠·护山大阵》核心逻辑自测 ===");
            Console.WriteLine();

            TestMiniJson();
            TestDefaultConfig();
            TestGridMap();
            TestWaveTable();
            TestSaveRoundTrip();
            TestAchievements();
            TestConfigRoundTrip();
            TestSimulationSmokeNoDefense();
            TestDeterminism();
            TestBalance();

            Console.WriteLine();
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("通过 " + _passed + " 项，失败 " + _failed + " 项");
            for (int i = 0; i < _failures.Count; i++)
            {
                Console.WriteLine("  [FAIL] " + _failures[i]);
            }
            Console.WriteLine("-------------------------------------");
            return _failed == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------ 断言

        private static void Check(bool condition, string message)
        {
            if (condition)
            {
                _passed++;
            }
            else
            {
                _failed++;
                _failures.Add(message);
                Console.WriteLine("  [FAIL] " + message);
            }
        }

        private static void Section(string name)
        {
            Console.WriteLine("· " + name);
        }

        // ------------------------------------------------------------ 各项测试

        private static void TestMiniJson()
        {
            Section("MiniJson 解析与序列化");

            string json = "{\"a\":1,\"b\":[1,2,3],\"c\":{\"d\":\"中文\\n换行\"},\"e\":true,\"f\":null,\"g\":-2.5e2}";
            Dictionary<string, object> root = MiniJson.DeserializeObject(json);
            Check(root != null, "应能解析嵌套 JSON");
            if (root != null)
            {
                Check(MiniJson.GetInt(root, "a", -1) == 1, "整数字段");
                Check(MiniJson.GetList(root, "b") != null && MiniJson.GetList(root, "b").Count == 3, "数组字段");
                Check(MiniJson.GetString(root, "c", "") != null, "嵌套对象");
                Check(MiniJson.GetBool(root, "e", false), "布尔字段");
                Check(Math.Abs(MiniJson.GetFloat(root, "g", 0f) - (-250f)) < 0.01f, "科学计数法");
            }

            Check(MiniJson.DeserializeObject("{不是 json") == null, "非法 JSON 应返回 null 而不是抛异常");

            Dictionary<string, object> rt = new Dictionary<string, object>();
            rt["name"] = "聚灵阵";
            rt["cost"] = 50;
            rt["ratio"] = 1.5f;
            rt["flag"] = true;
            rt["list"] = new List<object> { 1, 2, 3 };
            string text = MiniJson.Serialize(rt);
            Dictionary<string, object> back = MiniJson.DeserializeObject(text);
            Check(back != null && MiniJson.GetString(back, "name", "") == "聚灵阵", "中文往返");
            Check(back != null && MiniJson.GetInt(back, "cost", 0) == 50, "整数往返");
            Check(back != null && Math.Abs(MiniJson.GetFloat(back, "ratio", 0f) - 1.5f) < 0.001f, "浮点往返");
            Check(back != null && MiniJson.GetBool(back, "flag", false), "布尔往返");
        }

        private static void TestDefaultConfig()
        {
            Section("内置默认配置");
            GameDatabase db = DefaultConfig.Build();

            Check(db.towers.Count == 5, "应有 5 种阵法，实际 " + db.towers.Count);
            Check(db.spirits.Count == 3, "应有 3 种仙灵，实际 " + db.spirits.Count);
            Check(db.traps.Count == 3, "应有 3 种符箓，实际 " + db.traps.Count);
            Check(db.skills.Count == 3, "应有 3 个终极技能，实际 " + db.skills.Count);
            Check(db.enemies.Count == 4, "应有 4 种妖魔，实际 " + db.enemies.Count);
            Check(db.waves.Count == 20, "应有 20 波配置，实际 " + db.waves.Count);
            Check(db.difficulties.Count == 3, "应有 3 个难度");

            DifficultyConfig easy = db.GetDifficulty(Difficulty.Easy);
            DifficultyConfig normal = db.GetDifficulty(Difficulty.Normal);
            DifficultyConfig hard = db.GetDifficulty(Difficulty.Hard);
            Check(easy != null && easy.waveCount == 10, "简单难度应为 10 波");
            Check(normal != null && normal.waveCount == 15, "普通难度应为 15 波");
            Check(hard != null && hard.waveCount == 20, "困难难度应为 20 波");

            Check(db.GetWavesFor(Difficulty.Easy).Count == 10, "简单难度波表长度");
            Check(db.GetWavesFor(Difficulty.Normal).Count == 15, "普通难度波表长度");
            Check(db.GetWavesFor(Difficulty.Hard).Count == 20, "困难难度波表长度");

            // 每个难度的收官战都应该是 Boss 波，否则高潮不够
            Check(db.GetWavesFor(Difficulty.Easy)[9].IsBossWave, "简单难度第 10 波应为 Boss 波");
            Check(db.GetWavesFor(Difficulty.Normal)[14].IsBossWave, "普通难度第 15 波应为 Boss 波");
            Check(db.GetWavesFor(Difficulty.Hard)[19].IsBossWave, "困难难度第 20 波应为 Boss 波");

            for (int i = 0; i < db.towers.Count; i++)
            {
                TowerConfig c = db.towers[i];
                Check(c.cost > 0 && c.hp > 0 && c.maxLevel >= 1, "阵法 " + c.displayName + " 的基础数值应为正数");
                Check(!string.IsNullOrEmpty(c.sprite), "阵法 " + c.displayName + " 应指定贴图名");
            }
            for (int i = 0; i < db.enemies.Count; i++)
            {
                EnemyConfig c = db.enemies[i];
                Check(c.hp > 0f && c.speed > 0f && c.reward >= 0, "妖魔 " + c.displayName + " 的基础数值应为正数");
            }
            for (int i = 0; i < db.waves.Count; i++)
            {
                WaveConfig w = db.waves[i];
                Check(w.groups.Count > 0, "第 " + w.index + " 波必须至少有一组妖魔");
                Check(w.TotalEnemies > 0, "第 " + w.index + " 波妖魔总数应大于 0");
            }
        }

        private static void TestGridMap()
        {
            Section("棋盘与寻路");
            GameDatabase db = DefaultConfig.Build();
            GridMap map = new GridMap(db);

            Check(map.Columns == 6 && map.Rows == 9, "棋盘应为 6 列 9 行");

            bool allInBounds = true;
            for (int p = 0; p < 2; p++)
            {
                PathData path = map.GetPath(p);
                allInBounds = allInBounds && path.Cells.Count > 3;
                allInBounds = allInBounds && path.Length > 5f;
                for (int i = 0; i < path.Cells.Count; i++)
                {
                    if (!map.InBounds(path.Cells[i].x, path.Cells[i].y))
                    {
                        allInBounds = false;
                    }
                }
            }
            Check(allInBounds, "两路路径都应落在棋盘内且长度合理");

            // 两条路径不能重叠，否则妖魔会互相穿插
            bool overlap = false;
            HashSet<int> used = new HashSet<int>();
            for (int p = 0; p < 2; p++)
            {
                PathData path = map.GetPath(p);
                for (int i = 0; i < path.Cells.Count; i++)
                {
                    int key = path.Cells[i].y * map.Columns + path.Cells[i].x;
                    if (used.Contains(key))
                    {
                        overlap = true;
                    }
                    used.Add(key);
                }
            }
            Check(!overlap, "甲路与乙路不应有重叠格子");

            int buildable = 0;
            for (int y = 0; y < map.Rows; y++)
            {
                for (int x = 0; x < map.Columns; x++)
                {
                    if (map.IsBuildable(x, y))
                    {
                        buildable++;
                    }
                }
            }
            Check(buildable >= 24, "可布置格子应足够多，实际 " + buildable);
            Console.WriteLine("    可布置格子：" + buildable + " 个；甲路长度 "
                + map.PathA.Length.ToString("F1") + " 格，乙路长度 " + map.PathB.Length.ToString("F1") + " 格");

            // 采样：路径上的点必须在棋盘范围内，且是连续的
            PathData pa = map.PathA;
            Float2 start = pa.SampleAt(0f);
            Float2 mid = pa.SampleAt(pa.Length * 0.5f);
            Float2 end = pa.SampleAt(pa.Length);
            Check(Float2.Distance(start, end) > Float2.Distance(start, mid), "路径采样应单调前进");
            Check(pa.SampleAt(-5f).x >= 0f, "越界采样应被安全钳制");
            Check(pa.SampleAt(pa.Length + 100f).y > 0f, "超出长度采样应停在终点");
        }

        private static void TestWaveTable()
        {
            Section("波次表合理性");
            GameDatabase db = DefaultConfig.Build();
            float prevHp = 0f;
            bool monotonish = true;
            for (int i = 0; i < db.waves.Count; i++)
            {
                if (db.waves[i].hpMultiplier < prevHp)
                {
                    monotonish = false;
                }
                prevHp = db.waves[i].hpMultiplier;
            }
            Check(monotonish, "血量倍率应随波次单调不降");

            // 灵气消耗曲线：出怪间隔不能太密，否则卡顿且难度失控
            bool intervalsOk = true;
            for (int i = 0; i < db.waves.Count; i++)
            {
                for (int j = 0; j < db.waves[i].groups.Count; j++)
                {
                    WaveGroup g = db.waves[i].groups[j];
                    if (g.interval < 0.3f || g.interval > 8f)
                    {
                        intervalsOk = false;
                    }
                    if (g.pathIndex < 0 || g.pathIndex > 1)
                    {
                        intervalsOk = false;
                    }
                }
            }
            Check(intervalsOk, "出怪间隔与路径索引应在合理范围内");
        }

        private static void TestSaveRoundTrip()
        {
            Section("存档读写");
            SaveData s = new SaveData();
            s.bestWave[0] = 10;
            s.bestWave[2] = 17;
            s.bestTime[0] = 245.5f;
            s.totalBattles = 7;
            s.totalKills = 321;
            s.unlocked.Add(0);
            s.unlocked.Add(4);
            s.tutorialCompleted = true;
            s.musicVolume = 0.33f;
            s.sfxEnabled = false;
            s.preferredDifficulty = 2;

            string json = s.ToJson();
            SaveData back = SaveData.FromJson(json);

            Check(back.bestWave[0] == 10 && back.bestWave[2] == 17, "最高波次应完整往返");
            Check(Math.Abs(back.bestTime[0] - 245.5f) < 0.01f, "最快通关时间应完整往返");
            Check(back.totalBattles == 7 && back.totalKills == 321, "累计统计应完整往返");
            Check(back.IsUnlocked(0) && back.IsUnlocked(4) && !back.IsUnlocked(1), "成就解锁状态应完整往返");
            Check(back.tutorialCompleted, "新手引导标记应完整往返");
            Check(Math.Abs(back.musicVolume - 0.33f) < 0.001f, "音量设置应完整往返");
            Check(!back.sfxEnabled && back.preferredDifficulty == 2, "开关与难度偏好应完整往返");

            SaveData broken = SaveData.FromJson("{坏掉的存档");
            Check(broken.totalBattles == 0 && broken.bestWave.Length == 3, "损坏的存档应安全回退到默认值");
        }

        private static void TestAchievements()
        {
            Section("成就判定");
            Check(Achievements.Count >= 8, "至少应有 8 个成就");

            SaveData save = new SaveData();
            BattleReport r = new BattleReport();
            r.difficulty = Difficulty.Easy;
            r.victory = true;
            r.waveReached = 10;
            r.totalWaves = 10;
            r.duration = 300f;
            r.coreHpLeft = 500f;
            r.coreHpMax = 700f;
            r.kills = 120;
            r.leaked = 0;
            r.spiritEarned = 6500f;
            r.towerKindMask = 31;
            r.thunderCasts = 3;

            List<int> fresh = save.ApplyReport(r);
            Check(fresh.Contains(0), "完成一局应解锁「初入山门」");
            Check(fresh.Contains(1), "通关简单难度应解锁「守山有功」");
            Check(fresh.Contains(5), "累计 6000 灵气应解锁「灵气大亨」");
            Check(fresh.Contains(6), "布置齐 5 种阵法应解锁「五行俱全」");
            Check(fresh.Contains(7), "零漏怪通关应解锁「一尘不染」");
            Check(fresh.Contains(8), "天雷咒 3 次应解锁「雷动九天」");
            Check(!fresh.Contains(3), "简单难度不应解锁困难通关成就");
            Check(save.totalVictories == 1 && save.totalBattles == 1, "战报应正确写入累计统计");
            Check(save.BestWave(Difficulty.Easy) == 10, "最高波次应被记录");

            // 同一个成就不会重复解锁
            List<int> again = save.ApplyReport(r);
            Check(!again.Contains(0), "已解锁的成就不应重复上报");
            Check(save.totalBattles == 2, "第二局应继续累计");
        }

        private static void TestConfigRoundTrip()
        {
            Section("配置导出 / 载入往返");
            GameDatabase original = DefaultConfig.Build();
            string json = ConfigLoader.Export(original);
            Check(json.Length > 2000, "导出的配置文本应有实际内容");
            Check(json.IndexOf("spirit_gather") >= 0, "导出文本应包含阵法 id");
            Check(json.IndexOf("聚灵阵") >= 0, "导出文本应保留中文");

            GameDatabase loaded = ConfigLoader.Load(json);
            Check(loaded.towers.Count == original.towers.Count, "阵法数量应一致");
            Check(loaded.enemies.Count == original.enemies.Count, "妖魔数量应一致");
            Check(loaded.waves.Count == original.waves.Count, "波次数量应一致");
            Check(loaded.pathA.Count == original.pathA.Count, "甲路拐点数量应一致");
            Check(loaded.coreCells.Count == original.coreCells.Count, "山门格子数量应一致");
            Check(ConfigLoader.LastWarnings.Count == 0, "往返过程不应产生警告，实际：" + Join(ConfigLoader.LastWarnings));

            TowerConfig a = original.GetTower(TowerKind.AttackArray);
            TowerConfig b = loaded.GetTower(TowerKind.AttackArray);
            Check(a != null && b != null, "应能按枚举取到攻击法阵");
            if (a != null && b != null)
            {
                Check(Math.Abs(a.damage - b.damage) < 0.001f, "攻击法阵伤害应一致");
                Check(Math.Abs(a.range - b.range) < 0.001f, "攻击法阵射程应一致");
                Check(Math.Abs(a.cost - b.cost) < 0.001f, "攻击法阵造价应一致");
                Check(a.kind == b.kind, "阵法枚举应一致");
            }

            EnemyConfig ea = original.GetEnemy(EnemyKind.Boss);
            EnemyConfig eb = loaded.GetEnemy(EnemyKind.Boss);
            Check(ea != null && eb != null && ea.ability == eb.ability, "Boss 的特殊能力应一致");
            Check(eb != null && Math.Abs(eb.hp - ea.hp) < 0.001f, "Boss 血量应一致");

            WaveConfig wa = original.waves[19];
            WaveConfig wb = loaded.waves[19];
            Check(wa.groups.Count == wb.groups.Count, "第 20 波的分组数量应一致");
            Check(wb.IsBossWave, "载入后第 20 波仍应是 Boss 波");

            // 用户手改数值应当生效
            GameDatabase tuned = DefaultConfig.Build();
            ConfigLoader.Apply(tuned, "{\"towers\":[{\"kind\":\"AttackArray\",\"damage\":999,\"cost\":10}]}");
            TowerConfig tunedAttack = tuned.GetTower(TowerKind.AttackArray);
            Check(tunedAttack != null && Math.Abs(tunedAttack.damage - 999f) < 0.001f, "手改伤害应覆盖默认值");
            Check(tuned != null && tuned.towers.Count == 1, "只写一个阵法时应只保留该阵法");

            // 部分字段缺失时其余字段应保留默认值
            GameDatabase partial = DefaultConfig.Build();
            ConfigLoader.Apply(partial, "{\"gameName\":\"改个名字\"}");
            Check(partial.gameName == "改个名字", "应能改游戏名");
            Check(partial.towers.Count == 5, "缺失的字段应保留默认值");

            // 坏 JSON 不应炸
            GameDatabase bad = DefaultConfig.Build();
            ConfigLoader.Apply(bad, "{{{坏掉的配置");
            Check(bad.towers.Count == 5, "坏配置应被忽略并保留默认值");
        }

        private static void TestSimulationSmokeNoDefense()
        {
            Section("不设防时应当速败");
            GameDatabase db = DefaultConfig.Build();
            BattleSimulation sim = new BattleSimulation(db, Difficulty.Normal, 12345);
            sim.Begin();
            RunSimulation(sim, 60f * 12f, null);
            Check(sim.Finished, "12 分钟模拟内对局应已结束");
            Check(sim.Phase == BattlePhase.Defeat, "完全不设防应该守不住，实际：" + sim.Phase);
            Check(sim.CoreHp <= 0f, "核心耐久应被打空");
            Check(sim.LeakedCount > 0, "应有妖魔抵达山门");
            Check(sim.TotalKills == 0, "不设防时击杀数应为 0");
            Check(sim.ElapsedSeconds < 240f, "不设防应该很快崩盘，实际用了 " + sim.ElapsedSeconds.ToString("F0") + " 秒");
        }

        private static void TestDeterminism()
        {
            Section("同 seed 可复现");
            GameDatabase db = DefaultConfig.Build();

            BattleReport a = RunBotBattle(db, Difficulty.Normal, 777, 60f * 20f);
            BattleReport b = RunBotBattle(db, Difficulty.Normal, 777, 60f * 20f);
            Check(a.victory == b.victory, "同 seed 胜负应一致");
            Check(Math.Abs(a.duration - b.duration) < 0.05f, "同 seed 时长应一致");
            Check(a.kills == b.kills, "同 seed 击杀数应一致，实际 " + a.kills + " vs " + b.kills);
            Check(Math.Abs(a.coreHpLeft - b.coreHpLeft) < 0.05f, "同 seed 剩余耐久应一致");
        }

        private static void TestBalance()
        {
            Section("数值平衡（脚本机器人试打）");
            GameDatabase db = DefaultConfig.Build();

            Difficulty[] order = new Difficulty[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };
            for (int i = 0; i < order.Length; i++)
            {
                Difficulty d = order[i];
                int wins = 0;
                const int rounds = 3;
                float sumWave = 0f;
                float sumDuration = 0f;
                int sumTowers = 0;
                int sumMaxLevel = 0;
                float sumLeaked = 0f;
                BattleReport last = null;
                for (int k = 0; k < rounds; k++)
                {
                    BattleReport r = RunBotBattle(db, d, 1000 + k * 37, 60f * 25f);
                    last = r;
                    if (r.victory)
                    {
                        wins++;
                    }
                    sumWave += r.waveReached;
                    sumDuration += r.duration;
                    sumTowers += r.towersBuilt;
                    sumMaxLevel += r.towerKindMask;
                    sumLeaked += r.leaked;
                }
                Console.WriteLine("    " + db.GetDifficulty(d).displayName
                    + "：机器人胜 " + wins + "/" + rounds
                    + "，平均推进到第 " + (sumWave / rounds).ToString("F1") + " 波"
                    + "，平均耗时 " + (sumDuration / rounds).ToString("F0") + " 秒"
                    + "，平均建阵 " + (sumTowers / (float)rounds).ToString("F1") + " 座"
                    + "，平均漏怪 " + (sumLeaked / rounds).ToString("F1") + " 只");

                string diffName = db.GetDifficulty(d).displayName;
                if (last != null)
                {
                    Console.WriteLine("      · 上局明细：" + (last.victory ? "守山成功" : "山门失守")
                        + "，剩余耐久 " + (last.CoreHpRatio * 100f).ToString("F0") + "%"
                        + "，击杀 " + last.kills
                        + "，漏怪 " + last.leaked
                        + "，灵气收入 " + last.spiritEarned.ToString("F0")
                        + " / 支出 " + last.spiritSpent.ToString("F0")
                        + "，阵法种类掩码 " + last.towerKindMask);
                }
                if (d == Difficulty.Hard)
                {
                    // 困难难度是「设计上就该难」的一档：这里只要求它不至于完全没得打
                    // （机器人能推到第 15 波之后，说明难度是陡而不是崩）。
                    Check(sumWave / rounds >= 14f,
                        "困难难度应当至少能推进到第 14 波（脚本机器人平均只到 "
                        + (sumWave / rounds).ToString("F1") + " 波）");
                }
                else
                {
                    // 简单 / 普通必须能被一个「不算聪明、只会等备战时间攒钱」的机器人打通，
                    // 否则说明数值把普通玩家挡在门外了。
                    Check(wins >= 1, diffName + " 难度应至少可通关一次（当前 0/" + rounds + "）");
                }
            }
        }

        // ------------------------------------------------------------ 跑模拟

        private static BattleReport RunBotBattle(GameDatabase db, Difficulty difficulty, int seed, float maxSeconds)
        {
            BattleSimulation sim = new BattleSimulation(db, difficulty, seed);
            sim.Begin();
            BalanceBot bot = new BalanceBot(sim);
            RunSimulation(sim, maxSeconds, bot);
            return BattleReport.FromSimulation(sim);
        }

        private static void RunSimulation(BattleSimulation sim, float maxSeconds, BalanceBot bot)
        {
            const float dt = 1f / 60f;
            float elapsed = 0f;
            int guard = 0;
            int maxSteps = (int)(maxSeconds / dt) + 10;
            while (!sim.Finished && elapsed < maxSeconds && guard < maxSteps)
            {
                if (bot != null)
                {
                    bot.Update(dt);
                }
                sim.Tick(dt);
                sim.ClearEvents();
                elapsed += dt;
                guard++;
            }
        }

        private static string Join(List<string> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" / ");
                }
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------ 导出配置

        private static int ExportConfig(string path)
        {
            GameDatabase db = DefaultConfig.Build();
            string json = ConfigLoader.Export(db);
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, json, new UTF8Encoding(false));
            Console.WriteLine("已导出配置：" + Path.GetFullPath(path) + "（" + json.Length + " 字符）");
            return 0;
        }
    }
}
