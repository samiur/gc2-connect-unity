#!/bin/bash

# ABOUTME: Main build script for Android APK/AAB builds.
# ABOUTME: Handles plugin build, Unity build, and optional signing.

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY_VERSION="6000.3.2f1"
UNITY_PATH="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
BUILD_DIR="$PROJECT_ROOT/Builds/Android"
LOG_DIR="$PROJECT_ROOT/Builds/logs"
NATIVE_PLUGIN_DIR="$PROJECT_ROOT/NativePlugins/Android"
KEYSTORE_PROPS="$PROJECT_ROOT/configs/android/keystore.properties"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default options
SKIP_TESTS=false
SKIP_PLUGIN=false
DEVELOPMENT_BUILD=false
BUILD_AAB=false
VERSION=""
VERBOSE=false

echo_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

echo_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

echo_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

echo_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

show_help() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Build OpenRange for Android"
    echo ""
    echo "Options:"
    echo "  --skip-tests      Skip running tests before build"
    echo "  --skip-plugin     Use existing native plugin (skip rebuild)"
    echo "  --development     Development build with debugging enabled"
    echo "  --aab             Build Android App Bundle for Play Store (default: APK)"
    echo "  --version=X.Y.Z   Set version explicitly (default: from git tag)"
    echo "  --verbose         Show full Unity output"
    echo "  --help            Show this help message"
    echo ""
    echo "Environment variables:"
    echo "  ANDROID_HOME      Path to Android SDK (auto-detected if not set)"
    echo "  JAVA_HOME         Path to JDK (uses Android Studio JBR if not set)"
    echo ""
    echo "Examples:"
    echo "  $0                        # Full build with tests"
    echo "  $0 --skip-tests           # Quick build without tests"
    echo "  $0 --development          # Development build with debugging"
    echo "  $0 --aab --version=1.0.0  # Play Store release build"
}

parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            --skip-plugin)
                SKIP_PLUGIN=true
                shift
                ;;
            --development)
                DEVELOPMENT_BUILD=true
                shift
                ;;
            --aab)
                BUILD_AAB=true
                shift
                ;;
            --version=*)
                VERSION="${1#*=}"
                shift
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            --help)
                show_help
                exit 0
                ;;
            *)
                echo_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

check_prerequisites() {
    echo_step "Checking prerequisites..."

    local has_errors=false

    # Check Unity
    if [ ! -f "$UNITY_PATH" ]; then
        echo_error "Unity $UNITY_VERSION not found at: $UNITY_PATH"
        has_errors=true
    else
        echo_info "Unity: $UNITY_PATH"
    fi

    # Check Android SDK
    if [ -z "$ANDROID_HOME" ]; then
        # Try default locations
        if [ -d "$HOME/Library/Android/sdk" ]; then
            export ANDROID_HOME="$HOME/Library/Android/sdk"
        elif [ -d "/usr/local/share/android-sdk" ]; then
            export ANDROID_HOME="/usr/local/share/android-sdk"
        fi
    fi

    if [ -z "$ANDROID_HOME" ] || [ ! -d "$ANDROID_HOME" ]; then
        echo_error "Android SDK not found. Set ANDROID_HOME or install via Android Studio."
        has_errors=true
    else
        echo_info "Android SDK: $ANDROID_HOME"
    fi

    # Check Java
    if [ -z "$JAVA_HOME" ]; then
        if [ -d "/Applications/Android Studio.app/Contents/jbr/Contents/Home" ]; then
            export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
        fi
    fi

    if [ -n "$JAVA_HOME" ]; then
        export PATH="$JAVA_HOME/bin:$PATH"
        echo_info "Java: $JAVA_HOME"
    else
        # Check if java is available anyway
        if command -v java &> /dev/null; then
            echo_info "Java: $(which java)"
        else
            echo_error "Java not found. Install JDK or Android Studio."
            has_errors=true
        fi
    fi

    # Check native plugin
    local plugin_path="$PROJECT_ROOT/Assets/Plugins/Android/GC2AndroidPlugin.aar"
    if [ ! -f "$plugin_path" ] && [ "$SKIP_PLUGIN" = true ]; then
        echo_error "Native plugin not found and --skip-plugin specified."
        echo_error "Build plugin first: cd NativePlugins/Android && ./build_android_plugin.sh"
        has_errors=true
    fi

    # Check keystore for release builds
    if [ "$DEVELOPMENT_BUILD" = false ] && [ -f "$KEYSTORE_PROPS" ]; then
        echo_info "Keystore config: $KEYSTORE_PROPS"
    elif [ "$DEVELOPMENT_BUILD" = false ]; then
        echo_warn "No keystore config found. Build will use debug signing."
        echo_warn "For release builds, create: $KEYSTORE_PROPS"
    fi

    if [ "$has_errors" = true ]; then
        echo_error "Prerequisites check failed. Fix issues above and retry."
        exit 1
    fi

    echo_info "All prerequisites satisfied."
}

get_version() {
    if [ -n "$VERSION" ]; then
        echo "$VERSION"
        return
    fi

    # Try to get version from git tag
    cd "$PROJECT_ROOT"
    local git_version=$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || echo "")

    if [ -n "$git_version" ]; then
        echo "$git_version"
    else
        echo "0.1.0"
    fi
}

