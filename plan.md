# Open Range Unity - Prompt Plan

## Project Overview

**Goal**: Build a cross-platform Unity driving range simulator with native USB connection to the Foresight GC2 launch monitor, running on macOS, iPad, and Android.

**Key Architecture**:
- Unity 2022.3 LTS with Universal Render Pipeline (URP)
- 95% shared C# code
- Platform-specific native USB plugins (Obj-C/libusb for macOS, Swift/DriverKit for iPad, Kotlin/USB Host API for Android)
- Physics-accurate ball flight simulation
- GSPro-quality visuals with quality tier system

---

## Phase Breakdown

### Phase 1: Unity Project Foundation (Prompts 1-4)
Core Unity project setup, basic architecture, and data models.

### Phase 2: Physics Engine (Prompts 5-8)
Nathan model aerodynamics and trajectory simulation.

### Phase 3: 3D Visualization (Prompts 9-12)
Ball flight animation, camera system, and environment setup.

### Phase 4: USB Abstraction Layer (Prompts 13-15)
Platform-agnostic connection interface and protocol parsing.

### Phase 5: macOS Native Plugin (Prompts 16-18)
libusb-based USB plugin for macOS.

### Phase 6: Android Native Plugin (Prompts 19-21)
USB Host API plugin for Android tablets.

### Phase 7: iPad Native Plugin (Prompts 22-24)
DriverKit extension for iPad (M1+).

### Phase 8: UI System (Prompts 25-28)
Shot data display, connection status, settings panels.

### Phase 9: Integration & Polish (Prompts 29-32)
Wire everything together, quality tiers, testing.

---

## Detailed Prompts

---

### Prompt 1: Unity Project Setup

```text
Create a new Unity 2022.3 LTS project called "OpenRangeUnity" with the Universal Render Pipeline (URP).

Project requirements:
1. Set up the folder structure according to the TRD:
   - Assets/Scripts/Core/
   - Assets/Scripts/GC2/
   - Assets/Scripts/Physics/
   - Assets/Scripts/Visualization/
   - Assets/Scripts/UI/
   - Assets/Scripts/Network/
   - Assets/Scripts/Utilities/
   - Assets/Plugins/macOS/
   - Assets/Plugins/iOS/
   - Assets/Plugins/Android/
   - Assets/Prefabs/
   - Assets/Scenes/
   - Assets/Materials/
   - Assets/Textures/
   - Assets/Models/
   - Assets/Audio/
   - NativePlugins/macOS/
   - NativePlugins/iOS/
   - NativePlugins/Android/

2. Configure build targets:
   - macOS (Standalone, Universal architecture)
   - iOS (iPad only, minimum iOS 16.0)
   - Android (minimum API 26, ARM64 only)

3. Set up URP with three quality tiers:
   - URP_Low.asset
   - URP_Medium.asset
   - URP_High.asset

4. Create placeholder scenes:
   - Bootstrap.unity (loading scene)
   - MainMenu.unity
   - Ranges/Marina.unity

5. Configure project settings:
   - IL2CPP scripting backend
   - .NET Standard 2.1
   - Company name: OpenRange
   - Product name: Open Range Unity
   - Version: 2.0.0

6. Add .gitignore for Unity and create initial README.md

Write tests to verify:
- Project structure exists
- URP assets are configured
- Build targets are set correctly
```

---

### Prompt 2: Core Data Models

```text
Create the core data models for the GC2 integration in the OpenRangeUnity project.

Create the following C# classes in Assets/Scripts/GC2/:

1. GC2ShotData.cs - Shot data model
   - All ball data fields: ShotId, Timestamp, BallSpeed, LaunchAngle, Direction, TotalSpin, BackSpin, SideSpin, SpinAxis
   - All HMT club data fields: HasClubData, ClubSpeed, Path, AttackAngle, FaceToTarget, DynamicLoft, Lie
   - Add [Serializable] attribute for Unity serialization
   - Add JSON serialization attributes for native plugin communication
   - Include calculated property for SpinAxis from BackSpin/SideSpin
   - Include validation methods (IsValid, IsMisread)

2. GC2DeviceInfo.cs - Device information
   - SerialNumber
   - FirmwareVersion
   - HasHMT

3. GC2Protocol.cs - Protocol constants
   - VendorId = 0x2C79
   - ProductId = 0x0110
   - All field name constants (SHOT_ID, SPEED_MPH, etc.)
   - ParseRawData(string) method to convert raw USB data to GC2ShotData
   - Validation constants (min/max speed, misread patterns like 2222)

Write unit tests for:
- GC2ShotData serialization/deserialization
- SpinAxis calculation
- Misread detection (zero spin, 2222 pattern)
- Duplicate detection by ShotId
- GC2Protocol.ParseRawData with sample data from protocol spec
```

---

### Prompt 3: GC2 Connection Interface

```text
Create the platform-agnostic connection interface for GC2 communication.

Create the following in Assets/Scripts/GC2/:

1. IGC2Connection.cs - Interface
   - bool IsConnected { get; }
   - GC2DeviceInfo DeviceInfo { get; }
   - event Action<GC2ShotData> OnShotReceived;
   - event Action<bool> OnConnectionChanged;
   - event Action<string> OnError;
   - Task<bool> ConnectAsync();
   - void Disconnect();
   - bool IsDeviceAvailable();

2. GC2ConnectionFactory.cs - Platform factory
   - Create() method using #if directives:
     - UNITY_EDITOR → GC2MockConnection (for testing)
     - UNITY_STANDALONE_OSX → GC2MacConnection
     - UNITY_IOS → GC2iPadConnection
     - UNITY_ANDROID → GC2AndroidConnection
     - Default → GC2TCPConnection
   - CreateTCP(host, port) for network mode

3. GC2MockConnection.cs - Mock implementation for testing
   - Implements IGC2Connection
   - Simulates connection with configurable delay
   - Can generate test shots on demand
   - Useful for editor testing without hardware

4. Create placeholder classes (will implement later):
   - Platforms/MacOS/GC2MacConnection.cs
   - Platforms/iOS/GC2iPadConnection.cs
   - Platforms/Android/GC2AndroidConnection.cs
   - Network/GC2TCPConnection.cs

Write unit tests for:
- Factory creates correct implementation per platform
- Mock connection simulates connection lifecycle
- Mock connection can generate valid test shots
- Events are fired correctly
```

