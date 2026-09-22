using System;
using System.IO;
using System.Reflection;
using System.Text;
using MountainWardBarrier.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MountainWardBarrier.EditorTools
{
    /// <summary>
    /// 工程自动配置。
    ///
    /// 这个工程刻意**不提交** ProjectSettings 与 .meta —— 那些二进制/机器生成的字段
    /// 在 git 上只会带来冲突。取而代之：所有必须的设置都写在这里，第一次打开工程时自动跑一遍，
    /// 也可以随时从菜单「仙侠·护山大阵 / 一键配置工程」手动重跑。
    ///
    /// 它一共做四件事：
    ///   1. 生成主场景 Assets/Scenes/Main.unity 并放进 Build Settings
    ///   2. 设置包名、竖屏、最低 SDK、ARM64、关闭联网权限等玩家设置
    ///   3. 设定色彩空间与图形设置
    ///   4. 顺手把 Resources/Sprites 下的贴图按 Sprite 重新导入（九宫格面板会带上 border）
    /// </summary>
    public static class ProjectBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string SetupKey = "mwb.project_setup_version";
        private const string SetupVersion = "1.0.0";

        public static readonly string CompanyName = "QiaoQiao";
        public static readonly string ProductName = "仙侠·护山大阵";
        public static readonly string BundleId = "com.qiaoqiao.mountainwardbarrier";
        public static readonly string BundleVersion = "1.0.0";

        // ============================================================ 自动执行

        [InitializeOnLoadMethod]
        private static void AutoSetup()
        {
            // 每个工程路径记一个"配置版本"，升级版本号即可让所有人下次打开时自动重跑。
            string key = SetupKey + "." + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetString(key, "") == SetupVersion)
            {
                return;
            }
            EditorApplication.delayCall += delegate
            {
                try
                {
                    Configure(false);
                    // 顺手把 Android 打包要用的 SDK / NDK / JDK 路径接上，
                    // 省得第一次打包时还要去 Preferences → External Tools 手填三条。
                    ApplyAndroidPaths();
                    EditorPrefs.SetString(key, SetupVersion);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ProjectBootstrap] 自动配置失败，可从菜单手动执行：" + e.Message);
                }
            };
        }

        [MenuItem("仙侠·护山大阵/一键配置工程", false, 1)]
        public static void ConfigureFromMenu()
        {
            Configure(true);
        }

        // ============================================================ Android 打包路径

        /// <summary>
        /// 把 Android 打包要用的三条外部工具路径（SDK / NDK / JDK）一次性填好，
        /// 省得在 Preferences → External Tools 里手点三遍。
        ///
        /// 只写"确实存在的目录"，探测不到的保持原样不覆盖。两条写入通道都试一遍：
        ///   · EditorPrefs 的 AndroidSdkRoot / AndroidNdkRoot / JdkPath
        ///   · 内部类 AndroidExternalToolsSettings 的静态属性（不同小版本存储位置有差异）
        /// 两条都是"能写则写、写不了就跳过"，不会因为某个版本改了内部结构而报错。
        /// </summary>
        [MenuItem("仙侠·护山大阵/配置 Android 打包路径", false, 2)]
        public static void ConfigureAndroidPaths()
        {
            string text = ApplyAndroidPaths();
            EditorUtility.DisplayDialog("仙侠·护山大阵", text, "好");
        }

        /// <summary>
        /// 探测并把 SDK / NDK / JDK 三条路径接上，返回给人看的摘要。
        /// 拆出这个方法，是为了让首次打开工程时的自动配置走同一条逻辑，
        /// 又不必弹出对话框打断用户。
        /// </summary>
        internal static string ApplyAndroidPaths()
        {
            // applicationContentsPath 指向 <Unity安装目录>/Editor/Data
            string androidPlayer = Path.Combine(
                EditorApplication.applicationContentsPath,
                Path.Combine("PlaybackEngines", "AndroidPlayer"));

            string sdk = FirstExisting(new string[]
            {
                Path.Combine(androidPlayer, "SDK"),
                @"D:\App\Android\Sdk",
                @"C:\Android\Sdk",
            });
            string ndk = FirstExisting(new string[]
            {
                Path.Combine(androidPlayer, "NDK"),
            });
            string jdk = FirstExisting(new string[]
            {
                Path.Combine(androidPlayer, "OpenJDK"),
            });

            ApplyAndroidToolPath("AndroidSdkRoot", "sdkRootPath", sdk);
            ApplyAndroidToolPath("AndroidNdkRoot", "ndkRootPath", ndk);
            ApplyAndroidToolPath("JdkPath", "jdkRootPath", jdk);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Android 打包路径：");
            sb.AppendLine();
            sb.AppendLine("SDK：" + DescribePath(sdk));
            sb.AppendLine("NDK：" + DescribePath(ndk));
            sb.AppendLine("JDK：" + DescribePath(jdk));
            sb.AppendLine();

            bool complete = sdk != null && ndk != null && jdk != null;
            if (complete)
            {
                sb.AppendLine("三条都已接通，可以直接 File → Build Settings → Android → Build。");
            }
            else
            {
                sb.AppendLine("有路径没找到，先按上面的清单补装，");
                sb.AppendLine("或到 Edit → Preferences → External Tools 手动指定。");
            }

            string text = sb.ToString();
            Debug.Log("[ProjectBootstrap] " + text.Replace("\r\n", "  ").Replace("\n", "  "));
            return text;
        }

        private static void ApplyAndroidToolPath(string prefKey, string propName, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // 通道一：EditorPrefs（Unity 存外部工具路径的底层键）
            try
            {
                EditorPrefs.SetString(prefKey, path);
            }
            catch (Exception)
            {
                // 个别版本会拒绝，忽略即可
            }

            // 通道二：内部 API。类型/属性名在不同小版本可能改，所以全程反射 + 静默失败。
            try
            {
                Type settings = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    settings = assemblies[i].GetType(
                        "UnityEditor.Android.AndroidExternalToolsSettings", false);
                    if (settings != null)
                    {
                        break;
                    }
                }
                if (settings == null)
                {
                    return;
                }
                PropertyInfo prop = settings.GetProperty(
                    propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(null, path, null);
                }
            }
            catch (Exception)
            {
                // 走不通就只靠 EditorPrefs 那条通道
            }
        }

        private static string FirstExisting(string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (Directory.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }
            return null;
        }

        private static string DescribePath(string path)
        {
            return string.IsNullOrEmpty(path) ? "（未找到）" : path;
        }

        [MenuItem("仙侠·护山大阵/重新导入全部美术资源", false, 20)]
        public static void ReimportArt()
        {
            string[] folders = new string[] { "Assets/Resources/Sprites" };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
            Debug.Log("[ProjectBootstrap] 已重新导入 " + guids.Length + " 张贴图。");
        }

        [MenuItem("仙侠·护山大阵/打开主场景", false, 21)]
        public static void OpenMainScene()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        // ============================================================ 主流程

        private static void Configure(bool verbose)
        {
            ApplyPlayerSettings();
            ApplyGraphicsSettings();
            EnsureScene();
            ReimportArt();

            if (verbose)
            {
                Debug.Log("[ProjectBootstrap] 工程配置完成：场景、" + ProductName
                    + " 的包名与竖屏设置、贴图导入规则均已就绪。");
                EditorUtility.DisplayDialog("仙侠·护山大阵",
                    "工程已配置完成。\n\n"
                    + "· 主场景：Assets/Scenes/Main.unity（已加入 Build Settings）\n"
                    + "· 直接点 Play 即可开始游戏\n"
                    + "· 打包 Android：File → Build Settings → Android → Build", "好");
            }
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = BundleVersion;

            // 竖屏单机塔防：锁竖屏，不跟随设备旋转
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            ApplyAndroidSettings();
        }

        private static void ApplyAndroidSettings()
        {
            try
            {
                NamedBuildTarget android = NamedBuildTarget.Android;
                PlayerSettings.SetApplicationIdentifier(android, BundleId);
                PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard_2_0);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProjectBootstrap] 设置 Android 标识失败：" + e.Message);
            }

            try
            {
                PlayerSettings.Android.bundleVersionCode = 1;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                // 只出 ARM64：Google Play 要求，也能顺手把包体压下来
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                // 游戏是纯单机，明确不要联网与存储权限 —— 这是"零联网请求"的一部分
                PlayerSettings.Android.forceInternetPermission = false;
                PlayerSettings.Android.forceSDCardPermission = false;
                PlayerSettings.Android.androidIsGame = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProjectBootstrap] 设置 Android 参数失败：" + e.Message);
            }

            try
            {
                PlayerSettings.Android.blitType = AndroidBlitType.Never;
            }
            catch (Exception)
            {
                // 某些版本没有这个属性，忽略
            }
        }

        private static void ApplyGraphicsSettings()
        {
            // 素材是按 Gamma 空间画的，保持 Gamma 才能让颜色和 tools/ 里生成的一致
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
        }

        /// <summary>生成主场景。场景里只需要一个挂着 GameBootstrap 的空对象 —— 界面全是代码搭的。</summary>
        private static void EnsureScene()
        {
            if (File.Exists(ScenePath))
            {
                AddSceneToBuild();
                return;
            }
            string dir = Path.GetDirectoryName(Path.GetFullPath(ScenePath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject go = new GameObject("MountainWardBarrier");
            go.AddComponent<GameBootstrap>();

            // 相机由 GameBootstrap 在运行时自建（它会顺手补上 AudioListener），
            // 但编辑器里没有相机就没有 Game 视图，这里先放一个占位的并关掉渲染。
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.1f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            AddSceneToBuild();
            Debug.Log("[ProjectBootstrap] 已生成主场景：" + ScenePath);
        }

        private static void AddSceneToBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                    }
                    return;
                }
            }
            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[scenes.Length + 1];
            Array.Copy(scenes, next, scenes.Length);
            next[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = next;
        }
    }

    /// <summary>
    /// 贴图导入规则。
    ///
    /// tools/ 生成的是普通 PNG，导入时若按默认的 Texture 处理，代码里就只能靠
    /// SpriteLibrary 的 Texture2D 兜底路径拿到图 —— 能跑，但费一层。这里直接把
    /// Resources/Sprites 下的所有贴图钉成 Sprite，并给九宫格面板写上 border。
    /// </summary>
    public class SpriteImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Resources/Sprites/", StringComparison.Ordinal) < 0)
            {
                return;
            }
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            string file = Path.GetFileNameWithoutExtension(assetPath);
            if (file == "ui_panel" || file == "ui_panel_dark")
            {
                // 96x96、圆角 26 —— 留 28 的边框给 Sliced 拉伸，圆角不会被拉变形
                importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
            }
            else if (file == "ui_bar_bg" || file == "ui_bar_fill")
            {
                // 64x24 的胶囊条，左右各留 12
                importer.spriteBorder = new Vector4(12f, 12f, 12f, 12f);
            }
        }

        private void OnPreprocessAudio()
        {
            if (assetPath.IndexOf("/Resources/Audio/", StringComparison.Ordinal) < 0)
            {
                return;
            }
            AudioImporter importer = assetImporter as AudioImporter;
            if (importer == null)
            {
                return;
            }
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = assetPath.IndexOf("/BGM/", StringComparison.Ordinal) >= 0
                ? AudioClipLoadType.CompressedInMemory
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.8f;
            importer.defaultSampleSettings = settings;
            importer.preloadAudioData = true;
            importer.forceToMono = false;
        }
    }
}
