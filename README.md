# GC2 Connect Unity

A cross-platform driving range simulator for the Foresight GC2 launch monitor, built with Unity.

![Platforms](https://img.shields.io/badge/platforms-macOS%20%7C%20iPad%20%7C%20Android-blue)
![Unity](https://img.shields.io/badge/Unity-6000.3.2f1-green)
![License](https://img.shields.io/badge/license-MIT-orange)

## Features

- 🎯 **GSPro-Quality Visuals** - Beautiful 3D driving range environment
- 📊 **Physics-Accurate** - Nathan model ball flight simulation
- 🔌 **Direct USB** - Native USB connection to GC2 on all platforms
- 📱 **Cross-Platform** - Same app on Mac, iPad, and Android
- 🌐 **GSPro Mode** - Optional relay to GSPro for course play
- ✈️ **Offline** - Full functionality without network

## Platforms

| Platform | Status | USB Support |
|----------|--------|-------------|
| macOS (Intel + Apple Silicon) | 🚧 In Development | libusb |
| iPad Pro (M1+) | 🚧 In Development | DriverKit |
| Android Tablets | 🚧 In Development | USB Host API |
| Windows | 🔜 Planned | - |

> **Development Status**: Phase 4 in progress. Core visualization complete - ball flight animation, trajectory rendering, camera system, and landing effects all functional. You can fire test shots and watch the full ball flight with landing markers and dust particles. Working on environment polish and UI. See [todo.md](todo.md) for current status and [QUICKSTART.md](QUICKSTART.md) for setup instructions.

## Quick Start

### Requirements

- Unity 6 (6000.3.2f1)
- macOS (for development) or target platform
- Foresight GC2 launch monitor (optional - can test without hardware)

### Development Setup (No Hardware Required)

See **[QUICKSTART.md](QUICKSTART.md)** for detailed setup instructions. Quick overview:

1. Clone and open in Unity Hub
2. Configure URP render pipeline (Edit > Project Settings > Graphics)
3. Create "Ball" tag (Edit > Project Settings > Tags and Layers)
4. Run menu commands: **OpenRange > Generate All Scenes**
5. Open Bootstrap.unity and press Play
6. Use **OpenRange > Test Shot Window** to fire test shots

### Building for Release

1. Ensure all tests pass: `make test`
2. Select target platform (macOS, iOS, or Android)
3. Build: `make build` or via Unity Build Settings

### Usage with GC2 Hardware

1. Connect GC2 to your device via USB
2. Launch the app
3. Grant USB permission when prompted
4. Hit shots and watch them fly!

## Documentation

| Document | Description |
|----------|-------------|
| [QUICKSTART.md](QUICKSTART.md) | **Start here** - Development setup guide |
| [CLAUDE.md](CLAUDE.md) | Claude Code reference, local dev instructions |
| [plan.md](plan.md) | Implementation prompt plan |
| [todo.md](todo.md) | Development progress tracking |
| [docs/PRD.md](docs/PRD.md) | Product requirements |
| [docs/TRD.md](docs/TRD.md) | Technical architecture |
| [docs/PHYSICS.md](docs/PHYSICS.md) | Ball flight physics |
| [docs/GSPRO_API.md](docs/GSPRO_API.md) | GSPro Open Connect API |
| [docs/GC2_PROTOCOL.md](docs/GC2_PROTOCOL.md) | GC2 USB protocol |
| [docs/USB_PLUGINS.md](docs/USB_PLUGINS.md) | Native plugin guide |

## Architecture

```
┌─────────────────────────────────────────────────┐
│              Unity Application                   │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐           │
│  │ Physics │ │   3D    │ │   UI    │           │
│  │ Engine  │ │  View   │ │ System  │           │
│  └─────────┘ └─────────┘ └─────────┘           │
│                    │                            │
│  ┌─────────────────┴─────────────────┐         │
│  │        IGC2Connection             │         │
│  └─────────────────┬─────────────────┘         │
│        ┌───────────┼───────────┐               │
│        ▼           ▼           ▼               │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│  │  macOS  │ │  iPad   │ │ Android │          │
│  │ Plugin  │ │ Plugin  │ │ Plugin  │          │
│  └─────────┘ └─────────┘ └─────────┘          │
└─────────────────────────────────────────────────┘
```

## GC2 Device Info

```
Vendor ID:  0x2C79 (11385)
Product ID: 0x0110 (272)
```

## Contributing

Contributions welcome! Please read the documentation first, especially:
- `CLAUDE.md` for code conventions
- `docs/TRD.md` for architecture details

## License

MIT License - See [LICENSE](LICENSE) for details.

## Acknowledgments

- Prof. Alan Nathan (UIUC) - Trajectory physics model
- [libgolf](https://github.com/gdifiore/libgolf) - C++ golf physics reference implementation
- Washington State University - Aerodynamic coefficient data
- GSPro - Open Connect API inspiration