---

### Prompt 4: Core Game Manager

```text
Create the core game management scripts that wire the application together.

Create the following in Assets/Scripts/Core/:

1. GameManager.cs - Main game controller (singleton)
   - Initializes all systems on Awake()
   - Holds references to ShotProcessor, SessionManager, etc.
   - Handles application lifecycle (pause, resume, quit)
   - Coordinates between GC2 connection and visualization
   - Use [RuntimeInitializeOnLoadMethod] for early initialization

2. ShotProcessor.cs - Shot event handling
   - Subscribes to IGC2Connection.OnShotReceived
   - Validates incoming shots (filters misreads)
   - Converts shot data to physics parameters
   - Fires OnValidShotReceived event
   - Maintains last shot for comparison

3. SessionManager.cs - Session tracking
   - Session start time
   - Shot count
   - Shot history (List<GC2ShotData>)
   - Session statistics (average speed, etc.)
   - Clear/reset functionality

4. MainThreadDispatcher.cs - Thread-safe callback helper
   - Singleton with Update() loop
   - Queue<Action> for pending actions
   - Enqueue(Action) method
   - Essential for native plugin callbacks

5. PlatformManager.cs - Platform detection utilities
   - CurrentPlatform property
   - IsDesktop, IsMobile, IsTablet properties
   - Screen size helpers for responsive UI

Create Bootstrap.unity scene with:
- Empty GameObject "GameManager" with GameManager component
- Script execution order: MainThreadDispatcher (-100), GameManager (-50)

Write tests for:
- GameManager singleton pattern
- ShotProcessor filters misreads correctly
- SessionManager tracks shots correctly
- MainThreadDispatcher executes on main thread
```

---

### Prompt 5: Physics Constants and Units

```text
Create the physics foundation for ball flight simulation.

Create the following in Assets/Scripts/Physics/:

1. PhysicsConstants.cs - All physics constants
   - Gravity (9.80665 m/s^2)
   - Air density at sea level (1.225 kg/m^3)
   - Golf ball specifications:
     - Mass (0.04593 kg)
     - Diameter (0.04267 m)
     - Cross-sectional area
   - WSU aerodynamic coefficients for Cd and Cl
   - Ground roll friction coefficient

2. UnitConversions.cs - Unit conversion utilities
   - MphToMs(float mph) → m/s
   - MsToMph(float ms) → mph
   - DegreesToRadians(float deg)
   - RadiansToDegrees(float rad)
   - YardsToMeters(float yards)
   - MetersToYards(float meters)
   - FeetToMeters(float feet)
   - RpmToRadPerSec(float rpm)
   - All methods should have inverse versions

3. AtmosphericModel.cs - Atmospheric corrections
   - AirDensity(float temperatureC, float elevationM, float humidityPercent)
   - Implements ISA (International Standard Atmosphere) formulas
   - Default conditions: 15°C, sea level, 50% humidity

Write comprehensive unit tests for:
- All unit conversions (round-trip accuracy)
- Atmospheric model at known elevations (0m, 1000m, 2000m)
- Air density at various temperatures
- Constants match accepted golf ball physics values
```

---

### Prompt 6: Aerodynamics Model

```text
Implement the Nathan aerodynamics model for golf ball flight.

Create in Assets/Scripts/Physics/:

1. Aerodynamics.cs - Core aerodynamic calculations
   - CalculateDragCoefficient(float velocity, float spinRate, float spinRatio) → float
     - Uses WSU coefficient tables
     - Handles Reynolds number effects
   - CalculateLiftCoefficient(float spinRatio) → float
     - Based on spin parameter S = (radius * omega) / velocity
   - CalculateDragForce(Vector3 velocity, float airDensity, float Cd) → Vector3
   - CalculateLiftForce(Vector3 velocity, Vector3 spinAxis, float airDensity, float Cl) → Vector3
   - CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity, float airDensity) → Vector3

2. SpinDecay.cs - Spin rate decay over time
   - CalculateSpinDecay(float initialSpin, float time) → float
   - Empirical model for spin reduction during flight
   - Typical 10-15% decay over 5 seconds

3. SpinAxisCalculator.cs - Convert spin components to axis
   - CalculateSpinAxis(float backSpin, float sideSpin) → Vector3
   - CalculateTotalSpin(float backSpin, float sideSpin) → float
   - SpinAxisToComponents(Vector3 axis, float totalSpin) → (backSpin, sideSpin)

The aerodynamics should match validated data:
- A 150 mph ball speed, 12° launch, 2500 rpm backspin should carry ~280 yards
- Include test data from known trajectory calculators

Write tests for:
- Drag coefficient at various speeds and spins
- Lift coefficient at various spin ratios
- Force calculations with known inputs
- Spin decay model accuracy
- Spin axis conversions
```

---

### Prompt 7: Trajectory Simulator

```text
Implement the trajectory simulation engine using numerical integration.

Create in Assets/Scripts/Physics/:

1. TrajectorySimulator.cs - Main trajectory calculator
   - SimulateTrajectory(TrajectoryInput input) → TrajectoryResult
   - Uses 4th-order Runge-Kutta integration
   - Configurable time step (default 0.001s for accuracy)
   - Simulates until ball hits ground (y <= 0)

2. TrajectoryInput.cs - Input parameters
   - InitialPosition (Vector3)
   - BallSpeedMph (float)
   - LaunchAngleDeg (float)
   - LaunchDirectionDeg (float)
   - BackSpinRpm (float)
   - SideSpinRpm (float)
   - WindVelocity (Vector3)
   - AtmosphericConditions (temperature, elevation, humidity)

3. TrajectoryResult.cs - Output data
   - TrajectoryPoints (List<Vector3>) - sampled points for visualization
   - Apex (Vector3) - highest point
   - ApexHeight (float) - in yards
   - CarryDistance (float) - in yards
   - LandingPosition (Vector3)
   - LandingAngle (float) - descent angle
   - FlightTime (float) - seconds
   - OfflineDistance (float) - lateral deviation in yards

4. TrajectoryPoint.cs - Individual point data
   - Position (Vector3)
   - Velocity (Vector3)
   - Time (float)

Trajectory sampling:
- Store points every 0.05 seconds for smooth animation
- At least 100 points for typical driver shot

Write tests for:
- Driver shot (150 mph, 12°, 2500 rpm) → ~280-290 yard carry
- 7-iron shot (120 mph, 18°, 5500 rpm) → ~160-170 yard carry
- Draw (negative side spin) curves left
- Fade (positive side spin) curves right
- Wind effects on carry distance
- Apex height matches expected values
```

