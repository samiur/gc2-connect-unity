#!/bin/bash
# ABOUTME: Setup script for code signing in CI environments.
# ABOUTME: Imports certificates from base64 environment variables into a temporary keychain.

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# Configuration
KEYCHAIN_NAME="build.keychain"
KEYCHAIN_PATH="$HOME/Library/Keychains/${KEYCHAIN_NAME}-db"

# Environment variables required:
# APPLE_CERTIFICATE_BASE64 - Base64-encoded .p12 certificate
# APPLE_CERTIFICATE_PASSWORD - Password for the .p12 file
# KEYCHAIN_PASSWORD - Password for the temporary keychain

print_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --setup         Create keychain and import certificates"
    echo "  --cleanup       Remove temporary keychain"
    echo "  -h, --help      Show this help message"
    echo ""
    echo "Environment Variables Required:"
    echo "  APPLE_CERTIFICATE_BASE64    Base64-encoded .p12 certificate"
    echo "  APPLE_CERTIFICATE_PASSWORD  Password for the .p12 file"
    echo "  KEYCHAIN_PASSWORD           Password for the temporary keychain"
    echo ""
    echo "Examples:"
    echo "  $0 --setup      # Setup keychain for signing"
    echo "  $0 --cleanup    # Remove temporary keychain after build"
}

# Parse arguments
ACTION=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --setup)
            ACTION="setup"
            shift
            ;;
        --cleanup)
            ACTION="cleanup"
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

if [ -z "$ACTION" ]; then
    echo -e "${RED}Error: No action specified${NC}"
    print_usage
    exit 1
fi

# Setup keychain and import certificates
setup_keychain() {
    log_step "Setting up code signing keychain..."

    # Validate required environment variables
    if [ -z "$APPLE_CERTIFICATE_BASE64" ]; then
        log_error "APPLE_CERTIFICATE_BASE64 not set"
        exit 1
    fi

    if [ -z "$APPLE_CERTIFICATE_PASSWORD" ]; then
        log_error "APPLE_CERTIFICATE_PASSWORD not set"
        exit 1
    fi

    if [ -z "$KEYCHAIN_PASSWORD" ]; then
        log_error "KEYCHAIN_PASSWORD not set"
        exit 1
    fi

    # Create temporary file for certificate
    local cert_path="/tmp/certificate.p12"
    echo "$APPLE_CERTIFICATE_BASE64" | base64 --decode > "$cert_path"
    log_success "Decoded certificate to temporary file"

    # Delete existing keychain if present
    if [ -f "$KEYCHAIN_PATH" ]; then
        security delete-keychain "$KEYCHAIN_PATH" 2>/dev/null || true
        log_info "Removed existing build keychain"
    fi

    # Create new keychain
    security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
    log_success "Created build keychain"

    # Set keychain settings
    # - Prevent keychain from auto-locking
    # - Set to default keychain
    security set-keychain-settings -lut 21600 "$KEYCHAIN_PATH"
    security default-keychain -s "$KEYCHAIN_PATH"
    log_success "Configured keychain settings"

    # Unlock keychain
    security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
    log_success "Unlocked keychain"

    # Import certificate
    security import "$cert_path" \
        -k "$KEYCHAIN_PATH" \
        -P "$APPLE_CERTIFICATE_PASSWORD" \
        -T /usr/bin/codesign \
        -T /usr/bin/productsign
    log_success "Imported certificate"

    # Set partition list (allows codesign to access without prompt)
    security set-key-partition-list -S apple-tool:,apple:,codesign: \
        -s -k "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
    log_success "Set key partition list"

    # Add to search list
    security list-keychains -d user -s "$KEYCHAIN_PATH" $(security list-keychains -d user | tr -d '"')
    log_success "Added to keychain search list"

    # Clean up temporary certificate file
    rm -f "$cert_path"
    log_info "Cleaned up temporary certificate file"

    # Verify certificate was imported
    log_info "Verifying certificate import..."
    local cert_count
    cert_count=$(security find-identity -v -p codesigning "$KEYCHAIN_PATH" | grep -c "valid identities found" || echo "0")

    if security find-identity -v -p codesigning "$KEYCHAIN_PATH" | grep -q "Developer ID"; then
        log_success "Developer ID certificate found in keychain"
    else
        log_warning "No Developer ID certificate found"
        security find-identity -v -p codesigning "$KEYCHAIN_PATH"
    fi

    echo ""
    log_success "Keychain setup complete"
    log_info "Ready for code signing"
}

# Cleanup keychain
cleanup_keychain() {
    log_step "Cleaning up code signing keychain..."

    if [ -f "$KEYCHAIN_PATH" ]; then
        security delete-keychain "$KEYCHAIN_PATH"
        log_success "Removed build keychain"
    else
        log_info "Build keychain not found (already cleaned up)"
    fi

    # Reset to login keychain
    security default-keychain -s login.keychain-db 2>/dev/null || true
    log_info "Reset default keychain"
}

# Execute action
case $ACTION in
    setup)
        setup_keychain
        ;;
    cleanup)
        cleanup_keychain
        ;;
esac
