using System;
using System.IO;
using System.Text;
using MountainWardBarrier.Core;
using UnityEngine;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 本地存档。存到 PlayerPrefs 的一个字符串里（内容是 MiniJson 序列化后的存档对象）。
    ///
    /// 为什么不用 JsonUtility：存档模型在纯逻辑层（Core/SaveData.cs），
    /// 那边不依赖 UnityEngine，用自己写的 MiniJson 可以做往返测试。
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveKey = "mwb_save_v1";
        private const string TelemetryFile = "telemetry.jsonl";
        private const int TelemetryMaxBytes = 512 * 1024;

        private static SaveData _data;

        public static SaveData Data
        {
            get
            {
                if (_data == null)
                {
                    Load();
                }
                return _data;
            }
        }

        public static void Load()
        {
            string raw = null;
            try
            {
                raw = PlayerPrefs.GetString(SaveKey, "");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] 读取存档失败：" + e.Message);
            }
            _data = SaveData.FromJson(raw);

            if (AudioLibrary.Instance != null)
            {
                AudioLibrary.Instance.ApplySettings(_data.musicEnabled, _data.sfxEnabled,
                    _data.musicVolume, _data.sfxVolume);
            }
        }

        public static void Save()
        {
            if (_data == null)
            {
                return;
            }
            try
            {
                PlayerPrefs.SetString(SaveKey, _data.ToJson());
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] 写入存档失败：" + e.Message);
            }
        }

        /// <summary>清空存档（设置页的"抹除山门记录"）。</summary>
        public static void ResetAll()
        {
            _data = new SaveData();
            try
            {
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] 清空存档失败：" + e.Message);
            }
        }

        // ------------------------------------------------------------ 埋点落盘

        /// <summary>
        /// 把一局对局的埋点追加到本地文件（JSONL，一行一个事件）。
        /// 纯单机游戏没有服务器，埋点的意义是「自己回看哪一波劝退、哪个阵法没人用」。
        /// 文件超过 512KB 就整份截断重写，避免无限增长。
        /// </summary>
        public static string FlushTelemetry(TelemetryCollector collector, BattleReport report)
        {
            if (collector == null)
            {
                return null;
            }
            try
            {
                if (report != null)
                {
                    collector.LogBattleEnd(report);
                }
                string path = Path.Combine(Application.persistentDataPath, TelemetryFile);
                if (File.Exists(path))
                {
                    FileInfo fi = new FileInfo(path);
                    if (fi.Length > TelemetryMaxBytes)
                    {
                        File.Delete(path);
                    }
                }
                File.AppendAllText(path, collector.ToJsonLines(), new UTF8Encoding(false));
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] 写入埋点失败：" + e.Message);
                return null;
            }
        }

        public static string TelemetryPath
        {
            get { return Path.Combine(Application.persistentDataPath, TelemetryFile); }
        }

        /// <summary>把这一局的结果并入存档，返回新解锁的成就下标。</summary>
        public static System.Collections.Generic.List<int> CommitReport(BattleReport report)
        {
            System.Collections.Generic.List<int> fresh = Data.ApplyReport(report);
            Save();
            return fresh;
        }
    }
}
