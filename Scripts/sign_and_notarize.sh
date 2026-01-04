#!/bin/bash
# ABOUTME: Code signing and notarization script for macOS distribution.
# ABOUTME: Signs app bundle with Developer ID and submits for Apple notarization.

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

# Build configuration
BUILD_DIR="${PROJECT_ROOT}/Builds"
MACOS_BUILD_DIR="${BUILD_DIR}/macOS"
APP_NAME="OpenRange"
APP_BUNDLE="${MACOS_BUILD_DIR}/${APP_NAME}.app"
ENTITLEMENTS="${SCRIPT_DIR}/entitlements.plist"

# Distribution
DIST_DIR="${BUILD_DIR}/dist"

# Environment variables (should be set externally or via --env-file)
# APPLE_TEAM_ID - Your Apple Developer Team ID (e.g., ABCD1234EF)
# APPLE_DEVELOPER_ID - Certificate name (e.g., "Developer ID Application: Your Name (TEAMID)")
# APPLE_ID - Your Apple ID email for notarization
# APPLE_APP_PASSWORD - App-specific password from appleid.apple.com

# Parse command line arguments
SIGN_ONLY=false
NOTARIZE_ONLY=false
CREATE_DMG=false
VERBOSE=false
DRY_RUN=false

print_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --sign          Sign the app bundle only"
    echo "  --notarize      Notarize only (assumes already signed)"
    echo "  --dmg           Create DMG after signing/notarizing"
    echo "  --all           Full pipeline: sign, notarize, create DMG"
    echo "  --dry-run       Show what would be done without executing"
    echo "  --verbose       Show detailed output"
    echo "  -h, --help      Show this help message"
    echo ""
    echo "Environment Variables Required:"
    echo "  APPLE_TEAM_ID        Apple Developer Team ID"
    echo "  APPLE_DEVELOPER_ID   Developer ID Application certificate name"
    echo "  APPLE_ID             Apple ID email for notarization"
    echo "  APPLE_APP_PASSWORD   App-specific password for notarization"
    echo ""
    echo "Examples:"
    echo "  $0 --sign              # Sign app bundle"
    echo "  $0 --notarize          # Submit for notarization"
    echo "  $0 --all               # Full release pipeline"
    echo "  $0 --sign --dmg        # Sign and create DMG (skip notarization)"
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --sign)
            SIGN_ONLY=true
            shift
            ;;
        --notarize)
            NOTARIZE_ONLY=true
            shift
            ;;
        --dmg)
            CREATE_DMG=true
            shift
            ;;
        --all)
            SIGN_ONLY=false
            NOTARIZE_ONLY=false
            CREATE_DMG=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
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

# Run command (respects dry-run mode)
run_cmd() {
    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] $*"
    else
        if [ "$VERBOSE" = true ]; then
            "$@"
        else
            "$@" > /dev/null 2>&1
        fi
    fi
}

