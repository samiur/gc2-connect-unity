# Building GC2 Connect for Android

This document provides comprehensive instructions for building the GC2 Connect Unity application for Android.

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Unity | 6000.3.2f1 | Game engine |
| Android Studio | 2023.1+ | Android SDK and Gradle |
| JDK | 17+ | Java compilation (bundled with Android Studio) |

### Installing Dependencies

1. **Unity Hub**: Download from [unity.com](https://unity.com/download)
   - Install Unity version 6000.3.2f1
   - Include Android Build Support (IL2CPP)
   - Include Android SDK & NDK Tools

2. **Android Studio**: Download from [developer.android.com](https://developer.android.com/studio)
   - Accept SDK license agreements
   - Install SDK Platform 34 (Android 14)

3. **Environment Variables** (optional, auto-detected):
   ```bash
   export ANDROID_HOME=$HOME/Library/Android/sdk
   export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
   ```

## Quick Start

### Development Build (Fastest)

```bash
# Build development APK (skips tests, enables debugging)
make build-android-dev
```

### Full Build (Recommended for Testing)

```bash
# Runs tests first, then builds release APK
make build-android
```

### Play Store Build

```bash
# Build Android App Bundle for Play Store
make build-android-aab
```

## Build Targets

| Target | Description | Tests | Output |
|--------|-------------|-------|--------|
| `build-android-plugin` | Build native plugin only | No | AAR |
| `build-android` | Full build with tests | Yes | APK |
| `build-android-dev` | Development build | No | APK |
| `build-android-aab` | Play Store build | Yes | AAB |
| `android-config` | Configure Unity settings | No | - |
| `android-validate` | Validate setup | No | - |

## Build Script Options

The main build script (`Scripts/build_android.sh`) supports these options:

```bash
./Scripts/build_android.sh [options]

Options:
  --skip-tests      Skip running tests before build
  --skip-plugin     Use existing plugin (don't rebuild)
  --development     Create development build (debugging enabled)
  --aab             Build Android App Bundle instead of APK
  --version=X.Y.Z   Set version number explicitly
  --verbose         Show full build output
  --help            Show help message
```

### Examples

```bash
# Quick rebuild after code changes (reuse plugin)
./Scripts/build_android.sh --skip-plugin --skip-tests

# Verbose build for debugging build issues
./Scripts/build_android.sh --verbose

# Build specific version for Play Store
./Scripts/build_android.sh --aab --version=1.2.3
```

## Build Process

The build script performs these steps:

1. **Validate Environment**
   - Check Unity installation
   - Verify Android SDK (ANDROID_HOME)
   - Confirm Java/JDK availability

2. **Build Native Plugin**
   - Compile GC2AndroidPlugin.aar
   - Copy to Unity plugins directory

3. **Run Tests** (unless skipped)
   - Execute EditMode tests
   - Fail build if tests fail

4. **Configure Player Settings**
   - Set package name, API levels
   - Configure IL2CPP and ARM64

5. **Build Unity Project**
   - Build APK or AAB
   - Output to `Builds/Android/`

6. **Post-Build Verification**
   - Verify output exists
   - Report file size

## Output Structure

```
Builds/
├── Android/
│   ├── OpenRange.apk          # Release APK
│   ├── OpenRange-dev.apk      # Development APK
│   └── OpenRange.aab          # Play Store bundle
└── logs/
    ├── android-plugin.log
    ├── android-tests.log
    ├── android-config.log
    └── android-build.log
```

## Android Player Settings

The build configures these settings automatically:

| Setting | Value | Reason |
|---------|-------|--------|
| Package Name | `com.openrange.gc2connect` | Unique identifier |
| Min SDK | 26 (Android 8.0) | USB Host API improvements |
| Target SDK | 34 (Android 14) | Play Store requirement |
| Scripting Backend | IL2CPP | Performance |
| Architecture | ARM64 | Modern devices |
| Orientation | Landscape | Tablet optimization |

### USB Permissions

The native plugin includes `AndroidManifest.xml` with:
- `<uses-feature android:name="android.hardware.usb.host" />`
- USB device filters for GC2 (VID: 0x2C79, PID: 0x0110)

## Keystore Configuration

### For Development

Development builds use Android debug signing automatically. No configuration needed.

### For Release

1. **Create a keystore** (first time only):
   ```bash
   keytool -genkey -v -keystore release.jks -keyalg RSA -keysize 2048 \
           -validity 10000 -alias openrange
   ```

2. **Copy template and fill in values**:
   ```bash
   cp configs/android/keystore.properties.template configs/android/keystore.properties
   # Edit keystore.properties with your credentials
   ```

3. **Move keystore to config directory**:
   ```bash
   mv release.jks configs/android/
   ```

**IMPORTANT**: Never commit `keystore.properties` or `.jks` files to source control!

### Keystore Properties Format

```properties
# Path to keystore file
storeFile=configs/android/release.jks

# Keystore password
storePassword=your_keystore_password

# Key alias name
keyAlias=openrange

# Key password
keyPassword=your_key_password
```

## Testing on Device

### Prerequisites

1. Enable Developer Options on Android device
2. Enable USB Debugging
3. Install ADB (included with Android Studio)

### Install APK

```bash
# Connect device via USB
adb devices  # Verify device is listed

# Install APK
adb install -r Builds/Android/OpenRange.apk

# View logs (for debugging)
adb logcat -s Unity
```

### Testing GC2 Connection

1. Connect GC2 device to Android tablet via USB OTG cable
2. Accept USB permission prompt in app
3. Fire test shots and verify data reception

## Troubleshooting

### Common Issues

#### "Android SDK not found"
```
Error: Android SDK not found. Set ANDROID_HOME or install via Android Studio.
```
**Solution**: Install Android Studio or set ANDROID_HOME:
```bash
export ANDROID_HOME=$HOME/Library/Android/sdk
```

#### "Java not found"
```
Error: Java not found. Please install JDK or Android Studio.
```
**Solution**: Android Studio includes JBR (JetBrains Runtime). Set JAVA_HOME:
```bash
export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
```

#### "Native plugin not found"
```
Error: Native plugin not found at: Assets/Plugins/Android/GC2AndroidPlugin.aar
```
**Solution**: Build the plugin first:
```bash
cd NativePlugins/Android && ./build_android_plugin.sh
```

#### Build fails with Gradle error
```
FAILURE: Build failed with an exception.
```
**Solution**: Check Android SDK installation:
1. Open Android Studio > SDK Manager
2. Install Android SDK Build-Tools 34.x
3. Accept all SDK license agreements

#### "Android Build Support is not installed" or "build target was unsupported"
```
Error building player because build target was unsupported
```
**Solution**: Install Android Build Support module in Unity Hub:
1. Open Unity Hub
2. Go to **Installs** tab
3. Click the gear icon on Unity 6000.3.2f1
4. Select **Add Modules**
5. Check **Android Build Support** (this includes Android SDK & NDK Tools)
6. Click **Install**
7. After installation completes, run the build again

### Build Logs

All build logs are saved to `Builds/logs/`:
- `android-plugin.log` - Native plugin compilation
- `android-build.log` - Unity project build
- `android-config.log` - Player settings configuration
- `android-tests.log` - Test execution

### Verifying the Build

```bash
# Check APK exists and size
ls -la Builds/Android/OpenRange.apk

# Verify APK contents
unzip -l Builds/Android/OpenRange.apk | head -20

# Check for native library
unzip -l Builds/Android/OpenRange.apk | grep libGC2AndroidPlugin.so
```

## Play Store Submission

### Building for Play Store

```bash
# Build AAB (Android App Bundle)
make build-android-aab
```

### Upload Requirements

1. **Signed AAB**: Must be signed with release keystore
2. **App Icon**: 512x512 PNG (configure in Unity)
3. **Screenshots**: At least 2 phone + 2 tablet screenshots
4. **Privacy Policy**: URL to your privacy policy
5. **Content Rating**: Complete content rating questionnaire

### Play Console Steps

1. Create app in [Google Play Console](https://play.google.com/console)
2. Upload AAB to Internal Testing track first
3. Add required store listing details
4. Roll out to testers, then Production

## CI/CD Integration

### GitHub Actions

For GitHub Actions, see `.github/workflows/release.yml` (future).

Key considerations:
- Use `game-ci/unity-builder` action
- Store keystore as base64 secret
- Cache Android SDK for faster builds

### Environment Variables for CI

```yaml
env:
  ANDROID_KEYSTORE_BASE64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}
  ANDROID_KEYSTORE_PASSWORD: ${{ secrets.ANDROID_KEYSTORE_PASSWORD }}
  ANDROID_KEY_ALIAS: ${{ secrets.ANDROID_KEY_ALIAS }}
  ANDROID_KEY_PASSWORD: ${{ secrets.ANDROID_KEY_PASSWORD }}
```

## Version Management

Version is determined in this order:
1. `--version=X.Y.Z` command line argument
2. Git tag (e.g., `v1.0.0` → `1.0.0`)
3. Default: `0.1.0`

To create a release:
```bash
# Tag the release
git tag v1.0.0
git push --tags

# Build with that version
./Scripts/build_android.sh --aab --version=1.0.0
```

## Performance Notes

Build times (approximate, on M1 MacBook Pro):

| Step | Time |
|------|------|
| Native plugin (Gradle) | ~30 seconds |
| EditMode tests | ~60 seconds |
| Unity build (IL2CPP) | ~10-15 minutes |
| **Total** | **~12-17 minutes** |

Tips for faster development:
- Use `--skip-tests` for iterations
- Use `--skip-plugin` if plugin unchanged
- Use `--development` for faster builds

## Unity Editor Menu

Additional configuration can be done via Unity menu:

| Menu Item | Description |
|-----------|-------------|
| OpenRange > Android > Configure Android Settings | Apply all recommended settings |
| OpenRange > Android > Validate Android Setup | Check prerequisites and settings |
| OpenRange > Android > Create Keystore Template | Generate keystore.properties.template |
| OpenRange > Android > Build APK (Quick) | Development build from editor |
| OpenRange > Android > Build AAB (Release) | Play Store build from editor |
