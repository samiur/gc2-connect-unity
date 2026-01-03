# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 7.5 - UI Refinement & Polish
**Last Updated**: 2026-01-03
**Next Prompt**: 46 (Test Shot Panel - Runtime UI)
**Physics**: ✅ Carry validated (PR #3) | ✅ Bounce improved (PR #33) | ✅ Roll improved (PR #35) | ✅ Validation (PR #37)
**Protocol**: ✅ 0H shot parsing | ✅ 0M device status (PR #39)
**GSPro**: ✅ Client complete (PR #43) | ✅ Buffer management (PR #53)
**UI**: 🔄 Layout issues identified - Prompts 43-46 added for fixes and test shots
**Build**: 🔄 Prompts 36-41 added for macOS/iOS/Android builds and CI/CD release workflow

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

### Phase 7.5: UI Refinement & Polish (Continued)

- [ ] **Prompt 44**: Connection Panel and Settings Button Fixes
  - [ ] Fix Connection Panel height to fit all content
  - [ ] Fix panel positioning (center on screen with modal overlay)
  - [ ] Improve Close button size (32x32px minimum)
  - [ ] Fix action buttons visibility
  - [ ] Add scroll support if content exceeds height
  - [ ] Fix Settings button truncation ("Sett" → "Settings")
  - [ ] Update ConnectionStatusGenerator.cs
  - [ ] Regenerate ConnectionPanel.prefab
  - [ ] Unit tests for overflow and modal behavior

- [ ] **Prompt 45**: Settings Panel Dropdown and General UI Polish
  - [ ] Fix dropdown z-order (render above other elements)
  - [ ] Fix dropdown option height to show full text
  - [ ] Fix Settings panel scroll indicator
  - [ ] Fix "Connect GC2" button overlapping title
  - [ ] Verify Club Data Panel visibility
  - [ ] Verify Ball Ready Indicator position
  - [ ] General UI polish (consistent colors, spacing, fonts)
  - [ ] Update SceneGenerator for proper positioning
  - [ ] Unit tests for dropdown and panel visibility

- [ ] **Prompt 46**: Test Shot Panel (Runtime UI)
  - [ ] Create TestShotPanel.cs
    - [ ] Slide-out panel from left side
    - [ ] Toggle via button or T key
    - [ ] Quick preset buttons: Driver, 7-Iron, Wedge, Hook, Slice
    - [ ] Ball data sliders: Speed, Launch Angle, Direction
    - [ ] Spin data sliders: Backspin, Sidespin
    - [ ] Optional club data section toggle
    - [ ] Fire Shot button (green, prominent)
    - [ ] Reset Ball button
    - [ ] Event: OnTestShotFired(GC2ShotData)
  - [ ] Create TestShotPanelGenerator.cs
    - [ ] Menu: OpenRange > Create Test Shot Panel Prefab
    - [ ] Create panel hierarchy with all UI components
    - [ ] Left-side anchoring (300px width)
  - [ ] Scene Integration
    - [ ] Add Test Shot button to Marina header
    - [ ] MarinaSceneController: Add _testShotPanel field
    - [ ] SceneGenerator: Instantiate and wire prefab
    - [ ] Hide when GC2 is connected
  - [ ] Integration with ShotProcessor
    - [ ] Call ShotProcessor.ProcessShot() with generated data
    - [ ] Triggers full pipeline (ball, trajectory, UI, session)
  - [ ] Keyboard shortcuts (optional)
    - [ ] T: Toggle panel
    - [ ] D/I/W: Fire preset shots
    - [ ] Space: Fire current settings
  - [ ] Unit tests
    - [ ] Panel visibility toggle
    - [ ] Preset values applied correctly
    - [ ] GC2ShotData creation
    - [ ] Event firing
    - [ ] Club data toggle

---

### Phase 8: macOS Build & Release

- [ ] **Prompt 36**: macOS Build Script and Configuration
  - [ ] Update Makefile with comprehensive macOS targets
    - [ ] `make build-macos` - Full macOS build with native plugin
    - [ ] `make build-macos-dev` - Development build with debugging
    - [ ] `make package-macos` - Create DMG installer
    - [ ] `make clean-builds` - Clean all build artifacts
  - [ ] Create build configuration script (`Scripts/build_macos.sh`)
    - [ ] Build native plugin first (call `build_mac_plugin.sh`)
    - [ ] Unity CLI build with correct parameters
    - [ ] Post-build validation (bundle structure, library linking)
    - [ ] Architecture verification (arm64/x86_64/universal)
  - [ ] Update Unity Player Settings for production
    - [ ] Bundle identifier: `com.openrange.gc2connect`
    - [ ] Version numbering scheme (semver)
    - [ ] IL2CPP scripting backend
    - [ ] Architecture: Apple Silicon + Intel
  - [ ] Create DMG packaging script
    - [ ] Application folder symlink
    - [ ] Background image and icon placement
    - [ ] Compressed DMG output
  - [ ] Add build verification tests
  - [ ] Update CLAUDE.md with build instructions

- [ ] **Prompt 37**: macOS Code Signing and Notarization
  - [ ] Document Apple Developer requirements
    - [ ] Developer ID Application certificate
    - [ ] Developer ID Installer certificate
    - [ ] App-specific password for notarization
  - [ ] Create signing script (`Scripts/sign_macos.sh`)
    - [ ] Sign native plugin (GC2MacPlugin.bundle)
    - [ ] Sign libusb dylib
    - [ ] Sign main application bundle
    - [ ] Deep signing with entitlements
  - [ ] Create notarization script (`Scripts/notarize_macos.sh`)
    - [ ] Upload to Apple notary service
    - [ ] Wait for notarization result
    - [ ] Staple ticket to app
  - [ ] Create entitlements file for USB access
  - [ ] Add GitHub secrets documentation
  - [ ] Update Makefile with signing targets

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

**2026-01-03 (GSPro Mode Panel Layout Fixes)**: Prompt 43 complete (PR #55). Fixed GSPro Mode UI layout - panel width 280px, LED indicators 14px, styled indicator pills, 19 new layout validation tests.

**2026-01-03 (GSPro Buffer Management)**: Prompt 42 complete (PR #53). Added GSProResponse for shot confirmations, ParseFirstObject() for concatenated JSON, buffer clearing, 52 new tests.

**2026-01-03 (Ball Ready Indicator)**: Prompt 35 complete (PR #51). Visual states (Disconnected→Warming→Place Ball→Ready), pulse animation, GameManager integration, 49 tests.

**2026-01-03 (macOS C# Bridge - Real Hardware)**: Prompt 22 verified with real GC2. Fixed IL2CPP callback issue (UnitySendMessage NULL → MonoPInvokeCallback), fixed JSON field name mismatches.

**2026-01-03 (macOS USB Read Loop)**: Prompt 21 complete (PR #47). libusb INTERRUPT IN endpoint 0x82, 0H/0M parsing, misread detection (zero spin, 2222 error, speed range), message terminator (\n\t).