# Validate environment
validate_environment() {
    log_step "Validating environment..."

    local has_errors=false

    # Check app bundle exists
    if [ ! -d "$APP_BUNDLE" ]; then
        log_error "App bundle not found at $APP_BUNDLE"
        log_info "Run 'make build-app' first to create the app bundle"
        has_errors=true
    else
        log_success "App bundle found"
    fi

    # Check entitlements file
    if [ ! -f "$ENTITLEMENTS" ]; then
        log_error "Entitlements file not found at $ENTITLEMENTS"
        has_errors=true
    else
        log_success "Entitlements file found"
    fi

    # Check codesign
    if ! command -v codesign &> /dev/null; then
        log_error "codesign not found. Install Xcode command line tools."
        has_errors=true
    else
        log_success "codesign available"
    fi

    # Check notarytool (for notarization)
    if [ "$NOTARIZE_ONLY" = true ] || [ "$SIGN_ONLY" = false ]; then
        if ! command -v xcrun &> /dev/null; then
            log_error "xcrun not found. Install Xcode command line tools."
            has_errors=true
        else
            log_success "xcrun available"
        fi
    fi

    # Check required environment variables for signing
    if [ "$NOTARIZE_ONLY" = false ]; then
        if [ -z "$APPLE_DEVELOPER_ID" ]; then
            log_error "APPLE_DEVELOPER_ID not set"
            log_info "Set to your certificate name, e.g., 'Developer ID Application: Your Name (TEAMID)'"
            has_errors=true
        else
            log_success "APPLE_DEVELOPER_ID set"

            # Verify certificate exists in keychain
            if [ "$DRY_RUN" = false ]; then
                if ! security find-identity -v -p codesigning | grep -q "$APPLE_DEVELOPER_ID"; then
                    log_warning "Certificate '$APPLE_DEVELOPER_ID' not found in keychain"
                    log_info "Available signing identities:"
                    security find-identity -v -p codesigning | head -5
                fi
            fi
        fi
    fi

    # Check required environment variables for notarization
    if [ "$SIGN_ONLY" = false ]; then
        if [ -z "$APPLE_ID" ]; then
            log_error "APPLE_ID not set (your Apple ID email)"
            has_errors=true
        else
            log_success "APPLE_ID set"
        fi

        if [ -z "$APPLE_TEAM_ID" ]; then
            log_error "APPLE_TEAM_ID not set"
            has_errors=true
        else
            log_success "APPLE_TEAM_ID set"
        fi

        if [ -z "$APPLE_APP_PASSWORD" ]; then
            log_error "APPLE_APP_PASSWORD not set"
            log_info "Create at: https://appleid.apple.com/account/manage > App-Specific Passwords"
            has_errors=true
        else
            log_success "APPLE_APP_PASSWORD set"
        fi
    fi

    if [ "$has_errors" = true ]; then
        echo ""
        log_error "Environment validation failed. Please fix the issues above."
        exit 1
    fi
}

# Sign a single binary or bundle
sign_binary() {
    local binary_path="$1"
    local binary_name="$(basename "$binary_path")"

    log_info "Signing $binary_name..."

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] codesign --force --options runtime --entitlements $ENTITLEMENTS --sign \"$APPLE_DEVELOPER_ID\" \"$binary_path\""
        return
    fi

    codesign \
        --force \
        --options runtime \
        --entitlements "$ENTITLEMENTS" \
        --sign "$APPLE_DEVELOPER_ID" \
        --timestamp \
        "$binary_path"

    # Verify signature
    if codesign --verify --strict "$binary_path" 2>/dev/null; then
        log_success "$binary_name signed successfully"
    else
        log_error "Failed to sign $binary_name"
        exit 1
    fi
}

# Sign the app bundle
sign_app() {
    log_step "Signing app bundle..."

    # Sign libusb first (innermost)
    local libusb_path=""
    if [ -f "${APP_BUNDLE}/Contents/PlugIns/libusb-1.0.dylib" ]; then
        libusb_path="${APP_BUNDLE}/Contents/PlugIns/libusb-1.0.dylib"
    elif [ -f "${APP_BUNDLE}/Contents/Plugins/libusb-1.0.dylib" ]; then
        libusb_path="${APP_BUNDLE}/Contents/Plugins/libusb-1.0.dylib"
    fi

    if [ -n "$libusb_path" ]; then
        sign_binary "$libusb_path"
    else
        log_warning "libusb not found in app bundle"
    fi

    # Sign plugin bundle
    local plugin_path=""
    if [ -d "${APP_BUNDLE}/Contents/PlugIns/GC2MacPlugin.bundle" ]; then
        plugin_path="${APP_BUNDLE}/Contents/PlugIns/GC2MacPlugin.bundle"
    elif [ -d "${APP_BUNDLE}/Contents/Plugins/GC2MacPlugin.bundle" ]; then
        plugin_path="${APP_BUNDLE}/Contents/Plugins/GC2MacPlugin.bundle"
    fi

    if [ -n "$plugin_path" ]; then
        sign_binary "$plugin_path"
    else
        log_warning "GC2MacPlugin.bundle not found in app bundle"
    fi

    # Sign main app bundle (outermost, with deep)
    log_info "Signing main app bundle (deep)..."

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] codesign --deep --force --options runtime --entitlements $ENTITLEMENTS --sign \"$APPLE_DEVELOPER_ID\" \"$APP_BUNDLE\""
    else
        codesign \
            --deep \
            --force \
            --options runtime \
            --entitlements "$ENTITLEMENTS" \
            --sign "$APPLE_DEVELOPER_ID" \
            --timestamp \
            "$APP_BUNDLE"
    fi

    # Verify the complete app
    log_info "Verifying app bundle signature..."

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] codesign --verify --deep --strict \"$APP_BUNDLE\""
    else
        if codesign --verify --deep --strict "$APP_BUNDLE"; then
            log_success "App bundle signed and verified"
        else
            log_error "App bundle verification failed"
            exit 1
        fi

        # Show signature info
        if [ "$VERBOSE" = true ]; then
            echo ""
            log_info "Signature details:"
            codesign -dv --verbose=4 "$APP_BUNDLE" 2>&1 | head -20
        fi
    fi
}

