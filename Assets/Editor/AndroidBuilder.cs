// ABOUTME: Editor script for building Android APK/AAB from command line.
// ABOUTME: Called by Scripts/build_android.sh via -executeMethod.

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

namespace OpenRange.Editor
{
    /// <summary>
    /// Handles Android build process from command line.
    /// </summary>
    public static class AndroidBuilder
    {
        private const string DefaultOutputPath = "Builds/Android/OpenRange.apk";

        /// <summary>
        /// Main build entry point. Called via -executeMethod.
        /// Reads command line arguments for configuration.
        /// </summary>
        public static void Build()
        {
            Debug.Log("AndroidBuilder: Starting Android build...");

            try
            {
                // Parse command line arguments
                var args = Environment.GetCommandLineArgs();
                var outputPath = GetArgValue(args, "-outputPath") ?? DefaultOutputPath;
                var isDevelopment = HasArg(args, "-Development");
                var allowDebugging = HasArg(args, "-AllowDebugging");

                Debug.Log($"  Output: {outputPath}");
                Debug.Log($"  Development: {isDevelopment}");
                Debug.Log($"  Debugging: {allowDebugging}");

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Debug.Log($"  Created output directory: {outputDir}");
                }

                // Get scenes from build settings
                var scenes = GetBuildScenes();
                if (scenes.Length == 0)
                {
                    Debug.LogError("AndroidBuilder: No scenes in build settings!");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"  Scenes: {scenes.Length}");
                foreach (var scene in scenes)
                {
                    Debug.Log($"    - {scene}");
                }

                // Configure build options
                var options = BuildOptions.None;
                if (isDevelopment)
                {
                    options |= BuildOptions.Development;
                }
                if (allowDebugging)
                {
                    options |= BuildOptions.AllowDebugging;
                }

                // Determine if building AAB
                var isAAB = outputPath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);
                EditorUserBuildSettings.buildAppBundle = isAAB;
                Debug.Log($"  Format: {(isAAB ? "AAB" : "APK")}");

                // Build
                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = options
                };

                Debug.Log("AndroidBuilder: Building...");
                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                // Report results
                var summary = report.summary;
                Debug.Log($"AndroidBuilder: Build result: {summary.result}");
                Debug.Log($"  Duration: {summary.totalTime}");
                Debug.Log($"  Size: {summary.totalSize / (1024 * 1024):F1} MB");
                Debug.Log($"  Warnings: {summary.totalWarnings}");
                Debug.Log($"  Errors: {summary.totalErrors}");

                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("AndroidBuilder: Build failed!");

                    // Log build steps with errors
                    foreach (var step in report.steps)
                    {
                        foreach (var message in step.messages)
                        {
                            if (message.type == LogType.Error || message.type == LogType.Exception)
                            {
                                Debug.LogError($"  [{step.name}] {message.content}");
                            }
                        }
                    }

                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log("AndroidBuilder: Build succeeded!");

                // Verify output exists
                if (!File.Exists(outputPath))
                {
                    Debug.LogError($"AndroidBuilder: Output file not found: {outputPath}");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log($"AndroidBuilder: Output: {outputPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AndroidBuilder: Exception: {ex.Message}");
                Debug.LogError(ex.StackTrace);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Gets scenes from build settings that are enabled.
        /// </summary>
        private static string[] GetBuildScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        /// <summary>
        /// Checks if command line contains an argument.
        /// </summary>
        private static bool HasArg(string[] args, string arg)
        {
            return args.Contains(arg, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets value of a command line argument (e.g., -outputPath /path/to/file).
        /// </summary>
        private static string GetArgValue(string[] args, string arg)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Quick build for testing (can be called from Unity menu).
        /// </summary>
        [MenuItem("OpenRange/Android/Build APK (Quick)", priority = 210)]
        public static void QuickBuildAPK()
        {
            Debug.Log("AndroidBuilder: Quick APK build...");

            // Configure for development
            AndroidBuildSettings.ConfigureForDevelopment();

            var scenes = GetBuildScenes();
            var outputPath = "Builds/Android/OpenRange-dev.apk";

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"AndroidBuilder: Quick build succeeded! Output: {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError("AndroidBuilder: Quick build failed!");
            }
        }

        /// <summary>
        /// Build AAB for Play Store.
        /// </summary>
        [MenuItem("OpenRange/Android/Build AAB (Release)", priority = 211)]
        public static void BuildAAB()
        {
            Debug.Log("AndroidBuilder: AAB build for Play Store...");

            // Configure for release
            AndroidBuildSettings.ConfigureForAAB();

            var scenes = GetBuildScenes();
            var outputPath = "Builds/Android/OpenRange.aab";

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            EditorUserBuildSettings.buildAppBundle = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"AndroidBuilder: AAB build succeeded! Output: {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError("AndroidBuilder: AAB build failed!");
            }
        }
    }
}
