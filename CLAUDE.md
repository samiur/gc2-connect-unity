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
OpenRange.Core          // GameManager, ShotProcessor, SessionManager
OpenRange.GC2           // USB connection, protocol parsing
OpenRange.Physics       // Trajectory simulation, aerodynamics
OpenRange.Visualization // Ball, camera, effects
OpenRange.UI            // All UI components
OpenRange.Network       // GSPro TCP client
```

## Makefile Commands

The project includes a Makefile for common operations:

```bash
make help          # Show all available targets
make test          # Run all tests (EditMode + PlayMode)
make test-edit     # Run EditMode tests only
make test-physics  # Run physics validation tests only
make build         # Build macOS standalone (runs tests first)
make clean         # Remove build artifacts and test results
```

**IMPORTANT: Always run `make test` before creating a PR.**

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
Endpoint:   0x81 (Bulk IN)

Message format: KEY=VALUE pairs, newline-separated, double-newline terminated
Key fields: SPEED_MPH, ELEVATION_DEG, AZIMUTH_DEG, SPIN_RPM, BACK_RPM, SIDE_RPM
HMT fields: CLUBSPEED_MPH, HPATH_DEG, VPATH_DEG, FACE_T_DEG, LOFT_DEG
```

## GSPro Integration

```
Protocol: TCP JSON (GSPro Open Connect API v1)
Port: 921
Heartbeat: Every 2 seconds when idle
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
- Physics engine (TrajectorySimulator, Aerodynamics, GroundPhysics) - validated against Nathan model
- GC2 data models and protocol parser
- GameManager with connection lifecycle
- MainThreadDispatcher for native callbacks
- ShotProcessor with validation and physics integration (PR #1)
- SessionManager with shot history and statistics (PR #2)

**Not yet implemented:**
- SettingsManager
- All visualization (ball, camera, effects)
- All UI components
- Native USB plugins
- GSPro relay client
- Unity scenes

## Dependencies

- Unity 6 LTS (6000.3.2f1) with URP
- TextMeshPro, Input System, Newtonsoft JSON
- **macOS**: libusb 1.0.26 (bundled in plugin)
- **iPad**: DriverKit (requires Apple entitlement approval)
- **Android**: USB Host API (system)