# Submit for notarization
notarize_app() {
    log_step "Submitting for notarization..."

    mkdir -p "$DIST_DIR"
    local zip_path="${DIST_DIR}/${APP_NAME}-notarization.zip"

    # Create ZIP for notarization
    log_info "Creating ZIP for notarization..."

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] ditto -c -k --keepParent \"$APP_BUNDLE\" \"$zip_path\""
    else
        rm -f "$zip_path"
        ditto -c -k --keepParent "$APP_BUNDLE" "$zip_path"
        log_success "Created $zip_path ($(du -h "$zip_path" | cut -f1))"
    fi

    # Submit to Apple
    log_info "Submitting to Apple notary service..."
    log_info "(This may take several minutes)"

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] xcrun notarytool submit \"$zip_path\" --apple-id \"$APPLE_ID\" --team-id \"$APPLE_TEAM_ID\" --password \"***\" --wait"
    else
        local submit_output
        submit_output=$(xcrun notarytool submit "$zip_path" \
            --apple-id "$APPLE_ID" \
            --team-id "$APPLE_TEAM_ID" \
            --password "$APPLE_APP_PASSWORD" \
            --wait 2>&1)

        echo "$submit_output"

        # Check if successful
        if echo "$submit_output" | grep -q "status: Accepted"; then
            log_success "Notarization accepted!"

            # Extract submission ID for logs
            local submission_id
            submission_id=$(echo "$submit_output" | grep -oE "id: [a-f0-9-]+" | head -1 | cut -d' ' -f2)

            if [ -n "$submission_id" ]; then
                log_info "Submission ID: $submission_id"

                # Get detailed log
                if [ "$VERBOSE" = true ]; then
                    echo ""
                    log_info "Notarization log:"
                    xcrun notarytool log "$submission_id" \
                        --apple-id "$APPLE_ID" \
                        --team-id "$APPLE_TEAM_ID" \
                        --password "$APPLE_APP_PASSWORD" 2>&1 | head -30
                fi
            fi
        else
            log_error "Notarization failed"

            # Try to get error details
            local submission_id
            submission_id=$(echo "$submit_output" | grep -oE "id: [a-f0-9-]+" | head -1 | cut -d' ' -f2)

            if [ -n "$submission_id" ]; then
                echo ""
                log_info "Fetching notarization log for details..."
                xcrun notarytool log "$submission_id" \
                    --apple-id "$APPLE_ID" \
                    --team-id "$APPLE_TEAM_ID" \
                    --password "$APPLE_APP_PASSWORD" 2>&1
            fi

            exit 1
        fi
    fi

    # Staple ticket to app
    log_info "Stapling notarization ticket to app..."

    if [ "$DRY_RUN" = true ]; then
        echo "    [DRY-RUN] xcrun stapler staple \"$APP_BUNDLE\""
    else
        if xcrun stapler staple "$APP_BUNDLE"; then
            log_success "Notarization ticket stapled"
        else
            log_error "Failed to staple notarization ticket"
            exit 1
        fi
    fi

    # Clean up
    if [ "$DRY_RUN" = false ]; then
        rm -f "$zip_path"
        log_info "Cleaned up temporary ZIP"
    fi
}

# Get version from app bundle
get_version() {
    if [ -f "${APP_BUNDLE}/Contents/Info.plist" ]; then
        /usr/libexec/PlistBuddy -c "Print CFBundleShortVersionString" "${APP_BUNDLE}/Contents/Info.plist" 2>/dev/null || echo "1.0.0"
    else
        echo "1.0.0"
    fi
}