---

### Prompt 8: Ground Physics

```text
Implement ground interaction physics for landing, bounce, and roll.

Create in Assets/Scripts/Physics/:

1. GroundPhysics.cs - Landing and roll simulation
   - CalculateBounce(Vector3 velocity, Vector3 groundNormal, float landingAngle) → Vector3
     - Coefficient of restitution based on landing angle
     - Steeper landing = less bounce
   - CalculateRoll(Vector3 velocity, float groundSlope) → RollResult
     - Friction-based deceleration
     - Consider ground slope
   - SimulateGroundPhase(Vector3 landingPosition, Vector3 landingVelocity) → GroundResult

2. RollResult.cs
   - FinalPosition (Vector3)
   - RollDistance (float) - in yards
   - RollDirection (Vector3)
   - RollTime (float)

3. GroundResult.cs
   - TotalDistance (float) - carry + roll
   - FinalPosition (Vector3)
   - BouncePoints (List<Vector3>) - for animation
   - DidBounce (bool)

Ground assumptions:
- Flat ground for MVP (slope support later)
- Fairway-like conditions (short grass)
- Typical run is 10-20% of carry for mid-irons
- Less roll for high-spin shots
- More roll for low-spinning drivers

Write tests for:
- High-spin wedge shot has minimal roll
- Low-spin driver has significant roll
- Steep landing angle reduces bounce
- Roll distance is reasonable (not excessive)
- Bounce physics are realistic
```

---

### Prompt 9: Ball Prefab and Controller

```text
Create the golf ball prefab and controller for 3D visualization.

Create the following:

1. Assets/Prefabs/Ball/GolfBall.prefab
   - Sphere with golf ball texture and dimple normal map
   - Scale: 0.04267m (regulation ball diameter)
   - Layer: "Ball" (create layer)
   - No physics collider (we use our own physics)
   - Child objects:
     - TrailRenderer for flight tracer
     - ParticleSystem for landing dust effect

2. Assets/Scripts/Visualization/BallController.cs
   - AnimateFlight(TrajectoryResult trajectory) method
   - Uses coroutine for smooth animation
   - Interpolates between trajectory points
   - Rotates ball based on spin (visual spin)
   - Speed configurable (realtime, 2x, instant)
   - Events: OnFlightStart, OnApexReached, OnLanding, OnRollComplete
   - SkipToEnd() method to jump to final position
   - Reset() method to return to tee position

3. Assets/Scripts/Visualization/TrajectoryRenderer.cs
   - Uses LineRenderer to draw ball path
   - Can show predicted vs actual trajectory
   - Fades out over time after shot
   - Color-coded by altitude or speed

4. Assets/Scripts/Visualization/LandingMarker.cs
   - Spawns marker at landing position
   - Shows carry vs total distance
   - Persists for configurable time
   - Can show dispersion pattern

Integration with ShotProcessor:
- BallController listens to ShotProcessor.OnValidShotReceived
- Gets trajectory from TrajectorySimulator
- Animates the flight

Write tests for:
- Ball animates through all trajectory points
- Animation timing matches flight time
- Spin rotation is visible
- Landing effects trigger correctly
- Reset returns ball to start position
```

---

### Prompt 10: Camera System

```text
Create the camera control system for following ball flight.

Create in Assets/Scripts/Visualization/:

1. CameraController.cs - Main camera controller
   - Multiple camera modes:
     - Follow: Tracks ball during flight
     - Static: Fixed position, rotates to follow ball
     - TopDown: Overhead view for dispersion
     - FreeOrbit: User-controlled orbit around range
   - SwitchMode(CameraMode mode) method
   - Smooth transitions between modes
   - Returns to default position after shot

2. FollowCamera.cs - Follow mode behavior
   - Maintains distance behind ball
   - Smooth look-ahead based on velocity
   - Clamps to reasonable angles
   - Handles apex (camera rises with ball)

3. OrbitCamera.cs - User-controlled orbit
   - Touch controls: pinch zoom, two-finger rotate
   - Mouse controls: scroll zoom, right-drag rotate
   - Orbit around range center
   - Min/max distance limits
   - Smooth damping

4. CameraRig prefab (Assets/Prefabs/Camera/)
   - Main Camera with CameraController
   - Post-processing volume (URP)
   - Audio listener

Camera defaults:
- Initial position: Behind tee, elevated
- Default mode: Static for first shot, then Follow
- Return to overview after ball stops

Write tests for:
- Camera mode switching works correctly
- Follow mode tracks ball smoothly
- Orbit controls respect limits
- Transitions are smooth (no jumps)
- Camera doesn't clip through ground
```

---

### Prompt 11: Marina Range Environment

```text
Create the marina/coastal driving range environment (primary range).

Create the following:

1. Assets/Scenes/Ranges/Marina.unity - Main range scene
   Scene hierarchy:
   - Environment
     - Terrain (base ground)
     - Water (ocean plane with shader)
     - Mountains (background meshes)
     - Props (boats, buildings, trees)
   - Range
     - TeeMat (hitting area)
     - TargetGreens (multiple at 50, 100, 150, 200, 250 yards)
     - DistanceMarkers (signs at key distances)
     - Flags
   - Lighting
     - Directional Light (sun)
     - Skybox
     - Reflection Probes
   - Managers (empty, populated by GameManager)
   - UI (empty, populated by UIManager)

2. Assets/Scripts/Visualization/EnvironmentManager.cs
   - Manages environment assets
   - Handles time of day (optional)
   - Controls weather effects (future)
   - Quality tier adjustments (disable effects on low)

3. Create materials:
   - Water shader (URP, with reflections for High quality)
   - Grass material with detail maps
   - Skybox (procedural or HDR)

4. Target greens setup:
   - Visual landing zones
   - Flags that can animate
   - Distance text floating above

Quality tier considerations:
- High: Full water reflections, soft shadows, volumetric lighting
- Medium: Planar reflections, hard shadows
- Low: No reflections, baked shadows only

Write tests (play mode):
- Scene loads without errors
- Environment objects are positioned correctly
- Lighting is set up properly
- Performance meets target (60 FPS on medium hardware)
```

