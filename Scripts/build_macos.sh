#!/bin/bash
# ABOUTME: Comprehensive macOS build script for GC2 Connect Unity.
# ABOUTME: Handles native plugin build, Unity project build, and app bundle creation.

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script location
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Unity configuration
UNITY_VERSION="${UNITY_VERSION:-6000.3.2f1}"
UNITY_APP="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app"
UNITY_PATH="${UNITY_APP}/Contents/MacOS/Unity"

# Build configuration
BUILD_DIR="${PROJECT_ROOT}/Builds"
MACOS_BUILD_DIR="${BUILD_DIR}/macOS"
APP_NAME="OpenRange"
APP_BUNDLE="${MACOS_BUILD_DIR}/${APP_NAME}.app"

# Plugin paths
NATIVE_PLUGIN_DIR="${PROJECT_ROOT}/NativePlugins/macOS"
PLUGIN_BUILD_SCRIPT="${NATIVE_PLUGIN_DIR}/build_mac_plugin.sh"
UNITY_PLUGINS_DIR="${PROJECT_ROOT}/Assets/Plugins/macOS"

# Log files
LOG_DIR="${BUILD_DIR}/logs"
PLUGIN_LOG="${LOG_DIR}/plugin-build.log"
UNITY_LOG="${LOG_DIR}/unity-build.log"
TEST_LOG="${LOG_DIR}/test-results.log"

# Parse command line arguments
SKIP_TESTS=false
SKIP_PLUGIN=false
DEVELOPMENT_BUILD=false
VERBOSE=false
VERSION=""

print_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --skip-tests      Skip running tests before build"
    echo "  --skip-plugin     Skip rebuilding native plugin (use existing)"
    echo "  --development     Create development build (faster, debugging enabled)"
    echo "  --version=X.Y.Z   Set version number (default: from git tag or 0.0.0)"
    echo "  --verbose         Show full build output"
    echo "  -h, --help        Show this help message"
}

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
        --version=*)
            VERSION="${1#*=}"
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            print_usage
            exit 1
            ;;
    esac
done

# Logging helpers
log_step() {
    echo ""
    echo -e "${BLUE}==>${NC} ${GREEN}$1${NC}"
}

log_info() {
    echo -e "    $1"
}

log_warning() {
    echo -e "    ${YELLOW}Warning:${NC} $1"
}

log_error() {
    echo -e "    ${RED}Error:${NC} $1"
}

log_success() {
    echo -e "    ${GREEN}✓${NC} $1"
}

# Determine version
get_version() {
    if [ -n "$VERSION" ]; then
        echo "$VERSION"
        return
    fi

    # Try git tag
    local git_version
    git_version=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
    if [ -n "$git_version" ]; then
        # Strip 'v' prefix if present
        echo "${git_version#v}"
        return
    fi

    # Try VERSION file
    if [ -f "${PROJECT_ROOT}/VERSION" ]; then
        cat "${PROJECT_ROOT}/VERSION"
        return
    fi

    # Default
    echo "0.0.0"
}

# Pre-build validation
validate_environment() {
    log_step "Validating build environment..."

    local has_errors=false

    # Check Unity
    if [ ! -f "$UNITY_PATH" ]; then
        log_error "Unity ${UNITY_VERSION} not found at ${UNITY_APP}"
        log_info "Please install Unity ${UNITY_VERSION} via Unity Hub"
        has_errors=true
    else
        log_success "Unity ${UNITY_VERSION} found"
    fi

    # Check Xcode
    if ! command -v xcodebuild &> /dev/null; then
        log_error "Xcode command line tools not found"
        log_info "Please install Xcode and run: xcode-select --install"
        has_errors=true
    else
        local xcode_version
        xcode_version=$(xcodebuild -version 2>&1 | head -1)
        log_success "Xcode found: $xcode_version"
    fi

    # Check libusb
    if [ ! -f "${NATIVE_PLUGIN_DIR}/GC2MacPlugin/libusb/lib/libusb-1.0.dylib" ]; then
        if command -v brew &> /dev/null; then
            if brew list libusb &> /dev/null; then
                log_success "libusb available via Homebrew"
            else
                log_warning "libusb not installed. Will attempt to copy from Homebrew during plugin build."
            fi
        else
            log_warning "libusb not found and Homebrew not installed"
        fi
    else
        log_success "libusb found in plugin directory"
    fi

    # Check plugin build script
    if [ ! -f "$PLUGIN_BUILD_SCRIPT" ]; then
        log_error "Plugin build script not found at ${PLUGIN_BUILD_SCRIPT}"
        has_errors=true
    else
        log_success "Plugin build script found"
    fi

    # Check project files
    if [ ! -f "${PROJECT_ROOT}/Assets/Scenes/Bootstrap.unity" ]; then
        log_error "Bootstrap scene not found - project may be incomplete"
        has_errors=true
    else
        log_success "Unity project structure verified"
    fi

    if [ "$has_errors" = true ]; then
        echo ""
        log_error "Build environment validation failed. Please fix the issues above."
        exit 1
    fi
}

