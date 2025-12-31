# Open Range Unity - Development Todo

## Current Status
**Phase**: Not Started
**Last Updated**: 2024-12-31

---

## Phase 1: Unity Project Foundation

- [ ] **Prompt 1**: Unity Project Setup
  - [ ] Create Unity 2022.3 LTS project
  - [ ] Set up folder structure per TRD
  - [ ] Configure build targets (macOS, iOS, Android)
  - [ ] Create URP quality tier assets
  - [ ] Create placeholder scenes
  - [ ] Configure project settings
  - [ ] Add .gitignore and README

- [ ] **Prompt 2**: Core Data Models
  - [ ] Create GC2ShotData.cs
  - [ ] Create GC2DeviceInfo.cs
  - [ ] Create GC2Protocol.cs with parsing
  - [ ] Write unit tests for data models

- [ ] **Prompt 3**: GC2 Connection Interface
  - [ ] Create IGC2Connection interface
  - [ ] Create GC2ConnectionFactory
  - [ ] Create GC2MockConnection for testing
  - [ ] Create platform placeholder classes
  - [ ] Write unit tests

- [ ] **Prompt 4**: Core Game Manager
  - [ ] Create GameManager singleton
  - [ ] Create ShotProcessor
  - [ ] Create SessionManager
  - [ ] Create MainThreadDispatcher
  - [ ] Create PlatformManager
  - [ ] Set up Bootstrap scene
  - [ ] Write unit tests

---

## Phase 2: Physics Engine

- [ ] **Prompt 5**: Physics Constants and Units
  - [ ] Create PhysicsConstants.cs
  - [ ] Create UnitConversions.cs
  - [ ] Create AtmosphericModel.cs
  - [ ] Write unit tests

- [ ] **Prompt 6**: Aerodynamics Model
  - [ ] Create Aerodynamics.cs (Nathan model)
  - [ ] Create SpinDecay.cs
  - [ ] Create SpinAxisCalculator.cs
  - [ ] Write unit tests with validation data

- [ ] **Prompt 7**: Trajectory Simulator
  - [ ] Create TrajectorySimulator.cs
  - [ ] Create TrajectoryInput.cs
  - [ ] Create TrajectoryResult.cs
  - [ ] Implement RK4 integration
  - [ ] Write tests for known trajectories

- [ ] **Prompt 8**: Ground Physics
  - [ ] Create GroundPhysics.cs
  - [ ] Create RollResult.cs
  - [ ] Create GroundResult.cs
  - [ ] Write unit tests

---

## Phase 3: 3D Visualization

- [ ] **Prompt 9**: Ball Prefab and Controller
  - [ ] Create GolfBall.prefab
  - [ ] Create BallController.cs
  - [ ] Create TrajectoryRenderer.cs
  - [ ] Create LandingMarker.cs
  - [ ] Write play mode tests

- [ ] **Prompt 10**: Camera System
  - [ ] Create CameraController.cs
  - [ ] Create FollowCamera.cs
  - [ ] Create OrbitCamera.cs
  - [ ] Create CameraRig prefab
  - [ ] Write play mode tests

- [ ] **Prompt 11**: Marina Range Environment
  - [ ] Build Marina.unity scene
  - [ ] Create EnvironmentManager.cs
  - [ ] Set up terrain, water, props
  - [ ] Configure lighting
  - [ ] Performance testing

- [ ] **Prompt 12**: Visual Effects
  - [ ] Create BallTrail.prefab
  - [ ] Create LandingDust.prefab
  - [ ] Create WaterSplash.prefab
  - [ ] Create EffectsManager.cs
  - [ ] Write tests

---

## Phase 4: USB Abstraction Layer

- [ ] **Prompt 13**: USB Connection Events
  - [ ] Create GC2ConnectionManager.cs
  - [ ] Create ConnectionState enum
  - [ ] Create GC2ConnectionConfig.cs
  - [ ] Create GC2PermissionHandler.cs
  - [ ] Write unit tests

- [ ] **Prompt 14**: TCP Connection Implementation
  - [ ] Create GC2TCPConnection.cs
  - [ ] Create GSProClient.cs
  - [ ] Create GC2TCPListener.cs
  - [ ] Write unit tests

- [ ] **Prompt 15**: Protocol Parser Hardening
  - [ ] Enhance GC2Protocol.cs
  - [ ] Create GC2DataBuffer.cs
  - [ ] Improve validation
  - [ ] Create test data files
  - [ ] Write extensive tests

---

## Phase 5: macOS Native Plugin

- [ ] **Prompt 16**: macOS Plugin Header and Interface
  - [ ] Create GC2MacPlugin.h
  - [ ] Set up Xcode project
  - [ ] Configure libusb dependency
  - [ ] Create build script
  - [ ] Stub implementation

