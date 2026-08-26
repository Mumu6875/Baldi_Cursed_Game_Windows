#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public static class CursedWindowsSetup
{
    private const string SetupKey = "CursedBaldiWindowsX64Setup_v1";
    private const string ProductName = "Baldi Cursed Classroom";
    private const string ExecutableName = "BaldiCursedClassroom.exe";

    static CursedWindowsSetup()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    [MenuItem("Cursed Baldi/Apply Windows x86_64 Build Settings")]
    public static void ApplyWindowsSettings()
    {
        PlayerSettings.companyName = "Cursed Classroom Mods";
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = "1.13.7";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.forceSingleInstance = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);
        PlayerSettings.allowUnsafeCode = false;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.MTRendering = true;
        PlayerSettings.runInBackground = false;
        QualitySettings.vSyncCount = 1;
        EditorPrefs.SetBool(SetupKey, true);
        AssetDatabase.SaveAssets();
        Debug.Log("Cursed Baldi Windows x86_64 settings applied.");
    }

    [MenuItem("Cursed Baldi/Build Windows x86_64")]
    public static void BuildWindowsX64()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Choose Windows x86_64 output folder", "", "BaldiCursedClassroom_Windows_x86_64");
        if (string.IsNullOrEmpty(outputFolder)) return;
        BuildWindowsX64At(Path.Combine(outputFolder, ExecutableName), true);
    }

    public static void BuildWindowsX64Batch()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputFolder = Path.Combine(projectRoot, "Builds", "Windows-x86_64");
        BuildWindowsX64At(Path.Combine(outputFolder, ExecutableName), false);
    }

    private static void BuildWindowsX64At(string executablePath, bool revealWhenComplete)
    {
        ApplyWindowsSettings();
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            throw new BuildFailedException("Windows x86_64 build support is unavailable in this Unity installation.");
        }

        string outputFolder = Path.GetDirectoryName(Path.GetFullPath(executablePath));
        if (string.IsNullOrEmpty(outputFolder))
        {
            throw new BuildFailedException("Windows output folder could not be resolved.");
        }
        Directory.CreateDirectory(outputFolder);

        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes.ToArray();
        if (scenes.Count == 0)
        {
            throw new BuildFailedException("No enabled scenes were found in EditorBuildSettings.");
        }

        options.locationPathName = Path.Combine(outputFolder, ExecutableName);
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            if (revealWhenComplete) EditorUtility.RevealInFinder(outputFolder);
            Debug.Log("Windows x86_64 build created: " + options.locationPathName);
        }
        else
        {
            throw new BuildFailedException("Windows x86_64 build failed with result: " + report.summary.result);
        }
    }

    private static void ApplyOnce()
    {
        if (!EditorPrefs.GetBool(SetupKey, false))
        {
            ApplyWindowsSettings();
        }
    }
}

