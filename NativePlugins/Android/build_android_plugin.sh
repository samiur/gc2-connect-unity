#!/bin/bash

# ABOUTME: Build script for the GC2 Android Plugin.
# ABOUTME: Builds the AAR and copies it to the Unity Plugins directory.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/GC2AndroidPlugin"
UNITY_PLUGINS_DIR="$SCRIPT_DIR/../../Assets/Plugins/Android"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

echo_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

echo_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check for Android SDK
if [ -z "$ANDROID_HOME" ]; then
    # Try default locations
    if [ -d "$HOME/Library/Android/sdk" ]; then
        export ANDROID_HOME="$HOME/Library/Android/sdk"
    elif [ -d "/usr/local/share/android-sdk" ]; then
        export ANDROID_HOME="/usr/local/share/android-sdk"
    else
        echo_error "ANDROID_HOME not set and SDK not found at default locations"
        echo "Please set ANDROID_HOME to your Android SDK path"
        exit 1
    fi
fi

echo_info "Using Android SDK at: $ANDROID_HOME"

# Set up Java from Android Studio if not already available
if [ -z "$JAVA_HOME" ]; then
    if [ -d "/Applications/Android Studio.app/Contents/jbr/Contents/Home" ]; then
        export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
    fi
fi

# Add Java to PATH if JAVA_HOME is set
if [ -n "$JAVA_HOME" ]; then
    export PATH="$JAVA_HOME/bin:$PATH"
fi

# Verify Java is available
if ! command -v java &> /dev/null; then
    echo_error "Java not found. Please install JDK or Android Studio."
    exit 1
fi

echo_info "Using Java: $(java -version 2>&1 | head -1)"

# Navigate to project directory
cd "$PROJECT_DIR"

# Parse arguments
BUILD_TYPE="release"
CLEAN_BUILD=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --debug)
            BUILD_TYPE="debug"
            shift
            ;;
        --clean)
            CLEAN_BUILD=true
            shift
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  --debug    Build debug variant instead of release"
            echo "  --clean    Clean before building"
            echo "  --help     Show this help message"
            exit 0
            ;;
        *)
            echo_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Clean if requested
if [ "$CLEAN_BUILD" = true ]; then
    echo_info "Cleaning build directory..."
    ./gradlew clean
fi

# Build the AAR
echo_info "Building GC2AndroidPlugin ($BUILD_TYPE)..."

if [ "$BUILD_TYPE" = "release" ]; then
    ./gradlew assembleRelease
    AAR_PATH="$PROJECT_DIR/build/outputs/aar/GC2AndroidPlugin-release.aar"
else
    ./gradlew assembleDebug
    AAR_PATH="$PROJECT_DIR/build/outputs/aar/GC2AndroidPlugin-debug.aar"
fi

# Check if AAR was created
if [ ! -f "$AAR_PATH" ]; then
    echo_error "AAR not found at: $AAR_PATH"
    echo "Build may have failed. Check the output above for errors."
    exit 1
fi

echo_info "AAR created: $AAR_PATH"

# Create Unity plugins directory if it doesn't exist
mkdir -p "$UNITY_PLUGINS_DIR"

# Copy AAR to Unity
DEST_PATH="$UNITY_PLUGINS_DIR/GC2AndroidPlugin.aar"
cp "$AAR_PATH" "$DEST_PATH"

echo_info "Copied AAR to: $DEST_PATH"

# Verify the AAR
echo_info "Verifying AAR contents..."
unzip -l "$DEST_PATH" | head -20

echo ""
echo_info "Build complete!"
echo_info "AAR location: $DEST_PATH"
echo ""
echo "Next steps:"
echo "  1. Open Unity and check Assets/Plugins/Android/"
echo "  2. The AAR should be automatically detected"
echo "  3. Build your Unity project for Android"