# Create DMG
create_dmg() {
    log_step "Creating DMG..."

    mkdir -p "$DIST_DIR"

    local version
    version=$(get_version)
    local dmg_name="${APP_NAME}-${version}.dmg"
    local dmg_path="${DIST_DIR}/${dmg_name}"

    # Remove existing DMG
    rm -f "$dmg_path"

    # Check for create-dmg (pretty DMG)
    if command -v create-dmg &> /dev/null; then
        log_info "Using create-dmg for styled DMG..."

        if [ "$DRY_RUN" = true ]; then
            echo "    [DRY-RUN] create-dmg ... \"$dmg_path\" \"$APP_BUNDLE\""
        else
            create-dmg \
                --volname "$APP_NAME" \
                --window-pos 200 120 \
                --window-size 600 400 \
                --icon-size 100 \
                --icon "$APP_NAME.app" 175 120 \
                --hide-extension "$APP_NAME.app" \
                --app-drop-link 425 120 \
                "$dmg_path" \
                "$APP_BUNDLE" 2>&1 || true

            # create-dmg returns non-zero even on success sometimes
            if [ -f "$dmg_path" ]; then
                log_success "Created styled DMG"
            else
                log_warning "create-dmg failed, falling back to hdiutil"
                hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" \
                    -ov -format UDZO "$dmg_path"
            fi
        fi
    else
        log_info "Using hdiutil for simple DMG..."

        if [ "$DRY_RUN" = true ]; then
            echo "    [DRY-RUN] hdiutil create -volname \"$APP_NAME\" -srcfolder \"$APP_BUNDLE\" -ov -format UDZO \"$dmg_path\""
        else
            hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" \
                -ov -format UDZO "$dmg_path"
        fi
    fi

    # Sign the DMG
    if [ -n "$APPLE_DEVELOPER_ID" ]; then
        log_info "Signing DMG..."

        if [ "$DRY_RUN" = true ]; then
            echo "    [DRY-RUN] codesign --sign \"$APPLE_DEVELOPER_ID\" \"$dmg_path\""
        else
            codesign --sign "$APPLE_DEVELOPER_ID" --timestamp "$dmg_path"
            log_success "DMG signed"
        fi

        # Notarize DMG if we're doing full pipeline
        if [ "$SIGN_ONLY" = false ] && [ -n "$APPLE_ID" ]; then
            log_info "Notarizing DMG..."

            if [ "$DRY_RUN" = true ]; then
                echo "    [DRY-RUN] xcrun notarytool submit \"$dmg_path\" ... --wait"
            else
                local submit_output
                submit_output=$(xcrun notarytool submit "$dmg_path" \
                    --apple-id "$APPLE_ID" \
                    --team-id "$APPLE_TEAM_ID" \
                    --password "$APPLE_APP_PASSWORD" \
                    --wait 2>&1)

                if echo "$submit_output" | grep -q "status: Accepted"; then
                    xcrun stapler staple "$dmg_path"
                    log_success "DMG notarized and stapled"
                else
                    log_warning "DMG notarization failed (app is still notarized)"
                fi
            fi
        fi
    fi

    if [ "$DRY_RUN" = false ] && [ -f "$dmg_path" ]; then
        log_success "DMG created: $dmg_path ($(du -h "$dmg_path" | cut -f1))"
    fi
}

# Main
main() {
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  GC2 Connect - Code Signing${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""

    if [ "$DRY_RUN" = true ]; then
        log_warning "DRY-RUN MODE - No changes will be made"
    fi

    # Validate environment
    validate_environment

    # Execute requested operations
    if [ "$NOTARIZE_ONLY" = true ]; then
        notarize_app
    elif [ "$SIGN_ONLY" = true ]; then
        sign_app
        if [ "$CREATE_DMG" = true ]; then
            create_dmg
        fi
    else
        # Full pipeline
        sign_app
        notarize_app
        if [ "$CREATE_DMG" = true ]; then
            create_dmg
        fi
    fi

    # Summary
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}  Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""

    log_info "App bundle: $APP_BUNDLE"

    if [ "$CREATE_DMG" = true ]; then
        local version
        version=$(get_version)
        log_info "DMG: ${DIST_DIR}/${APP_NAME}-${version}.dmg"
    fi

    echo ""
    log_info "Next steps:"
    echo "    1. Test the app on a fresh Mac"
    echo "    2. Verify no Gatekeeper warnings appear"
    echo "    3. Upload to distribution channel"
    echo ""
}

# Run main
main "$@"
