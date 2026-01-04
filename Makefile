# ABOUTME: Makefile for Unity project build and test automation.
# ABOUTME: Provides targets for running tests, building, and CI operations.

# Unity configuration
UNITY_VERSION := 6000.3.2f1
UNITY_PATH := /Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/MacOS/Unity
PROJECT_PATH := $(shell pwd)

# Build output
BUILD_DIR := Builds
MACOS_BUILD := $(BUILD_DIR)/macOS/OpenRange.app

# Test configuration
TEST_RESULTS_DIR := TestResults
EDITMODE_RESULTS := $(TEST_RESULTS_DIR)/editmode-results.xml
PLAYMODE_RESULTS := $(TEST_RESULTS_DIR)/playmode-results.xml

.PHONY: all test test-edit test-play build clean help check-unity run run-marina generate \
       build-plugin build-app build-release build-app-dev package clean-builds \
       sign notarize dmg release-macos setup-signing cleanup-signing

# Default target
all: test

# Help
help:
	@echo "Unity Project Makefile"
	@echo ""
	@echo "Development targets:"
	@echo "  run           - Open Unity with Bootstrap scene (main entry point)"
	@echo "  run-marina    - Open Unity with Marina scene (direct testing)"
	@echo ""
	@echo "Test targets:"
	@echo "  test          - Run all tests (EditMode + PlayMode)"
	@echo "  test-edit     - Run EditMode tests only"
	@echo "  test-play     - Run PlayMode tests only"
	@echo "  test-physics  - Run physics validation tests only"
	@echo ""
	@echo "Build targets:"
	@echo "  build         - Build macOS standalone (runs tests + generate first)"
	@echo "  build-dev     - Build macOS development build (runs generate first)"
	@echo "  generate      - Regenerate all prefabs then all scenes from editor scripts"
	@echo ""
	@echo "macOS release targets:"
	@echo "  build-plugin  - Build native macOS plugin only"
	@echo "  build-app     - Full macOS app build (plugin + tests + Unity)"
	@echo "  build-release - Release build with version tagging"
	@echo "  package       - Create DMG for distribution"
	@echo ""
	@echo "Code signing targets:"
	@echo "  sign          - Sign app bundle with Developer ID"
	@echo "  notarize      - Submit for notarization and staple ticket"
	@echo "  dmg           - Create signed DMG for distribution"
	@echo "  release-macos - Full pipeline: build, sign, notarize, DMG"
	@echo "  setup-signing - Setup keychain for CI (import certificates)"
	@echo "  cleanup-signing - Remove CI keychain after build"
	@echo ""
	@echo "Utility targets:"
	@echo "  clean         - Remove build artifacts and test results"
	@echo "  clean-builds  - Remove only build outputs (keep test results)"
	@echo "  check-unity   - Verify Unity installation"
	@echo ""

# Check Unity is installed
check-unity:
	@if [ ! -f "$(UNITY_PATH)" ]; then \
		echo "Error: Unity $(UNITY_VERSION) not found at $(UNITY_PATH)"; \
		echo "Please install Unity $(UNITY_VERSION) via Unity Hub"; \
		exit 1; \
	fi
	@echo "Unity $(UNITY_VERSION) found"

# Create test results directory
$(TEST_RESULTS_DIR):
	mkdir -p $(TEST_RESULTS_DIR)

# Run all tests
test: test-edit test-play
	@echo ""
	@echo "All tests completed!"

# Run EditMode tests
test-edit: check-unity $(TEST_RESULTS_DIR)
	@echo "Running EditMode tests..."
	@$(UNITY_PATH) \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-runTests \
		-testPlatform EditMode \
		-testResults "$(PROJECT_PATH)/$(EDITMODE_RESULTS)" \
		-logFile - 2>&1 | tee $(TEST_RESULTS_DIR)/editmode.log | tail -20
	@if grep -q 'result="Passed"' "$(EDITMODE_RESULTS)" 2>/dev/null; then \
		echo ""; \
		echo "✓ EditMode tests PASSED"; \
		grep -o 'passed="[0-9]*"' "$(EDITMODE_RESULTS)" | head -1; \
	else \
		echo ""; \
		echo "✗ EditMode tests FAILED"; \
		grep -E "(message.*\]\]|failed=)" "$(EDITMODE_RESULTS)" 2>/dev/null | head -10; \
		exit 1; \
	fi

