using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MountainWardBarrier.EditorTools
{
    /// <summary>
    /// Android APK 打包入口。
    ///
    /// 共用一个 Build()，两个触发方式：
    ///   · 菜单「仙侠·护山大阵 / 打包 Android APK」—— 给人点
    ///   · 命令行 -executeMethod ...BuildFromCommandLine —— 给 tools/build_apk.bat 调
    ///
    /// 命令行那条路是重点：打包要跑十几分钟，放在界面上点容易点到一半去干别的，
    /// 出错了也不好把日志发出来。批处理跑完直接落一份完整日志。
    /// </summary>
    public static class AndroidBuilder
    {
        public const string OutputDir = "Build/Android";
        public const string ApkName = "MountainWardBarrier.apk";

        // ============================================================ 入口

        [MenuItem("仙侠·护山大阵/打包 Android APK", false, 40)]
        public static void BuildFromMenu()
        {
            string apk = Build();
            if (!string.IsNullOrEmpty(apk))
            {
                EditorUtility.RevealInFinder(apk);
            }
        }

        /// <summary>
        /// 命令行入口。Unity 用 -executeMethod 调用它时会忽略返回值，
        /// 所以要靠 EditorApplication.Exit 把成败传回给批处理，否则脚本永远"成功"。
        /// </summary>
        public static void BuildFromCommandLine()
        {
            int exitCode = 0;
            try
            {
                Build();
            }
            catch (Exception e)
            {
                Debug.LogError("[AndroidBuilder] 打包失败：" + e);
                exitCode = 1;
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
            else if (exitCode != 0)
            {
                EditorUtility.DisplayDialog("打包失败", "详情看 Console。", "好");
            }
        }

        // ============================================================ 主流程

        public static string Build()
        {
            Debug.Log("[AndroidBuilder] ===== 开始打包 Android =====");

            // 批处理模式下 delayCall 不保证执行，这里显式把该配的都配一遍
            ProjectBootstrap.EnsureConfigured();

            EnsureAndroidBuildTarget();
            EnsureApkNotBundle();

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), OutputDir);
            Directory.CreateDirectory(outputDir);
            string apkPath = Path.Combine(outputDir, ApkName);

            // 上一次的产物先删掉：留着的话万一这次构建失败，
            // 磁盘上那个旧的 APK 会让人误以为"打包成功了"。
            if (File.Exists(apkPath))
            {
                File.Delete(apkPath);
            }

            string[] scenes = CollectEnabledScenes();
            Debug.Log("[AndroidBuilder] 打包场景：" + string.Join(", ", scenes));
            Debug.Log("[AndroidBuilder] 输出：" + apkPath);

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes;
            options.locationPathName = apkPath;
            options.target = BuildTarget.Android;
            options.targetGroup = BuildTargetGroup.Android;
            options.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("BuildPipeline 返回 " + summary.result
                    + "，请往上翻日志里第一条 error。");
            }

            double mb = summary.totalSize / 1024.0 / 1024.0;
            Debug.Log("[AndroidBuilder] ===== 打包成功 =====");
            Debug.Log("[AndroidBuilder] APK：" + apkPath);
            Debug.Log("[AndroidBuilder] 体积：" + mb.ToString("0.0") + " MB");
            Debug.Log("[AndroidBuilder] 装到手机：adb install -r \"" + apkPath + "\"");
            return apkPath;
        }

        // ============================================================ 前置检查

        /// <summary>
        /// 切到 Android 平台。工程刚 clone 下来时 activeBuildTarget 是 StandaloneWindows，
        /// 不切的话 BuildPipeline 会直接报"目标平台不支持"。
        /// </summary>
        private static void EnsureAndroidBuildTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                return;
            }

            Debug.Log("[AndroidBuilder] 当前平台是 " + EditorUserBuildSettings.activeBuildTarget
                + "，切换到 Android …");
            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);
            if (!ok)
            {
                throw new Exception("切换 Android 平台失败。多半是 Android 模块没装全，"
                    + "到 Edit → Preferences → External Tools 检查 SDK / NDK / JDK 三条路径。");
            }
        }

        /// <summary>
        /// 确保出的是 .apk 而不是 .aab。
        /// Google Play 要 aab，但我们要的是能直接装到手机上的包，所以必须显式关掉。
        /// </summary>
        private static void EnsureApkNotBundle()
        {
            if (EditorUserBuildSettings.buildAppBundle)
            {
                Debug.Log("[AndroidBuilder] 关掉 buildAppBundle，改为输出 APK。");
                EditorUserBuildSettings.buildAppBundle = false;
            }
        }

        private static string[] CollectEnabledScenes()
        {
            EditorBuildSettingsScene[] all = EditorBuildSettings.scenes;
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].enabled)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                throw new Exception("Build Settings 里一个启用的场景都没有，"
                    + "先从菜单跑一次「一键配置工程」。");
            }

            string[] result = new string[count];
            int index = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].enabled)
                {
                    result[index++] = all[i].path;
                }
            }
            return result;
        }
    }
}
