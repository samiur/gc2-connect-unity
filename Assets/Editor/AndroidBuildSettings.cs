// ABOUTME: Editor script for configuring Unity Android Player Settings.
// ABOUTME: Provides menu items to configure Android build settings programmatically.

using UnityEditor;
using UnityEngine;
using System.IO;

namespace OpenRange.Editor
{
    /// <summary>
    /// Configures Unity Android Player Settings for the GC2 Connect application.
    /// </summary>
    public static class AndroidBuildSettings
    {
        // Build configuration constants
        private const string PackageName = "com.openrange.gc2connect";
        private const int MinSdkVersion = 26;  // Android 8.0 Oreo (required for USB Host API improvements)
        private const int TargetSdkVersion = 34;  // Android 14
        private const string ProductName = "OpenRange";
        private const string CompanyName = "OpenRange";

        // Paths
        private const string KeystoreTemplatePath = "configs/android/keystore.properties.template";
        private const string AndroidPluginPath = "Assets/Plugins/Android/GC2AndroidPlugin.aar";

        [MenuItem("OpenRange/Android/Configure Android Settings", priority = 200)]
        public static void ConfigureAndroidSettings()
        {
            Debug.Log("AndroidBuildSettings: Configuring Android Player Settings...");

            // Set application identifier
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            Debug.Log($"  Package name: {PackageName}");

            // Set product and company name
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;

            // Configure API levels
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdkVersion;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetSdkVersion;
            Debug.Log($"  API levels: min={MinSdkVersion}, target={TargetSdkVersion}");

            // Set scripting backend to IL2CPP for performance
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            Debug.Log("  Scripting backend: IL2CPP");

            // Target ARM64 only (modern devices)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("  Target architecture: ARM64");

            // Enable internet access (required for GSPro TCP connection)
            PlayerSettings.Android.forceInternetPermission = true;
            Debug.Log("  Internet permission: enabled");

            // Configure screen orientation (landscape for tablet use)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            Debug.Log("  Orientation: landscape only");

            // Configure graphics settings
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.Android.blitType = AndroidBlitType.Auto;
            Debug.Log("  GPU skinning: enabled");

            // Disable splash screen for pro license (optional)
            // PlayerSettings.SplashScreen.show = false;

            AssetDatabase.SaveAssets();
            Debug.Log("AndroidBuildSettings: Configuration complete!");
        }

        [MenuItem("OpenRange/Android/Validate Android Setup", priority = 201)]
        public static void ValidateAndroidSetup()
        {
            Debug.Log("AndroidBuildSettings: Validating Android setup...");

            bool hasErrors = false;
            bool hasWarnings = false;

            // Check native plugin
            if (!File.Exists(AndroidPluginPath))
            {
                Debug.LogError($"  [ERROR] Native plugin not found at: {AndroidPluginPath}");
                Debug.LogError("  Run: cd NativePlugins/Android && ./build_android_plugin.sh");
                hasErrors = true;
            }
            else
            {
                Debug.Log($"  [OK] Native plugin found: {AndroidPluginPath}");
            }

            // Check Android SDK
            string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot", "");
            if (string.IsNullOrEmpty(androidSdkRoot))
            {
                // Try environment variable
                androidSdkRoot = System.Environment.GetEnvironmentVariable("ANDROID_HOME")
                    ?? System.Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
                    ?? "";
            }

            if (string.IsNullOrEmpty(androidSdkRoot) || !Directory.Exists(androidSdkRoot))
            {
                Debug.LogWarning("  [WARN] Android SDK path not configured");
                Debug.LogWarning("  Set via: Edit > Preferences > External Tools > Android SDK");
                hasWarnings = true;
            }
            else
            {
                Debug.Log($"  [OK] Android SDK: {androidSdkRoot}");
            }

            // Check JDK
            string jdkPath = EditorPrefs.GetString("JdkPath", "");
            if (string.IsNullOrEmpty(jdkPath))
            {
                // Unity 6 often uses embedded JDK
                Debug.Log("  [OK] Using Unity embedded JDK (recommended)");
            }
            else
            {
                Debug.Log($"  [OK] JDK path: {jdkPath}");
            }

            // Check current settings
            var currentPackage = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (currentPackage != PackageName)
            {
                Debug.LogWarning($"  [WARN] Package name is '{currentPackage}', expected '{PackageName}'");
                Debug.LogWarning("  Run: OpenRange > Android > Configure Android Settings");
                hasWarnings = true;
            }
            else
            {
                Debug.Log($"  [OK] Package name: {currentPackage}");
            }

            // Check scripting backend
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                Debug.LogWarning($"  [WARN] Scripting backend is {backend}, expected IL2CPP");
                hasWarnings = true;
            }
            else
            {
                Debug.Log("  [OK] Scripting backend: IL2CPP");
            }