---

### Prompt 12: Visual Effects

```text
Create visual effects for ball flight, landing, and impacts.

Create in Assets/Prefabs/Effects/:

1. BallTrail.prefab - Flight tracer
   - Trail Renderer component
   - Gradient from white to transparent
   - Width: 0.02m at start, fading
   - Time: 2 seconds
   - Quality tiers: High = full, Low = simplified

2. LandingDust.prefab - Landing particle effect
   - Particle System
   - Brown/tan dust particles
   - Short burst on ground impact
   - Scales with landing velocity
   - Quality tiers: High = 50 particles, Low = 10

3. WaterSplash.prefab - Water landing effect
   - Particle System
   - White splash particles
   - Ripple effect (shader)
   - Only on water collision

4. Assets/Scripts/Visualization/EffectsManager.cs
   - SpawnLandingEffect(Vector3 position, GroundType type)
   - SpawnBounceEffect(Vector3 position)
   - Manages effect pooling for performance
   - Respects quality tier settings

5. Ball spin visualization:
   - Optional spin indicator during flight
   - Shows spin axis direction
   - Fades based on settings

6. Shot preview line:
   - Dotted line showing predicted trajectory
   - Updates in real-time (optional)
   - Can be toggled off

Write tests:
- Effects spawn at correct positions
- Pooling works correctly (no memory leaks)
- Quality tier switches affect particle counts
- Effects are visible but not distracting
```

---

### Prompt 13: USB Connection Events

```text
Create the event system for USB connection status and management.

Expand Assets/Scripts/GC2/:

1. GC2ConnectionManager.cs - High-level connection manager
   - Wraps IGC2Connection
   - Handles automatic reconnection
   - Monitors device availability
   - Fires user-friendly events
   - Manages connection state machine:
     - Disconnected → Detecting → Connecting → Connected
     - Connected → Disconnected (on error or unplug)

2. ConnectionState enum:
   - Disconnected
   - Detecting (looking for device)
   - WaitingPermission (Android/iOS permission dialog)
   - Connecting
   - Connected
   - Error

3. GC2ConnectionConfig.cs - Configuration
   - AutoReconnect (bool)
   - ReconnectDelayMs (int)
   - ConnectionTimeoutMs (int)
   - Saved to PlayerPrefs

4. GC2PermissionHandler.cs - Platform permission handling
   - RequestPermission() for Android USB permission
   - Handles DriverKit activation for iOS
   - Provides user-facing permission messages

Integration:
- GameManager creates GC2ConnectionManager on start
- Connection status shown in UI (next phase)
- Auto-connect on app start if device present

Write tests for:
- State machine transitions correctly
- Auto-reconnect attempts on disconnect
- Timeout handles stuck connections
- Permission flow completes correctly (mock)
```

---

### Prompt 14: TCP Connection Implementation

```text
Implement the TCP/network connection for GSPro mode and fallback.

Create Assets/Scripts/GC2/Network/GC2TCPConnection.cs:

1. Implements IGC2Connection
   - Connect to specified host:port
   - Default port: 8888 (GSPro standard)
   - Async socket operations
   - Reconnection support

2. Network protocol:
   - Uses same text protocol as USB
   - Newline-delimited messages
   - JSON wrapper for some GSPro compatibility

3. GSPro integration:
   - Can send shots TO GSPro (as OpenConnect client)
   - Can receive shots FROM external source (for testing)

4. Create Assets/Scripts/Network/GSProClient.cs:
   - Formats shot data for GSPro Open Connect API
   - JSON format per GSPro spec
   - Maintains connection to GSPro server
   - Can work alongside local visualization

5. GC2TCPListener.cs - Optional test server
   - Listens for connections
   - Useful for testing without hardware
   - Can replay recorded shot data

Error handling:
- Graceful disconnect on network errors
- Queue shots if temporarily disconnected (GSPro mode)
- Timeout on unresponsive server

Write tests for:
- TCP connection lifecycle
- Message parsing from network
- GSPro JSON formatting
- Reconnection on network failure
- Shot queuing during disconnect
```

---

### Prompt 15: Protocol Parser Hardening

```text
Harden the GC2 protocol parser for robustness with real-world data.

Enhance Assets/Scripts/GC2/GC2Protocol.cs:

1. Robust parsing:
   - Handle partial data (buffer until complete)
   - Handle malformed lines gracefully
   - Ignore unknown fields (forward compatibility)
   - Handle different line endings (\n, \r\n, \r)

2. Buffered data handling:
   - GC2DataBuffer class
   - Append(byte[] data)
   - TryExtractMessage() → string?
   - Clear()

3. Validation improvements:
   - Validate all numeric ranges
   - Check for NaN and Infinity
   - Sanitize string values
   - Log warnings for unexpected data

4. Misread filtering:
   - IsValidShot(GC2ShotData) → bool
   - Configurable thresholds
   - Common misread patterns:
     - Zero total spin
     - Backspin = 2222
     - Speed < 10 mph
     - Speed > 250 mph
     - Launch angle > 60° or < -10°

5. Data normalization:
   - Clamp values to valid ranges
   - Calculate derived values (spin axis)
   - Apply calibration offsets (future)

Create test data files:
- Sample valid shots (various clubs)
- Sample misreads
- Partial data scenarios
- Malformed data

Write extensive tests for:
- All validation rules
- Buffer handling with fragmented data
- Recovery from malformed data
- No crashes on any input
```

---

### Prompt 16: macOS Plugin Header and Interface

```text
Create the macOS native plugin interface for libusb communication.

Create NativePlugins/macOS/GC2MacPlugin/:

1. GC2MacPlugin.h - C interface header
   - Function declarations for Unity interop
   - Callback typedefs:
     - GC2ShotCallback(const char* jsonData)
     - GC2ConnectionCallback(bool connected)
     - GC2ErrorCallback(const char* error)
   - Exported functions:
     - GC2Mac_Initialize()
     - GC2Mac_Shutdown()
     - GC2Mac_IsDeviceAvailable()
     - GC2Mac_Connect()
     - GC2Mac_Disconnect()
     - GC2Mac_IsConnected()
     - GC2Mac_GetDeviceInfo()
     - GC2Mac_SetShotCallback(callback)
     - GC2Mac_SetConnectionCallback(callback)
     - GC2Mac_SetErrorCallback(callback)

2. Create Xcode project:
   - GC2MacPlugin.xcodeproj
   - Target: Bundle (.bundle)
   - Deployment target: macOS 11.0
   - Architectures: Universal (x86_64 + arm64)

3. Dependencies:
   - Add libusb via brew or embedded
   - Link IOKit.framework
   - Link CoreFoundation.framework

4. Build script for Unity integration:
   - build_mac_plugin.sh
   - Copies .bundle to Assets/Plugins/macOS/

Document USB permissions for macOS:
- No special permissions needed for libusb on macOS
- May need to disable SIP for some debugging

Write a stub implementation that:
- Compiles successfully
- Returns false for IsDeviceAvailable
- Logs initialization messages
```