# Run PlayMode tests
test-play: check-unity $(TEST_RESULTS_DIR)
	@echo "Running PlayMode tests..."
	@$(UNITY_PATH) \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-runTests \
		-testPlatform PlayMode \
		-testResults "$(PROJECT_PATH)/$(PLAYMODE_RESULTS)" \
		-logFile - 2>&1 | tee $(TEST_RESULTS_DIR)/playmode.log | tail -20
	@if grep -q 'result="Passed"' "$(PLAYMODE_RESULTS)" 2>/dev/null; then \
		echo ""; \
		echo "✓ PlayMode tests PASSED"; \
		grep -o 'passed="[0-9]*"' "$(PLAYMODE_RESULTS)" | head -1; \
	else \
		echo ""; \
		echo "✗ PlayMode tests FAILED (or no tests found)"; \
		grep -E "(message.*\]\]|failed=)" "$(PLAYMODE_RESULTS)" 2>/dev/null | head -10; \
	fi

# Run physics tests only
test-physics: check-unity $(TEST_RESULTS_DIR)
	@echo "Running physics validation tests..."
	@$(UNITY_PATH) \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-runTests \
		-testPlatform EditMode \
		-testFilter "PhysicsValidationTests" \
		-testResults "$(PROJECT_PATH)/$(TEST_RESULTS_DIR)/physics-results.xml" \
		-logFile - 2>&1 | tee $(TEST_RESULTS_DIR)/physics.log | tail -20
	@if grep -q 'result="Passed"' "$(TEST_RESULTS_DIR)/physics-results.xml" 2>/dev/null; then \
		echo ""; \
		echo "✓ Physics tests PASSED"; \
	else \
		echo ""; \
		echo "✗ Physics tests FAILED"; \
		grep -E "message.*yds" "$(TEST_RESULTS_DIR)/physics-results.xml" 2>/dev/null | head -10; \
		exit 1; \
	fi

# Regenerate all prefabs and scenes from editor scripts
generate: check-unity
	@echo "Regenerating all prefabs..."
	@mkdir -p $(BUILD_DIR)
	@$(UNITY_PATH) \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-executeMethod OpenRange.Editor.SceneGenerator.GenerateAllPrefabs \
		-quit \
		-logFile $(BUILD_DIR)/generate-prefabs.log 2>&1 || true
	@echo "Prefabs generated. Regenerating all scenes..."
	@$(UNITY_PATH) \
		-batchmode \
		-nographics \
		-silent-crashes \
		-projectPath "$(PROJECT_PATH)" \
		-executeMethod OpenRange.Editor.SceneGenerator.GenerateAllScenes \
		-quit \
		-logFile $(BUILD_DIR)/generate-scenes.log 2>&1 || true
	@echo "Generation complete"

# Build macOS standalone
build: check-unity test generate
	@echo "Building macOS standalone..."
	@mkdir -p $(BUILD_DIR)/macOS
	$(UNITY_PATH) \
		-batchmode \
		-quit \
		-projectPath "$(PROJECT_PATH)" \
		-buildTarget StandaloneOSX \
		-buildOSXUniversalPlayer "$(PROJECT_PATH)/$(MACOS_BUILD)" \
		-logFile $(BUILD_DIR)/build.log
	@echo "Build complete: $(MACOS_BUILD)"

# Build development build (faster, with debugging)
build-dev: check-unity generate
	@echo "Building macOS development build..."
	@mkdir -p $(BUILD_DIR)/macOS
	$(UNITY_PATH) \
		-batchmode \
		-quit \
		-projectPath "$(PROJECT_PATH)" \
		-buildTarget StandaloneOSX \
		-development \
		-buildOSXUniversalPlayer "$(PROJECT_PATH)/$(MACOS_BUILD)" \
		-logFile $(BUILD_DIR)/build-dev.log
	@echo "Dev build complete: $(MACOS_BUILD)"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	rm -rf $(BUILD_DIR)
	rm -rf $(TEST_RESULTS_DIR)
	rm -f TestResults*.xml
	@echo "Clean complete"

# Open Unity Editor with Bootstrap scene (main app entry point)
# Unity will automatically compile scripts when opening
run: check-unity
	@echo "Opening Unity Editor with Bootstrap scene..."
	@open -a "/Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app" --args -projectPath "$(PROJECT_PATH)" -openScene "Assets/Scenes/Bootstrap.unity"

# Open Unity Editor and load Marina scene directly (for testing)
run-marina: check-unity
	@echo "Opening Unity Editor with Marina scene..."
	@open -a "/Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app" --args -projectPath "$(PROJECT_PATH)" -openScene "Assets/Scenes/Ranges/Marina.unity"

# ============================================
# macOS Release Build Targets
# ============================================

