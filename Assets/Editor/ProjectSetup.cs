using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Batch-mode entry points for one-time project configuration. Invoked via
/// `Unity.exe -batchmode -projectPath ... -executeMethod ProjectSetup.X -quit`.
/// </summary>
public static class ProjectSetup
{
    private const string ProductName = "Golden Break";
    private const string CompanyName = "Golden Break";
    private static readonly string[] SceneNamesInBuildOrder = { "Boot", "MainMenu", "Gameplay" };

    public static void RunPhase0Setup()
    {
        ConfigurePlayerSettings();
        CreateFolderStructure();
        CreateScenes();
        Debug.Log("ProjectSetup: Phase 0 setup complete.");
    }

    public static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);

        // The grid (64 pooled SpriteRenderers, spec §7.5) and piece tray are
        // constructed/pooled at runtime rather than fully scene-authored, and
        // later phases add uGUI screens (Canvas/Image/ScrollRect) built the
        // same way. Unity's "Strip Engine Code" pass only keeps native engine
        // classes it can prove are referenced by something in a scene or
        // asset at build time — runtime-only construction is invisible to it
        // and gets stripped, producing a build that launches but renders
        // nothing. Disabling it up front avoids diagnosing that blind on a
        // device later.
        PlayerSettings.stripEngineCode = false;

        AssetDatabase.SaveAssets();
        Debug.Log("ProjectSetup: Player Settings configured successfully.");
    }

    public static void CreateScenes()
    {
        var scenePaths = new List<string>();

        foreach (string sceneName in SceneNamesInBuildOrder)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            bool saved = EditorSceneManager.SaveScene(scene, path);
            if (!saved || !System.IO.File.Exists(path))
            {
                throw new System.IO.IOException($"ProjectSetup: failed to save scene at {path} — check that its parent folder exists.");
            }

            scenePaths.Add(path);
        }

        // AssetPathToGUID for a scene saved moments ago in this same batch
        // can still return an all-zero GUID until the AssetDatabase has
        // actually imported it — constructing EditorBuildSettingsScene
        // before this refresh silently wrote zero-GUID entries into
        // EditorBuildSettings.asset despite each .unity.meta file already
        // holding a correct one.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in scenePaths)
        {
            string guidString = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guidString) || !GUID.TryParse(guidString, out GUID guid))
            {
                throw new System.InvalidOperationException($"ProjectSetup: AssetDatabase could not resolve a GUID for {path} after refresh.");
            }

            // The (string path, bool enabled) constructor does not populate
            // this entry's GUID at all — it serializes as all zeros into
            // EditorBuildSettings.asset even though the scene's own .meta
            // file has a correct one. The (GUID, bool) overload is the only
            // one that actually writes a real GUID into the asset.
            buildScenes.Add(new EditorBuildSettingsScene(guid, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ProjectSetup: Scenes created and added to Build Settings in order: Boot, MainMenu, Gameplay.");
    }

    public static void CreateFolderStructure()
    {
        string[] folders =
        {
            "Assets/Scripts/Core",
            "Assets/Scripts/Scoring",
            "Assets/Scripts/Meta",
            "Assets/Scripts/Ads",
            "Assets/Scripts/Analytics",
            "Assets/Scripts/UI",
            "Assets/Scripts/Audio",
            "Assets/Scripts/Util",
            "Assets/Scenes",
            "Assets/Tests/EditMode",
            "Assets/Tests/PlayMode",
            "Assets/Prefabs/ParticleEffects",
            "Assets/Resources/PieceDefinitions",
            "Assets/Resources/CeramicDefinitions",
            "Assets/Resources/Palettes",
            "Assets/Resources/Audio",
            "Assets/Resources/Fonts",
            "Assets/Plugins",
        };

        foreach (string folder in folders)
        {
            EnsureFolderExists(folder);
        }

        AssetDatabase.Refresh();
        Debug.Log("ProjectSetup: Folder structure created.");
    }

    public static void BuildAndroid()
    {
        BuildReport report = BuildAndroidInternal("Builds/Android/GoldenBreak.apk");
        ExitWithResult(report);
    }

    public static void BuildAndroidEmulator()
    {
        AndroidArchitecture originalArchitectures = PlayerSettings.Android.targetArchitectures;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;

        BuildReport report = BuildAndroidInternal("Builds/Android/GoldenBreak-emulator.apk");

        PlayerSettings.Android.targetArchitectures = originalArchitectures;
        AssetDatabase.SaveAssets();

        ExitWithResult(report);
    }

    private static void EnsureFolderExists(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = assetPath.Substring(0, assetPath.LastIndexOf('/'));
        string newFolderName = assetPath.Substring(assetPath.LastIndexOf('/') + 1);

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderExists(parent);
        }

        AssetDatabase.CreateFolder(parent, newFolderName);
    }

    private static BuildReport BuildAndroidInternal(string outputPath)
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        return BuildPipeline.BuildPlayer(options);
    }

    private static void ExitWithResult(BuildReport report)
    {
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"ProjectSetup: Android build succeeded. Size: {report.summary.totalSize} bytes. Output: {report.summary.outputPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"ProjectSetup: Android build failed with result {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
