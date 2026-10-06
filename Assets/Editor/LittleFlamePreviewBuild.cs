using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Builds the saved game scene into a separate directory for opt-in screenshot study.
// The regular player build and current scene are not regenerated or overwritten.
public static class LittleFlamePreviewBuild {
    public static void Build() {
        const string scene = "Assets/Scenes/RestaurantCity.unity";
        const string exe = "Builds/TruckArtPreview/RestaurantCity.exe";
        if (!File.Exists(scene)) throw new Exception("Saved Restaurant City scene missing");
        Directory.CreateDirectory("Builds/TruckArtPreview");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene }, locationPathName = exe,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Little Flame preview build failed: " + report.summary.result);
        UnityEngine.Debug.Log("LITTLE_FLAME_PREVIEW_BUILD_PASSED " + report.summary.totalSize);
    }
}
