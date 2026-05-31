using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CommandLineAndroidBuild
{
    private const string DefaultOutputPath = "Builds/MarineBiologyAR.apk";
    private const string DefaultBundleOutputPath = "Builds/MarineBiologyAR.aab";

    public static void BuildApk()
    {
        BuildAndroid(false, DefaultOutputPath);
    }

    public static void BuildAab()
    {
        BuildAndroid(true, DefaultBundleOutputPath);
    }

    private static void BuildAndroid(bool buildAppBundle, string defaultOutputPath)
    {
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        string previousKeystoreName = PlayerSettings.Android.keystoreName;
        string previousKeystorePass = PlayerSettings.Android.keystorePass;
        string previousKeyaliasName = PlayerSettings.Android.keyaliasName;
        string previousKeyaliasPass = PlayerSettings.Android.keyaliasPass;
        string previousBundleVersion = PlayerSettings.bundleVersion;
        int previousBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
        bool previousUseApkExpansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
        bool previousSplitApplicationBinary = PlayerSettings.Android.splitApplicationBinary;
        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        try
        {
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            PlayerSettings.Android.useAPKExpansionFiles = false;
            PlayerSettings.Android.splitApplicationBinary = buildAppBundle;
            ConfigureSigning();
            ConfigureVersion();

            string outputPath = GetArgumentValue("-outputPath", defaultOutputPath);
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in Build Settings.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed: {report.summary.result}");
            }

            string obbPath = Path.ChangeExtension(outputPath, ".main.obb");
            if (File.Exists(obbPath))
            {
                throw new InvalidOperationException($"Unexpected OBB was generated: {obbPath}");
            }

            UnityEngine.Debug.Log($"Android build succeeded: {Path.GetFullPath(outputPath)}");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useAPKExpansionFiles = previousUseApkExpansionFiles;
            PlayerSettings.Android.splitApplicationBinary = previousSplitApplicationBinary;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
            PlayerSettings.Android.keystoreName = previousKeystoreName;
            PlayerSettings.Android.keystorePass = previousKeystorePass;
            PlayerSettings.Android.keyaliasName = previousKeyaliasName;
            PlayerSettings.Android.keyaliasPass = previousKeyaliasPass;
            PlayerSettings.bundleVersion = previousBundleVersion;
            PlayerSettings.Android.bundleVersionCode = previousBundleVersionCode;
        }
    }

    private static void ConfigureSigning()
    {
        string keystorePath = GetArgumentValue("-keystorePath", string.Empty);
        string keystorePass = GetArgumentValue("-keystorePass", string.Empty);
        string keyaliasName = GetArgumentValue("-keyaliasName", string.Empty);
        string keyaliasPass = GetArgumentValue("-keyaliasPass", string.Empty);

        if (string.IsNullOrEmpty(keystorePath))
        {
            PlayerSettings.Android.useCustomKeystore = false;
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyaliasName;
        PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyaliasPass) ? keystorePass : keyaliasPass;
    }

    private static void ConfigureVersion()
    {
        string versionName = GetArgumentValue("-versionName", string.Empty);
        string versionCode = GetArgumentValue("-versionCode", string.Empty);

        if (!string.IsNullOrEmpty(versionName))
        {
            PlayerSettings.bundleVersion = versionName;
        }

        if (!string.IsNullOrEmpty(versionCode) && int.TryParse(versionCode, out int parsedVersionCode))
        {
            PlayerSettings.Android.bundleVersionCode = parsedVersionCode;
        }
    }

    private static string GetArgumentValue(string name, string defaultValue)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return defaultValue;
    }
}
