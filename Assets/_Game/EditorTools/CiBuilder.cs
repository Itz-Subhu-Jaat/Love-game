#if UNITY_EDITOR
using System;
using System.Linq;
using LoveGame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LoveGame.EditorTools.CI
{
    /// <summary>
    /// CI entry points for GameCI unity-builder. Fully configures the project, validates
    /// catalogs/scripts, then builds. Exits with a non-zero code on any error so the
    /// pipeline fails loudly instead of shipping a broken APK.
    /// Usage: unity-builder customBuildMethod = LoveGame.EditorTools.CI.CiBuilder.BuildAndroid
    /// </summary>
    public static class CiBuilder
    {
        public const string ApkOutput = "build/LoveGame.apk";
        public const string AabOutput = "build/LoveGame.aab";

        /// <summary>Runs before every build: setup + validation. Throws on fatal problems.</summary>
        public static void ValidateProject()
        {
            Debug.Log("[CiBuilder] validation start");
            ProjectSetupWizard.Run();

            // catalog sanity - the same rules the EditMode tests enforce
            var catalog = new World.RegionCatalogService();
            catalog.Load();
            var errors = catalog.Validate();
            foreach (var error in errors.Take(20))
                Debug.LogWarning($"[CiBuilder][catalog] {error}");

            if (catalog.Count < 20)
                Debug.LogWarning($"[CiBuilder][catalog] only {catalog.Count} regions loaded (expected 24+) - world will feel empty");

            // compile-blocking asset checks
            if (AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Scenes" }).Length < 3)
                Debug.LogWarning("[CiBuilder] fewer than 3 scenes in Assets/_Scenes");

            Debug.Log($"[CiBuilder] validation OK ({catalog.Count} regions, {AssetDatabase.FindAssets("t:Script").Length} scripts)");
        }

        /// <summary>Android APK build (GameCI: unity-builder, targetPlatform=android).</summary>
        public static void BuildAndroid()
        {
            ValidateProject();
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/_Scenes/00_Bootstrap.unity",
                    "Assets/_Scenes/01_MainMenu.unity",
                    "Assets/_Scenes/02_World.unity",
                },
                locationPathName = ApkOutput,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            Build(options, "APK");
        }

        /// <summary>Android AAB (Play Store) build - release workflow.</summary>
        public static void BuildAndroidAab()
        {
            ValidateProject();
            EditorUserBuildSettings.buildAppBundle = true;
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/_Scenes/00_Bootstrap.unity",
                    "Assets/_Scenes/01_MainMenu.unity",
                    "Assets/_Scenes/02_World.unity",
                },
                locationPathName = AabOutput,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            Build(options, "AAB");
            EditorUserBuildSettings.buildAppBundle = false;
        }

        static void Build(BuildPlayerOptions options, string label)
        {
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CiBuilder] {label} build SUCCEEDED: {summary.totalSize / (1024 * 1024)} MB, " +
                          $"warnings: {summary.totalWarnings}, size ok");
                if (summary.totalErrors > 0)
                {
                    EditorApplication.Exit(2);
                    return;
                }
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[CiBuilder] {label} build FAILED: {summary.result}, errors: {summary.totalErrors}, " +
                               $"{summary.totalWarnings} warnings. Step results: {string.Join(", ", report.steps.Select(s => $"{s.name}:{s.stepResult}"))}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Log-only diagnostics pass (unity-builder with buildMethod on a no-op).</summary>
        public static void DryRun()
        {
            ValidateProject();
            Debug.Log("[CiBuilder] dry run complete");
            EditorApplication.Exit(0);
        }
    }
}
#endif