            // Check architecture
            var arch = PlayerSettings.Android.targetArchitectures;
            if ((arch & AndroidArchitecture.ARM64) == 0)
            {
                Debug.LogWarning("  [WARN] ARM64 architecture not enabled");
                hasWarnings = true;
            }
            else
            {
                Debug.Log($"  [OK] Target architectures: {arch}");
            }

            // Check API levels
            var minSdk = (int)PlayerSettings.Android.minSdkVersion;
            var targetSdk = (int)PlayerSettings.Android.targetSdkVersion;
            Debug.Log($"  [INFO] API levels: min={minSdk}, target={targetSdk}");

            if (minSdk < MinSdkVersion)
            {
                Debug.LogWarning($"  [WARN] Min SDK {minSdk} is below recommended {MinSdkVersion}");
                hasWarnings = true;
            }

            // Summary
            Debug.Log("");
            if (hasErrors)
            {
                Debug.LogError("AndroidBuildSettings: Validation FAILED - see errors above");
            }
            else if (hasWarnings)
            {
                Debug.LogWarning("AndroidBuildSettings: Validation passed with warnings");
            }
            else
            {
                Debug.Log("AndroidBuildSettings: Validation passed!");
            }
        }

        [MenuItem("OpenRange/Android/Create Keystore Template", priority = 202)]
        public static void CreateKeystoreTemplate()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string templateDir = Path.Combine(projectRoot, "configs/android");
            string templatePath = Path.Combine(projectRoot, KeystoreTemplatePath);

            // Create directory if needed
            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
                Debug.Log($"Created directory: {templateDir}");
            }

            if (File.Exists(templatePath))
            {
                Debug.LogWarning($"Template already exists at: {templatePath}");
                return;
            }

            string template = @"# Android Keystore Configuration
# Copy this file to keystore.properties and fill in your values
# NEVER commit keystore.properties to source control!

# Path to your keystore file (relative to project root or absolute)
storeFile=configs/android/release.jks

# Keystore password
storePassword=

# Key alias name
keyAlias=openrange

# Key password (often same as storePassword)
keyPassword=
";

            File.WriteAllText(templatePath, template);
            Debug.Log($"Created keystore template at: {templatePath}");
            Debug.Log("Copy to keystore.properties and configure before building for release");
        }

        /// <summary>
        /// Configures Android settings for CI/CD builds.
        /// Called via -executeMethod from build scripts.
        /// </summary>
        public static void ConfigureForBuild()
        {
            ConfigureAndroidSettings();

            // Additional CI-specific settings
            Debug.Log("AndroidBuildSettings: Applying CI build configuration...");

            // Ensure consistent build settings
            EditorUserBuildSettings.buildAppBundle = false;  // APK by default
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.symlinkSources = false;

            // Set Android as active platform
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("  Switching to Android build target...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("AndroidBuildSettings: CI configuration complete!");
        }

        /// <summary>
        /// Configures for AAB (Android App Bundle) builds for Play Store.
        /// </summary>
        public static void ConfigureForAAB()
        {
            ConfigureAndroidSettings();
            EditorUserBuildSettings.buildAppBundle = true;
            Debug.Log("AndroidBuildSettings: Configured for AAB build");
        }

        /// <summary>
        /// Configures for development builds with debugging enabled.
        /// </summary>
        public static void ConfigureForDevelopment()
        {
            ConfigureAndroidSettings();
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "DEVELOPMENT_BUILD");
            Debug.Log("AndroidBuildSettings: Configured for development build");
        }
    }
}