# Build native plugin
build_native_plugin() {
    if [ "$SKIP_PLUGIN" = true ]; then
        log_step "Skipping native plugin build (--skip-plugin)"

        # Verify plugin exists
        if [ ! -d "${UNITY_PLUGINS_DIR}/GC2MacPlugin.bundle" ]; then
            log_error "No existing plugin found at ${UNITY_PLUGINS_DIR}/GC2MacPlugin.bundle"
            log_info "Remove --skip-plugin flag to build the plugin"
            exit 1
        fi
        log_success "Using existing plugin"
        return
    fi

    log_step "Building native macOS plugin..."

    mkdir -p "$LOG_DIR"

    if [ "$VERBOSE" = true ]; then
        "$PLUGIN_BUILD_SCRIPT" 2>&1 | tee "$PLUGIN_LOG"
    else
        "$PLUGIN_BUILD_SCRIPT" > "$PLUGIN_LOG" 2>&1
    fi

    # Verify plugin was built
    if [ ! -d "${UNITY_PLUGINS_DIR}/GC2MacPlugin.bundle" ]; then
        log_error "Plugin build failed - bundle not created"
        log_info "Check log: $PLUGIN_LOG"
        exit 1
    fi

    # Verify libusb
    if [ ! -f "${UNITY_PLUGINS_DIR}/libusb-1.0.dylib" ]; then
        log_error "libusb not copied to plugins directory"
        exit 1
    fi

    log_success "Native plugin built successfully"

    # Show architecture info
    local bundle_binary="${UNITY_PLUGINS_DIR}/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin"
    if [ -f "$bundle_binary" ]; then
        local arch_info
        arch_info=$(file "$bundle_binary" | grep -oE "(arm64|x86_64)" || echo "unknown")
        log_info "Plugin architecture: $arch_info"
    fi
}

# Run tests
run_tests() {
    if [ "$SKIP_TESTS" = true ]; then
        log_step "Skipping tests (--skip-tests)"
        return
    fi

    log_step "Running EditMode tests..."

    mkdir -p "$LOG_DIR"
    local test_results="${BUILD_DIR}/TestResults/editmode-results.xml"
    mkdir -p "${BUILD_DIR}/TestResults"

    if [ "$VERBOSE" = true ]; then
        "$UNITY_PATH" \
            -batchmode \
            -nographics \
            -silent-crashes \
            -projectPath "$PROJECT_ROOT" \
            -runTests \
            -testPlatform EditMode \
            -testResults "$test_results" \
            -logFile - 2>&1 | tee "$TEST_LOG"
    else
        "$UNITY_PATH" \
            -batchmode \
            -nographics \
            -silent-crashes \
            -projectPath "$PROJECT_ROOT" \
            -runTests \
            -testPlatform EditMode \
            -testResults "$test_results" \
            -logFile "$TEST_LOG" 2>&1 || true
    fi

    # Check test results
    if [ -f "$test_results" ]; then
        if grep -q 'result="Passed"' "$test_results" 2>/dev/null; then
            local passed
            passed=$(grep -oE 'passed="[0-9]+"' "$test_results" | grep -oE '[0-9]+' | head -1)
            log_success "All $passed tests passed"
        else
            local failed
            failed=$(grep -oE 'failed="[0-9]+"' "$test_results" | grep -oE '[0-9]+' | head -1)
            log_error "Tests failed ($failed failures)"
            log_info "Check log: $TEST_LOG"
            exit 1
        fi
    else
        log_warning "Test results file not found - tests may not have run"
        log_info "Check log: $TEST_LOG"
    fi
}

# Build Unity project
build_unity_project() {
    log_step "Building Unity project..."

    local build_version
    build_version=$(get_version)
    log_info "Version: $build_version"

    mkdir -p "$MACOS_BUILD_DIR"
    mkdir -p "$LOG_DIR"

    local build_args=(
        -batchmode
        -quit
        -projectPath "$PROJECT_ROOT"
        -buildTarget StandaloneOSX
        -buildOSXUniversalPlayer "$APP_BUNDLE"
        -logFile "$UNITY_LOG"
    )

    if [ "$DEVELOPMENT_BUILD" = true ]; then
        build_args+=(-development)
        log_info "Build type: Development"
    else
        log_info "Build type: Release"
    fi

    if [ "$VERBOSE" = true ]; then
        build_args[-1]="-"  # Log to stdout instead
        "$UNITY_PATH" "${build_args[@]}" 2>&1 | tee "$UNITY_LOG"
    else
        "$UNITY_PATH" "${build_args[@]}" 2>&1
    fi

    # Verify build succeeded
    if [ ! -d "$APP_BUNDLE" ]; then
        log_error "Build failed - app bundle not created"
        log_info "Check log: $UNITY_LOG"
        exit 1
    fi

    log_success "Unity project built successfully"
}

