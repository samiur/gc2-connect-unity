# GC2 Connect Unity - Prompt Plan

## Project Overview

**Goal**: Build a cross-platform Unity driving range simulator with native USB connection to the Foresight GC2 launch monitor, running on macOS, iPad, and Android.

**Key Architecture**:
- Unity 6 (6000.3.2f1) with Universal Render Pipeline (URP)
- 95% shared C# code
- Platform-specific native USB plugins (Obj-C/libusb for macOS, Swift/DriverKit for iPad, Kotlin/USB Host API for Android)
- Physics-accurate ball flight simulation (Nathan model, validated against libgolf reference)
- GSPro-quality visuals with quality tier system

---

## Current State (Skeleton Code)

The following components are **already implemented** in the skeleton:

### ✅ Fully Implemented
- **Physics Engine**: `TrajectorySimulator.cs`, `Aerodynamics.cs`, `GroundPhysics.cs`, `PhysicsConstants.cs`, `UnitConversions.cs`, `ShotResult.cs`
- **GC2 Data Models**: `GC2ShotData.cs`, `IGC2Connection.cs`, `GC2ConnectionFactory.cs`, `GC2Protocol.cs`
- **Core Framework**: `GameManager.cs` (singleton, mode switching, connection lifecycle)
- **Utilities**: `MainThreadDispatcher.cs`