---

### Prompt 17: macOS Plugin Implementation

```text
Implement the macOS native plugin using libusb.

Complete NativePlugins/macOS/GC2MacPlugin/GC2MacPlugin.mm:

1. Initialization:
   - libusb_init() context
   - Set up hotplug callbacks if available
   - Create dispatch queue for USB reads

2. Device detection:
   - Enumerate USB devices
   - Match VID: 0x2C79, PID: 0x0110
   - Cache device reference

3. Connection:
   - libusb_open_device_with_vid_pid()
   - libusb_claim_interface(0)
   - Find bulk IN endpoint (0x81)
   - Start read thread

4. Read loop (dispatch queue):
   - libusb_bulk_transfer() with timeout
   - Buffer received data
   - Parse complete messages
   - Convert to JSON
   - Call shot callback on main thread

5. Protocol parsing:
   - Parse KEY=VALUE format
   - Build NSDictionary
   - Serialize to JSON string
   - Handle partial data

6. Disconnection:
   - Stop read thread
   - libusb_release_interface()
   - libusb_close()
   - Fire connection callback(false)

7. Error handling:
   - Handle USB errors gracefully
   - Fire error callback with message
   - Auto-detect disconnection

Build and test:
- Build universal binary
- Test on Intel Mac
- Test on M1 Mac
- Verify device detection
- Verify shot data parsing

Write integration tests (requires hardware):
- Device detection works
- Connection establishes
- Shot data is received
- Disconnection is detected
```

---

### Prompt 18: macOS C# Bridge

```text
Create the C# wrapper for the macOS native plugin.

Create Assets/Scripts/GC2/Platforms/MacOS/GC2MacConnection.cs:

1. DllImport declarations:
   - All functions from GC2MacPlugin.h
   - Use "GC2MacPlugin" as library name
   - Handle callback marshalling

2. Callback delegates:
   - [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
   - Use [AOT.MonoPInvokeCallback] for IL2CPP

3. Implement IGC2Connection:
   - Map native calls to interface methods
   - Parse JSON from native callbacks
   - Fire C# events

4. Thread safety:
   - Use MainThreadDispatcher for callback dispatch
   - Cache delegate references to prevent GC collection

5. Lifecycle management:
   - Initialize on construction
   - Shutdown on destruction/dispose
   - Handle Unity application quit

6. JSON parsing:
   - Use JsonUtility or Newtonsoft.Json
   - Map native JSON to GC2ShotData
   - Handle missing fields gracefully

7. Conditional compilation:
   - Wrap with #if UNITY_STANDALONE_OSX
   - Prevent compilation errors on other platforms

Create editor test utility:
- Assets/Editor/GC2MacTestWindow.cs
- EditorWindow for testing connection
- Shows device status
- Can trigger test operations

Write tests for:
- Native function calls don't crash
- Callbacks are received on main thread
- JSON parsing handles all fields
- Connection state is accurate
```

---

### Prompt 19: Android Plugin Interface

```text
Create the Android native plugin for USB Host API.

Create NativePlugins/Android/GC2AndroidPlugin/:

1. Project structure:
   - build.gradle (AAR library)
   - src/main/AndroidManifest.xml
   - src/main/kotlin/com/openrange/gc2/

2. AndroidManifest.xml:
   - USB permissions
   - USB device filter for GC2 (VID/PID)
   - Intent filter for USB device attach

3. res/xml/gc2_device_filter.xml:
   - vendor-id: 11385 (0x2C79)
   - product-id: 272 (0x0110)

4. GC2Plugin.kt - Main plugin class:
   - Singleton pattern for Unity access
   - initialize(Context, callbackObject: String)
   - isDeviceAvailable(): Boolean
   - connect(Context): Boolean
   - disconnect()
   - isConnected(): Boolean

5. GC2Device.kt - USB device wrapper:
   - UsbDevice reference
   - UsbDeviceConnection
   - UsbEndpoint for bulk transfer
   - Read thread management

6. GC2Protocol.kt - Protocol parsing:
   - Parse KEY=VALUE format
   - Convert to JSON
   - Validate data

7. Unity callbacks:
   - UnitySendMessage for events
   - OnNativeShotReceived(json)
   - OnNativeConnectionChanged(connected)
   - OnNativeError(error)

Build configuration:
- Target SDK 33
- Min SDK 26
- Output: AAR file

Write build script:
- build_android_plugin.sh
- Copies AAR to Assets/Plugins/Android/
```

---

### Prompt 20: Android Plugin Implementation

```text
Implement the Android USB Host API integration.

Complete the Android plugin implementation:

1. USB Permission handling:
   - BroadcastReceiver for USB_PERMISSION
   - PendingIntent for permission request
   - Handle permission granted/denied

2. USB Connection:
   - UsbManager.openDevice()
   - claimInterface(0, true)
   - Find bulk IN endpoint
   - Start read thread

3. Read thread:
   - bulkTransfer with timeout
   - Buffer incoming data
   - Parse complete messages
   - Call Unity on main thread

4. Device detection:
   - Monitor USB_DEVICE_ATTACHED
   - Monitor USB_DEVICE_DETACHED
   - Auto-reconnect on reattach

5. Error handling:
   - Handle permission denied
   - Handle connection lost
   - Handle transfer errors
   - Clean up on error

6. Protocol parsing:
   - Same format as macOS
   - Convert to JSONObject
   - Serialize for Unity

7. Lifecycle management:
   - Handle Activity lifecycle
   - Clean up on app pause
   - Reconnect on resume

Test on Android devices:
- Samsung Galaxy Tab S8
- Other USB Host capable tablets
- Test permission flow
- Test hot-plug behavior

Write tests (requires hardware):
- Permission request appears
- Device is detected
- Data is received correctly
- Disconnection is handled
```

