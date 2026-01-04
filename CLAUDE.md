# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**GC2 Connect Unity** is a cross-platform driving range simulator that connects to the Foresight GC2 launch monitor via USB. It runs on macOS (Intel + Apple Silicon), iPad (M1+ with DriverKit), and Android tablets (USB Host API).

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 Unity Application (C#)                       │
│                                                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │
│  │ Physics  │ │   Viz    │ │    UI    │ │   Services   │   │
│  │ Engine   │ │  System  │ │  System  │ │              │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │
│                              │                              │
│  ┌───────────────────────────┴───────────────────────────┐  │
│  │              IGC2Connection Interface                  │  │
│  └───────────────────────────┬───────────────────────────┘  │
│         ┌────────────────────┼────────────────────┐         │
│         ▼                    ▼                    ▼         │
│  ┌────────────┐      ┌────────────┐      ┌────────────┐    │
│  │   macOS    │      │    iPad    │      │  Android   │    │
│  │   Plugin   │      │   Plugin   │      │   Plugin   │    │
│  └────────────┘      └────────────┘      └────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow
1. Native USB plugin receives raw data from GC2 (`KEY=VALUE\n` format)
2. `GC2Protocol.Parse()` converts to `GC2ShotData`
3. `ShotProcessor.ProcessShot()` validates and runs physics simulation
4. `TrajectorySimulator.Simulate()` returns `ShotResult` with trajectory points
5. `BallController` animates ball through trajectory
6. UI components update with shot metrics

### Key Interfaces
- **IGC2Connection** - Platform-agnostic USB connection interface. All platform plugins implement this.
- **GC2ConnectionFactory** - Uses `#if` directives to create platform-specific connection at runtime.

## Namespaces

```csharp
OpenRange.Core          // GameManager, ShotProcessor, SessionManager, SettingsManager, SceneLoader
OpenRange.GC2           // USB connection, protocol parsing
OpenRange.Physics       // Trajectory simulation, aerodynamics
OpenRange.Visualization // Ball, camera, effects
OpenRange.UI            // MainMenuController, MarinaSceneController, all UI components
OpenRange.Network       // GSPro TCP client
OpenRange.Utilities     // MainThreadDispatcher
OpenRange.Editor        // SceneGenerator, editor tools
```

## Scene Structure

After running `OpenRange > Generate All Scenes` from Unity menu:

```
Assets/Scenes/
├── Bootstrap.unity     (index 0) - Initializes managers, loads MainMenu
├── MainMenu.unity      (index 1) - Title screen, navigation
└── Ranges/
    └── Marina.unity    (index 2) - Main driving range
```

**Bootstrap Scene** contains DontDestroyOnLoad managers:
- GameManager (with ShotProcessor, SessionManager children)
- SettingsManager
- MainThreadDispatcher
- BootstrapLoader (triggers initialization and scene load)

## Makefile Commands

The project includes a Makefile for common operations:

```bash
# Development
make run           # Open Unity with Bootstrap scene (main entry point)
make run-marina    # Open Unity with Marina scene (direct testing)

# Testing
make help          # Show all available targets
make test          # Run all tests (EditMode + PlayMode)
make test-edit     # Run EditMode tests only
make test-play     # Run PlayMode tests only
make test-physics  # Run physics validation tests only

# Building (basic)
make generate      # Regenerate all prefabs AND scenes from editor scripts
make build         # Build macOS standalone (runs tests + generate first)
make build-dev     # Build development build (runs generate first, skips tests)

# macOS Release Builds
make build-plugin  # Build native macOS plugin only
make build-app     # Full macOS app build (plugin + tests + Unity)
make build-app-dev # Development build (faster, debugging enabled)
make build-release # Release build with version from git tag
make package       # Create DMG for distribution

# Code Signing (requires APPLE_DEVELOPER_ID, APPLE_ID, APPLE_TEAM_ID, APPLE_APP_PASSWORD)
make sign          # Sign app bundle with Developer ID
make notarize      # Submit for notarization and staple ticket
make dmg           # Create signed DMG for distribution
make release-macos # Full pipeline: build, sign, notarize, DMG
make setup-signing # Setup keychain for CI (import certificates)
make cleanup-signing # Remove CI keychain after build

# Cleanup
make clean         # Remove build artifacts and test results
make clean-builds  # Remove only build outputs (keep test results)
```

**IMPORTANT: Always run `make test` before creating a PR.**
**NOTE: CLI tests require Unity to be closed (batchmode conflict). Use Test Runner in Unity if project is open.**

## Building for macOS

### Quick Build

```bash
# Development build (fastest)
make build-app-dev

# Full build with tests
make build-app

# Release build
make build-release
```

### Build Script Options

The comprehensive build script supports various options:

```bash
Scripts/build_macos.sh [options]
  --skip-tests      Skip running tests
  --skip-plugin     Use existing plugin
  --development     Development build with debugging
  --version=X.Y.Z   Set version explicitly
  --verbose         Show full output
```

### Build Output

Builds are created in `Builds/macOS/OpenRange.app`. Logs are saved to `Builds/logs/`.

For detailed build instructions, troubleshooting, and distribution steps, see [docs/BUILD_MACOS.md](docs/BUILD_MACOS.md).

## Build Commands

```bash
# Using Makefile (recommended)
make build         # Runs tests first, then builds

# Manual Unity CLI build (macOS)
/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath . \
  -buildTarget StandaloneOSX \
  -buildOSXUniversalPlayer Builds/macOS/OpenRange.app

# Native plugins (must build before Unity)
cd NativePlugins/macOS && ./build_mac_plugin.sh
cd NativePlugins/Android && ./gradlew assembleRelease
```

## Physics Validation

The physics engine must match the Nathan model within tolerance:

| Ball Speed | Launch | Spin | Expected Carry | Tolerance |
|------------|--------|------|----------------|-----------|
| 167 mph | 10.9° | 2686 rpm | 275 yds | ±5% |
| 160 mph | 11.0° | 3000 rpm | 259 yds | ±3% |
| 120 mph | 16.3° | 7097 rpm | 172 yds | ±5% |
| 102 mph | 24.2° | 9304 rpm | 136 yds | ±5% |

## GC2 Protocol Reference

```
Vendor ID:  0x2C79 (11385)
Product ID: 0x0110 (272)
Endpoint:   0x82 (INTERRUPT IN) - NOT bulk!

Message types:
- 0H: Shot data (process) - SPEED_MPH, ELEVATION_DEG, AZIMUTH_DEG, BACK_RPM, SIDE_RPM, etc.
- 0M: Device status (parse for FLAGS/BALLS) - used for GSPro readiness

Message format: KEY=VALUE pairs, newline-separated
Terminator: \n\t (newline + tab) indicates message complete
Wait for: BACK_RPM and SIDE_RPM before processing shot (early readings may be incomplete)

Key fields: SPEED_MPH, ELEVATION_DEG, AZIMUTH_DEG, SPIN_RPM, BACK_RPM, SIDE_RPM
HMT fields: CLUBSPEED_MPH, HPATH_DEG, VPATH_DEG, FACE_T_DEG, LOFT_DEG

Misread detection: Zero spin, BACK_RPM == 2222, SPEED_MPH < 1.1 or > 250
Speed range: 1.1 mph (putts) to 250 mph (max), non-putt shots require 3.4+ mph

Device status (0M): FLAGS == 7 = ready, BALLS > 0 = ball detected
```

## GSPro Integration

```
Protocol: TCP JSON (GSPro Open Connect API v1)
Port: 921
Heartbeat: Every 2 seconds when idle
Readiness: LaunchMonitorIsReady (from FLAGS), LaunchMonitorBallDetected (from BALLS)

Critical Implementation Notes:
- TCP_NODELAY: Must set NoDelay = true (disables Nagle's algorithm) ✅ Done
- Response handling: Shots get responses, heartbeats/status do NOT - don't block!
- Buffer management: Clear receive buffer before sends, parse first JSON only (Prompt 42)
- Graceful shutdown: Clean socket close required, GSPro doesn't handle abrupt disconnections
```

## Key Documentation

| Document | Purpose |
|----------|---------|
| `docs/PRD.md` | Product requirements, success metrics |
| `docs/TRD.md` | Technical architecture, component design |
| `docs/PHYSICS.md` | Nathan model, aerodynamic coefficients |
| `docs/GSPRO_API.md` | GSPro Open Connect JSON format |
| `docs/GC2_PROTOCOL.md` | GC2 USB text protocol specification |
| `docs/USB_PLUGINS.md` | Native plugin implementation per platform |
| `plan.md` | Prompt-based implementation plan |
| `todo.md` | Current development state tracking |

## Current Implementation Status

**Implemented:**
- Physics engine (TrajectorySimulator, Aerodynamics, GroundPhysics) - carry validated, bounce improved (PR #33)
- GC2 data models and protocol parser
- GameManager with connection lifecycle
- MainThreadDispatcher for native callbacks
- ShotProcessor with validation and physics integration (PR #1)
- SessionManager with shot history and statistics (PR #2)
- SettingsManager with PlayerPrefs persistence (PR #4)
- PlatformManager and QualityManager with dynamic quality adjustment (PR #8)
- Scene infrastructure: SceneLoader, BootstrapLoader, scene controllers (PR #6)
- SceneGenerator editor tool for creating Unity scenes
- Golf ball prefab with BallVisuals, BallController, BallSpinner (PR #9, #11)
- TrajectoryRenderer with quality tiers (PR #13)
- Camera system with Follow, Orbit, Static, TopDown modes (PR #15)
- TestShotWindow editor tool for development testing
- Landing markers and effects with EffectsManager (PR #17)
- Marina environment components: EnvironmentManager, DistanceMarker, TargetGreen, TeeMat (PR #19)
- UI foundation: UIManager, UITheme, ResponsiveLayout, SafeAreaHandler (PR #21)
- Shot Data Bar with DataTile components (PR #23)
- Club Data Panel with SwingPathIndicator and AttackAngleIndicator (PR #25)
- Connection Status UI with indicator, panel, and toasts (PR #27)
- Session Info Panel with shot history and detail modal (PR #29)
- Settings Panel with SettingToggle, SettingSlider, SettingDropdown components (PR #31)
- GC2 device status parsing for GSPro integration (PR #39)
  - GC2DeviceStatus struct with IsReady, BallDetected, BallPosition
  - ParseDeviceStatus() for 0M message parsing
  - GameManager device status tracking with OnDeviceStatusChanged event
- TCP connection for Editor testing (PR #41)
  - GC2TCPConnection implementing IGC2Connection with Server/Client modes
  - GC2TCPListener standalone server for testing
  - GC2TestWindow editor window for sending test shots/status
- GSPro client for shot relay (PR #43)
  - GSProClient implementing IGSProClient with TCP connection
  - GSProMessage with BallData, ClubData, ShotDataOptions for JSON protocol
  - GSProModeUI for mode toggle, connection status, device readiness
  - 2-second heartbeat with LaunchMonitorIsReady/BallDetected flags
- macOS native plugin structure (PR #45)
  - GC2MacPlugin.h - C interface header with complete API
  - GC2MacPlugin.mm - Stub implementation with libusb init, device detection
  - Xcode project with bundle output, arm64 build
  - build_mac_plugin.sh - Build script with architecture detection
  - Weak-linked UnitySendMessage for Unity callback support
- macOS USB read loop implementation (PR #47)
  - Complete libusb integration with INTERRUPT IN endpoint (0x82)
  - 0H shot parsing with field accumulation, 0M device status parsing
  - Misread detection (zero spin, 2222 error, speed range)
  - Message terminator (\n\t) handling
- macOS C# bridge (PR #49) ✅ **Tested with real GC2 hardware**
  - GC2MacConnection.cs implementing IGC2Connection as MonoBehaviour
  - DllImport declarations for all GC2MacPlugin.bundle functions
  - Function pointer callbacks with `[MonoPInvokeCallback]` for IL2CPP compatibility
  - JSON parsing for shot data and device status via Unity's JsonUtility
  - Thread-safe event dispatching via MainThreadDispatcher
  - **IL2CPP fix**: UnitySendMessage doesn't work in IL2CPP builds - use function pointer callbacks instead
  - **JSON field fix**: Native plugin field names must match C# property names exactly for JsonUtility
- Ball Ready Indicator UI (PR #51)
  - BallReadyIndicator.cs showing device ready and ball detected status
  - Visual states: Disconnected (gray), Warming Up (yellow), Place Ball (green outline), Ready (solid green)
  - Pulse animation when ready to hit
  - BallReadyIndicatorGenerator.cs editor tool for prefab creation

- Android native plugin project structure (PR #65)
  - GC2Plugin.kt - Main entry point with UnitySendMessage callbacks
  - GC2Device.kt - Async UsbRequest pattern with 4 queued requests
  - GC2Protocol.kt - 0H shot parsing, 0M status parsing, misread detection
  - build_android_plugin.sh - Gradle build script
  - AAR built and copied to Assets/Plugins/Android/

**Not yet implemented:**
- Android native plugin C# bridge (Prompts 24-25)
- iPad native plugin (Prompts 26-28)

## Editor Tools

| Menu Item | Description |
|-----------|-------------|
| `OpenRange > Generate All Scenes` | Creates Bootstrap, MainMenu, Marina scenes with prefabs |
| `OpenRange > Generate Bootstrap Scene` | Creates only Bootstrap.unity |
| `OpenRange > Generate MainMenu Scene` | Creates only MainMenu.unity |
| `OpenRange > Generate Marina Scene` | Creates Marina.unity with GolfBall, TrajectoryLine, CameraRig, EffectsManager |
| `OpenRange > Update Build Settings` | Configures scene order in build settings |
| `OpenRange > Create Golf Ball Prefab` | Creates GolfBall.prefab and materials |
| `OpenRange > Create Trajectory Line Prefab` | Creates TrajectoryLine.prefab and materials |
| `OpenRange > Create Camera Rig Prefab` | Creates CameraRig.prefab with camera components |
| `OpenRange > Create Landing Marker Prefab` | Creates LandingMarker.prefab with ring and text |
| `OpenRange > Create Landing Dust Prefab` | Creates LandingDust.prefab particle system |
| `OpenRange > Create All Landing Effects` | Creates both landing effect prefabs |
| `OpenRange > Create URP Quality Assets` | Creates Low/Medium/High URP pipeline assets |
| `OpenRange > Create UI Canvas Prefab` | Creates UICanvas.prefab with responsive layout |
| `OpenRange > Create Toast Prefab` | Creates Toast.prefab for notifications |
| `OpenRange > Create All UI Prefabs` | Creates both UI prefabs |
| `OpenRange > Create Environment Materials` | Creates grass, water, tee mat materials |
| `OpenRange > Create Distance Marker Prefab` | Creates DistanceMarker.prefab |
| `OpenRange > Create Target Green Prefab` | Creates TargetGreen.prefab with flag |
| `OpenRange > Create Tee Mat Prefab` | Creates TeeMat.prefab |
| `OpenRange > Create Data Tile Prefab` | Creates DataTile.prefab for shot metrics |
| `OpenRange > Create Shot Data Bar Prefab` | Creates ShotDataBar.prefab with 10 tiles |
| `OpenRange > Create All Shot Data Bar Prefabs` | Creates both shot data bar prefabs |
| `OpenRange > Create Club Data Panel Prefab` | Creates ClubDataPanel.prefab for HMT data |
| `OpenRange > Create Swing Path Indicator Prefab` | Creates SwingPathIndicator.prefab |
| `OpenRange > Create Attack Angle Indicator Prefab` | Creates AttackAngleIndicator.prefab |
| `OpenRange > Create All Club Data Panel Prefabs` | Creates all HMT panel prefabs |
| `OpenRange > Create Connection Status Prefab` | Creates ConnectionStatus.prefab (top-right indicator) |
| `OpenRange > Create Connection Panel Prefab` | Creates ConnectionPanel.prefab (modal) |
| `OpenRange > Create All Connection UI Prefabs` | Creates all connection UI prefabs |
| `OpenRange > Create Session Info Panel Prefab` | Creates SessionInfoPanel.prefab (top-left compact stats) |
| `OpenRange > Create Shot History Panel Prefab` | Creates ShotHistoryPanel.prefab (expandable shot list) |
| `OpenRange > Create Shot History Item Prefab` | Creates ShotHistoryItem.prefab (shot row template) |
| `OpenRange > Create Shot Detail Modal Prefab` | Creates ShotDetailModal.prefab (full shot details) |
| `OpenRange > Create All Session Info Prefabs` | Creates all session info UI prefabs |
| `OpenRange > Create Setting Toggle Prefab` | Creates SettingToggle.prefab for boolean settings |
| `OpenRange > Create Setting Slider Prefab` | Creates SettingSlider.prefab for numeric ranges |
| `OpenRange > Create Setting Dropdown Prefab` | Creates SettingDropdown.prefab for options |
| `OpenRange > Create Settings Panel Prefab` | Creates SettingsPanel.prefab with all sections |
| `OpenRange > Create All Settings Panel Prefabs` | Creates all settings panel prefabs |
| `OpenRange > Test Shot Window` | Opens editor window for firing test shots (Play Mode) |
| `OpenRange > GC2 Test Window` | Opens TCP test window for simulating GC2 connection (Server/Client modes) |
| `OpenRange > Create GSPro Mode UI Prefab` | Creates GSProModeUI.prefab for mode toggle and connection status |
| `OpenRange > Create Ball Ready Indicator Prefab` | Creates BallReadyIndicator.prefab showing device/ball status |
| `OpenRange > Create Test Shot Panel Prefab` | Creates TestShotPanel.prefab for runtime test shots (works in builds) |

## Local Development on macOS

### First-Time Setup

1. **Clone and open project** in Unity Hub (Unity 6000.3.2f1)

2. **Configure URP render pipeline** (critical - materials will be pink without this):
   - Go to **Edit > Project Settings > Graphics**
   - Set **Default Render Pipeline** to a URP asset (e.g., `Assets/Settings/URP-HighQuality.asset`)
   - If no URP assets exist, run **OpenRange > Create URP Quality Assets** first

3. **Create Ball tag**:
   - Go to **Edit > Project Settings > Tags and Layers**
   - Add a new tag called `Ball`

4. **Generate prefabs** (run these menu commands):
   - **OpenRange > Create Golf Ball Prefab**
   - **OpenRange > Create Trajectory Line Prefab**
   - **OpenRange > Create Camera Rig Prefab**
   - **OpenRange > Create All Landing Effects**

5. **Generate scenes**:
   - **OpenRange > Generate All Scenes**
   - **OpenRange > Update Build Settings**

### Running the App

**Full app flow:**
1. Open `Assets/Scenes/Bootstrap.unity`
2. Press Play
3. Click "Open Range" to enter Marina

**Direct development (skip menus):**
1. Open `Assets/Scenes/Ranges/Marina.unity`
2. Press Play
3. Open **OpenRange > Test Shot Window**
4. Select preset (Driver, 7-Iron, Wedge) and click **Fire Test Shot**

### Troubleshooting

| Issue | Solution |
|-------|----------|
| All materials pink/magenta | URP not configured - set Default Render Pipeline in Graphics settings |
| "Tag: Ball is not defined" | Create "Ball" tag in Tags and Layers settings |
| Scene not found error | Run **OpenRange > Update Build Settings** |
| Ball doesn't animate | Ensure prefabs generated and Marina scene regenerated |
| ShotProcessor not found | Start from Bootstrap.unity or regenerate Marina scene |

## Dependencies

- Unity 6 LTS (6000.3.2f1) with URP
- TextMeshPro, Input System, Newtonsoft JSON
- **macOS**: libusb 1.0.26 (bundled in plugin)
- **iPad**: DriverKit (requires Apple entitlement approval)
- **Android**: USB Host API (system)

## Development Guidelines

### Assembly Definitions

This project uses Assembly Definitions (`.asmdef`) for code organization. When adding new features:

1. **Adding Unity module dependencies**: If using Unity APIs like `ParticleSystem`, `TextMeshPro`, etc., ensure the module is in `Packages/manifest.json`:
   ```json
   "com.unity.modules.particlesystem": "1.0.0"
   ```

2. **Adding assembly references**: If using external assemblies (e.g., TextMeshPro), add to `OpenRange.asmdef`:
   ```json
   "references": [
       "Unity.TextMeshPro"
   ]
   ```

3. **Test assemblies**: Also update `OpenRange.Tests.EditMode.asmdef` and `OpenRange.Tests.PlayMode.asmdef` with the same references if tests use those APIs.

### Scene Integration Checklist

When creating new visual/UI components that need to be in scenes:

1. **Create the component script** in `Assets/Scripts/Visualization/` or `Assets/Scripts/UI/`
2. **Create an editor generator** in `Assets/Editor/` to create prefabs
3. **Update the scene controller** (e.g., `MarinaSceneController.cs`):
   - Add `[SerializeField] private YourComponent _yourComponent;`
   - Clear it in `InitializeScene()` if needed
   - Update it in `OnShotProcessed()` or appropriate event handler
4. **Update SceneGenerator.cs** to:
   - Load and instantiate the prefab
   - Position it correctly (anchor, pivot, sizeDelta)
   - Wire up the reference to the scene controller via `SerializedObject`
5. **Regenerate the scene** after creating prefabs: `OpenRange > Generate Marina Scene`

**CRITICAL**: Steps 3 and 4 are often forgotten. Without them, the prefab won't appear in the scene even if you create it with the editor tool. Always verify:
- The scene controller has a serialized field for the new component
- The SceneGenerator instantiates the prefab AND wires it to the controller

### Prefab Creation Pattern

Follow the established pattern in editor tools:
1. Create materials first (with URP shader fallbacks)
2. Create GameObjects with components
3. Use `SerializedObject` to wire up private serialized fields
4. Save as prefab with `PrefabUtility.SaveAsPrefabAsset()`
5. Clean up with `Object.DestroyImmediate()`

### Unity UI Layout System

When creating UI prefabs programmatically, the layout system requires careful configuration to enforce size constraints.

#### Critical LayoutGroup Settings

**VerticalLayoutGroup / HorizontalLayoutGroup** have four critical properties:

| Property | Effect when `true` | Effect when `false` |
|----------|-------------------|---------------------|
| `childControlWidth` | Layout enforces width constraints | Children use natural width |
| `childControlHeight` | Layout enforces height constraints | Children use natural height |
| `childForceExpandWidth` | Children expand to fill available width | Children use preferred width |
| `childForceExpandHeight` | Children expand to fill available height | Children use preferred height |

**Common mistake**: Setting `childControlHeight = false` causes children to ignore `preferredHeight` and `minHeight` constraints, resulting in oversized elements.

**Recommended settings for compact layouts:**
```csharp
layoutGroup.childControlWidth = true;
layoutGroup.childControlHeight = true;
layoutGroup.childForceExpandWidth = false;  // or true depending on needs
layoutGroup.childForceExpandHeight = false;
```

#### Size Constraint Hierarchy

For reliable sizing, set constraints at multiple levels:

1. **RectTransform.sizeDelta** - The actual rendered size
   ```csharp
   rect.sizeDelta = new Vector2(width, height);
   ```

2. **LayoutElement** - Constraints for the layout system
   ```csharp
   layout.minWidth = width;
   layout.minHeight = height;
   layout.preferredWidth = width;
   layout.preferredHeight = height;
   layout.flexibleWidth = 0f;   // 0 = don't stretch
   layout.flexibleHeight = 0f;
   ```

**Both are needed!** `LayoutElement` controls layout calculation, but `RectTransform.sizeDelta` is what Unity actually renders.

#### Preventing Unwanted Stretching

To prevent elements from expanding:
- Set `flexibleWidth = 0f` and `flexibleHeight = 0f` on `LayoutElement`
- Set `childForceExpandWidth/Height = false` on parent `LayoutGroup`
- Use fixed `minWidth/minHeight` values

#### Prefab and Scene Regeneration

The `make generate` target now regenerates **both** prefabs and scenes:
```bash
make generate  # Regenerates all prefabs, then all scenes
```

This calls:
1. `SceneGenerator.GenerateAllPrefabs()` - All visualization, environment, and UI prefabs
2. `SceneGenerator.GenerateAllScenes()` - Bootstrap, MainMenu, and Marina scenes

To regenerate just a specific prefab or scene via CLI:
```bash
# Regenerate specific prefab
/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -executeMethod OpenRange.Editor.GSProModeUIGenerator.CreatePrefab -quit -logFile -

# Regenerate specific scene
/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -executeMethod OpenRange.Editor.SceneGenerator.GenerateMarinaScene -quit -logFile -
```

#### ContentSizeFitter Interaction

When using `ContentSizeFitter` with `VerticalFit = PreferredSize`:
- The parent will size itself based on children's `preferredHeight`
- Children must have `childControlHeight = true` on parent for this to work correctly
- Without it, children report incorrect sizes and the fitter calculates wrong

#### TMP_Dropdown (TextMeshPro Dropdown) Setup

Creating dropdowns programmatically requires careful attention to the template hierarchy:

1. **ScrollRect wiring is critical**:
   ```csharp
   var scrollRect = templateGo.AddComponent<ScrollRect>();
   scrollRect.viewport = viewportRect;    // MUST be assigned!
   scrollRect.content = contentRect;       // MUST be assigned!
   ```
   Without this wiring, dropdown items won't display or will show white areas.

2. **Z-order for dropdown overlay**:
   ```csharp
   var templateCanvas = templateGo.AddComponent<Canvas>();
   templateCanvas.overrideSorting = true;
   templateCanvas.sortingOrder = 100;  // Higher than normal UI
   ```

3. **Font limitations with Unicode**:
   - LiberationSans SDF (default TMP font) doesn't include many Unicode symbols
   - Unicode checkmark `\u2713` renders as hollow square `\u25A1`
   - Use Image components instead of text for symbols like checkmarks
   - Or use simple ASCII: "X" instead of "✕", styled with bold

4. **Template structure** (TMP_Dropdown expects this exact hierarchy):
   ```
   Dropdown (TMP_Dropdown)
   └── Template (inactive by default)
       └── Viewport (Mask + Image)
           └── Content (VerticalLayoutGroup)
               └── Item (Toggle template)
                   ├── Item Checkmark (Image, not text!)
                   └── Item Label (TextMeshProUGUI)
   ```

#### Button Click Area Issues

By default, TextMeshProUGUI has `raycastTarget = true`, which can intercept clicks intended for the parent Button:

```csharp
// BAD: Text intercepts clicks before they reach Button
var text = buttonGo.AddComponent<TextMeshProUGUI>();
// text.raycastTarget defaults to true!

// GOOD: Let clicks pass through to the Button
var text = buttonGo.AddComponent<TextMeshProUGUI>();
text.raycastTarget = false;  // Clicks go to Button
```

This is a common cause of "button only works when clicking on edges" issues.

### Coordinate System

The physics engine and Unity use different coordinate systems:

| Axis | Physics (Trajectory) | Unity |
|------|---------------------|-------|
| X | Forward (yards) | Right (lateral) |
| Y | Height (feet) | Up (height) |
| Z | Lateral (yards) | Forward (distance) |

When converting trajectory points to Unity world positions, swap X and Z:
```csharp
// Physics X (forward) → Unity Z, Physics Z (lateral) → Unity X
new Vector3(
    point.Position.z * yardsToMeters,  // lateral → X
    point.Position.y * feetToMeters,   // height → Y
    point.Position.x * yardsToMeters   // forward → Z
);
```

### Testing Notes

- Tests run via `make test` require Unity to be closed (batchmode conflict)
- If Unity is open, use the Test Runner window instead
- **Claude can quit Unity to run tests** using AppleScript: `osascript -e 'quit app "Unity"'`
- Add `using UnityEngine.TestTools;` for `LogAssert` in tests
- Use `Is.EqualTo().Or.EqualTo()` instead of `Is.AnyOf()` (not available in NUnit 3.x)

### Scene Integration Validation

The project includes `SceneIntegrationValidationTests.cs` that validates prefab existence and wiring. These tests run automatically in CI and catch common issues like:

- Missing prefabs (not generated via editor tools)
- Broken script references (caused by moving/renaming scripts)
- Missing scene controller fields
- Missing prefab wiring in SceneGenerator

**Key tests:**
- `AllRequiredPrefabs_Exist` - Verifies all 18 required prefabs exist
- `AllSubComponentPrefabs_Exist` - Verifies 8 sub-component prefabs
- `SceneGenerator_ReferencesAllExpectedPrefabs` - Checks SceneGenerator.cs mentions all prefabs
- `MarinaSceneController_HasAllExpectedSerializedFields` - Validates 10+ serialized fields

**Run locally:**
```bash
make test-edit  # Runs all EditMode tests including integration tests

# Or run just the scene integration tests:
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode \
  -testFilter "SceneIntegrationValidationTests" -logFile -
```

**Common fix for broken prefab script references:**
If a prefab shows `m_Script: {fileID: 0}` in YAML, the script reference is broken. This often happens when:
1. A MonoBehaviour is defined in the same file as another class
2. The script file was moved or renamed

Fix: Delete the prefab and regenerate via menu command (e.g., `OpenRange > Create Toast Prefab`)

### Native Plugin Development (IL2CPP)

When building native plugins for IL2CPP (standalone macOS/iOS builds):

1. **UnitySendMessage doesn't work in IL2CPP** - The weak-linked `UnitySendMessage` symbol is NULL in IL2CPP builds. Use function pointer callbacks instead:
   ```csharp
   // Define delegate matching native callback signature
   private delegate void NativeShotCallback(string jsonData);

   // Import callback registration function
   [DllImport(PluginName)]
   private static extern void GC2Mac_SetShotCallback(NativeShotCallback callback);

   // Static callback method with MonoPInvokeCallback attribute
   [MonoPInvokeCallback(typeof(NativeShotCallback))]
   private static void OnNativeShotCallbackStatic(string jsonData)
   {
       if (s_instance != null) s_instance.OnNativeShotReceived(jsonData);
   }

   // Register in initialization
   GC2Mac_SetShotCallback(OnNativeShotCallbackStatic);
   ```

2. **JSON field names must match exactly** - Unity's `JsonUtility` requires field names to match C# property names exactly. Native plugin JSON:
   ```
   ✗ BallSpeedMph, ShotNumber, SpinRpm
   ✓ BallSpeed, ShotId, TotalSpin (matching GC2ShotData properties)
   ```

3. **Player.log location** - Unity standalone player logs are at:
   ```
   ~/Library/Logs/<CompanyName>/<ProductName>/Player.log
   ```

4. **Debugging standalone builds**:
   - Run from terminal to see console output: `./OpenRange.app/Contents/MacOS/<executable>`
   - Or check Player.log for errors
   - Add debug logging in native plugin with `NSLog()` or similar

5. **Async USB for packet handling** - The GC2 sends packets 1-2ms apart (4+ packets per burst). Synchronous reads can't keep up, causing kernel buffer overflow and packet loss.

   **macOS (libusb):**
   ```objc
   // Allocate multiple transfers for continuous reading
   libusb_fill_interrupt_transfer(transfer, handle, endpoint, buffer, size, callback, user_data, 0);
   libusb_submit_transfer(transfer);

   // In callback: process data, then immediately re-submit
   static void LIBUSB_CALL TransferCallback(struct libusb_transfer *transfer) {
       // Process transfer->buffer
       if (g_isRunning) libusb_submit_transfer(transfer);  // Re-queue
   }

   // Event loop on background thread
   while (g_isRunning) {
       libusb_handle_events_timeout(ctx, &tv);  // Callbacks invoked here
   }
   ```

   **Android (UsbRequest):**
   ```kotlin
   // Queue 4 async UsbRequest objects
   for (request in usbRequests) {
       val buffer = requestBuffers[request]!!
       buffer.clear()
       buffer.limit(BUFFER_SIZE)
       request.queue(buffer)  // Non-blocking
   }

   // Reader thread waits for any completion
   while (running.get()) {
       val completedRequest = connection.requestWait()  // Blocking
       val buffer = requestBuffers[completedRequest]!!
       // Copy data to ConcurrentLinkedQueue, then immediately re-queue
       buffer.clear()
       buffer.limit(BUFFER_SIZE)
       completedRequest.queue(buffer)
   }

   // Separate processor thread consumes from queue
   while (running.get()) {
       val data = packetQueue.poll()
       if (data != null) processReceivedData(data)
   }
   ```

   Both platforms keep 4 requests queued at all times - when one completes, it's immediately re-submitted while others continue receiving.

### Batchmode Scene Generation

**Critical Issue:** `EditorUtility.DisplayDialog()` returns `false` in batchmode (CLI), causing menu methods to exit early without doing work.

**Symptom:** `make generate` runs but scenes aren't updated. Changes made in generators don't appear in scenes.

**Fix:** Check `Application.isBatchMode` to skip dialogs when running from CLI:
```csharp
[MenuItem("OpenRange/Generate All Scenes")]
public static void GenerateAllScenes()
{
    // In batchmode (CLI), skip the dialog and generate directly
    if (!Application.isBatchMode)
    {
        if (!EditorUtility.DisplayDialog("Title", "Message", "OK", "Cancel"))
        {
            return;  // User cancelled
        }
    }
    // Proceed with generation...
}
```

This pattern applies to any editor menu item that uses `DisplayDialog` for confirmation.

### Unity Built-in Sprites

For common UI icons (checkmarks, arrows, etc.), use Unity's built-in sprites instead of Unicode or custom assets:

```csharp
var checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
image.sprite = checkmark;
image.color = UITheme.AccentGreen;
image.preserveAspect = true;
```

Available built-in sprites (path prefix: `UI/Skin/`):
- `Checkmark.psd` - Checkmark icon for toggles/dropdowns
- `Background.psd` - Default UI background
- `UISprite.psd` - Generic UI sprite
- `InputFieldBackground.psd` - Input field background
- `Knob.psd` - Slider knob
- `DropdownArrow.psd` - Dropdown arrow indicator
- `UIMask.psd` - Mask sprite

This is more reliable than Unicode symbols which may not render correctly with all fonts.
