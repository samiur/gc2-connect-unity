# CLAUDE.md - GC2 Connect Unity

## Project Overview

**GC2 Connect Unity** is a cross-platform driving range simulator built with Unity that connects to the Foresight GC2 launch monitor via USB. It provides GSPro-quality 3D visualization of ball flight with physics-accurate trajectory simulation.

### Target Platforms
- **macOS** (Intel + Apple Silicon) - Primary development platform
- **iPad** (M1+ with iPadOS 16+) - Via DriverKit USB
- **Android tablets** (8.0+ with USB Host) - Via USB Host API

### Key Features
- Native USB connection to GC2 launch monitor on all platforms
- Physics-accurate ball flight (Nathan model)
- GSPro-quality 3D driving range environment
- Optional GSPro relay mode (send shots to GSPro via TCP)
- Offline practice capability

---

## Quick Reference

### GC2 USB Identifiers
```
Vendor ID:  0x2C79 (11385)
Product ID: 0x0110 (272)
```

### GSPro Connection
```
Default Port: 921
Protocol: TCP JSON (GSPro Open Connect API v1)
```

### Key Documentation
| Document | Location | Purpose |
|----------|----------|---------|
| PRD | `docs/PRD.md` | Product requirements |
| TRD | `docs/TRD.md` | Technical architecture |
| Physics Spec | `docs/PHYSICS.md` | Ball flight physics |
| GSPro API | `docs/GSPRO_API.md` | GSPro Open Connect protocol |
| GC2 Protocol | `docs/GC2_PROTOCOL.md` | GC2 USB data format |
| USB Plugins | `docs/USB_PLUGINS.md` | Native plugin guide |

---

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

---

## Code Conventions

### Namespaces
```csharp
OpenRange.Core          // GameManager, ShotProcessor, SessionManager
OpenRange.GC2           // USB connection, protocol parsing
OpenRange.Physics       // Trajectory simulation, aerodynamics
OpenRange.Visualization // Ball, camera, effects
OpenRange.UI            // All UI components
OpenRange.Network       // GSPro TCP client
```

### File Naming
- Scripts: `PascalCase.cs` (e.g., `TrajectorySimulator.cs`)
- Interfaces: `IPascalCase.cs` (e.g., `IGC2Connection.cs`)
- Native plugins: Platform prefix (e.g., `GC2MacPlugin.mm`)

### Unity Conventions
- Use `[SerializeField]` for inspector-exposed private fields
- Prefer composition over inheritance
- Use ScriptableObjects for configuration data
- Use events/delegates for loose coupling

---

## Key Classes

### Core
- `GameManager` - Main application controller, scene management
- `ShotProcessor` - Receives shots, triggers physics and visualization
- `SessionManager` - Tracks session state, shot history
- `SettingsManager` - Persistent settings via JSON

### GC2 Connection
- `IGC2Connection` - Platform-agnostic interface
- `GC2ConnectionFactory` - Creates platform-specific implementation
- `GC2Protocol` - Parses GC2 text protocol to `GC2ShotData`
- `GC2ShotData` - Shot data model (ball + club data)

### Physics
- `TrajectorySimulator` - Main physics engine (RK4 integration)
- `Aerodynamics` - Drag/lift coefficients, Reynolds number
- `GroundPhysics` - Bounce and roll simulation
- `PhysicsConstants` - Ball properties, atmosphere, tables

### Visualization
- `BallController` - Ball instantiation and animation
- `TrajectoryRenderer` - Line renderer for ball path
- `CameraController` - Camera modes (follow, static, overhead)
- `EnvironmentManager` - Range environment switching

### UI
- `ShotDataBar` - Bottom data display (GSPro style)
- `ClubDataPanel` - HMT club metrics (right panel)
- `SessionInfoPanel` - Time, shot count (top left)
- `ConnectionStatusUI` - GC2/GSPro connection state

---

## Build Commands

```bash
# Unity Editor (open project)
# File > Build Settings > Select Platform > Build

# Command line (requires Unity installed)
/Applications/Unity/Hub/Editor/2022.3.x/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath . \
  -buildTarget StandaloneOSX \
  -buildOSXUniversalPlayer Builds/macOS/OpenRange.app
```

---

## Testing

### Physics Validation
The physics engine must pass these validation tests (Nathan model):

| Ball Speed | Launch | Spin | Expected Carry | Tolerance |
|------------|--------|------|----------------|-----------|
| 167 mph | 10.9° | 2686 rpm | 275 yds | ±5% |
| 160 mph | 11.0° | 3000 rpm | 259 yds | ±3% |
| 120 mph | 16.3° | 7097 rpm | 172 yds | ±5% |
| 102 mph | 24.2° | 9304 rpm | 136 yds | ±5% |

### Manual Testing
1. Connect GC2 via USB
2. Verify connection status shows "Connected"
3. Hit a shot on GC2
4. Verify ball flight animates correctly
5. Verify data bar shows correct metrics
6. Verify carry distance within tolerance

---

## Common Tasks

### Adding a New Platform
1. Create native plugin in `NativePlugins/{platform}/`
2. Implement platform-specific USB communication
3. Create C# bridge in `Assets/Scripts/GC2/Platforms/{Platform}/`
4. Implement `IGC2Connection` interface
5. Update `GC2ConnectionFactory` with platform directive

### Modifying Physics
1. Edit `Assets/Scripts/Physics/` classes
2. Run validation tests against Nathan model
3. Verify in-game behavior matches expectations

### Adding UI Elements
1. Create component in `Assets/Scripts/UI/`
2. Create prefab in `Assets/Prefabs/UI/`
3. Wire up in scene hierarchy
4. Use `ResponsiveLayout` for multi-device support

---

## Dependencies

### Unity Packages (via Package Manager)
- Universal Render Pipeline (URP)
- TextMeshPro
- Input System
- Newtonsoft JSON (com.unity.nuget.newtonsoft-json)

### Native Dependencies
- **macOS**: libusb (bundled)
- **iPad**: DriverKit framework (system)
- **Android**: USB Host API (system)

---

## Known Issues / TODO

- [ ] iPad DriverKit requires Apple entitlement approval
- [ ] Android USB permission flow needs testing on various devices
- [ ] Quality tier auto-detection needs refinement
- [ ] GSPro relay mode not yet implemented

---

## Resources

- [Unity URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
- [Prof. Alan Nathan's Trajectory Calculator](http://baseball.physics.illinois.edu/trajectory-calculator.html)
- [GSPro Open Connect API](https://gsprogolf.com/openconnect)
- [Apple DriverKit Documentation](https://developer.apple.com/documentation/driverkit)
- [Android USB Host](https://developer.android.com/guide/topics/connectivity/usb/host)
