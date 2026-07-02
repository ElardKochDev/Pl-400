using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildApk
{
    public static void BuildAndroid()
    {
        PlayerSettings.companyName = "Jorge";
        PlayerSettings.productName = "Power Platform Developer Saga PL-400";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.jorge.pl400rpg");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Auto-rotación con ambas orientaciones permitidas: el juego FIJA la orientación
        // elegida (horizontal/vertical) en runtime vía Screen.orientation (opción del título).
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/AppIcon.png");
        if (icon != null)
        {
            var icons = new Texture2D[] { icon };
            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);
        }
        else
            Debug.LogWarning("AppIcon.png no encontrado; se usa el icono por defecto.");

        var outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds"));
        Directory.CreateDirectory(outputDir);
        var report = BuildPipeline.BuildPlayer(new[]
        {
            "Assets/Scenes/Main.unity"
        }, Path.Combine(outputDir, "PL400-RPG.apk"), BuildTarget.Android, BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Android build failed: " + report.summary.result);
    }

    public static void BuildWindows()
    {
        PlayerSettings.companyName = "Jorge";
        PlayerSettings.productName = "Power Platform Developer Saga PL-400";

        // Ventana 1280x720 redimensionable (no pantalla completa).
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/AppIcon.png");
        if (icon != null)
        {
            var icons = new Texture2D[] { icon };
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
        }
        else
            Debug.LogWarning("AppIcon.png no encontrado; se usa el icono por defecto.");

        var outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "PL400-RPG-Windows"));
        Directory.CreateDirectory(outputDir);
        var report = BuildPipeline.BuildPlayer(new[]
        {
            "Assets/Scenes/Main.unity"
        }, Path.Combine(outputDir, "PL400-RPG.exe"), BuildTarget.StandaloneWindows64, BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
    }
}