### ✅ Recently Implemented
- **Core Services**: `ShotProcessor.cs` (PR #1), `SessionManager.cs` (PR #2)
- **Physics Calibration**: Validated against Nathan model using libgolf reference (PR #3)
- **Tests**: PhysicsValidationTests, ShotProcessorTests, SessionManagerTests

### ❌ Not Yet Implemented
- **Core Services**: `SettingsManager.cs`, `QualityManager.cs`
- **Visualization**: All ball animation, camera, environment, effects
- **UI**: All panels, data display, settings
- **Native Plugins**: All platforms (macOS, Android, iPad)
- **Network**: GSProClient, TCP connections
- **Scenes**: Bootstrap, MainMenu, Marina range

---

## Phase Breakdown (Updated)

### Phase 1: Complete Core Services (Prompts 1-3)
Finish the core service layer that GameManager depends on.

### Phase 2: Create Scenes & Bootstrap (Prompts 4-5)
Set up the scene structure and boot sequence.

### Phase 3: Ball Visualization (Prompts 6-9)
Ball prefab, animation controller, trajectory rendering, camera system.

### Phase 4: Marina Environment (Prompts 10-11)
3D environment setup with distance markers and targets.

### Phase 5: UI System (Prompts 12-17)
Shot data display, HMT panel, connection status, settings, responsive layout.

### Phase 6: TCP/Network Layer (Prompts 18-19)
TCP connection for testing and GSPro relay mode.

### Phase 7: macOS Native Plugin (Prompts 20-22)
libusb-based USB plugin for macOS.

### Phase 8: Android Native Plugin (Prompts 23-25)
USB Host API plugin for Android tablets.

### Phase 9: iPad Native Plugin (Prompts 26-28)
DriverKit extension for iPad (M1+).

### Phase 10: Quality & Polish (Prompts 29-31)
Quality tier system, integration testing, final polish.

---

## Detailed Prompts

---

### Prompt 1: ShotProcessor Service

```text
Create the ShotProcessor service that handles incoming shots from GC2.

Context: The GameManager.cs already exists and calls _shotProcessor.ProcessShot(shot). We need to implement ShotProcessor that:
1. Validates incoming GC2ShotData (using GC2Protocol.IsValidShot)
2. Runs the physics simulation via TrajectorySimulator
3. Fires events for visualization and UI updates
4. Handles both OpenRange and GSPro modes

Create Assets/Scripts/Core/ShotProcessor.cs:

Requirements:
- MonoBehaviour that can be referenced by GameManager
- ProcessShot(GC2ShotData shot) method that:
  - Validates the shot
  - Creates a TrajectorySimulator with current environmental conditions
  - Calls Simulate() with shot parameters
  - Fires OnShotProcessed event with (GC2ShotData, ShotResult)
- SetMode(AppMode mode) to switch between OpenRange/GSPro
- Environmental conditions properties (tempF, elevationFt, etc.)
- Reference to GSProClient for relay mode (nullable for now)
- Events:
  - OnShotProcessed(GC2ShotData shot, ShotResult result)
  - OnShotRejected(GC2ShotData shot, string reason)

Write unit tests for:
- Shot validation filters invalid shots
- Valid shots produce ShotResult
- Events are fired correctly
- Mode switching works

The ShotProcessor is the bridge between raw GC2 data and the visualization/UI systems.
```

---

### Prompt 2: SessionManager Service

```text
Create the SessionManager service for tracking session state and shot history.

Context: GameManager.cs references _sessionManager but it doesn't exist yet. SessionManager tracks the current practice session.

Create Assets/Scripts/Core/SessionManager.cs:

Requirements:
- MonoBehaviour
- Session tracking:
  - SessionStartTime (DateTime)
  - ElapsedTime (TimeSpan, computed)
  - TotalShots (int)
  - IsActive (bool)
- Shot history:
  - List<SessionShot> where SessionShot contains (GC2ShotData, ShotResult, timestamp)
  - MaxHistorySize = 500 (configurable)
  - GetShot(int index)
  - GetLatestShot()
  - ClearHistory()
- Statistics:
  - AverageBallSpeed
  - AverageCarryDistance
  - LongestCarry
  - BestShot (by carry distance)
- Session control:
  - StartNewSession()
  - EndSession()
  - RecordShot(GC2ShotData shot, ShotResult result)
- Events:
  - OnSessionStarted
  - OnSessionEnded
  - OnShotRecorded(SessionShot)
  - OnStatisticsUpdated

Write unit tests for:
- Session lifecycle (start/end)
- Shot recording and retrieval
- Statistics calculations
- History size limit
```

---

### Prompt 3: SettingsManager Service

```text
Create the SettingsManager service for persistent settings.

Context: GameManager.cs references _settingsManager. This manages all user preferences.

Create Assets/Scripts/Core/SettingsManager.cs:

Requirements:
- MonoBehaviour with Singleton pattern (DontDestroyOnLoad)
- Settings categories:

  Graphics:
  - QualityTier (Low/Medium/High/Auto)
  - TargetFrameRate (30/60/120)

  Units:
  - DistanceUnit (Yards/Meters)
  - SpeedUnit (MPH/KPH)
  - TemperatureUnit (Fahrenheit/Celsius)

  Environment:
  - TemperatureF (float, default 70)
  - ElevationFt (float, default 0)
  - HumidityPct (float, default 50)
  - WindEnabled (bool)
  - WindSpeedMph (float)
  - WindDirectionDeg (float)

  Connection:
  - AutoConnect (bool, default true)
  - GSProHost (string)
  - GSProPort (int, default 921)

  Audio:
  - MasterVolume (float 0-1)
  - EffectsVolume (float 0-1)

- Persistence using PlayerPrefs
- LoadSettings() on Awake
- SaveSettings() on changes and OnApplicationPause
- ResetToDefaults()
- Events:
  - OnSettingsChanged

Create settings data class (can be nested or separate):
- AppSettings with all properties

Write unit tests for:
- Settings persistence (save/load)
- Default values
- Reset functionality
- Event firing on changes
```

---

### Prompt 4: Unity Scene Structure

```text
Create the Unity scene structure and project configuration.

Context: No scenes exist yet. We need Bootstrap, MainMenu, and Marina scenes.

Create the following Unity scenes:

1. Assets/Scenes/Bootstrap.unity:
   - Empty scene that loads on app start
   - Contains:
     - GameManager (with all core references)
     - MainThreadDispatcher
     - SettingsManager
     - EventSystem (for UI)
   - Script: BootstrapLoader.cs that initializes systems and loads MainMenu

2. Assets/Scenes/MainMenu.unity:
   - Simple menu scene (placeholder for now)
   - Contains:
     - Canvas with basic UI
     - "Open Range" button → loads Marina
     - Connection status indicator
     - Settings button (placeholder)

3. Assets/Scenes/Ranges/Marina.unity:
   - Main driving range scene (placeholder structure)
   - Contains:
     - Directional Light (sun)
     - Ground plane (temporary)
     - ShotProcessor
     - SessionManager
     - UI Canvas (placeholder)
     - Main Camera

Create Assets/Scripts/Core/BootstrapLoader.cs:
- Initializes all managers in order
- Waits for settings to load
- Auto-detects quality tier
- Loads MainMenu scene

Create Assets/Scripts/Core/SceneLoader.cs:
- Static helper for scene transitions
- LoadScene(string sceneName)
- LoadSceneAsync with loading screen

Configure:
- Build Settings with scene order (Bootstrap first)
- Script execution order (MainThreadDispatcher -100, Managers -50)

Write play mode tests for:
- Bootstrap loads correctly
- Scene transitions work
- Managers persist across scenes
```

---

### Prompt 5: PlatformManager and QualityManager

```text
Create platform detection and quality tier management.

Context: Need to detect platform capabilities and auto-configure quality.

Create Assets/Scripts/Core/PlatformManager.cs:
- Static utility class
- Properties:
  - CurrentPlatform (enum: Mac, iPad, Android, Windows, Editor)
  - IsDesktop, IsMobile, IsTablet
  - ScreenCategory (Compact/Regular/Large based on diagonal inches)
  - HasUSBHostSupport
  - DeviceModel (string from SystemInfo)
  - SupportsDriverKit (iPad M1+ check)
- Methods:
  - GetDiagonalInches()
  - IsAppleSilicon()

Create Assets/Scripts/Core/QualityManager.cs:
- MonoBehaviour with singleton
- Quality tier system:
  - DetectOptimalTier() → QualityTier
  - ApplyQualityTier(QualityTier tier)
  - CurrentTier property
- Detection logic per platform:
  - Mac M1+ → High
  - iPad Pro M1+ → High, iPad Air → Medium
  - Android: based on GPU/RAM
- Settings per tier (reference TRD):
  - FPS target
  - Shadow quality
  - Reflection mode
  - Texture resolution
  - Anti-aliasing
  - Post-processing
- URP asset switching:
  - References to URP-Low, URP-Medium, URP-High assets
  - Switches QualitySettings.renderPipeline
- Dynamic adjustment:
  - Monitor frame rate
  - Auto-downgrade if consistently below target
  - Notify user of changes

Create placeholder URP assets:
- Assets/Settings/URP-LowQuality.asset
- Assets/Settings/URP-MediumQuality.asset
- Assets/Settings/URP-HighQuality.asset

Write tests for:
- Platform detection is correct
- Quality tier detection is reasonable
- Tier switching applies correctly
```

---

### Prompt 6: Golf Ball Prefab and Materials

```text
Create the golf ball prefab with proper materials and visual quality.

Context: Need a high-quality golf ball that will be animated during flight.

Create Assets/Prefabs/Ball/GolfBall.prefab:
- Sphere mesh (scale: 0.04267m = regulation diameter)
- MeshRenderer with GolfBall material
- Layer: "Ball" (create this layer)
- Tag: "Ball"
- No colliders (we use our own physics)
- Child GameObjects:
  - "SpinIndicator" - optional visual for spin direction
  - TrailRenderer attachment point

Create Assets/Materials/Ball/GolfBall.mat:
- URP/Lit shader
- Albedo: White with slight texture
- Smoothness: 0.8 (slightly shiny)
- Normal map: Golf ball dimple pattern (placeholder or generate)

Create Assets/Prefabs/Ball/BallTrail.prefab:
- TrailRenderer component
- Width: 0.02m start, 0.005m end
- Time: 1.5 seconds
- Color: White → Transparent gradient
- Quality tier variants:
  - High: Full trail, 30 vertices
  - Low: Simplified, 10 vertices

Create Assets/Scripts/Visualization/BallVisuals.cs:
- Component on GolfBall prefab
- Manages visual state:
  - SetTrailEnabled(bool)
  - SetSpinVisualization(Vector3 spinAxis, float rpm)
  - ResetVisuals()
- Quality tier adjustments

Write tests for:
- Prefab instantiates correctly
- Materials are applied
- Trail renderer works
- Visuals reset properly
```

---

### Prompt 7: BallController Animation System

```text
Create the BallController that animates ball flight from trajectory data.

Context: ShotProcessor produces ShotResult with trajectory points. BallController animates the ball through these points.

Create Assets/Scripts/Visualization/BallController.cs:

Requirements:
- MonoBehaviour attached to GolfBall prefab instance
- Reference to ball GameObject
- Animation methods:
  - PlayShot(ShotResult result) - starts animation
  - SkipToEnd() - jumps to final position
  - Reset() - returns ball to tee position
  - Pause() / Resume()
- Animation state:
  - IsAnimating (bool)
  - CurrentPhase (Phase enum)
  - Progress (0-1 float)
- Playback speed:
  - PlaybackSpeed property (1.0 = realtime, 2.0 = 2x, etc.)
  - TimeScale (realtime, fast, instant)
- Animation implementation:
  - Coroutine-based interpolation through TrajectoryPoints
  - Smooth lerp between points
  - Match time stamps from trajectory
  - Rotate ball based on spin
- Events:
  - OnFlightStarted
  - OnApexReached (when MaxHeight reached)
  - OnLanded (first ground contact)
  - OnRollStarted
  - OnStopped (final position)

Integration:
- ShotProcessor calls PlayShot when processing completes
- BallController animates the result

Create Assets/Scripts/Visualization/BallSpinner.cs:
- Handles ball rotation during flight
- CalculateRotation(float backSpin, float sideSpin, float time)
- Visual spin that slows down over time

Write tests for:
- Animation plays through all trajectory points
- Timing matches flight time
- Events fire at correct moments
- SkipToEnd works correctly
- Reset returns to start position
```

---

### Prompt 8: TrajectoryRenderer

```text
Create the trajectory line renderer for showing ball path.

Context: Need to visualize the ball's flight path as a line.

Create Assets/Scripts/Visualization/TrajectoryRenderer.cs:

Requirements:
- MonoBehaviour with LineRenderer component
- Modes:
  - Predicted: Shows expected path before/during shot
  - Actual: Shows path after shot completes
  - Both: Shows both (actual overlays predicted)
- Configuration:
  - LineWidth (start/end)
  - ColorGradient
  - VertexCount (quality tier adjustable)
- Methods:
  - ShowTrajectory(ShotResult result)
  - ShowPrediction(ShotResult result) - dotted/different style
  - Hide()
  - FadeOut(float duration)
- Implementation:
  - Convert TrajectoryPoints to world positions
  - Sample points for smooth curve
  - Apply gradient based on height or speed
- Quality tiers:
  - High: 100 vertices, smooth gradient
  - Medium: 50 vertices
  - Low: 20 vertices or disabled

Create Assets/Materials/TrajectoryLine.mat:
- Additive or transparent shader
- White/cyan color

Create Assets/Prefabs/Effects/TrajectoryLine.prefab:
- LineRenderer component
- TrajectoryRenderer script
- Configured materials

Wire up:
- BallController triggers TrajectoryRenderer when shot starts
- Fades out after ball stops

Write tests for:
- Line renders correctly from trajectory data
- Fade out animation works
- Quality tier changes vertex count
```

---

### Prompt 9: Camera System

```text
Create the camera system for following ball flight and user control.

Context: Need multiple camera modes - follow ball, orbit, top-down.

Create Assets/Scripts/Visualization/CameraController.cs:

Requirements:
- Main camera controller
- Camera modes (enum):
  - Static: Fixed position, rotates to track ball
  - Follow: Tracks behind ball during flight
  - TopDown: Overhead view for dispersion
  - FreeOrbit: User-controlled orbit around range
- Mode switching:
  - SetMode(CameraMode mode)
  - Smooth transition between modes
  - Auto-mode selection based on context
- Default behavior:
  - Start in Static mode
  - Switch to Follow when ball launches
  - Return to Static after ball stops
- Position presets:
  - TeePosition (behind tee, elevated)
  - OverheadPosition (for dispersion view)

Create Assets/Scripts/Visualization/FollowCamera.cs:
- ICameraMode implementation
- Tracks ball with offset
- Smooth follow with damping
- Look-ahead based on velocity
- Rises with ball to apex
- Configurable offset and damping

Create Assets/Scripts/Visualization/OrbitCamera.cs:
- ICameraMode implementation
- Orbits around range center (or ball)
- Touch controls: pinch zoom, two-finger rotate
- Mouse controls: scroll zoom, right-drag rotate
- Min/max distance limits
- Ground collision avoidance

Create interface Assets/Scripts/Visualization/ICameraMode.cs:
- Enter()
- Exit()
- UpdateCamera(float deltaTime)
- ProcessInput()

Create Assets/Prefabs/Camera/CameraRig.prefab:
- Main Camera with CameraController
- Post-processing volume (URP)
- Audio listener

Integration:
- CameraController subscribes to BallController events
- Switches modes automatically or via UI

Write tests for:
- Camera follows ball correctly
- Mode switching is smooth
- Orbit controls work
- No camera clipping through ground
```

---

### Prompt 10: Landing Marker and Effects

```text
Create landing effects and markers for where ball lands/stops.

Context: Need visual feedback for landing position and impact effects.

Create Assets/Scripts/Visualization/LandingMarker.cs:
- Spawns at landing position
- Shows:
  - Ring or target graphic
  - Distance text (carry)
  - Optional: total distance after roll
- Configurable:
  - Auto-hide after duration
  - Show/hide on demand
  - Multiple markers for history view
- Pool management for performance

Create Assets/Prefabs/Effects/LandingMarker.prefab:
- Circle/ring projector or 3D object
- TextMeshPro for distance
- Fade in/out animation

Create Assets/Scripts/Visualization/ImpactEffect.cs:
- Particle effect for landing
- Different effects by surface:
  - Fairway: Small dust puff
  - Water: Splash (future)
- Scales with landing velocity

Create Assets/Prefabs/Effects/LandingDust.prefab:
- Particle System
- Tan/brown dust particles
- Short burst on impact
- Quality tier scaling:
  - High: 30 particles
  - Low: 10 particles

Create Assets/Scripts/Visualization/EffectsManager.cs:
- MonoBehaviour singleton
- Object pooling for effects
- Methods:
  - SpawnLandingEffect(Vector3 position, float velocity)
  - SpawnBounceEffect(Vector3 position)
  - SpawnMarker(Vector3 position, float distance)
- Quality tier awareness

Integration:
- BallController.OnLanded triggers effects
- EffectsManager handles spawning and cleanup

Write tests for:
- Effects spawn at correct positions
- Pool reuses objects
- Quality tier affects particle count
```

---

### Prompt 11: Marina Environment Setup

```text
Create the Marina driving range environment.

Context: Need a beautiful coastal driving range matching GSPro aesthetic. This is the primary range scene.

Expand Assets/Scenes/Ranges/Marina.unity:

Environment Structure:
- Terrain or Ground plane with grass texture
- Water plane with ocean shader
- Skybox (procedural or HDRI)
- Distant mountains (low-poly meshes or skybox)
- Decorative elements (boats, buildings - can use primitives initially)

Create Assets/Scripts/Visualization/EnvironmentManager.cs:
- MonoBehaviour in Marina scene
- Manages environment state:
  - Time of day (optional for v1)
  - Weather conditions (future)
- Quality tier adjustments:
  - Toggle reflections
  - Adjust draw distance
  - Enable/disable particle effects
- Distance markers management

Create distance markers and targets:

Assets/Prefabs/Environment/DistanceMarker.prefab:
- Sign/post with distance text
- Placed at 50, 100, 150, 200, 250, 300 yards

Assets/Prefabs/Environment/TargetGreen.prefab:
- Circular green area
- Flag (animated optional)
- Placed at key distances

Assets/Prefabs/Environment/TeeMat.prefab:
- Hitting mat area
- Ball spawn position marker

Scene setup:
- Position markers correctly (convert yards to Unity units)
- Set up proper lighting:
  - Directional light as sun
  - Ambient lighting
  - Reflection probes (High quality)
- Configure URP settings per quality tier

Materials needed:
- Fairway grass
- Green (putting surface)
- Water
- Sky

Integration:
- Marina scene loads from MainMenu
- Contains ShotProcessor, SessionManager, UI

This prompt focuses on the scene structure. Visual polish will be iterative.

Write play mode tests for:
- Scene loads without errors
- Distance markers at correct positions
- Lighting is configured
- Quality tier switching affects environment
```

---

### Prompt 12: UIManager and Layout System

```text
Create the UI management system and responsive layout.

Context: Need a UI system that matches GSPro aesthetic and adapts to different screen sizes.

Create Assets/Scripts/UI/UIManager.cs:
- MonoBehaviour singleton
- Main UI controller
- References to all UI panels
- Theme/style management
- Methods:
  - ShowPanel(string panelName)
  - HidePanel(string panelName)
  - TogglePanel(string panelName)
  - ShowToast(string message, float duration)
- Integration:
  - Subscribes to ShotProcessor events for data updates
  - Subscribes to GameManager for connection status

Create Assets/Scripts/UI/ResponsiveLayout.cs:
- Detects screen size and orientation
- Breakpoints:
  - Compact: < 800px width (small tablets)
  - Regular: 800-1200px (iPads, tablets)
  - Large: > 1200px (Mac, large tablets)
- OnLayoutChanged event
- Methods:
  - GetScreenCategory()
  - GetSafeArea()

Create Assets/Scripts/UI/SafeAreaHandler.cs:
- Handles device safe areas (notches, home indicator)
- Adjusts RectTransform padding
- Apply to UI root

Create UI color/style constants:
Assets/Scripts/UI/UITheme.cs:
- Panel background: #1a1a2e (semi-transparent)
- Accent green: #2d5a27
- Text white: #ffffff
- Total distance red: #ff6b6b
- Font sizes per breakpoint

Create base prefabs:

Assets/Prefabs/UI/UICanvas.prefab:
- Canvas (Screen Space - Overlay)
- CanvasScaler (Scale With Screen Size, 1920 reference)
- GraphicRaycaster
- SafeAreaHandler
- ResponsiveLayout

Assets/Prefabs/UI/Toast.prefab:
- Slide-in notification
- Auto-dismiss timer
- Queue support

Set up Marina scene UI:
- Add UICanvas to Marina.unity
- Structure:
  - TopPanel (session info)
  - BottomPanel (shot data bar)
  - RightPanel (HMT data)
  - ConnectionStatus
  - Overlays (settings, history)

Write tests for:
- Breakpoint detection correct
- Safe area applied
- Toast shows and hides
```

---

### Prompt 13: Shot Data Bar (Bottom Panel)

```text
Create the GSPro-style shot data bar that displays at the bottom.

Context: Primary data display matching GSPro layout.

Reference layout from PRD:
```
┌────────┬──────────┬───────┬──────────┬──────────┬───────┬─────────┬───────┬─────┬───────┐
│ BALL   │DIRECTION │ ANGLE │ BACK     │ SIDE     │ APEX  │ OFFLINE │ CARRY │ RUN │ TOTAL │
│ SPEED  │          │       │ SPIN     │ SPIN     │       │         │       │     │       │
├────────┼──────────┼───────┼──────────┼──────────┼───────┼─────────┼───────┼─────┼───────┤
│ 104.5  │   L4.0   │ 24.0  │  4,121   │   R311   │ 30.7  │   L7.2  │ 150.0 │ 4.6 │ 154.6 │
│  mph   │   deg    │  deg  │   rpm    │   rpm    │  yd   │    yd   │   yd  │ yd  │   yd  │
└────────┴──────────┴───────┴──────────┴──────────┴───────┴─────────┴───────┴─────┴───────┘
```

Create Assets/Scripts/UI/ShotDataBar.cs:
- MonoBehaviour on bottom panel
- References to DataTile components
- UpdateDisplay(GC2ShotData shot, ShotResult result)
- AnimateNewShot() - highlight animation on update
- Clear()
- Data tiles:
  - BallSpeed (mph)
  - Direction (L/R deg)
  - Angle (deg)
  - BackSpin (rpm)
  - SideSpin (L/R rpm)
  - Apex (yd)
  - Offline (L/R yd)
  - Carry (yd)
  - Run (yd)
  - Total (yd) - highlighted in red

Create Assets/Scripts/UI/DataTile.cs:
- Reusable data display component
- Properties:
  - Label (string)
  - Value (string)
  - Unit (string)
- SetValue(float value, string format)
- SetValueWithDirection(float value, bool showLR)
- Animate on change
- Color coding support

Create Assets/Prefabs/UI/DataTile.prefab:
- Background panel (semi-transparent dark)
- Label (top, small)
- Value (center, large)
- Unit (bottom, small)
- All using TextMeshPro

Create Assets/Prefabs/UI/ShotDataBar.prefab:
- Horizontal layout group
- 10 DataTile instances
- Anchored to bottom
- Padding for safe area

Formatting:
- Numbers: 1 decimal for distances, 0 for spin
- Direction: "L4.0" or "R4.0" for left/right
- Thousands separator for spin

Integration:
- UIManager subscribes to ShotProcessor.OnShotProcessed
- Calls ShotDataBar.UpdateDisplay

Write tests for:
- All values display correctly
- Direction formatting works (L/R)
- Animation triggers on new data
```

---

### Prompt 14: Club Data Panel (HMT)

```text
Create the HMT club data panel for swing metrics (right side).

Context: When GC2 has HMT add-on, display club metrics.

Create Assets/Scripts/UI/ClubDataPanel.cs:
- MonoBehaviour on right panel
- Only visible when HasClubData is true
- Data fields:
  - Club Speed (mph)
  - Path (in-to-out / out-to-in)
  - Attack Angle (up/down)
  - Face to Target (open/closed)
  - Dynamic Loft (deg)
- UpdateDisplay(GC2ShotData shot)
- Show() / Hide() with animation

Create Assets/Scripts/UI/SwingPathIndicator.cs:
- Visual diagram showing swing path
- Top-down club path arrow
- Face angle indicator
- Color coded:
  - In-to-out (draw): Blue
  - Out-to-in (fade): Orange
- Updates from path/face values

Create Assets/Scripts/UI/AttackAngleIndicator.cs:
- Side-view diagram
- Shows descending vs ascending
- Arrow indicating angle
- Updates from attack angle value

Create Assets/Prefabs/UI/ClubDataPanel.prefab:
- Vertical layout
- Header: "CLUB DATA"
- Data rows for each metric
- Path indicator graphic
- Attack angle indicator graphic
- Anchored to right side
- Collapsible on small screens

Create Assets/Prefabs/UI/SwingPathIndicator.prefab:
- Simple 2D graphic
- Arrow for path direction
- Line for face angle

Integration:
- Check shot.HasClubData
- Show panel only when HMT data present
- Hide after timeout or next shot without HMT

Write tests for:
- Panel shows only with HMT data
- Values display correctly
- Indicators match data direction
- Hide/show animation works
```

---

### Prompt 15: Connection Status UI

```text
Create connection status indicators and panels.

Context: User needs to see GC2 connection state clearly.

Create Assets/Scripts/UI/ConnectionStatusUI.cs:
- Small indicator in corner (top-right)
- States:
  - Connected: Green dot, "GC2 Connected"
  - Connecting: Yellow dot, "Connecting..."
  - Disconnected: Red dot, "Disconnected"
  - DeviceNotFound: Gray dot, "No GC2 Detected"
- Tap to expand details panel
- Subscribes to GameManager.OnConnectionStateChanged

Create Assets/Scripts/UI/ConnectionPanel.cs:
- Expanded detail panel (modal)
- Shows:
  - Connection status
  - Device info (serial, firmware if available)
  - Connection mode (USB/TCP)
  - Last shot time
  - Platform-specific info
- Buttons:
  - Connect/Disconnect
  - Retry
  - Settings link
- Close button / tap outside to dismiss

Create Assets/Scripts/UI/ConnectionToast.cs:
- Toast notifications for connection events
- "GC2 Connected" (green)
- "GC2 Disconnected" (red)
- "Connection lost, reconnecting..." (yellow)
- Auto-dismiss after 3 seconds
- Queue multiple if needed

Create Assets/Prefabs/UI/ConnectionStatus.prefab:
- Small status indicator
- LED dot + text
- Anchored top-right

Create Assets/Prefabs/UI/ConnectionPanel.prefab:
- Modal panel
- Device info display
- Action buttons

Integration:
- GameManager.OnConnectionStateChanged → updates indicator
- GameManager.OnConnectionStateChanged → shows toast
- Tap indicator → shows panel

Write tests for:
- Status reflects connection state
- Toasts appear on state changes
- Panel displays correct info
```

---

### Prompt 16: Session Info Panel

```text
Create the session info panel and shot history.

Context: Display session statistics and allow reviewing past shots.

Create Assets/Scripts/UI/SessionInfoPanel.cs:
- Top-left corner display
- Shows:
  - Session time (elapsed, live updating)
  - Total shots
  - Average ball speed
  - Longest carry
- Compact by default
- Tap to expand history

Create Assets/Scripts/UI/ShotHistoryPanel.cs:
- Expandable modal/side panel
- Scrollable list of all shots
- Each row shows:
  - Shot number
  - Ball speed
  - Carry distance
  - Time ago
- Tap shot to:
  - Show full details
  - Replay trajectory (calls BallController)
- Statistics summary at top
- Clear history button

Create Assets/Scripts/UI/ShotHistoryItem.cs:
- Individual shot row component
- SetData(SessionShot shot)
- Highlight when selected
- OnClick → ShotDetailModal or replay

Create Assets/Scripts/UI/ShotDetailModal.cs:
- Full shot detail view
- All shot data
- Trajectory mini-view (optional)
- Compare to session average
- Close button

Create Assets/Prefabs/UI/SessionInfoPanel.prefab:
- Compact display
- Anchored top-left
- Live updating time

Create Assets/Prefabs/UI/ShotHistoryPanel.prefab:
- Scrollable list
- Virtual scrolling for performance (if many shots)

Create Assets/Prefabs/UI/ShotHistoryItem.prefab:
- Row layout
- Key metrics

Integration:
- SessionManager.OnShotRecorded → adds to history
- SessionManager.OnStatisticsUpdated → updates stats
- Replay button → BallController.PlayShot

Write tests for:
- Session time updates live
- History shows all shots
- Statistics are correct
- Replay works from history
```

---

### Prompt 17: Settings Panel

```text
Create the settings panel for user preferences.

Context: Allow users to configure app settings.

Create Assets/Scripts/UI/SettingsPanel.cs:
- Full-screen modal panel
- Tabbed or scrollable sections
- Save/Cancel/Reset buttons
- Reads from SettingsManager
- Writes to SettingsManager on save

Settings sections:

Graphics:
- Quality (Low/Medium/High/Auto dropdown)
- Target FPS (30/60/120 dropdown)

Units:
- Distance (Yards/Meters toggle)
- Speed (MPH/KPH toggle)
- Temperature (F/C toggle)

Environment:
- Temperature slider (40-100°F)
- Elevation slider (0-8000 ft)
- Humidity slider (0-100%)
- Wind toggle
- Wind speed slider (0-30 mph)
- Wind direction slider (0-360°)

Connection:
- Auto-connect toggle
- GSPro host input field
- GSPro port input field

Audio:
- Master volume slider
- Effects volume slider

Create UI components:

Assets/Scripts/UI/SettingToggle.cs:
- Two-option toggle component

Assets/Scripts/UI/SettingSlider.cs:
- Slider with min/max labels
- Optional value display

Assets/Scripts/UI/SettingDropdown.cs:
- Dropdown selection

Create Assets/Prefabs/UI/SettingsPanel.prefab:
- Full-screen overlay
- Scrollable content
- All settings organized by section

Integration:
- Accessible from MainMenu and Marina
- Settings apply immediately (or on save)
- SettingsManager persists changes

Write tests for:
- All settings reflect current values
- Changes are persisted
- Reset restores defaults
- Unit changes affect display throughout app
```

---

### Prompt 18: TCP Connection for Testing

```text
Create the TCP connection implementation for editor testing and GSPro relay.

Context: Need TCP/network connection for testing without hardware and for GSPro mode.

Create Assets/Scripts/GC2/Platforms/TCP/GC2TCPConnection.cs:
- Implements IGC2Connection
- TCP client connection
- Can act as:
  - Server (receive shots from external source)
  - Client (connect to existing GC2 desktop app)
- Configuration:
  - Host (string)
  - Port (int)
  - Mode (Server/Client)
- Same protocol parsing as USB (newline-delimited key=value)
- Reconnection support
- Events map to interface

Implementation:
- TcpClient for client mode
- TcpListener for server mode
- Async read loop
- MainThreadDispatcher for callbacks

Create Assets/Scripts/GC2/Platforms/TCP/GC2TCPListener.cs:
- Simple TCP server for testing
- Listens on port 8888
- Accepts connections
- Forwards data to connection
- Useful for editor testing

Create Assets/Editor/GC2TestWindow.cs:
- EditorWindow for testing
- Connection status display
- "Fire Test Shot" button
- Manual shot parameter entry
- Useful for development

Usage:
- In Unity Editor, GC2ConnectionFactory returns GC2TCPConnection
- Can connect to GC2 Connect Desktop on same machine
- Can receive test shots from network

Write tests for:
- TCP connection lifecycle
- Shot parsing from network data
- Reconnection after disconnect
```

---

### Prompt 19: GSPro Client

```text
Create the GSPro Open Connect client for relay mode.

Context: In GSPro mode, send shots to GSPro in addition to local visualization.

Create Assets/Scripts/Network/GSProClient.cs:

Based on GSPRO_API.md specification:
- TCP client to GSPro (port 921 default)
- JSON message format per API spec
- Heartbeat support (every 2 seconds)
- Shot sending
- Connection lifecycle

Implementation:
- ConnectAsync(string host, int port)
- Disconnect()
- SendShot(GC2ShotData shot)
- IsConnected property
- Auto-reconnect on disconnect
- Events: OnConnected, OnDisconnected, OnError

Message classes (from GSPRO_API.md):
- GSProMessage
- GSProBallData
- GSProClubData
- GSProShotOptions

Create Assets/Scripts/Network/GSProMessage.cs:
- All message class definitions
- JSON serialization

Heartbeat:
- Runs on background task
- Every 2 seconds when idle
- Skips around shots

Integration:
- ShotProcessor checks mode
- If GSPro mode, calls GSProClient.SendShot
- Shows connection status in UI

Create Assets/Scripts/UI/GSProModeUI.cs:
- Mode toggle (OpenRange / GSPro)
- GSPro connection status
- Host/port configuration

Write tests for:
- JSON message format matches spec
- Heartbeat timing
- Shot sending works
- Reconnection logic
```

---

### Prompt 20: macOS Plugin Header and Project

```text
Create the macOS native plugin project structure.

Context: macOS uses libusb for USB communication.

Create NativePlugins/macOS/GC2MacPlugin/:

1. GC2MacPlugin.h (from USB_PLUGINS.md):
   - C interface header
   - Function declarations:
     - GC2Mac_Initialize(callbackObject)
     - GC2Mac_Shutdown()
     - GC2Mac_IsDeviceAvailable()
     - GC2Mac_Connect()
     - GC2Mac_Disconnect()
     - GC2Mac_IsConnected()
     - GC2Mac_SetShotCallback(callback)
     - GC2Mac_SetConnectionCallback(callback)
     - GC2Mac_SetErrorCallback(callback)
   - Callback typedefs

2. Create Xcode project:
   - GC2MacPlugin.xcodeproj
   - Target: Bundle (.bundle)
   - Deployment: macOS 11.0
   - Architecture: Universal (x86_64 + arm64)

3. Dependencies:
   - libusb (embedded or brew)
   - IOKit.framework
   - Foundation.framework

4. Build script:
   - build_mac_plugin.sh
   - Builds universal binary
   - Copies to Assets/Plugins/macOS/

5. Placeholder implementation:
   - Stubs that compile
   - Return false for device available
   - Log messages for debugging

Create NativePlugins/macOS/README.md:
- Build instructions
- libusb installation
- Testing notes

At this point, the plugin structure exists but doesn't communicate with USB yet.

Write verification tests:
- Project builds successfully
- Bundle loads in Unity
- Functions can be called (even if returning false)
```

---

### Prompt 21: macOS Plugin Implementation

```text
Implement the macOS native plugin USB communication.

Context: Complete the libusb-based GC2 communication.

Complete NativePlugins/macOS/GC2MacPlugin/GC2MacPlugin.mm:

Based on USB_PLUGINS.md implementation:

1. libusb initialization:
   - libusb_init() context
   - Error handling

2. Device detection:
   - Enumerate USB devices
   - Match VID: 0x2C79, PID: 0x0110
   - libusb_get_device_list()

3. Connection:
   - libusb_open_device_with_vid_pid()
   - libusb_detach_kernel_driver() if needed
   - libusb_claim_interface(0)
   - Find bulk IN endpoint (0x81)

4. Read loop (dispatch queue):
   - libusb_bulk_transfer() with 100ms timeout
   - Buffer management
   - Parse complete messages
   - Call Unity callback with JSON

5. Protocol parsing:
   - Parse key=value format
   - Build NSDictionary
   - Convert to JSON
   - Handle partial data buffering

6. Disconnection:
   - Stop read thread
   - libusb_release_interface()
   - libusb_close()

7. Unity communication:
   - UnitySendMessage for callbacks
   - Main thread dispatch for safety

Build and test:
- Build universal binary
- Test device detection
- Test shot data reception

Provide libusb binary:
- Include or document how to get libusb.dylib
- Place in Assets/Plugins/macOS/

Write tests (hardware required):
- Device is detected
- Connection establishes
- Shot data is parsed correctly
```

---

### Prompt 22: macOS C# Bridge

```text
Create the C# wrapper for the macOS native plugin.

Context: Bridge between native Objective-C plugin and Unity C#.

Create Assets/Scripts/GC2/Platforms/MacOS/GC2MacConnection.cs:

Based on USB_PLUGINS.md C# bridge:

1. DllImport declarations:
   - [DllImport("GC2MacPlugin")]
   - All functions from header

2. MonoBehaviour requirement:
   - Need GameObject for UnitySendMessage
   - Add component via factory

3. Callback handling:
   - Native code calls UnitySendMessage
   - Method names match native calls:
     - OnNativeShotReceived(string json)
     - OnNativeConnectionChanged(string connected)
     - OnNativeError(string error)

4. Implement IGC2Connection:
   - IsConnected → GC2Mac_IsConnected()
   - IsDeviceAvailable() → GC2Mac_IsDeviceAvailable()
   - ConnectAsync() → GC2Mac_Connect() wrapped in Task
   - Disconnect() → GC2Mac_Disconnect()
   - Events wired to callbacks

5. JSON parsing:
   - Parse JSON from native to GC2ShotData
   - Use JsonUtility or Newtonsoft.Json

6. Lifecycle:
   - Initialize on Awake
   - Shutdown on OnDestroy
   - Handle application quit

7. Conditional compilation:
   - #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

Update GC2ConnectionFactory:
- Return GC2MacConnection for UNITY_STANDALONE_OSX

Create editor test utility:
- Context menu items for testing
- Manual connect/disconnect

Write tests for:
- Plugin loads correctly
- Callbacks are received
- JSON parsing works
- Connection state is accurate
```

---

### Prompt 23: Android Plugin Project

```text
Create the Android native plugin project structure.

Context: Android uses USB Host API via Kotlin.

Create NativePlugins/Android/GC2AndroidPlugin/:

1. Project structure:
   - build.gradle (library)
   - settings.gradle
   - src/main/
     - kotlin/com/openrange/gc2/
     - AndroidManifest.xml
     - res/xml/usb_device_filter.xml

2. build.gradle:
   - Android library plugin
   - Kotlin support
   - Min SDK 26, Target SDK 33
   - Output: AAR

3. AndroidManifest.xml:
   - <uses-feature android:name="android.hardware.usb.host" />

4. usb_device_filter.xml:
   - vendor-id: 11385 (0x2C79)
   - product-id: 272 (0x0110)

5. Kotlin files (stubs):
   - GC2Plugin.kt - Main entry point
   - GC2Device.kt - USB device wrapper
   - GC2Protocol.kt - Protocol parsing

6. Unity integration:
   - Uses UnityPlayer.UnitySendMessage

7. Build script:
   - build_android_plugin.sh
   - Runs ./gradlew assembleRelease
   - Copies AAR to Assets/Plugins/Android/

Create NativePlugins/Android/README.md:
- Build instructions
- Required Android SDK
- Testing on device

Write verification tests:
- Project builds AAR
- AAR can be imported to Unity
```

---

### Prompt 24: Android Plugin Implementation

```text
Implement the Android USB Host API communication.

Context: Complete Kotlin USB implementation.

Complete NativePlugins/Android/GC2AndroidPlugin/:

1. GC2Plugin.kt:
   - Companion object with getInstance()
   - initialize(context, callbackObject)
   - isDeviceAvailable()
   - connect(context)
   - disconnect()
   - isConnected()

2. USB Permission handling:
   - BroadcastReceiver for USB_PERMISSION
   - PendingIntent for permission request
   - Handle granted/denied

3. Device enumeration:
   - UsbManager.deviceList
   - Find by VID/PID

4. Connection:
   - UsbManager.openDevice()
   - UsbDeviceConnection.claimInterface()
   - Find bulk IN endpoint

5. Read thread:
   - Thread with bulk transfer loop
   - 100ms timeout
   - Buffer management
   - Parse complete messages

6. Protocol parsing (GC2Protocol.kt):
   - Same key=value format
   - Convert to JSONObject
   - Serialize to string

7. Unity callbacks:
   - UnityPlayer.UnitySendMessage()
   - Main thread if needed

8. Device attach/detach:
   - BroadcastReceiver for USB_DEVICE_ATTACHED
   - BroadcastReceiver for USB_DEVICE_DETACHED
   - Auto-reconnect on reattach

9. GC2Device.kt:
   - Wrapper for USB device/connection
   - Clean interface for plugin

Build and test on device:
- Samsung Galaxy Tab or similar
- USB OTG connection
- Permission flow

Write tests (device required):
- Permission dialog appears
- Device detected
- Data received
```

---

### Prompt 25: Android C# Bridge

```text
Create the C# wrapper for the Android native plugin.

Context: Bridge between Kotlin plugin and Unity C#.

Create Assets/Scripts/GC2/Platforms/Android/GC2AndroidConnection.cs:

1. MonoBehaviour (required for UnitySendMessage):
   - Attached to GameObject in scene
   - DontDestroyOnLoad

2. AndroidJavaObject calls:
   - Get plugin via AndroidJavaClass
   - Call methods via AndroidJavaObject.Call

3. Implement IGC2Connection:
   - Wrap all plugin methods
   - Pass Activity context for permissions

4. Message handlers:
   - OnNativeShotReceived(string json)
   - OnNativeConnectionChanged(string connected)
   - OnNativeError(string error)
   - Called by native via UnitySendMessage

5. JSON parsing:
   - JsonUtility.FromJson<GC2ShotData>

6. Lifecycle:
   - OnApplicationPause - handle disconnect
   - OnApplicationFocus
   - OnDestroy - cleanup

7. Conditional compilation:
   - #if UNITY_ANDROID && !UNITY_EDITOR

Update GC2ConnectionFactory:
- Add component to host GameObject
- Return GC2AndroidConnection for UNITY_ANDROID

Create Assets/Prefabs/GC2/GC2AndroidManager.prefab:
- With GC2AndroidConnection component
- Instantiated by factory if needed

Write tests for:
- Plugin initialization
- Permission flow
- Shot data parsing
- Connection lifecycle
```

---

### Prompt 26: iPad Plugin Structure (DriverKit)

```text
Create the iPad native plugin project structure.

Context: iPad requires DriverKit for USB access. This is more complex and requires Apple entitlements.

Important: DriverKit requires entitlements from Apple which may take weeks. This prompt sets up the structure.

Create NativePlugins/iOS/:

1. GC2iOSPlugin/ (App-side framework):
   - GC2iOSPlugin.swift
   - GC2iOSPlugin.h (bridging header for Unity)
   - Info.plist

2. GC2Driver/ (DriverKit extension - separate target):
   - GC2Driver.swift (IOUserService)
   - GC2UserClient.swift (App communication)
   - Info.plist (USB matching)
   - Entitlements.plist

3. Xcode project:
   - GC2iOS.xcodeproj
   - Targets:
     - GC2iOSPlugin.framework
     - GC2Driver.dext

4. Entitlements (from USB_PLUGINS.md):
   - com.apple.developer.driverkit
   - com.apple.developer.driverkit.transport.usb
   - USB VID/PID in array

5. Info.plist for driver:
   - IOKitPersonalities
   - USB matching by VID/PID
   - IOUserClass, IOUserServerName

Create NativePlugins/iOS/README.md:
- DriverKit overview
- Entitlement request process
- Build and deployment
- Testing requirements (M1+ iPad only)

Create placeholder implementation:
- GC2iOSPlugin.swift with stubs
- Returns "DriverKit not configured" error
- Allows Unity to build for iOS

Write verification:
- Framework builds
- Can be imported to Unity iOS build
- Stub methods can be called
```

---

### Prompt 27: iPad DriverKit Implementation

```text
Implement the DriverKit extension for iPad USB.

Context: This is the most complex native plugin. Requires approved DriverKit entitlements.

Note: Full implementation may not be testable until Apple approves entitlements.

Implement GC2Driver/ (DriverKit extension):

1. GC2Driver.swift (IOUserService):
   - IOUSBHostDevice matching
   - Start() - called when device matched
   - Stop() - cleanup
   - USB configuration
   - Interface claiming
   - Endpoint management

2. GC2UserClient.swift:
   - IOUserClient subclass
   - ExternalMethod dispatch for app communication
   - Methods:
     - Connect
     - Disconnect
     - Read (async)
   - Shared memory for data transfer
   - Notifications to app

3. Async read implementation:
   - AsyncCompletion for USB transfers
   - Buffer management
   - Notify client of new data

Implement GC2iOSPlugin/:

1. GC2iOSPlugin.swift:
   - Check driver availability
   - IOServiceGetMatchingService()
   - IOUserClientConnection
   - Send/receive from driver
   - Parse protocol
   - Unity callbacks via UnitySendMessage

2. System extension activation:
   - Check if extension approved
   - OSSystemExtensionRequest if needed
   - Handle activation state

3. Error handling:
   - "DriverKit not supported on this device"
   - "Extension needs approval in Settings"
   - "Device not connected"

Build:
- Build framework and dext
- Special signing with DriverKit profile
- Both need distribution provisioning

Document:
- User flow for enabling extension
- Settings → Privacy → Extensions

Write tests (requires hardware + entitlements):
- Driver loads for matching device
- Data flows through user client
```

---

### Prompt 28: iPad C# Bridge

```text
Create the C# wrapper for the iPad native plugin.

Context: Bridge between Swift DriverKit plugin and Unity C#.

Create Assets/Scripts/GC2/Platforms/iOS/GC2iPadConnection.cs:

1. Native function imports:
   - [DllImport("__Internal")] for iOS
   - All plugin functions

2. Callback handling:
   - [AOT.MonoPInvokeCallback]
   - Static delegates for IL2CPP

3. Implement IGC2Connection:
   - Map to Swift plugin calls
   - Handle DriverKit-specific states

4. DriverKit state handling:
   - Extension not installed
   - Extension needs approval
   - Extension active
   - Device connected/disconnected

5. User guidance:
   - Show message for extension activation
   - Deep link to Settings if possible
   - Clear error messages

6. Conditional compilation:
   - #if UNITY_IOS && !UNITY_EDITOR

Create Assets/Scripts/UI/DriverKitSetupUI.cs:
- Panel explaining DriverKit setup for iPad
- Steps for user to enable
- Troubleshooting tips

Update GC2ConnectionFactory:
- Return GC2iPadConnection for UNITY_IOS

Note: Until DriverKit entitlements are approved, this will return appropriate error states.

Write tests for:
- Plugin initialization
- State detection
- Error message display
- User guidance shown
```

---

### Prompt 29: Integration Testing

```text
Create integration tests and wire everything together.

Context: Ensure all components work together end-to-end.

Create Assets/Tests/:

1. Edit Mode Tests (Assets/Tests/EditMode/):
   - PhysicsTests.cs - Validate physics accuracy
   - ProtocolTests.cs - GC2 protocol parsing
   - SettingsTests.cs - Settings persistence
   - DataModelTests.cs - GC2ShotData validation

2. Play Mode Tests (Assets/Tests/PlayMode/):
   - SceneLoadTests.cs - Bootstrap → MainMenu → Marina
   - ShotFlowTests.cs - Full shot processing flow
   - UITests.cs - UI updates on shot
   - SessionTests.cs - Session tracking

3. Key validation tests (from PHYSICS.md):
   - Driver 167mph/10.9°/2686rpm → ~275 yards (±5%)
   - 7-iron 120mph/16.3°/7097rpm → ~172 yards (±5%)
   - Wedge 102mph/24.2°/9304rpm → ~136 yards (±5%)

Create test shot injection:
- TestShotGenerator utility
- Predefined test shots (driver, iron, wedge)
- Random shot generator

Full flow verification:
1. Bootstrap loads
2. Managers initialize
3. Navigate to Marina
4. Fire test shot
5. Physics calculates trajectory
6. Ball animates
7. UI updates
8. Session records shot

Platform verification checklist:
- [ ] macOS Intel build runs
- [ ] macOS M1 build runs
- [ ] iPad build compiles (Xcode project)
- [ ] Android build compiles

Write comprehensive tests for:
- End-to-end shot flow
- Physics accuracy
- UI responsiveness
- Scene transitions
```

---

### Prompt 30: Quality Tier Polish

```text
Finalize quality tier system and visual polish.

Context: Ensure quality tiers work correctly and look good.

Complete QualityManager implementation:

1. URP Asset Configuration:
   Assets/Settings/URP-LowQuality.asset:
   - Render Scale: 0.75
   - Shadows: None
   - MSAA: None
   - No post-processing

   Assets/Settings/URP-MediumQuality.asset:
   - Render Scale: 1.0
   - Shadows: Hard only
   - MSAA: 2x
   - Basic post-processing

   Assets/Settings/URP-HighQuality.asset:
   - Render Scale: 1.0
   - Shadows: Soft
   - MSAA: 4x
   - Full post-processing

2. Dynamic adjustment:
   - Frame rate monitoring
   - Auto-downgrade if below target for 5 seconds
   - Notify user of change
   - Manual override in settings

3. Environment adjustments:
   - EnvironmentManager responds to tier
   - Disable reflections on Low
   - Reduce particle counts
   - Lower LOD distances

4. UI adjustments:
   - Simpler animations on Low
   - Reduced effects

5. Per-platform defaults:
   - Mac M1: High
   - iPad Pro M1: High
   - iPad Air: Medium
   - High-end Android: High
   - Mid Android: Medium
   - Low-end Android: Low

Visual polish tasks:
- Verify ball is visible at all distances
- Trajectory line looks good
- Landing effects visible
- UI readable on all screens

Write tests for:
- Tier detection is appropriate
- Tier switching changes URP asset
- Frame rate improves on lower tiers
- Visual quality acceptable on all tiers
```

---

### Prompt 31: Final Polish and Documentation

```text
Final polish, cleanup, and documentation.

Context: Prepare for release.

Code cleanup:
- Remove debug logs (or make conditional)
- Remove commented code
- Ensure consistent formatting
- Add missing XML documentation
- Verify all TODO comments addressed

Performance verification:
- Profile on each platform
- Verify frame rate targets met
- Check memory usage
- Battery impact on mobile

Build verification:
- macOS: Universal binary, notarization ready
- iPad: Archive builds, TestFlight ready
- Android: APK/AAB builds, signed

Create documentation:

1. README.md (project root):
   - Project overview
   - Build instructions
   - Platform requirements
   - Getting started

2. CLAUDE.md (project root):
   - Project structure
   - Code conventions
   - Development workflow
   - Testing approach

3. docs/DEVELOPMENT.md:
   - Development setup
   - Native plugin building
   - Testing with/without hardware

Final checklist:
- [ ] All P0 features complete
- [ ] Physics validated (±5%)
- [ ] 60 FPS on iPad Pro
- [ ] 30+ FPS on mid-range Android
- [ ] USB works on all platforms (or documented limitations)
- [ ] No critical bugs
- [ ] Documentation complete

Write final tests:
- Full regression suite
- Performance benchmarks
- Platform-specific tests
```

---

## Appendix A: Testing Strategy

### Test Categories

1. **Edit Mode Tests** (no Unity runtime):
   - Physics calculations
   - Protocol parsing
   - Data validation
   - Unit conversions

2. **Play Mode Tests** (Unity runtime):
   - Scene loading
   - Component interactions
   - UI behavior
   - Animation

3. **Integration Tests** (hardware optional):
   - Full shot flow
   - Network connections
   - USB (when hardware available)

### Validation Data

| Test | Ball Speed | Launch | Spin | Expected Carry | Tolerance |
|------|------------|--------|------|----------------|-----------|
| Driver High | 167 mph | 10.9° | 2686 rpm | 275 yds | ±5% |
| Driver Mid | 160 mph | 11.0° | 3000 rpm | 259 yds | ±3% |
| 7-Iron | 120 mph | 16.3° | 7097 rpm | 172 yds | ±5% |
| Wedge | 102 mph | 24.2° | 9304 rpm | 136 yds | ±5% |

---

## Appendix B: Dependencies

### Unity Packages
```json
{
  "dependencies": {
    "com.unity.render-pipelines.universal": "14.0.8",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

### Native Dependencies
- **macOS**: libusb 1.0.26 (bundled)
- **iPad**: DriverKit (system framework)
- **Android**: USB Host API (system)

---

## Appendix C: Known Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| iPad DriverKit approval delayed | High | Start early; Mac+Android work without it |
| USB plugin complexity | Medium | TCP fallback for testing |
| Android device fragmentation | Medium | Aggressive quality tiers |
| GC2 protocol variations | Low | Robust parsing; version detection |
