using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MountainWardBarrier.Core
{
    /// <summary>
    /// 把 JSON 配置载入 GameDatabase，或者把 GameDatabase 导出成 JSON。
    ///
    /// 关键设计：导出的 JSON 由内置默认配置生成，所以字段名永远不会和代码脱节；
    /// 载入时采用「逐字段宽松覆盖」策略 —— JSON 里没写的字段保留默认值，
    /// 写错的字段直接忽略并记一条警告，绝不会因为配置出错而让游戏打不开。
    /// </summary>
    public static class ConfigLoader
    {
        public static readonly List<string> LastWarnings = new List<string>();

        /// <summary>载入配置。json 为空 / 解析失败时返回内置默认配置。</summary>
        public static GameDatabase Load(string json)
        {
            GameDatabase db = DefaultConfig.Build();
            if (string.IsNullOrEmpty(json))
            {
                return db;
            }
            Apply(db, json);
            return db;
        }

        /// <summary>把 JSON 覆盖到已有配置上（宽松模式）。</summary>
        public static void Apply(GameDatabase db, string json)
        {
            LastWarnings.Clear();
            Dictionary<string, object> root = MiniJson.DeserializeObject(json);
            if (root == null)
            {
                LastWarnings.Add("配置文件不是合法的 JSON，已忽略，使用内置默认数值。");
                return;
            }

            db.version = MiniJson.GetInt(root, "version", db.version);
            db.gameName = MiniJson.GetString(root, "gameName", db.gameName);
            db.columns = MiniJson.GetInt(root, "columns", db.columns);
            db.rows = MiniJson.GetInt(root, "rows", db.rows);

            if (db.columns < 3)
            {
                LastWarnings.Add("columns 太小，已回退到默认值。");
                db.columns = DefaultConfig.Columns;
            }
            if (db.rows < 3)
            {
                LastWarnings.Add("rows 太小，已回退到默认值。");
                db.rows = DefaultConfig.Rows;
            }

            db.pathA = ReadPath(root, "pathA", db.pathA);
            db.pathB = ReadPath(root, "pathB", db.pathB);
            db.coreCells = ReadPath(root, "coreCells", db.coreCells);

            db.towers = ReadTowers(root, db.towers);
            db.spirits = ReadSpirits(root, db.spirits);
            db.traps = ReadTraps(root, db.traps);
            db.skills = ReadSkills(root, db.skills);
            db.enemies = ReadEnemies(root, db.enemies);
            db.waves = ReadWaves(root, db.waves);
            db.difficulties = ReadDifficulties(root, db.difficulties);
        }

        // ------------------------------------------------------------ 读取

        private static List<GridPos> ReadPath(Dictionary<string, object> root, string key, List<GridPos> fallback)
        {
            List<object> arr = MiniJson.GetList(root, key);
            if (arr == null)
            {
                return fallback;
            }
            List<GridPos> result = new List<GridPos>();
            for (int i = 0; i < arr.Count; i++)
            {
                List<object> pair = MiniJson.AsList(arr[i]);
                if (pair != null && pair.Count >= 2)
                {
                    int x = ToInt(pair[0]);
                    int y = ToInt(pair[1]);
                    result.Add(new GridPos(x, y));
                }
            }
            if (result.Count == 0)
            {
                LastWarnings.Add(key + " 解析为空，已保留默认路径。");
                return fallback;
            }
            return result;
        }

        private static List<TowerConfig> ReadTowers(Dictionary<string, object> root, List<TowerConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "towers");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<TowerConfig> list = new List<TowerConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                TowerConfig c = new TowerConfig();
                c.kind = ParseEnum<TowerKind>(MiniJson.GetString(d, "kind", null), (TowerKind)i);
                c.id = MiniJson.GetString(d, "id", "tower_" + i);
                c.displayName = MiniJson.GetString(d, "displayName", c.id);
                c.description = MiniJson.GetString(d, "description", "");
                c.sprite = MiniJson.GetString(d, "sprite", "");
                c.cost = MiniJson.GetInt(d, "cost", 50);
                c.upgradeCostBase = MiniJson.GetInt(d, "upgradeCostBase", 40);
                c.maxLevel = Math.Max(1, MiniJson.GetInt(d, "maxLevel", 5));
                c.hp = MiniJson.GetFloat(d, "hp", 100f);
                c.damage = MiniJson.GetFloat(d, "damage", 0f);
                c.attackInterval = MiniJson.GetFloat(d, "attackInterval", 0f);
                c.range = MiniJson.GetFloat(d, "range", 0f);
                c.spiritPerSecond = MiniJson.GetFloat(d, "spiritPerSecond", 0f);
                c.slowFactor = MiniJson.GetFloat(d, "slowFactor", 0f);
                c.dodgeChance = MiniJson.GetFloat(d, "dodgeChance", 0f);
                c.shieldPool = MiniJson.GetFloat(d, "shieldPool", 0f);
                c.shieldRegen = MiniJson.GetFloat(d, "shieldRegen", 0f);
                c.projectileSpeed = MiniJson.GetFloat(d, "projectileSpeed", 0f);
                c.splashRadius = MiniJson.GetFloat(d, "splashRadius", 0f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<SpiritConfig> ReadSpirits(Dictionary<string, object> root, List<SpiritConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "spirits");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<SpiritConfig> list = new List<SpiritConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                SpiritConfig c = new SpiritConfig();
                c.kind = ParseEnum<SpiritKind>(MiniJson.GetString(d, "kind", null), (SpiritKind)i);
                c.id = MiniJson.GetString(d, "id", "spirit_" + i);
                c.displayName = MiniJson.GetString(d, "displayName", c.id);
                c.description = MiniJson.GetString(d, "description", "");
                c.sprite = MiniJson.GetString(d, "sprite", "");
                c.cost = MiniJson.GetInt(d, "cost", 60);
                c.hp = MiniJson.GetFloat(d, "hp", 100f);
                c.damage = MiniJson.GetFloat(d, "damage", 0f);
                c.attackInterval = MiniJson.GetFloat(d, "attackInterval", 0f);
                c.range = MiniJson.GetFloat(d, "range", 0f);
                c.duration = MiniJson.GetFloat(d, "duration", 20f);
                c.spiritPerSecond = MiniJson.GetFloat(d, "spiritPerSecond", 0f);
                c.coreRepairPerSecond = MiniJson.GetFloat(d, "coreRepairPerSecond", 0f);
                c.splash = MiniJson.GetBool(d, "splash", false);
                c.splashRadius = MiniJson.GetFloat(d, "splashRadius", 0f);
                c.projectileSpeed = MiniJson.GetFloat(d, "projectileSpeed", 0f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<TrapConfig> ReadTraps(Dictionary<string, object> root, List<TrapConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "traps");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<TrapConfig> list = new List<TrapConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                TrapConfig c = new TrapConfig();
                c.kind = ParseEnum<TrapKind>(MiniJson.GetString(d, "kind", null), (TrapKind)i);
                c.id = MiniJson.GetString(d, "id", "trap_" + i);
                c.displayName = MiniJson.GetString(d, "displayName", c.id);
                c.description = MiniJson.GetString(d, "description", "");
                c.sprite = MiniJson.GetString(d, "sprite", "");
                c.cost = MiniJson.GetInt(d, "cost", 35);
                c.damage = MiniJson.GetFloat(d, "damage", 50f);
                c.radius = MiniJson.GetFloat(d, "radius", 1f);
                c.slowFactor = MiniJson.GetFloat(d, "slowFactor", 0f);
                c.slowDuration = MiniJson.GetFloat(d, "slowDuration", 0f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<SkillConfig> ReadSkills(Dictionary<string, object> root, List<SkillConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "skills");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<SkillConfig> list = new List<SkillConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                SkillConfig c = new SkillConfig();
                c.kind = ParseEnum<SkillKind>(MiniJson.GetString(d, "kind", null), (SkillKind)i);
                c.id = MiniJson.GetString(d, "id", "skill_" + i);
                c.displayName = MiniJson.GetString(d, "displayName", c.id);
                c.description = MiniJson.GetString(d, "description", "");
                c.sprite = MiniJson.GetString(d, "sprite", "");
                c.cost = MiniJson.GetInt(d, "cost", 150);
                c.cooldown = MiniJson.GetFloat(d, "cooldown", 40f);
                c.damage = MiniJson.GetFloat(d, "damage", 0f);
                c.duration = MiniJson.GetFloat(d, "duration", 0f);
                c.coreRepairPercent = MiniJson.GetFloat(d, "coreRepairPercent", 0f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<EnemyConfig> ReadEnemies(Dictionary<string, object> root, List<EnemyConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "enemies");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<EnemyConfig> list = new List<EnemyConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                EnemyConfig c = new EnemyConfig();
                c.kind = ParseEnum<EnemyKind>(MiniJson.GetString(d, "kind", null), (EnemyKind)i);
                c.id = MiniJson.GetString(d, "id", "enemy_" + i);
                c.displayName = MiniJson.GetString(d, "displayName", c.id);
                c.sprite = MiniJson.GetString(d, "sprite", "");
                c.hp = MiniJson.GetFloat(d, "hp", 100f);
                c.speed = MiniJson.GetFloat(d, "speed", 1f);
                c.armor = MiniJson.GetFloat(d, "armor", 0f);
                c.reward = MiniJson.GetInt(d, "reward", 10);
                c.coreDamage = MiniJson.GetFloat(d, "coreDamage", 5f);
                c.attackDamage = MiniJson.GetFloat(d, "attackDamage", 10f);
                c.attackInterval = MiniJson.GetFloat(d, "attackInterval", 1f);
                c.attackRange = MiniJson.GetFloat(d, "attackRange", 0.8f);
                c.ability = ParseEnum<EnemyAbility>(MiniJson.GetString(d, "ability", null), EnemyAbility.None);
                c.shieldAmount = MiniJson.GetFloat(d, "shieldAmount", 0f);
                c.shieldRegenInterval = MiniJson.GetFloat(d, "shieldRegenInterval", 8f);
                c.frenzyThreshold = MiniJson.GetFloat(d, "frenzyThreshold", 0.5f);
                c.frenzySpeedMultiplier = MiniJson.GetFloat(d, "frenzySpeedMultiplier", 1.5f);
                c.frenzyAttackMultiplier = MiniJson.GetFloat(d, "frenzyAttackMultiplier", 1.5f);
                c.visualScale = MiniJson.GetFloat(d, "visualScale", 0.7f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<WaveConfig> ReadWaves(Dictionary<string, object> root, List<WaveConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "waves");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<WaveConfig> list = new List<WaveConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                WaveConfig w = new WaveConfig();
                w.index = MiniJson.GetInt(d, "index", i + 1);
                w.label = MiniJson.GetString(d, "label", w.index + " 波");
                w.prepTime = MiniJson.GetFloat(d, "prepTime", 6f);
                w.hpMultiplier = MiniJson.GetFloat(d, "hpMultiplier", 1f);
                w.speedMultiplier = MiniJson.GetFloat(d, "speedMultiplier", 1f);
                w.clearBonus = MiniJson.GetInt(d, "clearBonus", 20);

                List<object> groups = MiniJson.GetList(d, "groups");
                if (groups != null)
                {
                    for (int j = 0; j < groups.Count; j++)
                    {
                        Dictionary<string, object> gd = MiniJson.AsDict(groups[j]);
                        if (gd == null)
                        {
                            continue;
                        }
                        WaveGroup g = new WaveGroup();
                        g.kind = ParseEnum<EnemyKind>(MiniJson.GetString(gd, "kind", null), EnemyKind.Minion);
                        g.count = Math.Max(1, MiniJson.GetInt(gd, "count", 1));
                        g.interval = MiniJson.GetFloat(gd, "interval", 1f);
                        g.pathIndex = MiniJson.GetInt(gd, "pathIndex", 0);
                        g.startDelay = MiniJson.GetFloat(gd, "startDelay", 0f);
                        w.groups.Add(g);
                    }
                }
                if (w.groups.Count > 0)
                {
                    list.Add(w);
                }
            }
            return list.Count > 0 ? list : fallback;
        }

        private static List<DifficultyConfig> ReadDifficulties(Dictionary<string, object> root, List<DifficultyConfig> fallback)
        {
            List<object> arr = MiniJson.GetList(root, "difficulties");
            if (arr == null || arr.Count == 0)
            {
                return fallback;
            }
            List<DifficultyConfig> list = new List<DifficultyConfig>();
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> d = MiniJson.AsDict(arr[i]);
                if (d == null)
                {
                    continue;
                }
                DifficultyConfig c = new DifficultyConfig();
                c.difficulty = ParseEnum<Difficulty>(MiniJson.GetString(d, "difficulty", null), (Difficulty)i);
                c.displayName = MiniJson.GetString(d, "displayName", "难度" + i);
                c.description = MiniJson.GetString(d, "description", "");
                c.waveCount = Math.Max(1, MiniJson.GetInt(d, "waveCount", 10));
                c.enemyHpMultiplier = MiniJson.GetFloat(d, "enemyHpMultiplier", 1f);
                c.enemySpeedMultiplier = MiniJson.GetFloat(d, "enemySpeedMultiplier", 1f);
                c.spiritGainMultiplier = MiniJson.GetFloat(d, "spiritGainMultiplier", 1f);
                c.startSpirit = MiniJson.GetFloat(d, "startSpirit", 100f);
                c.spiritRegenPerSecond = MiniJson.GetFloat(d, "spiritRegenPerSecond", 1f);
                c.coreHp = MiniJson.GetFloat(d, "coreHp", 500f);
                list.Add(c);
            }
            return list.Count > 0 ? list : fallback;
        }

        // ------------------------------------------------------------ 导出

        /// <summary>
        /// 把当前配置导出成 JSON 文本。
        /// tools/CoreTests 里的 --export-config 就是用它生成 Resources/Config/game_config.json，
        /// 从而保证「代码里的默认数值」和「JSON 里的数值」始终一致。
        /// </summary>
        public static string Export(GameDatabase db)
        {
            Dictionary<string, object> root = new Dictionary<string, object>();
            root["_comment"] = "《仙侠·护山大阵》数值配置表。改这里即可调整平衡性，无需重新编译。删除本文件则使用代码内置默认值。";
            root["version"] = db.version;
            root["gameName"] = db.gameName;
            root["columns"] = db.columns;
            root["rows"] = db.rows;
            root["pathA"] = ExportPath(db.pathA);
            root["pathB"] = ExportPath(db.pathB);
            root["coreCells"] = ExportPath(db.coreCells);

            List<object> towers = new List<object>();
            for (int i = 0; i < db.towers.Count; i++)
            {
                TowerConfig c = db.towers[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["kind"] = c.kind.ToString();
                d["id"] = c.id;
                d["displayName"] = c.displayName;
                d["description"] = c.description;
                d["sprite"] = c.sprite;
                d["cost"] = c.cost;
                d["upgradeCostBase"] = c.upgradeCostBase;
                d["maxLevel"] = c.maxLevel;
                d["hp"] = c.hp;
                d["damage"] = c.damage;
                d["attackInterval"] = c.attackInterval;
                d["range"] = c.range;
                d["spiritPerSecond"] = c.spiritPerSecond;
                d["slowFactor"] = c.slowFactor;
                d["dodgeChance"] = c.dodgeChance;
                d["shieldPool"] = c.shieldPool;
                d["shieldRegen"] = c.shieldRegen;
                d["projectileSpeed"] = c.projectileSpeed;
                d["splashRadius"] = c.splashRadius;
                towers.Add(d);
            }
            root["towers"] = towers;

            List<object> spirits = new List<object>();
            for (int i = 0; i < db.spirits.Count; i++)
            {
                SpiritConfig c = db.spirits[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["kind"] = c.kind.ToString();
                d["id"] = c.id;
                d["displayName"] = c.displayName;
                d["description"] = c.description;
                d["sprite"] = c.sprite;
                d["cost"] = c.cost;
                d["hp"] = c.hp;
                d["damage"] = c.damage;
                d["attackInterval"] = c.attackInterval;
                d["range"] = c.range;
                d["duration"] = c.duration;
                d["spiritPerSecond"] = c.spiritPerSecond;
                d["coreRepairPerSecond"] = c.coreRepairPerSecond;
                d["splash"] = c.splash;
                d["splashRadius"] = c.splashRadius;
                d["projectileSpeed"] = c.projectileSpeed;
                spirits.Add(d);
            }
            root["spirits"] = spirits;

            List<object> traps = new List<object>();
            for (int i = 0; i < db.traps.Count; i++)
            {
                TrapConfig c = db.traps[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["kind"] = c.kind.ToString();
                d["id"] = c.id;
                d["displayName"] = c.displayName;
                d["description"] = c.description;
                d["sprite"] = c.sprite;
                d["cost"] = c.cost;
                d["damage"] = c.damage;
                d["radius"] = c.radius;
                d["slowFactor"] = c.slowFactor;
                d["slowDuration"] = c.slowDuration;
                traps.Add(d);
            }
            root["traps"] = traps;

            List<object> skills = new List<object>();
            for (int i = 0; i < db.skills.Count; i++)
            {
                SkillConfig c = db.skills[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["kind"] = c.kind.ToString();
                d["id"] = c.id;
                d["displayName"] = c.displayName;
                d["description"] = c.description;
                d["sprite"] = c.sprite;
                d["cost"] = c.cost;
                d["cooldown"] = c.cooldown;
                d["damage"] = c.damage;
                d["duration"] = c.duration;
                d["coreRepairPercent"] = c.coreRepairPercent;
                skills.Add(d);
            }
            root["skills"] = skills;

            List<object> enemies = new List<object>();
            for (int i = 0; i < db.enemies.Count; i++)
            {
                EnemyConfig c = db.enemies[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["kind"] = c.kind.ToString();
                d["id"] = c.id;
                d["displayName"] = c.displayName;
                d["sprite"] = c.sprite;
                d["hp"] = c.hp;
                d["speed"] = c.speed;
                d["armor"] = c.armor;
                d["reward"] = c.reward;
                d["coreDamage"] = c.coreDamage;
                d["attackDamage"] = c.attackDamage;
                d["attackInterval"] = c.attackInterval;
                d["attackRange"] = c.attackRange;
                d["ability"] = c.ability.ToString();
                d["shieldAmount"] = c.shieldAmount;
                d["shieldRegenInterval"] = c.shieldRegenInterval;
                d["frenzyThreshold"] = c.frenzyThreshold;
                d["frenzySpeedMultiplier"] = c.frenzySpeedMultiplier;
                d["frenzyAttackMultiplier"] = c.frenzyAttackMultiplier;
                d["visualScale"] = c.visualScale;
                enemies.Add(d);
            }
            root["enemies"] = enemies;

            List<object> waves = new List<object>();
            for (int i = 0; i < db.waves.Count; i++)
            {
                WaveConfig w = db.waves[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["index"] = w.index;
                d["label"] = w.label;
                d["prepTime"] = w.prepTime;
                d["hpMultiplier"] = w.hpMultiplier;
                d["speedMultiplier"] = w.speedMultiplier;
                d["clearBonus"] = w.clearBonus;
                List<object> groups = new List<object>();
                for (int j = 0; j < w.groups.Count; j++)
                {
                    WaveGroup g = w.groups[j];
                    Dictionary<string, object> gd = new Dictionary<string, object>();
                    gd["kind"] = g.kind.ToString();
                    gd["count"] = g.count;
                    gd["interval"] = g.interval;
                    gd["pathIndex"] = g.pathIndex;
                    gd["startDelay"] = g.startDelay;
                    groups.Add(gd);
                }
                d["groups"] = groups;
                waves.Add(d);
            }
            root["waves"] = waves;

            List<object> difficulties = new List<object>();
            for (int i = 0; i < db.difficulties.Count; i++)
            {
                DifficultyConfig c = db.difficulties[i];
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["difficulty"] = c.difficulty.ToString();
                d["displayName"] = c.displayName;
                d["description"] = c.description;
                d["waveCount"] = c.waveCount;
                d["enemyHpMultiplier"] = c.enemyHpMultiplier;
                d["enemySpeedMultiplier"] = c.enemySpeedMultiplier;
                d["spiritGainMultiplier"] = c.spiritGainMultiplier;
                d["startSpirit"] = c.startSpirit;
                d["spiritRegenPerSecond"] = c.spiritRegenPerSecond;
                d["coreHp"] = c.coreHp;
                difficulties.Add(d);
            }
            root["difficulties"] = difficulties;

            return MiniJson.Serialize(root);
        }

        private static List<object> ExportPath(List<GridPos> path)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < path.Count; i++)
            {
                List<object> pair = new List<object>();
                pair.Add(path[i].x);
                pair.Add(path[i].y);
                list.Add(pair);
            }
            return list;
        }

        // ------------------------------------------------------------ 工具

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

        private static T ParseEnum<T>(string s, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(s))
            {
                return fallback;
            }
            try
            {
                if (Enum.IsDefined(typeof(T), s))
                {
                    return (T)Enum.Parse(typeof(T), s, false);
                }
            }
            catch (Exception)
            {
                LastWarnings.Add("无法识别的枚举值：" + s);
            }
            return fallback;
        }
    }
}