# Native plugin directory
NATIVE_PLUGIN_DIR := NativePlugins/macOS
PLUGIN_BUILD_SCRIPT := $(NATIVE_PLUGIN_DIR)/build_mac_plugin.sh

# Build native macOS plugin only
build-plugin:
	@echo "Building native macOS plugin..."
	@if [ ! -f "$(PLUGIN_BUILD_SCRIPT)" ]; then \
		echo "Error: Plugin build script not found at $(PLUGIN_BUILD_SCRIPT)"; \
		exit 1; \
	fi
	@chmod +x "$(PLUGIN_BUILD_SCRIPT)"
	@"$(PLUGIN_BUILD_SCRIPT)"
	@echo "Native plugin built successfully"

# Full macOS app build using comprehensive build script
build-app: check-unity
	@echo "Building complete macOS application..."
	@chmod +x Scripts/build_macos.sh
	@Scripts/build_macos.sh

# Release build with version from git tag
build-release: check-unity
	@echo "Building release version..."
	@chmod +x Scripts/build_macos.sh
	@VERSION=$$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//') || VERSION="0.0.0"; \
	Scripts/build_macos.sh --version=$$VERSION

# Development build (faster, with debugging)
build-app-dev: check-unity
	@echo "Building development version..."
	@chmod +x Scripts/build_macos.sh
	@Scripts/build_macos.sh --development --skip-tests

# Create DMG package for distribution
# Note: Requires code signing to be done first (see Scripts/sign_and_notarize.sh)
package: $(MACOS_BUILD)
	@echo "Creating DMG package..."
	@mkdir -p $(BUILD_DIR)/dist
	@if command -v create-dmg &> /dev/null; then \
		create-dmg \
			--volname "OpenRange" \
			--window-pos 200 120 \
			--window-size 600 400 \
			--icon-size 100 \
			--icon "OpenRange.app" 175 120 \
			--hide-extension "OpenRange.app" \
			--app-drop-link 425 120 \
			"$(BUILD_DIR)/dist/OpenRange.dmg" \
			"$(MACOS_BUILD)"; \
	else \
		echo "create-dmg not found. Creating simple DMG..."; \
		hdiutil create -volname "OpenRange" -srcfolder "$(MACOS_BUILD)" \
			-ov -format UDZO "$(BUILD_DIR)/dist/OpenRange.dmg"; \
	fi
	@echo "DMG created: $(BUILD_DIR)/dist/OpenRange.dmg"

# Clean only build outputs (keep test results for debugging)
clean-builds:
	@echo "Cleaning build outputs..."
	rm -rf $(BUILD_DIR)/macOS
	rm -rf $(BUILD_DIR)/dist
	rm -rf $(BUILD_DIR)/logs
	@echo "Build outputs cleaned"

# ============================================
# Code Signing and Notarization Targets
# ============================================

# Sign app bundle with Developer ID
# Requires: APPLE_DEVELOPER_ID environment variable
sign: $(MACOS_BUILD)
	@echo "Signing app bundle..."
	@chmod +x Scripts/sign_and_notarize.sh
	@Scripts/sign_and_notarize.sh --sign

# Submit for notarization and staple ticket
# Requires: APPLE_ID, APPLE_TEAM_ID, APPLE_APP_PASSWORD
notarize: $(MACOS_BUILD)
	@echo "Submitting for notarization..."
	@chmod +x Scripts/sign_and_notarize.sh
	@Scripts/sign_and_notarize.sh --notarize

# Create signed DMG for distribution
# Requires: App to be signed first
dmg: $(MACOS_BUILD)
	@echo "Creating signed DMG..."
	@chmod +x Scripts/sign_and_notarize.sh
	@Scripts/sign_and_notarize.sh --sign --dmg

# Full release pipeline: build, sign, notarize, create DMG
# Requires: All code signing environment variables
release-macos: build-app
	@echo "Running full release pipeline..."
	@chmod +x Scripts/sign_and_notarize.sh
	@Scripts/sign_and_notarize.sh --all

# Setup keychain for CI (import certificates from base64 env vars)
# Requires: APPLE_CERTIFICATE_BASE64, APPLE_CERTIFICATE_PASSWORD, KEYCHAIN_PASSWORD
setup-signing:
	@echo "Setting up signing keychain..."
	@chmod +x Scripts/setup_signing.sh
	@Scripts/setup_signing.sh --setup

# Remove CI keychain after build
cleanup-signing:
	@echo "Cleaning up signing keychain..."
	@chmod +x Scripts/setup_signing.sh
	@Scripts/setup_signing.sh --cleanup