---

### Prompt 21: Android C# Bridge

```text
Create the C# wrapper for the Android native plugin.

Create Assets/Scripts/GC2/Platforms/Android/GC2AndroidConnection.cs:

1. This must be a MonoBehaviour:
   - Android uses UnitySendMessage which requires a GameObject
   - Create as singleton with DontDestroyOnLoad

2. AndroidJavaObject calls:
   - Get plugin instance via AndroidJavaClass
   - Call methods via AndroidJavaObject.Call
   - Pass Activity context where needed

3. Implement IGC2Connection:
   - Wrap all plugin methods
   - Handle Android-specific lifecycle

4. Unity message handlers (called by native):
   - OnNativeShotReceived(string json)
   - OnNativeConnectionChanged(string connected)
   - OnNativeError(string error)

5. JSON parsing:
   - JsonUtility.FromJson<GC2ShotData>
   - Handle field naming differences

6. Lifecycle integration:
   - OnApplicationPause
   - OnApplicationFocus
   - OnDestroy

7. Conditional compilation:
   - #if UNITY_ANDROID && !UNITY_EDITOR
   - Prevent compilation on other platforms

Create prefab:
   - Assets/Prefabs/GC2/GC2AndroidManager.prefab
   - With GC2AndroidConnection component
   - Instantiated by GC2ConnectionFactory

Write tests for:
- Plugin initialization
- Message handlers parse correctly
- Lifecycle events are handled
- Errors are propagated
```

---

### Prompt 22: iPad Plugin Interface (DriverKit)

```text
Create the iPad native plugin structure for DriverKit USB communication.

Note: DriverKit requires Apple entitlements and is more complex than other platforms.

Create NativePlugins/iOS/:

1. GC2iOSPlugin/ - App-side Swift framework:
   - GC2iOSPlugin.swift - Main plugin class
   - GC2iOSPlugin.h - Objective-C bridging header for Unity
   - Info.plist

2. GC2Driver/ - DriverKit extension (separate target):
   - GC2Driver.swift - IOUserService implementation
   - GC2UserClient.swift - User client for app communication
   - Info.plist with driver entitlements

3. Xcode project structure:
   - GC2iOS.xcodeproj
   - Targets:
     - GC2iOSPlugin.framework
     - GC2Driver.dext (DriverKit extension)

4. Entitlements required:
   - com.apple.developer.driverkit
   - com.apple.developer.driverkit.family.usb
   - com.apple.developer.driverkit.transport.usb

5. Interface (GC2iOSPlugin.swift):
   - public class GC2iOSPlugin
   - static let shared = GC2iOSPlugin()
   - initialize(callbackObject: String)
   - isDeviceAvailable() -> Bool
   - connect() -> Bool
   - disconnect()
   - isConnected() -> Bool

6. DriverKit communication:
   - IOUserClientConnection for app-driver IPC
   - Shared memory for data transfer
   - Async callbacks

Document DriverKit requirements:
- Must request entitlements from Apple (may take weeks)
- Only works on M1+ iPads (iPadOS 16+)
- Requires system extension approval by user
- More complex than Android USB Host

Create placeholder implementation that:
- Returns appropriate "not available" messages
- Documents what's needed for full implementation
```

---

### Prompt 23: iPad DriverKit Implementation

```text
Implement the DriverKit extension for iPad USB communication.

Note: This is the most complex native plugin due to DriverKit requirements.

Create GC2Driver.dext:

1. GC2Driver.swift - IOUserService:
   - IOUSBHostDevice matching
   - USB configuration
   - Interface claiming
   - Endpoint management

2. USB matching:
   - IOProviderClass: IOUSBHostDevice
   - idVendor: 0x2C79
   - idProduct: 0x0110

3. GC2UserClient.swift:
   - IOUserClient subclass
   - External method dispatch
   - Shared memory for data
   - Async notifications to app

4. Read loop:
   - Async bulk IN transfers
   - Buffer management
   - Notify app of new data

Implement GC2iOSPlugin.framework:

1. Driver communication:
   - IOServiceGetMatchingService()
   - IOUserClientConnection
   - Shared memory mapping
   - Read data from driver

2. Protocol parsing:
   - Same as macOS/Android
   - Convert to JSON

3. Unity callbacks:
   - UnitySendMessage()
   - Main thread dispatch

4. System extension lifecycle:
   - Check if extension is approved
   - Request activation if needed
   - Handle activation flow

Build process:
- Build framework for iOS
- Build .dext with special signing
- Both need distribution provisioning

Testing notes:
- Must test on real iPad Pro M1+
- Cannot test in simulator
- System extension requires user approval
```

---

### Prompt 24: iPad C# Bridge

```text
Create the C# wrapper for the iPad native plugin.

Create Assets/Scripts/GC2/Platforms/iOS/GC2iPadConnection.cs:

1. Native function imports:
   - [DllImport("__Internal")] for iOS
   - All plugin functions

2. Callback handling:
   - Same pattern as macOS
   - [AOT.MonoPInvokeCallback]
   - Static delegates

3. Implement IGC2Connection:
   - Map to Swift plugin calls
   - Handle DriverKit state

4. DriverKit-specific handling:
   - Check extension approval status
   - Prompt for extension activation
   - Handle "waiting for approval" state

5. Permission UI:
   - DriverKit requires user to:
     1. Go to Settings → Privacy → Extensions
     2. Enable the driver
   - Provide user guidance

6. Error messages:
   - Clear messages for:
     - "DriverKit not supported (older iPad)"
     - "Driver not installed"
     - "Driver needs approval in Settings"
     - "Device not connected"

7. Conditional compilation:
   - #if UNITY_IOS && !UNITY_EDITOR

Create user guidance:
   - UI panel explaining DriverKit setup
   - Link to Settings if possible
   - Troubleshooting help

Write tests for:
- Plugin initialization
- State detection
- Error handling
- Callback routing
```

---

### Prompt 25: Shot Data UI

