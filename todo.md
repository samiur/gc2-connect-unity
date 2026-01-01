# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 3 - Ball Visualization (4 of 4 complete) ✅
**Last Updated**: 2026-01-01
**Next Prompt**: 10 (Landing Marker and Effects)
**Physics**: ✅ Validated - All 16 tests passing (PR #3)

---

## Already Implemented (Skeleton)

These components exist and don't need to be rebuilt:

- [x] **Physics Engine** (complete)
  - [x] TrajectorySimulator.cs - RK4 integration
  - [x] Aerodynamics.cs - Nathan model
  - [x] GroundPhysics.cs - Bounce and roll
  - [x] PhysicsConstants.cs - Constants
  - [x] UnitConversions.cs - Unit helpers
  - [x] ShotResult.cs - Result model

- [x] **GC2 Data Layer** (complete)
  - [x] GC2ShotData.cs - Shot data model
  - [x] IGC2Connection.cs - Interface
  - [x] GC2ConnectionFactory.cs - Factory
  - [x] GC2Protocol.cs - Protocol parser

- [x] **Core Framework** (partial)
  - [x] GameManager.cs - App controller
  - [x] MainThreadDispatcher.cs - Thread safety

---

## Phase 1: Complete Core Services

- [x] **Prompt 1**: ShotProcessor Service (PR #1)
  - [x] Create ShotProcessor.cs
  - [x] ProcessShot() method
  - [x] Physics integration
  - [x] GSPro mode support
  - [x] Events (OnShotProcessed, OnShotRejected)
  - [x] Unit tests

- [x] **Prompt 2**: SessionManager Service (PR #2)
  - [x] Create SessionManager.cs
  - [x] Session tracking
  - [x] Shot history
  - [x] Statistics calculation
  - [x] Events
  - [x] Unit tests

- [x] **Prompt 3**: SettingsManager Service (PR #4)
  - [x] Create SettingsManager.cs
  - [x] All settings categories
  - [x] PlayerPrefs persistence
  - [x] Default values
  - [x] Events
  - [x] Unit tests

---

## Phase 2: Scenes & Bootstrap

- [x] **Prompt 4**: Unity Scene Structure (PR #6)
  - [x] Create Bootstrap.unity (via SceneGenerator)
  - [x] Create MainMenu.unity (via SceneGenerator)
  - [x] Create Ranges/Marina.unity (via SceneGenerator)
  - [x] Create BootstrapLoader.cs
  - [x] Create SceneLoader.cs
  - [x] Create MainMenuController.cs
  - [x] Create MarinaSceneController.cs
  - [x] Configure build settings (via SceneGenerator)
  - [x] Play mode tests (16 new tests)

- [x] **Prompt 5**: PlatformManager and QualityManager (PR #8)
  - [x] Create PlatformManager.cs
  - [x] Create QualityManager.cs
  - [x] Create URPQualitySetup.cs editor tool
  - [x] Platform detection (Mac, iPad, Android, Windows, Editor)
  - [x] Quality tier switching with URP assets
  - [x] Dynamic FPS monitoring with auto-downgrade
  - [x] Tests (57 new tests)

---

## Phase 3: Ball Visualization

- [x] **Prompt 6**: Golf Ball Prefab and Materials (PR #9)
  - [x] Create GolfBall.prefab (via GolfBallPrefabGenerator editor tool)
  - [x] Create GolfBall.mat (via GolfBallPrefabGenerator editor tool)
  - [x] Create BallTrail.prefab (via GolfBallPrefabGenerator editor tool)
  - [x] Create BallVisuals.cs
  - [x] Tests (34 new tests)

- [x] **Prompt 7**: BallController Animation System (PR #11)
  - [x] Create BallController.cs
  - [x] Animation system
  - [x] Playback controls
  - [x] Events
  - [x] Create BallSpinner.cs
  - [x] Tests (78 new tests)

- [x] **Prompt 8**: TrajectoryRenderer (PR #13)
  - [x] Create TrajectoryRenderer.cs
  - [x] Line renderer setup
  - [x] Quality tier variants
  - [x] Create TrajectoryLineGenerator.cs (editor tool)
  - [x] Tests (43 new tests)

- [x] **Prompt 9**: Camera System (PR #15)
  - [x] Create CameraController.cs
  - [x] Create FollowCamera.cs
  - [x] Create OrbitCamera.cs
  - [x] Create ICameraMode.cs
  - [x] Create CameraRigGenerator.cs (editor tool for CameraRig.prefab)
  - [x] Create TestShotWindow.cs (editor tool for testing)
  - [x] Tests (79 new tests)

---

## Phase 4: Marina Environment

- [ ] **Prompt 10**: Landing Marker and Effects
  - [ ] Create LandingMarker.cs
  - [ ] Create LandingMarker.prefab
  - [ ] Create ImpactEffect.cs
  - [ ] Create LandingDust.prefab
  - [ ] Create EffectsManager.cs
  - [ ] Tests

- [ ] **Prompt 11**: Marina Environment Setup
  - [ ] Expand Marina.unity scene
  - [ ] Create EnvironmentManager.cs
  - [ ] Create DistanceMarker.prefab
  - [ ] Create TargetGreen.prefab
  - [ ] Create TeeMat.prefab
  - [ ] Environment materials
  - [ ] Play mode tests

---

## Phase 5: UI System

- [ ] **Prompt 12**: UIManager and Layout System
  - [ ] Create UIManager.cs
  - [ ] Create ResponsiveLayout.cs
  - [ ] Create SafeAreaHandler.cs
  - [ ] Create UITheme.cs
  - [ ] Create UICanvas.prefab
  - [ ] Create Toast.prefab
  - [ ] Tests

- [ ] **Prompt 13**: Shot Data Bar (Bottom Panel)
  - [ ] Create ShotDataBar.cs
  - [ ] Create DataTile.cs
  - [ ] Create DataTile.prefab
  - [ ] Create ShotDataBar.prefab
  - [ ] Value formatting
  - [ ] Animation
  - [ ] Tests

- [ ] **Prompt 14**: Club Data Panel (HMT)
  - [ ] Create ClubDataPanel.cs
  - [ ] Create SwingPathIndicator.cs
  - [ ] Create AttackAngleIndicator.cs
  - [ ] Create ClubDataPanel.prefab
  - [ ] Create SwingPathIndicator.prefab
  - [ ] Tests

- [ ] **Prompt 15**: Connection Status UI
  - [ ] Create ConnectionStatusUI.cs
  - [ ] Create ConnectionPanel.cs
  - [ ] Create ConnectionToast.cs
  - [ ] Create ConnectionStatus.prefab
  - [ ] Create ConnectionPanel.prefab
  - [ ] Tests

- [ ] **Prompt 16**: Session Info Panel
  - [ ] Create SessionInfoPanel.cs
  - [ ] Create ShotHistoryPanel.cs
  - [ ] Create ShotHistoryItem.cs
  - [ ] Create ShotDetailModal.cs
  - [ ] Create prefabs
  - [ ] Tests

- [ ] **Prompt 17**: Settings Panel
  - [ ] Create SettingsPanel.cs
  - [ ] Create SettingToggle.cs
  - [ ] Create SettingSlider.cs
  - [ ] Create SettingDropdown.cs
  - [ ] Create SettingsPanel.prefab
  - [ ] Tests

---

## Phase 6: TCP/Network Layer

- [ ] **Prompt 18**: TCP Connection for Testing
  - [ ] Create GC2TCPConnection.cs
  - [ ] Create GC2TCPListener.cs
  - [ ] Create GC2TestWindow.cs (Editor)
  - [ ] Tests

- [ ] **Prompt 19**: GSPro Client
  - [ ] Create GSProClient.cs
  - [ ] Create GSProMessage.cs
  - [ ] Heartbeat system
  - [ ] Create GSProModeUI.cs
  - [ ] Tests

---

## Phase 7: macOS Native Plugin

- [ ] **Prompt 20**: macOS Plugin Header and Project
  - [ ] Create GC2MacPlugin.h
  - [ ] Create Xcode project
  - [ ] Configure build settings
  - [ ] Create build script
  - [ ] Stub implementation
  - [ ] Verification tests

- [ ] **Prompt 21**: macOS Plugin Implementation
  - [ ] Complete GC2MacPlugin.mm
  - [ ] libusb integration
  - [ ] Device detection
  - [ ] Read loop
  - [ ] Protocol parsing
  - [ ] Tests (hardware required)

- [ ] **Prompt 22**: macOS C# Bridge
  - [ ] Create GC2MacConnection.cs
  - [ ] DllImport declarations
  - [ ] Callback handling
  - [ ] Update factory
  - [ ] Tests

---

## Phase 8: Android Native Plugin

- [ ] **Prompt 23**: Android Plugin Project
  - [ ] Create Gradle project
  - [ ] Configure manifest
  - [ ] USB device filter
  - [ ] Kotlin stubs
  - [ ] Build script
  - [ ] Verification tests

- [ ] **Prompt 24**: Android Plugin Implementation
  - [ ] Complete GC2Plugin.kt
  - [ ] USB permission handling
  - [ ] Device enumeration
  - [ ] Read thread
  - [ ] Protocol parsing
  - [ ] Tests (device required)

- [ ] **Prompt 25**: Android C# Bridge
  - [ ] Create GC2AndroidConnection.cs
  - [ ] AndroidJavaObject calls
  - [ ] Message handlers
  - [ ] Update factory
  - [ ] Create prefab
  - [ ] Tests

---

## Phase 9: iPad Native Plugin (DriverKit)

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

## Phase 10: Quality & Polish

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

From PHYSICS.md - all validated ✅ (PR #3):

- [x] Driver: 167 mph / 10.9° / 2686 rpm → 275 yds (±5%) ✅
- [x] Driver: 160 mph / 11.0° / 3000 rpm → 259 yds (±3%) ✅
- [x] 7-Iron: 120 mph / 16.3° / 7097 rpm → 172 yds (±5%) ✅
- [x] Wedge: 102 mph / 24.2° / 9304 rpm → 136 yds (±5%) ✅

Additional physics tests also passing:
- [x] Sidespin direction (positive = curves right)
- [x] Wind effects (headwind/tailwind/crosswind)
- [x] Environmental conditions (altitude/temperature)

---

## Notes

- Each prompt should be executed in order
- Mark items as complete with [x] when done
- Add notes for issues or deviations below
- Update "Last Updated" date when making changes
- Update "Next Prompt" when moving forward

### Issue Log

**2026-01-01 (Dev Testing Session)**: Successfully tested full ball visualization pipeline end-to-end:
- Discovered URP render pipeline must be explicitly assigned in Graphics settings (Default Render Pipeline)
- "Ball" tag must be manually created in Tags and Layers
- SceneLoader.MarinaScene path fixed from "Ranges/Marina" to just "Marina"
- SceneGenerator now automatically includes GolfBall, TrajectoryLine, CameraRig prefabs in Marina scene
- Created comprehensive QUICKSTART.md with setup instructions and troubleshooting
- Updated CLAUDE.md with "Local Development on macOS" section

**2026-01-01 (Camera System)**: Prompt 9 complete. Created camera system and test shot window:
- `ICameraMode.cs` - Interface for camera behavior abstraction
- `CameraController.cs` - Mode manager with smooth transitions (Static, Follow, TopDown, FreeOrbit)
- `FollowCamera.cs` - Ball tracking with damping, height tracking, look-ahead, ground avoidance
- `OrbitCamera.cs` - User-controlled orbit with touch (pinch/rotate) and mouse (scroll/drag) controls
- `CameraRigGenerator.cs` - Editor tool to create CameraRig.prefab
- `TestShotWindow.cs` - Editor window for firing test shots without GC2 hardware
  - Presets: Driver, 7-Iron, Wedge, Hook, Slice
  - Environmental conditions support
- 79 new unit tests, all passing (431 total)
- Run `OpenRange > Create Camera Rig Prefab` to generate camera prefab
- Run `OpenRange > Test Shot Window` to open test shot editor (requires Play Mode)

**2026-01-01 (TrajectoryRenderer)**: Prompt 8 complete. Created trajectory visualization:
- `TrajectoryRenderer.cs` - Renders ball flight trajectory as LineRenderer
- Display modes: Actual, Predicted, Both
- Quality tiers: High (100 vertices), Medium (50), Low (20)
- FadeOut animation with configurable duration
- Color gradients for actual (white→cyan) and predicted (yellow→orange) paths
- `TrajectoryLineGenerator.cs` - Editor tool to create prefab and materials
- 43 new unit tests, all passing
- Run `OpenRange > Create Trajectory Line Prefab` to generate prefab and materials

**2026-01-01 (BallController)**: Prompt 7 complete. Created ball animation system:
- `BallController.cs` - Animates ball through trajectory with playback controls (play, pause, skip, reset)
- `BallSpinner.cs` - Handles ball rotation based on spin data with decay
- 78 new unit tests (45 BallController, 33 BallSpinner), all passing
- Integrates with BallVisuals for trail and spin visualization
- Events: OnFlightStarted, OnApexReached, OnLanded, OnRollStarted, OnStopped, OnPhaseChanged

**2026-01-01 (Golf Ball Prefab)**: Prompt 6 complete. Created golf ball visualization foundation:
- `BallVisuals.cs` - Component managing trail and spin visualization with quality tier support
- `GolfBallPrefabGenerator.cs` - Editor tool to create GolfBall.prefab, GolfBall.mat, BallTrail.prefab
- 34 new unit tests for BallVisuals, all passing
- Run `OpenRange > Create Golf Ball Prefab` to generate prefab and materials
- Run `OpenRange > Create Ball Layer` to add "Ball" layer for physics

**2025-12-31 (Quality Management)**: Prompt 5 complete. Created platform detection and quality management:
- `PlatformManager.cs` - Static utility for platform detection, Apple Silicon check, screen category
- `QualityManager.cs` - MonoBehaviour singleton for quality tier management with dynamic adjustment
- `URPQualitySetup.cs` - Editor tool to create URP quality assets
- 57 new unit tests, all passing
- Run `OpenRange > Create URP Quality Assets` to generate Low/Medium/High URP assets
- Run `OpenRange > Configure QualityManager References` to wire up the assets

**2025-12-31 (Scene Infrastructure)**: Prompt 4 complete. Created scene infrastructure with SceneGenerator editor tool. Key additions:
- `SceneLoader.cs` - Static utility with sync/async scene loading and progress callbacks
- `BootstrapLoader.cs` - Initializes managers in order, loads MainMenu
- `SceneGenerator.cs` - Editor tool creates Bootstrap, MainMenu, Marina scenes
- 16 new PlayMode tests for scene loading and manager persistence
- Run `OpenRange > Generate All Scenes` after importing to create .unity files

**2025-12-31 (Physics)**: Physics calibration complete. Used libgolf C++ library as reference implementation for Nathan model coefficients. Key changes:
- Quadratic lift formula: `Cl = 1.99×S - 3.25×S²` (capped at 0.305)
- Spin-dependent drag: `Cd = Cd_base + CdSpin × S`
- Updated coefficients: CdLow=0.50, CdHigh=0.212, CdSpin=0.15