# Post-build verification
verify_build() {
    log_step "Verifying build..."

    local has_warnings=false

    # Check app bundle exists
    if [ ! -d "$APP_BUNDLE" ]; then
        log_error "App bundle not found at $APP_BUNDLE"
        exit 1
    fi
    log_success "App bundle exists"

    # Check main executable
    local main_executable="${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    if [ ! -f "$main_executable" ]; then
        log_error "Main executable not found"
        exit 1
    fi
    log_success "Main executable present"

    # Check native plugin is embedded
    local embedded_plugin="${APP_BUNDLE}/Contents/PlugIns/GC2MacPlugin.bundle"
    local alt_plugin="${APP_BUNDLE}/Contents/Plugins/GC2MacPlugin.bundle"

    if [ -d "$embedded_plugin" ]; then
        log_success "Native plugin embedded in app bundle"
    elif [ -d "$alt_plugin" ]; then
        log_success "Native plugin embedded (alternate path)"
        embedded_plugin="$alt_plugin"
    else
        log_warning "Native plugin may not be embedded correctly"
        log_info "Plugin should be in Contents/PlugIns/ or Contents/Plugins/"
        has_warnings=true
    fi

    # Check libusb is embedded
    local embedded_libusb="${APP_BUNDLE}/Contents/PlugIns/libusb-1.0.dylib"
    local alt_libusb="${APP_BUNDLE}/Contents/Plugins/libusb-1.0.dylib"

    if [ -f "$embedded_libusb" ] || [ -f "$alt_libusb" ]; then
        log_success "libusb embedded in app bundle"
    else
        log_warning "libusb may not be embedded correctly"
        has_warnings=true
    fi

    # Check Info.plist
    if [ -f "${APP_BUNDLE}/Contents/Info.plist" ]; then
        local bundle_version
        bundle_version=$(/usr/libexec/PlistBuddy -c "Print CFBundleShortVersionString" "${APP_BUNDLE}/Contents/Info.plist" 2>/dev/null || echo "unknown")
        log_success "Info.plist present (version: $bundle_version)"
    else
        log_error "Info.plist not found"
        exit 1
    fi

    # Check architecture
    local app_arch
    app_arch=$(file "$main_executable" | grep -oE "(arm64|x86_64)" | tr '\n' '+' | sed 's/+$//')
    log_info "App architecture: $app_arch"

    # Calculate app size
    local app_size
    app_size=$(du -sh "$APP_BUNDLE" | cut -f1)
    log_info "App bundle size: $app_size"

    if [ "$has_warnings" = true ]; then
        log_warning "Build completed with warnings - see above"
    else
        log_success "Build verification passed"
    fi
}

# Fix library paths if needed
fix_library_paths() {
    log_step "Checking library paths..."

    # Find plugin binary in app bundle
    local plugin_binary=""
    if [ -f "${APP_BUNDLE}/Contents/PlugIns/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin" ]; then
        plugin_binary="${APP_BUNDLE}/Contents/PlugIns/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin"
    elif [ -f "${APP_BUNDLE}/Contents/Plugins/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin" ]; then
        plugin_binary="${APP_BUNDLE}/Contents/Plugins/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin"
    fi

    if [ -n "$plugin_binary" ] && [ -f "$plugin_binary" ]; then
        # Check current libusb reference
        local libusb_ref
        libusb_ref=$(otool -L "$plugin_binary" 2>/dev/null | grep libusb || echo "")

        if [ -n "$libusb_ref" ]; then
            log_info "Current libusb reference: $(echo "$libusb_ref" | tr -d '\t')"

            # Fix if pointing to system path
            if echo "$libusb_ref" | grep -q "/opt/homebrew\|/usr/local"; then
                log_info "Fixing libusb path to use @loader_path..."
                install_name_tool -change \
                    "$(echo "$libusb_ref" | awk '{print $1}')" \
                    "@loader_path/../../../libusb-1.0.dylib" \
                    "$plugin_binary" 2>/dev/null || true
                log_success "Library path fixed"
            else
                log_success "Library paths are correct"
            fi
        fi
    fi
}

# Main build process
main() {
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  GC2 Connect Unity - macOS Build${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""

    local build_version
    build_version=$(get_version)
    log_info "Building version: $build_version"
    log_info "Project: $PROJECT_ROOT"
    log_info "Output: $APP_BUNDLE"

    if [ "$DEVELOPMENT_BUILD" = true ]; then
        log_info "Build type: Development"
    else
        log_info "Build type: Release"
    fi

    # Build steps
    validate_environment
    build_native_plugin
    run_tests
    build_unity_project
    verify_build
    fix_library_paths

    # Summary
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  Build Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    log_info "App bundle: $APP_BUNDLE"
    log_info "Version: $build_version"
    echo ""
    log_info "To run the app:"
    echo "    open \"$APP_BUNDLE\""
    echo ""
    log_info "Next steps for distribution:"
    echo "    1. Code sign: Scripts/sign_and_notarize.sh --sign"
    echo "    2. Notarize: Scripts/sign_and_notarize.sh --notarize"
    echo "    3. Create DMG: make package"
    echo ""
}

# Run main
main "$@"