```text
Create the shot data display UI (GSPro-style bottom bar).

Create in Assets/Scripts/UI/:

1. UIManager.cs - Main UI controller:
   - Canvas setup (Screen Space - Overlay)
   - References to all UI panels
   - Theme/style management
   - Responsive layout coordinator

2. ShotDataBar.cs - Bottom data bar:
   - Layout: Horizontal row of data tiles
   - Tiles for:
     - Ball Speed (mph)
     - Direction (° L/R)
     - Launch Angle (°)
     - Total Spin (rpm)
     - Apex (yards)
     - Offline (yards L/R)
     - Carry (yards)
     - Run (yards)
     - Total (yards)
   - Animates on new shot data
   - Color coding (positive/negative for direction)

3. DataTile.cs - Individual data display:
   - Label (top)
   - Value (large, center)
   - Unit (small, bottom)
   - Animation on value change
   - Color theming

Create UI assets:

4. Assets/Prefabs/UI/ShotDataBar.prefab:
   - Horizontal Layout Group
   - 9 DataTile instances
   - Anchored to bottom of screen

5. Assets/Prefabs/UI/DataTile.prefab:
   - TextMeshPro for all text
   - Background image
   - Layout for proper sizing

UI styling:
- Dark semi-transparent background
- White/light text
- Accent colors for values
- Consistent with GSPro aesthetic

Write tests for:
- UI updates on shot received
- All fields display correctly
- Values are formatted properly (1 decimal for most)
- Negative offline shows "L" suffix
- Positive offline shows "R" suffix
```

---

### Prompt 26: Club Data Panel (HMT)

```text
Create the HMT club data panel for swing metrics.

Create in Assets/Scripts/UI/:

1. ClubDataPanel.cs - Right-side HMT panel:
   - Only visible when shot has HMT data
   - Displays club metrics:
     - Club Speed (mph)
     - Path (° in-to-out / out-to-in)
     - Attack Angle (° up/down)
     - Face to Target (° open/closed)
     - Dynamic Loft (°)
   - Visual indicator diagrams

2. SwingPathIndicator.cs - Visual swing path:
   - Top-down view graphic
   - Arrow showing path direction
   - Face angle indicator
   - Color coded (in-to-out = draw, out-to-in = fade)

3. AttackAngleIndicator.cs - Side view:
   - Shows angle of attack
   - Descending (negative) vs ascending (positive)
   - Club head graphic

Create prefabs:

4. Assets/Prefabs/UI/ClubDataPanel.prefab:
   - Vertical layout
   - Data rows
   - Indicator graphics
   - Anchored to right side

5. Integration:
   - Subscribes to ShotProcessor
   - Shows/hides based on HasClubData
   - Animates on new data

Styling:
- Matches ShotDataBar aesthetic
- Compact for tablet screens
- Optional collapse for more viewing area

Write tests for:
- Panel shows only with HMT data
- All HMT fields display correctly
- Path indicator matches data
- Attack angle indicator correct
- Hide/show animation works
```

---

### Prompt 27: Connection Status UI

```text
Create the connection status UI and indicators.

Create in Assets/Scripts/UI/:

1. ConnectionStatusUI.cs - Connection indicator:
   - Shows current connection state
   - Status icon (connected, disconnected, connecting)
   - Device info when connected
   - Tap to expand for details

2. ConnectionPanel.cs - Expanded connection panel:
   - Device information (serial, firmware)
   - Connection mode (USB, TCP)
   - Last shot time
   - Signal quality (if applicable)
   - Disconnect button
   - Settings link

3. ConnectionToast.cs - Toast notifications:
   - "GC2 Connected"
   - "GC2 Disconnected"
   - "Connection lost, reconnecting..."
   - Auto-dismiss after 3 seconds

Create prefabs:

4. Assets/Prefabs/UI/ConnectionStatus.prefab:
   - Small icon in corner (top-right)
   - Expands to ConnectionPanel on tap
   - Status LED (green/yellow/red)

5. Assets/Prefabs/UI/ConnectionPanel.prefab:
   - Modal overlay
   - Close button
   - All connection details

6. Assets/Prefabs/UI/Toast.prefab:
   - Slide-in notification
   - Auto-dismiss
   - Queue multiple toasts

Integration:
- Subscribes to GC2ConnectionManager
- Updates on state changes
- Shows appropriate messages

Write tests for:
- Status updates on connection change
- Toast appears on connect/disconnect
- Panel shows correct device info
- Tap interactions work
```

---

### Prompt 28: Settings Panel

```text
Create the settings panel for app configuration.

Create in Assets/Scripts/UI/:

1. SettingsPanel.cs - Main settings panel:
   - Modal overlay
   - Tabbed or scrollable sections
   - Save/Cancel buttons

2. Settings sections:

   a) Graphics:
      - Quality tier (Low/Medium/High/Auto)
      - Target frame rate
      - Effect toggles

   b) Units:
      - Distance (Yards/Meters)
      - Speed (MPH/KPH)
      - Temperature (F/C)

   c) Conditions:
      - Temperature slider
      - Elevation slider
      - Humidity slider
      - Wind toggle and settings

   d) Connection:
      - Auto-connect toggle
      - Reconnect delay
      - GSPro mode settings
      - TCP host/port (when applicable)

   e) Audio:
      - Master volume
      - Effects volume
      - Music volume (future)

3. SettingsManager.cs (Assets/Scripts/Core/):
   - Singleton settings store
   - Uses PlayerPrefs for persistence
   - Default values
   - Change events
   - Apply() method

Create prefabs:

4. Assets/Prefabs/UI/SettingsPanel.prefab:
   - Full screen overlay
   - Scroll view for content
   - Section headers
   - Various input controls

5. Setting input prefabs:
   - SliderSetting.prefab
   - ToggleSetting.prefab
   - DropdownSetting.prefab
   - InputFieldSetting.prefab

Write tests for:
- Settings persist across sessions
- Settings apply correctly
- Quality tier affects rendering
- Unit conversions work throughout app
- Default values are sensible
```

---

### Prompt 29: Session Info Panel