public sealed class CursedBuildValidation : IPreprocessBuildWithReport
{
    private const string WarningAssetPath = "Assets/Resources/CursedMod/PiracyWarningPhase1.jpg";
    private const string RulerAudioAssetPath = "Assets/Resources/CursedMod/BaldiRulerLoud.ogg";
    private const string HelpMeExitAssetPath = "Assets/Resources/CursedMod/HelpMeExitSign.png";
    private const string Phase2CompletionAssetPath = "Assets/Resources/CursedMod/Phase2Completion.png";
    private const string Phase3PasswordAssetPath = "Assets/Resources/CursedMod/Phase3Password.png";
    private const string Phase4FinalAssetPath = "Assets/Resources/CursedMod/Phase4Final.png";
    private const string TestRoomPosterAssetPath = "Assets/Resources/CursedMod/TestRoomEntityPoster.png";
    public int callbackOrder { get { return -1000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            throw new BuildFailedException("This repository only permits Windows x86_64 builds. Requested target: " + report.summary.platform);
        }

        if (!File.Exists(WarningAssetPath))
        {
            throw new BuildFailedException("Required Phase 1 warning image file is missing: " + WarningAssetPath);
        }
        Texture2D warning = ImportTextureWithoutNpotScaling(WarningAssetPath);
        if (warning == null)
        {
            throw new BuildFailedException("Required Phase 1 warning image is missing: " + WarningAssetPath);
        }
        if (warning.width < 1280 || warning.height < 720)
        {
            throw new BuildFailedException("Phase 1 warning image has an invalid resolution: " + warning.width + "x" + warning.height);
        }
        Debug.Log("Verified Phase 1 notice image: " + warning.width + "x" + warning.height);

        if (!File.Exists(HelpMeExitAssetPath))
        {
            throw new BuildFailedException("Required Phase 2 HELP ME exit sign is missing: " + HelpMeExitAssetPath);
        }
        Texture2D helpMeExit = ImportTextureWithoutNpotScaling(HelpMeExitAssetPath);
        if (helpMeExit == null || helpMeExit.width != 128 || helpMeExit.height != 128)
        {
            throw new BuildFailedException("Phase 2 HELP ME exit sign must be exactly 128x128: " + HelpMeExitAssetPath);
        }
        Debug.Log("Verified Phase 2 HELP ME exit sign: " + helpMeExit.width + "x" + helpMeExit.height);

        if (!File.Exists(Phase2CompletionAssetPath))
        {
            throw new BuildFailedException("Required Phase 2 completion image is missing: " + Phase2CompletionAssetPath);
        }
        Texture2D completion = ImportTextureWithoutNpotScaling(Phase2CompletionAssetPath);
        if (completion == null || completion.width != 1672 || completion.height != 941)
        {
            throw new BuildFailedException("Phase 2 completion image must import as 1672x941: " + Phase2CompletionAssetPath + FormatImportedSize(completion));
        }
        Debug.Log("Verified Phase 2 completion image: " + completion.width + "x" + completion.height);

        if (!File.Exists(Phase3PasswordAssetPath))
        {
            throw new BuildFailedException("Required Phase 3 password image is missing: " + Phase3PasswordAssetPath);
        }
        Texture2D phase3 = ImportTextureWithoutNpotScaling(Phase3PasswordAssetPath);
        if (phase3 == null || phase3.width != 1672 || phase3.height != 941)
        {
            throw new BuildFailedException("Phase 3 password image must import as 1672x941: " + Phase3PasswordAssetPath + FormatImportedSize(phase3));
        }
        Debug.Log("Verified Phase 3 password image: " + phase3.width + "x" + phase3.height);

        if (!File.Exists(Phase4FinalAssetPath))
        {
            throw new BuildFailedException("Required Phase 4 final image is missing: " + Phase4FinalAssetPath);
        }
        Texture2D phase4 = ImportTextureWithoutNpotScaling(Phase4FinalAssetPath);
        if (phase4 == null || phase4.width != 1672 || phase4.height != 941)
        {
            throw new BuildFailedException("Phase 4 final image must import as 1672x941: " + Phase4FinalAssetPath + FormatImportedSize(phase4));
        }
        Debug.Log("Verified Phase 4 final image: " + phase4.width + "x" + phase4.height);

        if (!File.Exists(TestRoomPosterAssetPath))
        {
            throw new BuildFailedException("Required TestRoom laboratory poster is missing: " + TestRoomPosterAssetPath);
        }
        Texture2D testRoomPoster = ImportTextureWithoutNpotScaling(TestRoomPosterAssetPath);
        if (testRoomPoster == null || testRoomPoster.width != 1672 || testRoomPoster.height != 941)
        {
            throw new BuildFailedException("TestRoom laboratory poster must import as 1672x941: " + TestRoomPosterAssetPath + FormatImportedSize(testRoomPoster));
        }
        Debug.Log("Verified TestRoom laboratory poster: " + testRoomPoster.width + "x" + testRoomPoster.height);

        if (!File.Exists(RulerAudioAssetPath))
        {
            throw new BuildFailedException("Required replacement Baldi ruler sound is missing: " + RulerAudioAssetPath);
        }
        AssetDatabase.ImportAsset(RulerAudioAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AudioClip rulerAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(RulerAudioAssetPath);
        if (rulerAudio == null || rulerAudio.length < 0.5f || rulerAudio.length > 1.0f || rulerAudio.channels != 1)
        {
            throw new BuildFailedException("Replacement Baldi ruler sound must be a 0.5-1.0 second mono AudioClip: " + RulerAudioAssetPath);
        }
        Debug.Log("Verified replacement Baldi ruler sound: " + rulerAudio.length.ToString("F2") + " seconds, " + rulerAudio.frequency + " Hz.");
    }

    private static Texture2D ImportTextureWithoutNpotScaling(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.npotScale != TextureImporterNPOTScale.None)
        {
            // The generated phase screens are 1672x941. Unity's legacy/default
            // NPOT setting can silently resize them to 2048x1024 in Cloud Build.
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static string FormatImportedSize(Texture2D texture)
    {
        return texture == null
            ? " (the TextureImporter returned null)"
            : " (actual imported size: " + texture.width + "x" + texture.height + ")";
    }
}
#endif