- [ ] **Prompt 17**: macOS Plugin Implementation
  - [ ] Implement GC2MacPlugin.mm
  - [ ] Device detection with libusb
  - [ ] USB read loop
  - [ ] Protocol parsing
  - [ ] Build and test

- [ ] **Prompt 18**: macOS C# Bridge
  - [ ] Create GC2MacConnection.cs
  - [ ] DllImport declarations
  - [ ] Callback handling
  - [ ] Create editor test window
  - [ ] Write integration tests

---

## Phase 6: Android Native Plugin

- [ ] **Prompt 19**: Android Plugin Interface
  - [ ] Create Gradle project
  - [ ] Set up manifest and filters
  - [ ] Create GC2Plugin.kt
  - [ ] Create GC2Device.kt
  - [ ] Create build script

- [ ] **Prompt 20**: Android Plugin Implementation
  - [ ] USB permission handling
  - [ ] USB Host API integration
  - [ ] Read thread
  - [ ] Protocol parsing
  - [ ] Test on devices

- [ ] **Prompt 21**: Android C# Bridge
  - [ ] Create GC2AndroidConnection.cs
  - [ ] AndroidJavaObject calls
  - [ ] Message handlers
  - [ ] Create prefab
  - [ ] Write tests

---

## Phase 7: iPad Native Plugin (DriverKit)

- [ ] **Prompt 22**: iPad Plugin Interface
  - [ ] Create GC2iOSPlugin project
  - [ ] Create GC2Driver.dext structure
  - [ ] Set up entitlements
  - [ ] Placeholder implementation

- [ ] **Prompt 23**: iPad DriverKit Implementation
  - [ ] Implement GC2Driver
  - [ ] Implement GC2UserClient
  - [ ] Implement GC2iOSPlugin framework
  - [ ] Build and sign

- [ ] **Prompt 24**: iPad C# Bridge
  - [ ] Create GC2iPadConnection.cs
  - [ ] DriverKit state handling
  - [ ] User guidance UI
  - [ ] Write tests

---

## Phase 8: UI System

- [ ] **Prompt 25**: Shot Data UI
  - [ ] Create UIManager.cs
  - [ ] Create ShotDataBar.cs
  - [ ] Create DataTile.cs
  - [ ] Create prefabs
  - [ ] Write tests

- [ ] **Prompt 26**: Club Data Panel (HMT)
  - [ ] Create ClubDataPanel.cs
  - [ ] Create SwingPathIndicator.cs
  - [ ] Create AttackAngleIndicator.cs
  - [ ] Create prefabs
  - [ ] Write tests

- [ ] **Prompt 27**: Connection Status UI
  - [ ] Create ConnectionStatusUI.cs
  - [ ] Create ConnectionPanel.cs
  - [ ] Create ConnectionToast.cs
  - [ ] Create prefabs
  - [ ] Write tests

- [ ] **Prompt 28**: Settings Panel
  - [ ] Create SettingsPanel.cs
  - [ ] Create SettingsManager.cs
  - [ ] Create setting input prefabs
  - [ ] Write tests

---

## Phase 9: Integration & Polish

- [ ] **Prompt 29**: Session Info Panel
  - [ ] Create SessionInfoPanel.cs
  - [ ] Create ShotHistoryPanel.cs
  - [ ] Create ShotDetailModal.cs
  - [ ] Enhance SessionManager
  - [ ] Write tests

- [ ] **Prompt 30**: Responsive Layout System
  - [ ] Create ResponsiveLayout.cs
  - [ ] Create ResponsiveElement.cs
  - [ ] Create SafeAreaHandler.cs
  - [ ] Test at various resolutions

- [ ] **Prompt 31**: Quality Tier System
  - [ ] Enhance QualityManager.cs
  - [ ] Create URP assets for each tier
  - [ ] Implement dynamic adjustment
  - [ ] Write tests

- [ ] **Prompt 32**: Full Integration and Polish
  - [ ] Wire up Marina.unity
  - [ ] Implement boot sequence
  - [ ] Create MainMenu.unity
  - [ ] Full shot flow testing
  - [ ] Error handling
  - [ ] Final polish
  - [ ] Platform testing

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
- [ ] DriverKit approval

### Android
- [ ] Samsung Galaxy Tab S8+
- [ ] Pixel Tablet
- [ ] Budget tablet test
- [ ] USB-C connection with GC2

---

## Notes

- Each prompt should be executed in order
- Mark items as complete with [x] when done
- Add notes for issues or deviations
- Update "Last Updated" date when making changes