```text
Create the session information display and shot history.

Create in Assets/Scripts/UI/:

1. SessionInfoPanel.cs - Top-left info panel:
   - Session start time
   - Elapsed time (live updating)
   - Total shots
   - Average ball speed
   - Longest carry
   - Best shot indicator

2. ShotHistoryPanel.cs - Shot history list:
   - Expandable panel
   - Scrollable list of shots
   - Each row shows:
     - Shot number
     - Club (if tracked)
     - Ball speed
     - Carry distance
   - Tap to replay trajectory
   - Tap to show full details

3. ShotDetailModal.cs - Full shot details:
   - All shot data
   - Trajectory visualization (mini)
   - Compare to session average
   - Share/export options (future)

Create in Assets/Scripts/Core/:

4. Enhance SessionManager.cs:
   - CalculateAverages()
   - GetBestShot() by various metrics
   - Export session data (JSON)
   - Clear history option

Create prefabs:

5. Assets/Prefabs/UI/SessionInfoPanel.prefab:
   - Compact display
   - Live updating time
   - Collapsible

6. Assets/Prefabs/UI/ShotHistoryPanel.prefab:
   - List view
   - Virtual scrolling for performance
   - Shot row prefab

Write tests for:
- Session stats calculate correctly
- History shows all shots
- Replay works from history
- Best shot is identified correctly
- Export produces valid JSON
```

---

### Prompt 30: Responsive Layout System

```text
Create the responsive layout system for different screen sizes.

Create in Assets/Scripts/UI/:

1. ResponsiveLayout.cs - Layout coordinator:
   - Detects screen size and orientation
   - Defines breakpoints:
     - Compact (< 800px width): small tablets
     - Regular (800-1200px): iPads, tablets
     - Large (> 1200px): Mac, large tablets
   - Fires OnLayoutChange event
   - Handles orientation changes

2. ResponsiveElement.cs - Element behavior:
   - Component for adaptive UI elements
   - Configurable per-breakpoint:
     - Position
     - Size
     - Visibility
     - Font size

3. Layout configurations:

   Compact layout:
   - Bottom bar: smaller tiles, less padding
   - Side panels: hidden or collapsed
   - Session info: minimal

   Regular layout:
   - Bottom bar: full size
   - Side panels: visible when relevant
   - Session info: visible

   Large layout:
   - Extra data visible
   - More detailed panels
   - Larger fonts

4. SafeAreaHandler.cs - Device safe areas:
   - Handle notches (iPad Pro)
   - Handle home indicator
   - Adjust UI margins

Create test scenes:
- Test at various resolutions
- Test on different devices

Write tests for:
- Breakpoint detection correct
- Layout changes apply
- Orientation changes handled
- Safe area respected
- No UI clipping
```

---

### Prompt 31: Quality Tier System

```text
Implement the quality tier system for performance optimization.

Enhance Assets/Scripts/Core/QualityManager.cs:

1. Quality tier detection:
   - Analyze device capabilities:
     - GPU memory
     - System memory
     - Processor type
     - Screen resolution
   - Map to Low/Medium/High
   - Store detection result

2. Quality settings per tier:

   Low:
   - 30 FPS target
   - No shadows
   - Baked lighting
   - Simplified particles
   - No post-processing
   - Reduced draw distance

   Medium:
   - 60 FPS target
   - Hard shadows
   - Basic reflections
   - Normal particles
   - Minimal post-processing
   - Full draw distance

   High:
   - 120 FPS target (where supported)
   - Soft shadows
   - SSR reflections
   - Full particles
   - Full post-processing
   - Volumetric effects

3. Dynamic adjustment:
   - Monitor frame rate
   - Auto-downgrade if struggling
   - Notify user of changes
   - Manual override in settings

4. Create URP assets:
   - Assets/Settings/URP_Low.asset
   - Assets/Settings/URP_Medium.asset
   - Assets/Settings/URP_High.asset
   - Configure each appropriately

5. Environment quality hooks:
   - EnvironmentManager responds to tier changes
   - Disables/enables visual elements
   - LOD adjustments

Write tests for:
- Quality detection is reasonable
- Tier switching applies correctly
- Frame rate improves on lower tiers
- Manual override works
- Settings persist
```

---

### Prompt 32: Full Integration and Polish

```text
Wire everything together and polish the complete application.

Integration tasks:

1. Scene setup (Marina.unity):
   - Add all manager GameObjects
   - Wire up prefab references
   - Set up initial state
   - Configure lighting for each quality tier

2. Boot sequence (Bootstrap.unity):
   - Show loading screen
   - Initialize managers in order:
     1. MainThreadDispatcher
     2. SettingsManager (load persisted)
     3. QualityManager (detect/apply)
     4. GameManager
     5. GC2ConnectionManager
     6. UIManager
   - Transition to MainMenu or Marina

3. MainMenu.unity:
   - Mode selection: Open Range / GSPro
   - Settings access
   - Connection status
   - Environment selection (future)

4. Full shot flow test:
   - GC2 connected
   - Shot received via USB
   - Shot validated and processed
   - Trajectory calculated
   - Ball animated
   - Landing effects
   - UI updated
   - History updated

5. Error handling:
   - Connection lost mid-session
   - Invalid shot data
   - Low memory warning
   - Battery warnings (mobile)

6. Final polish:
   - Loading indicators
   - Transition animations
   - Sound effects (optional)
   - Haptic feedback (mobile)

7. Platform testing checklist:
   - macOS Intel
   - macOS M1/M2
   - iPad Pro M1
   - Android tablet (Samsung Tab)
   - Test with real GC2 hardware

Write comprehensive end-to-end tests:
- Full session simulation
- Multiple shots
- Reconnection scenarios
- Quality tier switching
- All UI interactions
```

---

## Appendix A: Testing Strategy

Each prompt should include tests. Testing priorities:

1. **Unit Tests** (edit mode):
   - Data model serialization
   - Physics calculations
   - Protocol parsing
   - Unit conversions

2. **Integration Tests** (play mode):
   - Scene loading
   - Manager initialization
   - UI updates
   - Event flow

3. **End-to-End Tests** (requires hardware):
   - USB connection
   - Full shot flow
   - Platform-specific features

## Appendix B: Dependencies

External packages to add:
- Newtonsoft.Json (com.unity.nuget.newtonsoft-json)
- TextMeshPro (built-in)
- Input System (com.unity.inputsystem)

Native dependencies:
- libusb (macOS)
- DriverKit (iOS)
- Android USB Host API (built-in)

## Appendix C: Known Risks

1. **iPad DriverKit**: May require Apple approval process
2. **USB Permissions**: Platform-specific handling required
3. **Performance on low-end Android**: May need aggressive optimization
4. **GC2 Protocol variations**: May need adjustment for different firmware versions
