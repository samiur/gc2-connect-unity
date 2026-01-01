# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 1 - Core Services (2 of 3 complete)
**Last Updated**: 2025-12-31
**Next Prompt**: 3 (SettingsManager)
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

- [ ] **Prompt 3**: SettingsManager Service
  - [ ] Create SettingsManager.cs
  - [ ] All settings categories
  - [ ] PlayerPrefs persistence
  - [ ] Default values
  - [ ] Events
  - [ ] Unit tests

---

## Phase 2: Scenes & Bootstrap

- [ ] **Prompt 4**: Unity Scene Structure
  - [ ] Create Bootstrap.unity
  - [ ] Create MainMenu.unity
  - [ ] Create Ranges/Marina.unity (placeholder)
  - [ ] Create BootstrapLoader.cs
  - [ ] Create SceneLoader.cs
  - [ ] Configure build settings
  - [ ] Play mode tests

- [ ] **Prompt 5**: PlatformManager and QualityManager
  - [ ] Create PlatformManager.cs
  - [ ] Create QualityManager.cs
  - [ ] Create URP quality assets
  - [ ] Platform detection
  - [ ] Quality tier switching
  - [ ] Tests

---

## Phase 3: Ball Visualization

- [ ] **Prompt 6**: Golf Ball Prefab and Materials
  - [ ] Create GolfBall.prefab
  - [ ] Create GolfBall.mat
  - [ ] Create BallTrail.prefab
  - [ ] Create BallVisuals.cs
  - [ ] Tests

- [ ] **Prompt 7**: BallController Animation System
  - [ ] Create BallController.cs
  - [ ] Animation system
  - [ ] Playback controls
  - [ ] Events
  - [ ] Create BallSpinner.cs
  - [ ] Tests

- [ ] **Prompt 8**: TrajectoryRenderer
  - [ ] Create TrajectoryRenderer.cs
  - [ ] Line renderer setup
  - [ ] Quality tier variants
  - [ ] Create TrajectoryLine.prefab
  - [ ] Tests

- [ ] **Prompt 9**: Camera System
  - [ ] Create CameraController.cs
  - [ ] Create FollowCamera.cs
  - [ ] Create OrbitCamera.cs
  - [ ] Create ICameraMode.cs
  - [ ] Create CameraRig.prefab
  - [ ] Tests

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

**2025-12-31**: Physics calibration complete. Used libgolf C++ library as reference implementation for Nathan model coefficients. Key changes:
- Quadratic lift formula: `Cl = 1.99×S - 3.25×S²` (capped at 0.305)
- Spin-dependent drag: `Cd = Cd_base + CdSpin × S`
- Updated coefficients: CdLow=0.50, CdHigh=0.212, CdSpin=0.15
