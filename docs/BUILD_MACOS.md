# Building GC2 Connect for macOS

This document provides comprehensive instructions for building the GC2 Connect Unity application for macOS.

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Unity | 6000.3.2f1 | Game engine |
| Xcode | 15.0+ | Native plugin compilation |
| libusb | 1.0.26+ | USB communication |

### Installing Dependencies

1. **Unity Hub**: Download from [unity.com](https://unity.com/download)
   - Install Unity version 6000.3.2f1
   - Include macOS Build Support (Mono and IL2CPP)

2. **Xcode**: Download from the Mac App Store
   - After installation: `xcode-select --install`

3. **libusb**: Install via Homebrew
   ```bash
   brew install libusb
   ```

## Quick Start

### Development Build (Fastest)

```bash
# Build development version (skips tests, enables debugging)
make build-app-dev
```

### Full Build (Recommended for Testing)

```bash
# Runs tests first, then builds release
make build-app
```

### Release Build

```bash
# Uses git tag for version, full validation
make build-release
```

## Build Targets

| Target | Description | Tests | Plugin |
|--------|-------------|-------|--------|
| `build-plugin` | Build native plugin only | No | Yes |
| `build-app` | Full build with tests | Yes | Yes |
| `build-app-dev` | Development build | No | Yes |
| `build-release` | Release build from git tag | Yes | Yes |
| `package` | Create DMG for distribution | No | No |

## Build Script Options

The main build script (`Scripts/build_macos.sh`) supports these options:

```bash
./Scripts/build_macos.sh [options]

Options:
  --skip-tests      Skip running tests before build
  --skip-plugin     Use existing plugin (don't rebuild)
  --development     Create development build (debugging enabled)
  --version=X.Y.Z   Set version number explicitly
  --verbose         Show full build output
  -h, --help        Show help message
```

### Examples

```bash
# Quick rebuild after code changes (reuse plugin)
./Scripts/build_macos.sh --skip-plugin --skip-tests

# Verbose build for debugging build issues
./Scripts/build_macos.sh --verbose

# Build specific version
./Scripts/build_macos.sh --version=1.2.3
```

## Build Process

The build script performs these steps:

1. **Validate Environment**
   - Check Unity installation
   - Verify Xcode and command-line tools
   - Confirm libusb availability

2. **Build Native Plugin**
   - Compile GC2MacPlugin.bundle
   - Copy libusb.dylib to Unity plugins directory
   - Fix library paths

3. **Run Tests** (unless skipped)
   - Execute EditMode tests
   - Fail build if tests fail

4. **Build Unity Project**
   - Generate prefabs and scenes
   - Build IL2CPP standalone for macOS
   - Output to `Builds/macOS/OpenRange.app`

5. **Post-Build Verification**
   - Verify app bundle structure
   - Check native plugin embedding
   - Validate library paths

## Output Structure

```
Builds/
├── macOS/
│   └── OpenRange.app/
│       └── Contents/
│           ├── MacOS/
│           │   └── OpenRange          # Main executable
│           ├── PlugIns/
│           │   ├── GC2MacPlugin.bundle/
│           │   └── libusb-1.0.dylib
│           ├── Info.plist
│           └── Resources/
├── dist/
│   └── OpenRange.dmg                  # After 'make package'
└── logs/
    ├── plugin-build.log
    ├── unity-build.log
    └── test-results.log
```

## Native Plugin Architecture

The GC2MacPlugin is currently built for the native architecture:
- **Apple Silicon Mac**: arm64
- **Intel Mac**: x86_64

To create a universal binary (both architectures), you need:
1. libusb compiled for both architectures
2. Use `lipo` to combine the binaries

```bash
# Example: Create universal libusb (if you have both builds)
lipo -create libusb-arm64.dylib libusb-x86_64.dylib -output libusb-1.0.dylib
```

## Troubleshooting

### Common Issues

#### "Unity not found"
```
Error: Unity 6000.3.2f1 not found
```
**Solution**: Install Unity 6000.3.2f1 via Unity Hub.

#### "xcodebuild not found"
```
Error: Xcode command line tools not found
```
**Solution**: Run `xcode-select --install` or install Xcode from App Store.

#### "libusb not found"
```
Warning: libusb not found
```
**Solution**: Install via Homebrew: `brew install libusb`

#### Plugin not loaded in app
```
Warning: Native plugin may not be embedded correctly
```
**Solution**: Check that Unity's build includes the plugin. Verify `Assets/Plugins/macOS/` contains the bundle.

#### Tests failing
```
Error: Tests failed (X failures)
```
**Solution**:
1. Check log: `Builds/logs/test-results.log`
2. Run specific tests: `make test-edit`
3. Use `--skip-tests` for urgent builds (not recommended)

### Build Logs

All build logs are saved to `Builds/logs/`:
- `plugin-build.log` - Native plugin compilation
- `unity-build.log` - Unity project build
- `test-results.log` - Test execution

### Verifying the Build

```bash
# Check app bundle exists
ls -la Builds/macOS/OpenRange.app

# Check architecture
file Builds/macOS/OpenRange.app/Contents/MacOS/OpenRange

# Check plugin is embedded
ls -la Builds/macOS/OpenRange.app/Contents/PlugIns/

# Verify code signature (after signing)
codesign --verify --deep --strict Builds/macOS/OpenRange.app
```

## Creating DMG for Distribution

After building:

```bash
# Create DMG package
make package
```

This creates `Builds/dist/OpenRange.dmg`.

For a prettier DMG with custom layout, install `create-dmg`:
```bash
brew install create-dmg
```

## Code Signing and Notarization

For distributing outside the Mac App Store, you need to:
1. Code sign with Developer ID certificate
2. Notarize with Apple
3. Staple the ticket to the app

See [Prompt 37 documentation](../plan.md) for detailed signing instructions.

Quick reference:
```bash
# Sign (requires Apple Developer ID)
codesign --deep --force --verify --verbose \
    --options runtime \
    --sign "Developer ID Application: Your Name (TEAMID)" \
    Builds/macOS/OpenRange.app

# Notarize (requires app-specific password)
xcrun notarytool submit Builds/dist/OpenRange.dmg \
    --apple-id your@email.com \
    --team-id YOURTEAMID \
    --password @keychain:AC_PASSWORD \
    --wait

# Staple
xcrun stapler staple Builds/dist/OpenRange.dmg
```

## CI/CD Integration

For GitHub Actions, see `.github/workflows/release.yml` (Prompt 41).

Key considerations:
- Use `game-ci/unity-builder` action
- Store certificates as base64 secrets
- Cache Unity installation for faster builds

## Version Management

Version is determined in this order:
1. `--version=X.Y.Z` command line argument
2. Git tag (e.g., `v1.0.0` → `1.0.0`)
3. `VERSION` file in project root
4. Default: `0.0.0`

To create a release:
```bash
# Tag the release
git tag v1.0.0
git push --tags

# Build with that version
make build-release
```

## Performance Notes

Build times (approximate, on M1 MacBook Pro):
| Step | Time |
|------|------|
| Native plugin | ~10 seconds |
| EditMode tests | ~60 seconds |
| Unity build (IL2CPP) | ~5-10 minutes |
| **Total** | **~7-12 minutes** |

Tips for faster development:
- Use `--skip-tests` for iterations
- Use `--skip-plugin` if plugin unchanged
- Use `--development` for faster builds
