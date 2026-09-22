// ============================================================================
//  UnityEditor API 桩
// ============================================================================
//
//  和 UnityStubs.cs 一样，只是为了让 Assets/Editor/ProjectBootstrap.cs
//  在没有 Unity 的机器上也能被编译检查。签名对齐 Unity 2022 LTS。
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethod : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoad : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditor : Attribute
    {
        public CustomEditor(Type inspectedType) { }
    }

    public delegate void CallbackFunction();

    public static class EditorApplication
    {
        public static CallbackFunction delayCall;
        public static CallbackFunction update;
        public static bool isPlaying { get { return false; } }
        public static bool isPlayingOrWillChangePlaymode { get { return false; } }
        // <Unity安装目录>/Editor/Data
        public static string applicationContentsPath { get { return ""; } }
        // <Unity安装目录>/Editor/Unity.exe
        public static string applicationPath { get { return ""; } }
        public static void ExecuteMenuItem(string menuItemPath) { }
    }

    public static class EditorPrefs
    {
        public static string GetString(string key, string defaultValue) { return defaultValue; }
        public static void SetString(string key, string value) { }
        public static bool GetBool(string key, bool defaultValue) { return defaultValue; }
        public static void SetBool(string key, bool value) { }
        public static int GetInt(string key, int defaultValue) { return defaultValue; }
        public static void SetInt(string key, int value) { }
        public static void DeleteKey(string key) { }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) { return true; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return true; }
        public static void SetDirty(UnityEngine.Object target) { }
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static void ClearProgressBar() { }
    }

    public enum ImportAssetOptions
    {
        Default = 0, ForceUpdate = 1, ForceSynchronousImport = 8,
        ImportRecursive = 256, DontDownloadFromCacheServer = 8192, ForceUncompressedImport = 16384
    }

    public static class AssetDatabase
    {
        public static string[] FindAssets(string filter) { return new string[0]; }
        public static string[] FindAssets(string filter, string[] searchInFolders) { return new string[0]; }
        public static string GUIDToAssetPath(string guid) { return ""; }
        public static string AssetPathToGUID(string path) { return ""; }
        public static void ImportAsset(string path) { }
        public static void ImportAsset(string path, ImportAssetOptions options) { }
        public static void Refresh() { }
        public static void SaveAssets() { }
        public static bool DeleteAsset(string path) { return true; }
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object { return null; }
    }

    public class AssetImporter : UnityEngine.Object
    {
        public string assetPath { get; set; }
        public string userData { get; set; }
        public static AssetImporter GetAtPath(string path) { return null; }
    }

    public class AssetPostprocessor
    {
        public string assetPath { get { return ""; } }
        public AssetImporter assetImporter { get { return null; } }
    }

    public enum TextureImporterType
    {
        Default = 0, NormalMap = 1, GUI = 2, Sprite = 8, Cursor = 7, Cookie = 4,
        Lightmap = 6, SingleChannel = 10, Shadowmask = 11, DirectionalLightmap = 12
    }

    public enum SpriteImportMode { None = 0, Single = 1, Multiple = 2, Polygon = 3 }

    public enum TextureImporterCompression
    {
        Uncompressed = 0, Compressed = 1, CompressedHQ = 2, CompressedLQ = 3
    }

    public enum TextureImporterNPOTScale { None = 0, ToNearest = 1, ToLarger = 2, ToSmaller = 3 }

    public enum SpriteMeshTypeEditor { FullRect = 0, Tight = 1 }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public SpriteImportMode spriteImportMode { get; set; }
        public float spritePixelsPerUnit { get; set; }
        public Vector4 spriteBorder { get; set; }
        public bool mipmapEnabled { get; set; }
        public bool alphaIsTransparency { get; set; }
        public bool isReadable { get; set; }
        public bool crunchedCompression { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int maxTextureSize { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public TextureImporterNPOTScale npotScale { get; set; }
        public bool sRGBTexture { get; set; }
        public void SetPlatformTextureSettings(TextureImporterPlatformSettings platformSettings) { }
    }

    public struct TextureImporterPlatformSettings
    {
        public string name;
        public bool overridden;
        public int maxTextureSize;
        public TextureImporterCompression format;
    }

    public struct AudioImporterSampleSettings
    {
        public AudioClipLoadType loadType;
        public AudioCompressionFormat compressionFormat;
        public float quality;
        public int sampleRateSetting;
        public int sampleRateOverride;
        public int conversionMode;
    }

    public class AudioImporter : AssetImporter
    {
        public AudioImporterSampleSettings defaultSampleSettings { get; set; }
        public bool preloadAudioData { get; set; }
        public bool forceToMono { get; set; }
        public bool loadInBackground { get; set; }
    }

    public enum UIOrientation
    {
        Portrait = 0, PortraitUpsideDown = 1, LandscapeRight = 2,
        LandscapeLeft = 3, AutoRotation = 4
    }

    public enum ScriptingImplementation { Mono2x = 0, IL2CPP = 1, WinRTDotNET = 2 }

    public enum ApiCompatibilityLevel
    {
        NET_2_0 = 1, NET_2_0_Subset = 2, NET_4_6 = 3, NET_Web = 4,
        NET_Micro = 5, NET_Standard_2_0 = 6, NET_Unity_4_8 = 7
    }

    public enum AndroidSdkVersions
    {
        AndroidApiLevelAuto = 0,
        AndroidApiLevel22 = 22, AndroidApiLevel23 = 23, AndroidApiLevel24 = 24,
        AndroidApiLevel25 = 25, AndroidApiLevel26 = 26, AndroidApiLevel27 = 27,
        AndroidApiLevel28 = 28, AndroidApiLevel29 = 29, AndroidApiLevel30 = 30,
        AndroidApiLevel31 = 31, AndroidApiLevel32 = 32, AndroidApiLevel33 = 33,
        AndroidApiLevel34 = 34, AndroidApiLevel35 = 35
    }

    [Flags]
    public enum AndroidArchitecture
    {
        None = 0, ARMv7 = 1, ARM64 = 2, X86 = 4, X86_64 = 8, All = 15
    }

    public enum AndroidBlitType { Always = 0, Never = 1, Auto = 2 }

    public enum AndroidSplashScreenScale { Center = 0, ScaleToFill = 1, ScaleToFit = 2 }

    public enum MobileTextureSubtarget { Generic = 0, DXT = 1, PVRTC = 2, ETC = 4, ETC2 = 5, ASTC = 6 }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }
        public static string applicationIdentifier { get; set; }

        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
        public static bool useAnimatedAutorotation { get; set; }

        public static ColorSpace colorSpace { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool defaultIsNativeResolution { get; set; }
        public static bool runInBackground { get; set; }
        public static bool resizableWindow { get; set; }

        public static void SetApplicationIdentifier(Build.NamedBuildTarget buildTarget, string identifier) { }
        public static void SetScriptingBackend(Build.NamedBuildTarget buildTarget, ScriptingImplementation backend) { }
        public static void SetApiCompatibilityLevel(Build.NamedBuildTarget buildTarget, ApiCompatibilityLevel level) { }
        public static void SetIl2CppCompilerConfiguration(Build.NamedBuildTarget buildTarget, int configuration) { }

        public static class Android
        {
            public static int bundleVersionCode { get; set; }
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static bool forceInternetPermission { get; set; }
            public static bool forceSDCardPermission { get; set; }
            public static bool androidIsGame { get; set; }
            public static AndroidBlitType blitType { get; set; }
            public static bool useAPKExpansionFiles { get; set; }
            public static MobileTextureSubtarget androidBuildSubtarget { get; set; }
        }

        public static class SplashScreen
        {
            public static bool show { get; set; }
            public static bool showUnityLogo { get; set; }
            public static AndroidSplashScreenScale androidSplashScreenScale { get; set; }
        }
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene() { }
        public EditorBuildSettingsScene(string path, bool enabled) { }
        public string path { get; set; }
        public bool enabled { get; set; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }

    public static class Selection
    {
        public static UnityEngine.Object activeObject { get; set; }
    }
}

namespace UnityEditor.Build
{
    public struct NamedBuildTarget
    {
        public string TargetName { get { return ""; } }
        public static NamedBuildTarget Unknown { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Standalone { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Android { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget iOS { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget WebGL { get { return default(NamedBuildTarget); } }
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene = 0, DefaultGameObjects = 1 }

    public enum NewSceneMode { Single = 0, Additive = 1 }

    public enum OpenSceneMode { Single = 0, Additive = 1, AdditiveWithoutLoading = 2 }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup) { return default(Scene); }
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return default(Scene); }
        public static Scene OpenScene(string scenePath) { return default(Scene); }
        public static Scene OpenScene(string scenePath, OpenSceneMode mode) { return default(Scene); }
        public static bool SaveScene(Scene scene) { return true; }
        public static bool SaveScene(Scene scene, string dstScenePath) { return true; }
        public static bool SaveScene(Scene scene, string dstScenePath, bool saveAsCopy) { return true; }
        public static bool SaveOpenScenes() { return true; }
        public static void MarkSceneDirty(Scene scene) { }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
    }
}
