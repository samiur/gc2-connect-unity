#!/bin/bash
# ABOUTME: Build script for the macOS GC2 native plugin.
# ABOUTME: Builds universal binary and copies to Unity Assets/Plugins/macOS/.

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Building GC2MacPlugin ===${NC}"

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_DIR="$SCRIPT_DIR/GC2MacPlugin"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
UNITY_PLUGINS_DIR="$PROJECT_ROOT/Assets/Plugins/macOS"

# Check for Xcode
if ! command -v xcodebuild &> /dev/null; then
    echo -e "${RED}Error: xcodebuild not found. Please install Xcode.${NC}"
    exit 1
fi

# Check for project
if [ ! -d "$PLUGIN_DIR/GC2MacPlugin.xcodeproj" ]; then
    echo -e "${RED}Error: Xcode project not found at $PLUGIN_DIR/GC2MacPlugin.xcodeproj${NC}"
    exit 1
fi

# Check for libusb
if [ ! -f "$PLUGIN_DIR/libusb/lib/libusb-1.0.dylib" ]; then
    echo -e "${YELLOW}Warning: libusb not found. Attempting to copy from Homebrew...${NC}"
    if [ -f "/opt/homebrew/opt/libusb/lib/libusb-1.0.0.dylib" ]; then
        mkdir -p "$PLUGIN_DIR/libusb/lib"
        cp /opt/homebrew/opt/libusb/lib/libusb-1.0.0.dylib "$PLUGIN_DIR/libusb/lib/libusb-1.0.dylib"
        echo -e "${GREEN}Copied libusb from Homebrew (arm64)${NC}"
    elif [ -f "/usr/local/opt/libusb/lib/libusb-1.0.0.dylib" ]; then
        mkdir -p "$PLUGIN_DIR/libusb/lib"
        cp /usr/local/opt/libusb/lib/libusb-1.0.0.dylib "$PLUGIN_DIR/libusb/lib/libusb-1.0.dylib"
        echo -e "${GREEN}Copied libusb from Homebrew (x86_64)${NC}"
    else
        echo -e "${RED}Error: libusb not found. Please install with: brew install libusb${NC}"
        exit 1
    fi
fi

# Create output directory
mkdir -p "$UNITY_PLUGINS_DIR"

# Clean previous build
echo "Cleaning previous build..."
xcodebuild -project "$PLUGIN_DIR/GC2MacPlugin.xcodeproj" \
           -scheme GC2MacPlugin \
           -configuration Release \
           clean 2>/dev/null || true

# Build for current architecture only
# Note: Building universal requires libusb for both architectures
# For now, build for native architecture only (arm64 on Apple Silicon, x86_64 on Intel)
echo "Building GC2MacPlugin..."

BUILD_DIR="$PLUGIN_DIR/build"
DERIVED_DATA="$BUILD_DIR/DerivedData"

# Determine current architecture
CURRENT_ARCH=$(uname -m)
if [ "$CURRENT_ARCH" = "arm64" ]; then
    BUILD_ARCH="arm64"
else
    BUILD_ARCH="x86_64"
fi
echo "Building for architecture: $BUILD_ARCH"

xcodebuild -project "$PLUGIN_DIR/GC2MacPlugin.xcodeproj" \
           -scheme GC2MacPlugin \
           -configuration Release \
           -derivedDataPath "$DERIVED_DATA" \
           ARCHS="$BUILD_ARCH" \
           ONLY_ACTIVE_ARCH=YES \
           build

# Find the built bundle
BUILT_BUNDLE="$DERIVED_DATA/Build/Products/Release/GC2MacPlugin.bundle"
if [ ! -d "$BUILT_BUNDLE" ]; then
    echo -e "${RED}Error: Build failed - bundle not found${NC}"
    exit 1
fi

# Copy to Unity
echo "Copying to Unity..."
rm -rf "$UNITY_PLUGINS_DIR/GC2MacPlugin.bundle"
cp -R "$BUILT_BUNDLE" "$UNITY_PLUGINS_DIR/"

# Copy libusb dylib alongside bundle
cp "$PLUGIN_DIR/libusb/lib/libusb-1.0.dylib" "$UNITY_PLUGINS_DIR/"

# Fix dylib rpath for the bundle
echo "Fixing library paths..."
BUNDLE_BINARY="$UNITY_PLUGINS_DIR/GC2MacPlugin.bundle/Contents/MacOS/GC2MacPlugin"
if [ -f "$BUNDLE_BINARY" ]; then
    # Update libusb reference to look in same directory
    install_name_tool -change \
        "@rpath/libusb-1.0.dylib" \
        "@loader_path/../../../libusb-1.0.dylib" \
        "$BUNDLE_BINARY" 2>/dev/null || true
fi

# Verify
echo ""
echo -e "${GREEN}=== Build Complete ===${NC}"
echo "Bundle: $UNITY_PLUGINS_DIR/GC2MacPlugin.bundle"
echo "libusb: $UNITY_PLUGINS_DIR/libusb-1.0.dylib"
echo ""

# Show architecture info
echo "Architecture info:"
file "$BUNDLE_BINARY" 2>/dev/null || echo "  Bundle binary not found (expected for stub build)"
file "$UNITY_PLUGINS_DIR/libusb-1.0.dylib"

echo ""
echo -e "${YELLOW}Note: For universal (x86_64 + arm64) builds, you need:${NC}"
echo "  1. libusb compiled for both architectures"
echo "  2. Use lipo to create universal dylib"
echo "  3. Or install Rosetta 2 for Intel compatibility"
