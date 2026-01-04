# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 8 - macOS Build & Release
**Last Updated**: 2026-01-03
**Next Prompt**: 23 (Android Plugin Project) or 29 (Integration Testing)
**Prompts 43-46**: ✅ UI Refinement complete (Phase 7.5 done)
**Physics**: ✅ Carry validated (PR #3) | ✅ Bounce improved (PR #33) | ✅ Roll improved (PR #35) | ✅ Validation (PR #37)
**Protocol**: ✅ 0H shot parsing | ✅ 0M device status (PR #39)
**GSPro**: ✅ Client complete (PR #43) | ✅ Buffer management (PR #53)
**UI**: ✅ Prompts 43-46 complete (PR #55-59)
**Build**: ✅ Prompt 36 complete (PR #61) | 🔄 Prompts 37-41 pending for signing/notarization and CI/CD

---

## Already Implemented (Skeleton)

- [x] **Physics Engine** - TrajectorySimulator, Aerodynamics, GroundPhysics (Penner COR), PhysicsConstants, UnitConversions, ShotResult
- [x] **GC2 Data Layer** - GC2ShotData, IGC2Connection, GC2ConnectionFactory, GC2Protocol
- [x] **Core Framework** - GameManager, MainThreadDispatcher

---

## Completed Phases (Summary)

### Phase 1: Core Services
- [x] **Prompt 1**: ShotProcessor Service (PR #1) - ProcessShot(), physics integration, GSPro mode, events
- [x] **Prompt 2**: SessionManager Service (PR #2) - Session tracking, shot history, statistics
- [x] **Prompt 3**: SettingsManager Service (PR #4) - PlayerPrefs persistence, all settings categories

### Phase 2: Scenes & Bootstrap
- [x] **Prompt 4**: Unity Scene Structure (PR #6) - Bootstrap, MainMenu, Marina scenes; SceneLoader; BootstrapLoader; controllers
- [x] **Prompt 5**: PlatformManager and QualityManager (PR #8) - Platform detection, quality tiers, dynamic FPS adjustment

### Phase 3: Ball Visualization
- [x] **Prompt 6**: Golf Ball Prefab (PR #9) - GolfBall.prefab, BallVisuals, materials
- [x] **Prompt 7**: BallController Animation (PR #11) - Animation system, playback controls, BallSpinner
- [x] **Prompt 8**: TrajectoryRenderer (PR #13) - Line renderer, quality tiers, fade animation
- [x] **Prompt 9**: Camera System (PR #15) - CameraController, FollowCamera, OrbitCamera; TestShotWindow

### Phase 4: Marina Environment
- [x] **Prompt 10**: Landing Effects (PR #17) - LandingMarker, ImpactEffect, EffectsManager with pooling
- [x] **Prompt 11**: Marina Environment (PR #19) - EnvironmentManager, DistanceMarker, TargetGreen, TeeMat

### Phase 5: UI System
- [x] **Prompt 12**: UIManager and Layout (PR #21) - UIManager, ResponsiveLayout, SafeAreaHandler, UITheme
- [x] **Prompt 13**: Shot Data Bar (PR #23) - ShotDataBar with 10 DataTiles, value formatting
- [x] **Prompt 14**: Club Data Panel (PR #25) - ClubDataPanel, SwingPathIndicator, AttackAngleIndicator
- [x] **Prompt 15**: Connection Status UI (PR #27) - ConnectionStatusUI, ConnectionPanel, ConnectionToast
- [x] **Prompt 16**: Session Info Panel (PR #29) - SessionInfoPanel, ShotHistoryPanel, ShotDetailModal
- [x] **Prompt 17**: Settings Panel (PR #31) - SettingsPanel, SettingToggle/Slider/Dropdown

### Phase 5.5: Ground Physics Improvement
- [x] **Prompt 32**: Spin-Dependent Bounce (PR #33) - Penner COR, spin braking, spin reversal
- [x] **Prompt 33**: Improved Roll Model (PR #35) - Spin-enhanced deceleration, spin-back, surface behavior
- [x] **Prompt 34**: Physics Validation (PR #37) - Landing data tracking, TrackMan validation

### Phase 5.6: Ball Ready Indicator
- [x] **Prompt 35**: Ball Ready Indicator (PR #51) - Visual states, pulse animation, GameManager integration

### Phase 6: TCP/Network Layer
- [x] **Prompt 18**: Device Status Interface (PR #39) - GC2DeviceStatus, ParseDeviceStatus(), GameManager tracking
- [x] **Prompt 18b**: TCP Connection (PR #41) - GC2TCPConnection, GC2TCPListener, GC2TestWindow
- [x] **Prompt 19**: GSPro Client (PR #43) - GSProClient, GSProMessage, GSProModeUI, heartbeat

### Phase 6.5: GSPro Improvements
- [x] **Prompt 42**: GSPro Buffer Management (PR #53) - GSProResponse, buffer clearing, response parsing

### Phase 7: macOS Native Plugin
- [x] **Prompt 20**: macOS Plugin Structure (PR #45) - GC2MacPlugin.h/mm, Xcode project, build script
- [x] **Prompt 21**: macOS USB Read Loop (PR #47) - libusb INTERRUPT IN, 0H/0M parsing, misread detection
- [x] **Prompt 22**: macOS C# Bridge (PR #49) - GC2MacConnection, MonoPInvokeCallback, JSON parsing ✅ **TESTED WITH REAL HARDWARE**

### Phase 7.5: UI Refinement (Partial)
- [x] **Prompt 43**: GSPro Mode Panel Fixes (PR #55) - Panel width, LED indicators, styled pills, layout constants

---

## Incomplete Phases (Full Detail)

### Phase 7.5: UI Refinement & Polish ✅ Complete

- [x] **Prompt 44**: Connection Panel and Settings Button Fixes (PR #57)
  - [x] Fix Connection Panel height to fit all content (420px vs ~400px required)
  - [x] Fix panel positioning (center on screen with modal overlay at 60% opacity)
  - [x] Improve Close button size (36px, exceeds 32px minimum)
  - [x] Fix action buttons visibility (100px min width, 44px height)
  - [x] Add interactable/blocksRaycasts modal behavior
  - [x] Fix Settings button truncation ("Sett" → "Settings", 120px width)
  - [x] Update ConnectionStatusGenerator.cs with layout constants
  - [x] Regenerate ConnectionPanel.prefab
  - [x] Unit tests: 19 layout validation + 7 modal behavior tests
  - [x] Created OpenRange.Editor.asmdef for test references

- [x] **Prompt 45**: Settings Panel Dropdown and General UI Polish (PR #58)
  - [x] Fix dropdown z-order (Canvas.overrideSorting = 100)
  - [x] Fix dropdown option height (44px for accessibility)
  - [x] Fix dropdown ScrollRect wiring (viewport/content not assigned)
  - [x] Fix checkmark indicator (Unicode → Unity built-in Checkmark.psd sprite)
  - [x] Fix X button (Unicode "✕" → simple "X" text)
  - [x] Fix button raycastTarget (text was intercepting clicks)
  - [x] Move Settings button to left side (next to Back button)
  - [x] Fix batchmode scene generation (DisplayDialog returns false in CLI)
  - [x] Unit tests: 13 layout validation tests in SettingsPanelGeneratorTests.cs

- [x] **Prompt 46**: Test Shot Panel (Runtime UI) ✅ (PR #59)
  - [x] Create TestShotPanel.cs
    - [x] Slide-out panel from left side
    - [x] Toggle via button or T key
    - [x] Quick preset buttons: Driver, 7-Iron, Wedge, Hook, Slice
    - [x] Ball data sliders: Speed, Launch Angle, Direction
    - [x] Spin data sliders: Backspin, Sidespin
    - [x] Optional club data section toggle
    - [x] Fire Shot button (green, prominent)
    - [x] Reset Ball button
    - [x] Event: OnTestShotFired(GC2ShotData)
  - [x] Create TestShotPanelGenerator.cs
    - [x] Menu: OpenRange > Create Test Shot Panel Prefab
    - [x] Create panel hierarchy with all UI components
    - [x] Left-side anchoring (300px width)
  - [x] Scene Integration
    - [x] Add Test Shot button to Marina header
    - [x] MarinaSceneController: Add _testShotPanel field
    - [x] SceneGenerator: Instantiate and wire prefab
    - [x] Hide when GC2 is connected
  - [x] Integration with ShotProcessor
    - [x] Call ShotProcessor.ProcessShot() with generated data
    - [x] Triggers full pipeline (ball, trajectory, UI, session)
  - [x] Keyboard shortcuts (optional)
    - [x] T: Toggle panel
    - [x] D/I/W: Fire preset shots
    - [x] Space: Fire current settings
  - [x] Unit tests
    - [x] Panel visibility toggle
    - [x] Preset values applied correctly
    - [x] GC2ShotData creation
    - [x] Event firing
    - [x] Club data toggle

---

### Phase 8: macOS Build & Release

- [x] **Prompt 36**: macOS Build Script and Configuration ✅ (PR #61)
  - [x] Created Scripts/build_macos.sh with comprehensive build orchestration
    - [x] Pre-build validation (Unity, Xcode, libusb)
    - [x] Native plugin build (calls build_mac_plugin.sh)
    - [x] Unity CLI build with IL2CPP
    - [x] Post-build verification (bundle structure, library paths)
    - [x] Options: --skip-tests, --skip-plugin, --development, --version=X.Y.Z, --verbose
  - [x] Updated Makefile with new targets
    - [x] `make build-plugin` - Native plugin only
    - [x] `make build-app` - Full build with tests
    - [x] `make build-app-dev` - Development build (skip tests)
    - [x] `make build-release` - Release with git tag version
    - [x] `make package` - Create DMG for distribution
    - [x] `make clean-builds` - Clean build outputs only
  - [x] Created docs/BUILD_MACOS.md with comprehensive documentation
    - [x] Prerequisites and installation
    - [x] Quick start commands
    - [x] Build options reference
    - [x] Troubleshooting guide
    - [x] Native plugin architecture notes
  - [x] Updated CLAUDE.md with build instructions

- [x] **Prompt 37**: macOS Code Signing and Notarization ✅ (PR #63)
  - [x] Created Scripts/entitlements.plist with required entitlements
    - [x] IL2CPP JIT and memory entitlements
    - [x] USB device access entitlement
    - [x] Library validation bypass for libusb
  - [x] Created Scripts/sign_and_notarize.sh with full workflow
    - [x] Sign libusb.dylib, GC2MacPlugin.bundle, OpenRange.app
    - [x] Deep signing with hardened runtime
    - [x] Notarization submission and stapling
    - [x] DMG creation and signing
    - [x] Dry-run mode for testing
  - [x] Created Scripts/setup_signing.sh for CI
    - [x] Import certificates from base64 env vars
    - [x] Create temporary keychain
    - [x] Cleanup target for post-build
  - [x] Updated Makefile with signing targets
    - [x] sign, notarize, dmg, release-macos
    - [x] setup-signing, cleanup-signing
  - [x] Updated docs/BUILD_MACOS.md with comprehensive signing docs
  - [x] Updated CLAUDE.md with signing commands

---

### Phase 9: Android Native Plugin

- [ ] **Prompt 23**: Android Plugin Project
  - [ ] Create Gradle project
  - [ ] Configure manifest with USB host permission
  - [ ] USB device filter (VID 11385, PID 272)
  - [ ] Kotlin stubs
  - [ ] Build script
  - [ ] Verification tests

- [ ] **Prompt 24**: Android Plugin Implementation
  - [ ] Complete GC2Plugin.kt
  - [ ] USB permission handling (BroadcastReceiver)
  - [ ] Device enumeration with INTERRUPT IN endpoint
  - [ ] Read thread with 64-byte interrupt transfers
  - [ ] Message type filtering (0H for shots, 0M for status)
  - [ ] Data accumulation until BACK_RPM/SIDE_RPM received
  - [ ] Wait for message terminator (\n\t)
  - [ ] Device status callback from 0M (FLAGS=7 ready, BALLS>0 detected)
  - [ ] Misread detection (zero spin, 2222 error, speed range)
  - [ ] Tests (device required)

- [ ] **Prompt 25**: Android C# Bridge
  - [ ] Create GC2AndroidConnection.cs
  - [ ] AndroidJavaObject calls
  - [ ] Message handlers (OnNativeShotReceived, OnNativeDeviceStatus)
  - [ ] Update factory
  - [ ] Create prefab
  - [ ] Tests

---

### Phase 10: iPad Native Plugin (DriverKit)

- [ ] **Prompt 26**: iPad Plugin Structure
  - [ ] Create GC2iOSPlugin project
  - [ ] Create GC2Driver structure
  - [ ] Configure entitlements
  - [ ] Stub implementation
  - [ ] Documentation
  - [ ] Verification

- [ ] **Prompt 27**: iPad DriverKit Implementation
  - [ ] Implement GC2Driver
  - [ ] Implement GC2UserClient
  - [ ] Implement GC2iOSPlugin
  - [ ] System extension handling
  - [ ] Tests (hardware + entitlements required)

- [ ] **Prompt 28**: iPad C# Bridge
  - [ ] Create GC2iPadConnection.cs
  - [ ] DriverKit state handling
  - [ ] Create DriverKitSetupUI.cs
  - [ ] Update factory
  - [ ] Tests

---

### Phase 14: Visual Enhancements

Visual inspiration from reference projects:
- **ProceduralGolf** (SolomonBaarda): Toon shaders, water with foam, stylized skybox, outline rendering
- **Super-Golf** (jzgom067): Tropical island aesthetic, trail renderers, dramatic landscapes
- **golf_simulator** (JanWalsh91): Custom shaders (14% ShaderLab), beautiful landscapes

- [ ] **Prompt 47**: Stylized Skybox and Lighting Setup
  - [ ] Create StylizedSkybox.shader (procedural gradient, clouds, sun)
  - [ ] Create MarinaSkybox.mat with "Golden Hour" preset
  - [ ] Configure directional light, ambient, reflection probe
  - [ ] Create LightingSetupGenerator.cs editor tool
  - [ ] Quality tier integration (soft/hard/no shadows)
  - [ ] Unit tests

- [ ] **Prompt 48**: Enhanced Grass Shader with Wind Animation
  - [ ] Create StylizedGrass.shader (vertex displacement, wind)
  - [ ] Create WindController.cs singleton
  - [ ] Create grass material presets (Fairway, Rough, Green)
  - [ ] Create GrassShaderSetup.cs editor tool
  - [ ] Integration with SettingsManager wind settings
  - [ ] Unit tests

- [ ] **Prompt 49**: Water Shader with Foam and Reflections
  - [ ] Create StylizedWater.shader (scrolling normals, depth foam)
  - [ ] Create WaterController.cs (planar reflection, quality tiers)
  - [ ] Create water material presets (Ocean, Pond)
  - [ ] Create WaterSetupGenerator.cs editor tool
  - [ ] Document URP depth texture requirements
  - [ ] Unit tests

- [ ] **Prompt 50**: Post-Processing Volume Configuration
  - [ ] Create Volume Profiles (High/Medium/Low quality)
  - [ ] Configure Bloom, Color Adjustments, Vignette, SSAO
  - [ ] Create PostProcessingController.cs
  - [ ] Create PostProcessingSetupGenerator.cs editor tool
  - [ ] Scene integration with MarinaSceneController
  - [ ] Unit tests

- [ ] **Prompt 51**: Enhanced Ball Trail and Trajectory Visuals
  - [ ] Create TrailGlow.shader (additive, HDR, fade)
  - [ ] Create TrajectoryLine.shader (glow, optional dashes)
  - [ ] Create enhanced materials (trajectory, ball trail)
  - [ ] Create TrajectoryEnhancer.cs component
  - [ ] Shot quality color customization
  - [ ] Create TrajectoryVisualsGenerator.cs editor tool
  - [ ] Unit tests

- [ ] **Prompt 52**: Environment Props - Trees and Scenery
  - [ ] Create PalmTree, RockCluster, DistantMountain prefabs
  - [ ] Create Foliage and Rock materials with LOD
  - [ ] Create PropPlacer.cs and LODController.cs
  - [ ] Create EnvironmentPropsGenerator.cs editor tool
  - [ ] GPU instancing and batching
  - [ ] Unit tests

- [ ] **Prompt 53**: Toon/Outline Shader Option
  - [ ] Create ToonLit.shader (cel shading, rim lighting)
  - [ ] Create ToonOutline.shader (inverted hull)
  - [ ] Create toon material presets
  - [ ] Create ToonModeController.cs
  - [ ] Add "Visual Style" setting to Settings Panel
  - [ ] Create ToonMaterialGenerator.cs editor tool
  - [ ] Unit tests

- [ ] **Prompt 54**: Visual Polish and Integration
  - [ ] Create VisualManager.cs (coordinates all visual systems)
  - [ ] Update SceneGenerator.cs for visual systems
  - [ ] Quality tier polish and validation
  - [ ] Create VisualSystemValidator.cs editor tool
  - [ ] Visual presets (Day, Sunset, Overcast)
  - [ ] Performance validation (60 FPS targets)
  - [ ] Documentation updates
  - [ ] Integration and PlayMode tests

---

### Phase 11: Quality & Polish

- [ ] **Prompt 29**: Integration Testing
  - [ ] Create Edit Mode tests
  - [ ] Create Play Mode tests
  - [ ] Physics validation tests
  - [ ] Test shot generator
  - [ ] Platform verification

- [ ] **Prompt 30**: Quality Tier Polish
  - [ ] Complete URP asset configuration
  - [ ] Dynamic adjustment
  - [ ] Environment adjustments
  - [ ] UI adjustments
  - [ ] Visual polish
  - [ ] Tests

- [ ] **Prompt 31**: Final Polish and Documentation
  - [ ] Code cleanup
  - [ ] Performance verification
  - [ ] Build verification
  - [ ] README.md
  - [ ] CLAUDE.md
  - [ ] DEVELOPMENT.md
  - [ ] Final tests

---

### Phase 12: iOS & Android Builds [Future]

- [ ] **Prompt 38**: iOS Build Configuration [Future]
  - [ ] Prerequisites: iPad native plugin complete (Prompts 26-28)
  - [ ] Configure Unity iOS Player Settings
  - [ ] Create Xcode export options plist
  - [ ] Create iOS build script
  - [ ] Document TestFlight submission process

- [ ] **Prompt 39**: Android Build Configuration [Future]
  - [ ] Prerequisites: Android native plugin complete (Prompts 23-25)
  - [ ] Configure Unity Android Player Settings
  - [ ] Create keystore for signing
  - [ ] Create Android build script
  - [ ] Document Play Store submission process

- [ ] **Prompt 40**: Mobile Build Environment Setup [Future]
  - [ ] Document Xcode requirements
  - [ ] Document Android SDK requirements
  - [ ] Create combined mobile build script
  - [ ] Add mobile build targets to Makefile

---

### Phase 13: CI/CD Release Workflow [Future]

- [ ] **Prompt 41**: GitHub Actions Release Workflow
  - [ ] Create `.github/workflows/release.yml`
    - [ ] Trigger on version tags (v*)
    - [ ] Build matrix: macOS, iOS (future), Android (future)
    - [ ] Use `game-ci/unity-builder` for Unity builds
  - [ ] macOS release job
    - [ ] Build native plugin
    - [ ] Build Unity app (IL2CPP)
    - [ ] Code sign with secrets
    - [ ] Notarize with Apple
    - [ ] Package as DMG
    - [ ] Upload as release artifact
  - [ ] iOS release job [Future]
  - [ ] Android release job [Future]
  - [ ] Create GitHub Release with all artifacts
  - [ ] Document required secrets
  - [ ] Add release badge to README.md

---

## Hardware Testing Checklist

### macOS
- [ ] MacBook Pro M1/M2
- [ ] MacBook Air M1
- [ ] iMac (Intel)
- [ ] USB connection with GC2

### iPad
- [ ] iPad Pro 11" M1
- [ ] iPad Pro 12.9" M2
- [ ] USB-C connection with GC2
- [ ] DriverKit approval obtained

### Android
- [ ] Samsung Galaxy Tab S8+
- [ ] Pixel Tablet
- [ ] Budget tablet test
- [ ] USB-C connection with GC2

---

## Physics Validation Checklist

All validated ✅ (PR #3):

- [x] Driver: 167 mph / 10.9° / 2686 rpm → 275 yds (±5%) ✅
- [x] Driver: 160 mph / 11.0° / 3000 rpm → 259 yds (±3%) ✅
- [x] 7-Iron: 120 mph / 16.3° / 7097 rpm → 172 yds (±5%) ✅
- [x] Wedge: 102 mph / 24.2° / 9304 rpm → 136 yds (±5%) ✅
- [x] Sidespin direction (positive = curves right) ✅
- [x] Wind effects (headwind/tailwind/crosswind) ✅
- [x] Environmental conditions (altitude/temperature) ✅

---

## Notes

- Each prompt should be executed in order
- Mark items as complete with [x] when done
- Add notes for issues or deviations below
- Update "Last Updated" date when making changes
- Update "Next Prompt" when moving forward

---

## Recent Issue Log (Last 5 Entries)

**2026-01-03 (macOS Build Script)**: Prompt 36 complete (PR #61). Created Scripts/build_macos.sh with comprehensive build orchestration (validation, plugin, tests, Unity build, verification). Updated Makefile with build-plugin, build-app, build-app-dev, build-release, package, clean-builds targets. Created docs/BUILD_MACOS.md with full documentation. Updated CLAUDE.md with build instructions.

**2026-01-03 (Settings Panel Dropdown Fixes)**: Prompt 45 complete (PR #58). Fixed dropdown issues - ScrollRect wiring (viewport/content missing), z-order (Canvas.sortingOrder=100), item height (44px), checkmark (Unity's built-in Checkmark.psd instead of Unicode). Fixed Settings button moved to left side. Critical fix: EditorUtility.DisplayDialog() returns false in batchmode - added Application.isBatchMode check to SceneGenerator.GenerateAllScenes(). 13 new layout tests.

**2026-01-03 (GSPro Mode Panel Layout Fixes)**: Prompt 43 complete (PR #55). Fixed GSPro Mode UI layout - panel width 280px, LED indicators 14px, styled indicator pills, 19 new layout validation tests.

**2026-01-03 (GSPro Buffer Management)**: Prompt 42 complete (PR #53). Added GSProResponse for shot confirmations, ParseFirstObject() for concatenated JSON, buffer clearing, 52 new tests.

**2026-01-03 (Ball Ready Indicator)**: Prompt 35 complete (PR #51). Visual states (Disconnected→Warming→Place Ball→Ready), pulse animation, GameManager integration, 49 tests.

**2026-01-03 (macOS C# Bridge - Real Hardware)**: Prompt 22 verified with real GC2. Fixed IL2CPP callback issue (UnitySendMessage NULL → MonoPInvokeCallback), fixed JSON field name mismatches.

**2026-01-03 (macOS USB Read Loop)**: Prompt 21 complete (PR #47). libusb INTERRUPT IN endpoint 0x82, 0H/0M parsing, misread detection (zero spin, 2222 error, speed range), message terminator (\n\t).
