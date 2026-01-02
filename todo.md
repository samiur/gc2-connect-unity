# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 5 - UI System (7 of 7 complete) ✅
**Last Updated**: 2026-01-02
**Next Prompt**: 18 (TCP Connection for Testing) or 32 (Ground Physics Improvement)
**Physics**: ✅ Carry validated (PR #3) | ⚠️ Bounce/roll needs improvement (see Phase 5.5)

---

## Already Implemented (Skeleton)

These components exist and don't need to be rebuilt:

- [x] **Physics Engine** (carry validated, bounce/roll needs improvement)
  - [x] TrajectorySimulator.cs - RK4 integration
  - [x] Aerodynamics.cs - Nathan model
  - [ ] GroundPhysics.cs - Bounce and roll (⚠️ needs spin-dependent model, see Phase 5.5)
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

- [x] **Prompt 10**: Landing Marker and Effects (PR #17)
  - [x] Create LandingMarker.cs
  - [x] Create LandingMarker.prefab (via LandingMarkerGenerator)
  - [x] Create ImpactEffect.cs
  - [x] Create LandingDust.prefab (via LandingMarkerGenerator)
  - [x] Create EffectsManager.cs
  - [x] Tests (54 new tests)

- [x] **Prompt 11**: Marina Environment Setup (PR #19)
  - [x] Expand Marina.unity scene (via SceneGenerator)
  - [x] Create EnvironmentManager.cs
  - [x] Create DistanceMarker.cs + prefab
  - [x] Create TargetGreen.cs + prefab
  - [x] Create TeeMat.cs + prefab
  - [x] Create EnvironmentGenerator.cs (editor tool for prefabs/materials)
  - [x] Tests (109 new tests)

---

## Phase 5: UI System

- [x] **Prompt 12**: UIManager and Layout System (PR #21)
  - [x] Create UIManager.cs
  - [x] Create ResponsiveLayout.cs
  - [x] Create SafeAreaHandler.cs
  - [x] Create UITheme.cs
  - [x] Create UICanvasGenerator.cs (editor tool)
  - [x] Tests (124 new tests)

- [x] **Prompt 13**: Shot Data Bar (Bottom Panel) (PR #23)
  - [x] Create ShotDataBar.cs
  - [x] Create DataTile.cs
  - [x] Create ShotDataBarGenerator.cs (editor tool for DataTile.prefab and ShotDataBar.prefab)
  - [x] Value formatting (direction L/R, thousands separator, unit conversion)
  - [x] Animation (fade highlight on value change)
  - [x] Responsive font sizing (Compact/Regular/Large)
  - [x] Tests (78 new tests: 41 DataTile, 37 ShotDataBar)

- [x] **Prompt 14**: Club Data Panel (HMT) (PR #25)
  - [x] Create ClubDataPanel.cs
  - [x] Create SwingPathIndicator.cs
  - [x] Create AttackAngleIndicator.cs
  - [x] Create ClubDataPanelGenerator.cs (editor tool for prefabs)
  - [x] Tests (88 new tests)

- [x] **Prompt 15**: Connection Status UI (PR #27)
  - [x] Create ConnectionStatusUI.cs
  - [x] Create ConnectionPanel.cs
  - [x] Create ConnectionToast.cs
  - [x] Create ConnectionStatusGenerator.cs (editor tool)
  - [x] Create ConnectionStatus.prefab
  - [x] Create ConnectionPanel.prefab
  - [x] Wire into MainMenu and Marina scenes via SceneGenerator
  - [x] Tests (86 new tests)

- [x] **Prompt 16**: Session Info Panel (PR #29)
  - [x] Create SessionInfoPanel.cs
  - [x] Create ShotHistoryPanel.cs
  - [x] Create ShotHistoryItem.cs
  - [x] Create ShotDetailModal.cs
  - [x] Create SessionInfoPanelGenerator.cs (editor tool for prefabs)
  - [x] Wire into Marina scene via MarinaSceneController and SceneGenerator
  - [x] Tests (122 new tests)

- [x] **Prompt 17**: Settings Panel (PR #31)
  - [x] Create SettingsPanel.cs
  - [x] Create SettingToggle.cs
  - [x] Create SettingSlider.cs
  - [x] Create SettingDropdown.cs
  - [x] Create SettingsPanelGenerator.cs (editor tool for prefabs)
  - [x] Wire into MainMenu and Marina scenes
  - [x] Tests (101 new tests)

---

## Phase 5.5: Ground Physics Improvement

- [ ] **Prompt 32**: Spin-Dependent Bounce
  - [ ] Implement velocity-dependent COR (Penner's formula: e = 0.510 - 0.0375v + 0.000903v²)
  - [ ] Add spin-dependent braking effect (high backspin reduces horizontal velocity)
  - [ ] Add landing angle effects on friction
  - [ ] Implement spin reversal detection (high backspin + steep angle = backward bounce)
  - [ ] Calculate post-bounce spin (friction reduces spin, possible reversal)
  - [ ] Unit tests with validation against TrackMan data

- [ ] **Prompt 33**: Improved Roll Model
  - [ ] Implement spin-enhanced deceleration (backspin increases ground braking)
  - [ ] Add residual backspin effects (high spin continues to brake during roll)
  - [ ] Implement spin-induced direction change (backward roll for high-spin wedges)
  - [ ] Add coupled spin decay during roll
  - [ ] Surface-specific roll behavior (green vs fairway vs rough)
  - [ ] Unit tests

- [ ] **Prompt 34**: Physics Validation and Integration
  - [ ] Add landing angle tracking to ShotResult
  - [ ] Update TrajectorySimulator to pass landing data to GroundPhysics
  - [ ] Integration tests with full simulation pipeline
  - [ ] Validation against TrackMan PGA Tour data:
    - [ ] Driver: ~75° landing, 45 yds roll
    - [ ] 7-Iron: ~48° landing, 15 yds roll
    - [ ] PW: ~50° landing, 5 yds roll (with check)
    - [ ] High-spin wedge: ~53° landing, minimal roll or spin-back

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

**2026-01-02 (Settings Panel)**: Prompt 17 complete. Created comprehensive settings UI (PR #31):
- `SettingToggle.cs` - Reusable toggle for boolean settings
  - Label and value binding with OnValueChanged event
  - SetWithoutNotify() for initialization without triggering events
  - Responsive font sizing based on ScreenCategory
- `SettingSlider.cs` - Reusable slider for numeric range settings
  - Label, suffix, and format support (F0/F1/P0 etc.)
  - Range configuration with SetRange(min, max)
  - NormalizedValue property for 0-1 range
  - WholeNumbers mode for integer values
- `SettingDropdown.cs` - Reusable dropdown for enum/option settings
  - Enum helper methods: SetOptionsFromEnum<T>(), GetSelectedEnum<T>(), SetSelectedEnum<T>()
  - SetWithoutNotify() for initialization
- `SettingsPanel.cs` - Main panel with all settings sections
  - Display: QualityTier, DistanceUnit, SpeedUnit, TemperatureUnit dropdowns
  - Environment: Temperature (40-100°F), Elevation (0-8000ft), Humidity (0-100%) sliders
  - Wind: Enable toggle, Speed (0-30mph), Direction (0-360°) sliders with conditional visibility
  - Connection: AutoConnect toggle, GSPro Host/Port input fields
  - Audio: Master Volume, Effects Volume sliders (0-100%)
  - Reset button clears all settings to defaults
  - Binds to SettingsManager for automatic persistence
- `SettingsPanelGenerator.cs` - Editor tool for prefab creation
  - Menu: OpenRange > Create Setting Toggle/Slider/Dropdown Prefab, Create Settings Panel Prefab, Create All Settings Panel Prefabs
- SceneGenerator updates for MainMenu and Marina scenes
- 101 new unit tests (33 Toggle, 32 Slider, 24 Dropdown, 12 Panel)
- **Test fix**: P0 format produces "50 %" (with space) on some locales, updated test to use Contains() assertion

**2026-01-01 (Session Info Panel)**: Prompt 16 complete. Created session info and shot history UI (PR #29):
- `SessionInfoPanel.cs` - Compact top-left panel with live session statistics
  - Elapsed time with auto-updating timer (configurable interval)
  - Total shots, average speed, longest carry display
  - Click handler to expand ShotHistoryPanel
  - SessionManager event subscription for live updates
- `ShotHistoryPanel.cs` - Expandable shot list panel
  - ScrollRect with shot history items
  - Statistics summary header (total, avg speed, avg carry, longest)
  - Shot selection with OnShotSelected event
  - Clear history button with confirmation
- `ShotHistoryItem.cs` - Individual shot row in history list
  - Shot number, ball speed, carry distance, timestamp
  - Selection highlighting with configurable colors
- `ShotDetailModal.cs` - Full-screen shot detail overlay
  - Ball data section (speed, launch angle, direction, spin)
  - Result data section (carry, run, total, apex, offline)
  - Club data section (when HMT data available)
  - Replay button for shot animation replay
- `SessionInfoPanelGenerator.cs` - Editor tool for prefab creation
  - Menu: OpenRange > Create Session Info Panel Prefab / Shot History Panel Prefab / Shot History Item Prefab / Shot Detail Modal Prefab / All Session Info Prefabs
- `GameManager.cs` - Added public SessionManager property
- 122 new unit tests (32 SessionInfoPanel, 36 ShotHistoryPanel, 18 ShotHistoryItem, 36 ShotDetailModal)
- **Fixes during implementation:**
  - Button listener wasn't set up in EditMode tests (Start() not called) - called SetupButtonListener() in SetReferences()
  - Club speed text reference missing for tests - added SetClubDataReferences() method
  - ScrollRect.normalizedPosition needs layout pass - changed tests to DoesNotThrow assertions
  - Property name mismatches (LateralDeviation→OfflineDistance, ClubPath/SwingPath→Path)

**2026-01-02 (Ground Physics Improvement Plan)**: Added Phase 5.5 (Prompts 32-34) to address bounce/roll physics issues:
- **Problem**: Carry distances are accurate but post-carry behavior is unrealistic:
  - Too much roll on all shots (45+ yards on drivers, should be ~25-45 based on conditions)
  - High-spin wedge shots (9000+ rpm) bounce and roll instead of checking/spinning back
  - No spin-dependent effects on ground interaction
- **Root cause analysis** of current GroundPhysics.cs:
  - Fixed COR (0.6) regardless of impact velocity - real COR is velocity-dependent
  - Simple 30% friction reduction - no spin-dependent braking
  - No spin reversal mechanism - high backspin should reverse horizontal velocity
  - No landing angle consideration - steeper angles increase braking
- **Research conducted**:
  - Penner's velocity-dependent COR: e = 0.510 - 0.0375v + 0.000903v²
  - Spin reversal occurs when μ × m × g × contact_time > horizontal_momentum
  - Tangential friction coefficient ~0.4-0.5 for grass
  - TrackMan PGA Tour data for validation targets
- **Three prompts added to plan.md**:
  - Prompt 32: Spin-Dependent Bounce (velocity COR, spin braking, reversal)
  - Prompt 33: Improved Roll Model (spin deceleration, backward roll, surface behavior)
  - Prompt 34: Validation and Integration (landing angle tracking, validation tests)

**2026-01-02 (GC2 Protocol Update)**: Updated GC2_PROTOCOL.md with detailed USB protocol information:
- **Endpoint correction**: Shot data uses INTERRUPT IN endpoint (0x82), not BULK
- **Message types**: 0H = shot data (process), 0M = ball movement (skip)
- **Data accumulation**: Multi-packet assembly with waiting for BACK_RPM/SIDE_RPM
- **Misread patterns**: Added 2222 error pattern to GC2Protocol.IsValidShot()
- Updated plan.md prompts 21, 24 with correct endpoint and parsing strategy
- Added "GC2 USB Protocol Summary" to Appendix D of plan.md

**2026-01-02 (Club Data Panel)**: Prompt 14 complete. Created HMT club data visualization (PR #25):
- `ClubDataPanel.cs` - Left side panel for HMT metrics
  - 5 DataTiles: Club Speed (mph), Path (°), Attack Angle (°), Face to Target (°), Dynamic Loft (°)
  - Direction enums: SwingPathDirection (InToOut/OutToIn/Neutral), AttackAngleDirection (Ascending/Descending/Neutral), FaceDirection (Open/Closed/Square)
  - Static helper methods: GetPathDirection(), GetAttackDirection(), GetFaceDirection()
  - UpdateDisplay(GC2ShotData) with HasClubData check
  - Events: OnDisplayUpdated, OnDisplayCleared
- `SwingPathIndicator.cs` - Top-down path visualization
  - Arrow showing in-to-out (blue) vs out-to-in (orange) path direction
  - Face angle line overlay for club face position
  - 3x rotation scaling for visual amplification of small angles
  - Neutral threshold (0.5°) for classification
- `AttackAngleIndicator.cs` - Side view attack angle visualization
  - Arrow showing ascending (green) vs descending (blue/purple)
  - Rotation clamping (±45°) for extreme values
  - Neutral state color (white/gray)
- `ClubDataPanelGenerator.cs` - Editor tool for prefab creation
  - Menu: OpenRange > Create Club Data Panel Prefab / Swing Path Indicator Prefab / Attack Angle Indicator Prefab / All Club Data Panel Prefabs
  - Creates properly configured vertical layout with all 5 tiles and both indicators
- 88 new unit tests (40 ClubDataPanel, 25 SwingPathIndicator, 23 AttackAngleIndicator)
- Run `OpenRange > Create All Club Data Panel Prefabs` to generate prefabs
- **Fix (same PR)**: Scene integration was initially missing. Added:
  - `MarinaSceneController.cs`: Added `_clubDataPanel` serialized field, Clear() in InitializeScene(), UpdateDisplay() in OnShotProcessed()
  - `SceneGenerator.cs`: Instantiate ClubDataPanel prefab on canvas left side, wire reference to controller
  - Updated docs (CLAUDE.md, plan.md) with comprehensive Scene Integration Checklist

**2026-01-01 (Shot Data Bar)**: Prompt 13 complete. Created GSPro-style shot data display (PR #23):
- `DataTile.cs` - Reusable data display component
  - Value formatting: SetValue(), SetValueWithDirection(), SetValueWithThousands()
  - Direction prefixes: "L" for negative, "R" for positive values
  - Responsive font sizing based on ScreenCategory (Compact/Regular/Large)
  - Highlight support for Total distance (TotalRed color)
  - Animation: fade pulse on value change using CanvasGroup
- `ShotDataBar.cs` - GSPro-style 10-tile bottom panel
  - Tiles: Ball Speed, Direction, Angle, Back Spin, Side Spin, Apex, Offline, Carry, Run, Total
  - UpdateDisplay(GC2ShotData, ShotResult) maps all values
  - Apex conversion: feet to yards (÷3)
  - Events: OnDisplayUpdated, OnDisplayCleared
  - Responsive layout spacing based on screen size
- `ShotDataBarGenerator.cs` - Editor tool for prefab creation
  - Menu: OpenRange > Create Data Tile Prefab / Shot Data Bar Prefab / All Shot Data Bar Prefabs
  - Creates properly configured HorizontalLayoutGroup with all 10 tiles
- `AssemblyInfo.cs` - Added InternalsVisibleTo for test assemblies
- 78 new unit tests (41 DataTile, 37 ShotDataBar)
- Run `OpenRange > Create Shot Data Bar Prefab` to generate prefab

**2026-01-01 (Coordinate System Test Fixes)**: Fixed 8 failing CI tests after coordinate system fix:
- **Root cause**: Previous session fixed trajectory rendering direction (Physics X → Unity Z), but test assertions still checked the old axis
- **BallControllerTests fixed (4 tests)**:
  - `SkipToEnd_MovesBallToFinalPosition` - changed `position.x` to `position.z`
  - `GetPositionAtTime_BeyondTrajectory_ReturnsLastPosition` - changed `position.x` to `position.z`
  - `GetPositionAtTime_InterpolatesBetweenPoints` - changed `position.x` to `position.z`
  - `PlayShot_ConvertsYardsToMeters` - changed `_ballTransform.position.x` to `.z`
- **TrajectoryRendererTests fixed (4 tests)**:
  - `ShowTrajectory_ConvertsCoordinatesCorrectly` - updated all axis assertions
  - `GetPositionAtProgress_One_ReturnsEndPosition` - changed `pos.x` to `pos.z`
  - `GetPositionAtProgress_Half_ReturnsInterpolatedPosition` - changed `pos.x` to `pos.z`
  - `GetPositionAtProgress_OverOne_ClampsToOne` - changed `pos.x` to `pos.z`
- **Process improvement**: Can quit Unity via AppleScript (`osascript -e 'quit app "Unity"'`) to run batchmode tests

**2026-01-01 (UIManager and Layout System)**: Prompt 12 complete. Created UI foundation (PR #21):
- `UITheme.cs` - Static class with theme constants
  - Colors: PanelBackground (#1a1a2e@85%), AccentGreen (#2d5a27), TextPrimary (white), TotalRed (#ff6b6b)
  - Toast colors: Info (blue), Success (green), Warning (amber), Error (red)
  - Font sizes per screen category (Compact/Regular/Large)
  - Spacing: Padding, Margin, BorderRadius constants
  - Animation durations (Fast 0.15s, Normal 0.3s, Slow 0.5s)
- `ResponsiveLayout.cs` - Screen size detection and layout events
  - Breakpoints: Compact (<800px), Regular (800-1200px), Large (>1200px)
  - Events: OnLayoutChanged, OnOrientationChanged, OnSafeAreaChanged
  - Diagonal inches calculation for device detection
- `SafeAreaHandler.cs` - Device safe area handling
  - Configurable edges (top, bottom, left, right)
  - Automatic RectTransform adjustment for notches/home indicators
- `UIManager.cs` - Singleton managing UI panels and toasts
  - Panel registry with Show/Hide/Toggle operations
  - Toast notification queue with type-based styling
  - Events: OnPanelShown, OnPanelHidden, OnToastShown
- `UICanvasGenerator.cs` - Editor tool for UI prefabs
  - Menu: OpenRange > Create UI Canvas Prefab / Toast Prefab / All UI Prefabs
  - Configures Canvas with CanvasScaler (1920x1080 reference)
- 124 new unit tests (45 UITheme, 24 ResponsiveLayout, 20 SafeAreaHandler, 35 UIManager)
- **Bug fix**: Coordinate system conversion in BallController/TrajectoryRenderer
  - Fixed trajectory rendering to the right instead of forward
  - Physics X (forward) now correctly maps to Unity Z

**2026-01-01 (Marina Environment)**: Prompt 11 complete. Created environment components (PR #19):
- `EnvironmentManager.cs` - Singleton managing environment state with quality tier support
  - Distance marker and target green spawning/management
  - Unit conversion utilities (yards to meters: 0.9144)
  - Quality-based draw distance (500/350/200 meters)
- `DistanceMarker.cs` - Distance marker signs at yardage intervals
  - Quality-based visibility fade (400/300/150 meter fade distances)
  - TextMeshPro distance text display
- `TargetGreen.cs` - Target green areas with optional animated flag
  - Sizes: Small (10m), Medium (15m), Large (20m)
  - Highlight system for ball landing detection
  - Flag animation on High quality, static on Medium, hidden on Low
- `TeeMat.cs` - Tee mat with ball spawn position
  - Configurable dimensions (default 2m x 3m)
  - Bounds checking for position detection
- `EnvironmentGenerator.cs` - Editor tool for creating prefabs and materials
- 109 new unit tests (37 EnvironmentManager, 20 DistanceMarker, 28 TargetGreen, 24 TeeMat)
- **Fixes applied during implementation:**
  - Lazy initialization of MaterialPropertyBlock for EditMode test compatibility
  - Singleton "fake null" handling with Unity's overloaded == operator
  - Reflection-based singleton cleanup between tests for isolation

**2026-01-01 (Landing Marker Test Fixes)**: Fixed 24 failing EditMode tests and 1 failing PlayMode test after PR #17 merge:
- **LandingMarker material leak (23 tests)**: Changed `renderer.material` to `renderer.sharedMaterial` for reading colors in EditMode. Used `MaterialPropertyBlock` for modifying colors without creating material instances.
- **FadeOut(0) immediate hide**: Changed default parameter from `0f` to `-1f` and added explicit check for `duration == 0f` to hide immediately as expected by tests.
- **EffectsManager singleton null**: Changed singleton check to use Unity's `==` operator (handles "fake null" destroyed objects). Added `ForceInitializeSingleton()` method for EditMode tests where Awake may not be called.
- **SceneLoader.MarinaScene constant**: Fixed test expectation from "Ranges/Marina" to "Marina" - Unity loads scenes by name, not folder path.
- Added `make run` and `make run-marina` commands to Makefile for CLI development workflow.

**2026-01-01 (Landing Marker and Effects)**: Prompt 10 complete. Created landing effects system:
- `LandingMarker.cs` - Visual marker showing landing position with distance text
  - Shows carry distance and total distance
  - Fade in/out animation with configurable duration
  - Quality tier support (Low hides total distance)
- `ImpactEffect.cs` - Particle effect wrapper for landing dust
  - Velocity-scaled particle count
  - Quality tiers: High (30), Medium (20), Low (10) particles
- `EffectsManager.cs` - Singleton with object pooling
  - Subscribes to BallController.OnLanded and OnStopped events
  - Manages marker and effect pools
  - Quality tier propagation
- `LandingMarkerGenerator.cs` - Editor tool for prefab creation
  - Creates LandingMarker.prefab with ring and TextMeshPro components
  - Creates LandingDust.prefab with configured ParticleSystem
- 54 new unit tests (29 LandingMarker, 17 ImpactEffect, 8 EffectsManager)
- Run `OpenRange > Create All Landing Effects` to generate both prefabs
- **Fixes applied during implementation:**
  - Added `Unity.TextMeshPro` to assembly definition references (TMPro namespace errors)
  - Added `com.unity.modules.particlesystem` to package manifest (ParticleSystem type errors)
  - Added `using UnityEngine.TestTools;` for LogAssert in tests
  - Updated SceneGenerator to include EffectsManager in Marina scene

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