build_native_plugin() {
    if [ "$SKIP_PLUGIN" = true ]; then
        echo_step "Skipping native plugin build (--skip-plugin)"
        return
    fi

    echo_step "Building native Android plugin..."

    local plugin_script="$NATIVE_PLUGIN_DIR/build_android_plugin.sh"
    if [ ! -f "$plugin_script" ]; then
        echo_error "Plugin build script not found: $plugin_script"
        exit 1
    fi

    cd "$NATIVE_PLUGIN_DIR"

    if [ "$VERBOSE" = true ]; then
        ./build_android_plugin.sh
    else
        ./build_android_plugin.sh > "$LOG_DIR/android-plugin.log" 2>&1
    fi

    local plugin_path="$PROJECT_ROOT/Assets/Plugins/Android/GC2AndroidPlugin.aar"
    if [ ! -f "$plugin_path" ]; then
        echo_error "Native plugin build failed. Check $LOG_DIR/android-plugin.log"
        exit 1
    fi

    echo_info "Native plugin built successfully."
}

run_tests() {
    if [ "$SKIP_TESTS" = true ]; then
        echo_step "Skipping tests (--skip-tests)"
        return
    fi

    echo_step "Running EditMode tests..."

    local test_log="$LOG_DIR/android-tests.log"

    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -silent-crashes \
        -projectPath "$PROJECT_ROOT" \
        -runTests \
        -testPlatform EditMode \
        -testResults "$PROJECT_ROOT/TestResults/android-editmode.xml" \
        -logFile "$test_log"

    local test_result=$?

    if [ $test_result -ne 0 ]; then
        echo_error "Tests failed. Check $test_log for details."
        if [ "$VERBOSE" = true ]; then
            tail -50 "$test_log"
        fi
        exit 1
    fi

    echo_info "All tests passed."
}

configure_android_settings() {
    echo_step "Configuring Android Player Settings..."

    local config_log="$LOG_DIR/android-config.log"
    local method="OpenRange.Editor.AndroidBuildSettings.ConfigureForBuild"

    if [ "$DEVELOPMENT_BUILD" = true ]; then
        method="OpenRange.Editor.AndroidBuildSettings.ConfigureForDevelopment"
    elif [ "$BUILD_AAB" = true ]; then
        method="OpenRange.Editor.AndroidBuildSettings.ConfigureForAAB"
    fi

    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -silent-crashes \
        -quit \
        -projectPath "$PROJECT_ROOT" \
        -executeMethod "$method" \
        -logFile "$config_log"

    local config_result=$?

    if [ $config_result -ne 0 ]; then
        echo_error "Failed to configure Android settings. Check $config_log"
        exit 1
    fi

    echo_info "Android settings configured."
}

build_android() {
    echo_step "Building Android application..."

    local version=$(get_version)
    echo_info "Version: $version"

    # Create build directory
    mkdir -p "$BUILD_DIR"
    mkdir -p "$LOG_DIR"

    local build_log="$LOG_DIR/android-build.log"
    local output_name="OpenRange"
    local output_path="$BUILD_DIR/$output_name.apk"

    if [ "$BUILD_AAB" = true ]; then
        output_path="$BUILD_DIR/$output_name.aab"
    fi

    # Build options
    local build_options=""
    if [ "$DEVELOPMENT_BUILD" = true ]; then
        build_options="-Development -AllowDebugging"
    fi

    echo_info "Building to: $output_path"

    # Unity Android build
    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -silent-crashes \
        -quit \
        -projectPath "$PROJECT_ROOT" \
        -buildTarget Android \
        -executeMethod OpenRange.Editor.AndroidBuilder.Build \
        -outputPath "$output_path" \
        -logFile "$build_log" \
        $build_options

    local build_result=$?

    if [ $build_result -ne 0 ]; then
        echo_error "Android build failed. Check $build_log for details."
        if [ "$VERBOSE" = true ]; then
            tail -100 "$build_log"
        fi
        exit 1
    fi

    # Verify output
    if [ ! -f "$output_path" ]; then
        echo_error "Build completed but output not found: $output_path"
        exit 1
    fi

    local size=$(du -h "$output_path" | cut -f1)
    echo_info "Build successful: $output_path ($size)"
}

print_summary() {
    echo ""
    echo "=========================================="
    echo_info "Android build complete!"
    echo "=========================================="
    echo ""

    local output_name="OpenRange"
    if [ "$BUILD_AAB" = true ]; then
        echo "  Output: $BUILD_DIR/$output_name.aab"
    else
        echo "  Output: $BUILD_DIR/$output_name.apk"
    fi
    echo "  Logs:   $LOG_DIR/"
    echo ""

    if [ "$BUILD_AAB" = false ]; then
        echo "Next steps:"
        echo "  1. Connect Android device with USB debugging enabled"
        echo "  2. Install: adb install -r $BUILD_DIR/$output_name.apk"
        echo "  3. Test GC2 USB connection"
    else
        echo "Next steps:"
        echo "  1. Upload to Google Play Console"
        echo "  2. Submit for internal testing"
    fi
}

# Main execution
main() {
    echo "=========================================="
    echo "  OpenRange Android Build"
    echo "=========================================="
    echo ""

    parse_args "$@"

    # Create log directory
    mkdir -p "$LOG_DIR"

    check_prerequisites
    build_native_plugin
    run_tests
    configure_android_settings
    build_android
    print_summary
}

main "$@"
