# Product Requirements Document (PRD)
# Open Range Unity - Cross-Platform Edition

## Overview

### Product Name
Open Range Unity (Cross-Platform)

### Version
2.0.0

### Last Updated
December 2024

### Author
Samiur Rahman

---

## Executive Summary

Open Range Unity Cross-Platform is a unified driving range simulator built with Unity that runs natively on macOS, iPad, and Android tablets. By using Unity across all platforms, we achieve consistent GSPro-quality visuals everywhere while sharing 95%+ of the codebase.

This replaces the previous multi-technology approach (Python/NiceGUI for desktop, React Native for mobile) with a single Unity application that handles both visualization AND GC2 USB communication via native plugins.

---

## Problem Statement

### Current Multi-Technology Approach

| Platform | Current Tech | Limitations |
|----------|--------------|-------------|
| macOS | Python + NiceGUI + Three.js | Web-based 3D, limited quality |
| iPad | React Native + SceneKit/WebView | Two rendering paths, inconsistent |
| Android | React Native + WebView | Weakest visuals, performance issues |

### Pain Points
1. **Inconsistent visuals**: Each platform looks different
2. **Maintenance burden**: Three separate codebases to maintain
3. **Feature parity**: Hard to keep features synchronized
4. **Quality ceiling**: Web-based rendering limits visual fidelity
5. **Performance**: WebView overhead on mobile

### Unity Advantage
1. **Single codebase**: Write once, deploy everywhere
2. **Consistent quality**: Same renderer on all platforms
3. **Native performance**: Compiled code, GPU optimization
4. **Mature mobile support**: Unity excels at mobile games
5. **Asset reuse**: Same 3D assets across all platforms

---

## Goals & Success Metrics

### Primary Goals
1. Unified GSPro-quality visuals on Mac, iPad, and Android
2. Single codebase with platform-specific USB plugins
3. Native performance on all target devices
4. Offline capability (no network required for Open Range)

### Success Metrics

| Metric | Mac (M1+) | iPad Pro M1 | Android Tablet |
|--------|-----------|-------------|----------------|
| Frame Rate | 120 FPS | 60 FPS | 30-60 FPS |
| Visual Quality | High | High | Medium-High |
| Shot Latency | < 50ms | < 75ms | < 100ms |
| Battery/Hour | N/A | < 15% | < 20% |
| App Size | < 200MB | < 150MB | < 150MB |

---

## Platform Support Matrix

### Supported Platforms

| Platform | USB Method | Min Spec | Status |
|----------|------------|----------|--------|
| **macOS** | Native plugin (libusb) | macOS 11+, Intel/M1 | Primary |
| **iPad Pro** | DriverKit native plugin | iPadOS 16+, M1+ | Primary |
| **Android Tablet** | USB Host API plugin | Android 8+, USB Host | Primary |
| **iPhone** | Not supported | - | Apple restriction |
| **Windows** | Native plugin | Windows 10+ | Future v2.1 |

### USB Integration Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    Unity Application                             │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Cross-Platform C# Code (95%)                 │   │
│  │  • Physics Engine      • UI/UX        • Shot Processing  │   │
│  │  • 3D Visualization    • Settings     • Session Mgmt     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              USB Abstraction Layer (C#)                   │   │
│  │                    IGCB2Connection                        │   │
│  └──────────────────────────────────────────────────────────┘   │
│           │                  │                    │              │
│           ▼                  ▼                    ▼              │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────────┐   │
│  │ macOS Plugin   │ │ iPad Plugin    │ │ Android Plugin     │   │
│  │ (Obj-C/Swift)  │ │ (Swift)        │ │ (Java/Kotlin)      │   │
│  │ libusb wrapper │ │ DriverKit      │ │ USB Host API       │   │
│  └────────┬───────┘ └────────┬───────┘ └──────────┬─────────┘   │
└───────────┼──────────────────┼────────────────────┼─────────────┘
            │                  │                    │
            ▼                  ▼                    ▼
      ┌──────────┐       ┌──────────┐        ┌──────────┐
      │   GC2    │       │   GC2    │        │   GC2    │
      │  (USB)   │       │  (USB)   │        │  (USB)   │
      └──────────┘       └──────────┘        └──────────┘
```

---

## Features & Requirements

### P0 - Must Have (MVP)

#### F1: Cross-Platform GC2 USB Connection
- **Description**: Native USB communication on all platforms
- **Requirements**:
  - Unified C# interface for USB operations
  - Platform-specific native plugins
  - Auto-detect GC2 (VID: 0x2C79, PID: 0x0110)
  - Handle permissions per platform
  - Graceful disconnect/reconnect
- **Platform-Specific**:
  - macOS: libusb via native Objective-C plugin
  - iPad: DriverKit extension with Swift bridge
  - Android: USB Host API via Java plugin
- **Acceptance Criteria**:
  - GC2 detected within 3 seconds on all platforms
  - No missed shots
  - Clean permission flow

#### F2: GSPro-Quality 3D Environment
- **Description**: Beautiful driving range matching GSPro aesthetics
- **Requirements**:
  - Coastal/marina environment (primary)
  - High-quality lighting (HDR, shadows)
  - Realistic water rendering
  - Volumetric clouds/atmosphere
  - Distance markers and target greens
  - Decorative elements (boats, mountains)
- **Quality Tiers**:
  - High: Mac, iPad Pro (full effects)
  - Medium: Older iPad, high-end Android
  - Low: Mid-range Android (reduced effects)
- **Acceptance Criteria**:
  - Visually comparable to GSPro
  - Automatic quality detection
  - Manual quality override option

#### F3: Physics-Accurate Ball Flight
- **Description**: Nathan model physics (shared across platforms)
- **Requirements**:
  - Same C# physics engine everywhere
  - WSU aerodynamic coefficients
  - Atmospheric corrections
  - Wind effects (optional)
  - Trajectory visualization
- **Acceptance Criteria**:
  - Identical physics results on all platforms
  - Within 5% of validation data

#### F4: Ball Animation & Effects
- **Description**: Smooth, visually appealing ball flight
- **Requirements**:
  - High-quality ball model with dimples
  - Spin visualization
  - Trail/tracer effect
  - Landing impact effect (dust/divot)
  - Bounce and roll animation
- **Acceptance Criteria**:
  - 60 FPS animation on primary devices
  - Visually satisfying flight

#### F5: Comprehensive Shot Data UI
- **Description**: GSPro-style data display
- **Requirements**:
  - Bottom data bar (Ball Speed, Direction, Angle, Spin, Apex, Offline, Carry, Run, Total)
  - Right panel for HMT data (Path, Attack, Loft, Face)
  - Top-left session info (Time, Shots)
  - Responsive layout for different screen sizes
- **Acceptance Criteria**:
  - All metrics displayed correctly
  - Readable on all screen sizes
  - Immediate updates on shot

#### F6: Touch & Mouse/Keyboard Input
- **Description**: Platform-appropriate controls
- **Requirements**:
  - Touch gestures on mobile (pinch zoom, swipe rotate)
  - Mouse/keyboard on Mac
  - Large touch targets on mobile
  - Consistent interaction patterns
- **Acceptance Criteria**:
  - Intuitive controls on each platform
  - No accidental inputs

### P1 - Should Have

#### F7: GSPro Mode (Network Relay)
- **Description**: Send shots to GSPro instead of local range
- **Requirements**:
  - Mode selector: "Open Range" vs "GSPro"
  - TCP client to GSPro
  - Queue shots if temporarily disconnected
  - Works alongside local visualization
- **Acceptance Criteria**:
  - Seamless mode switching
  - < 150ms network latency
  - Reliable delivery

#### F8: Club Selector
- **Description**: Visual club selection
- **Requirements**:
  - Club bag visualization
  - Quick-select buttons
  - Tee/ground toggle
  - Affects test shot generation
- **Acceptance Criteria**:
  - One-tap selection
  - Visual feedback

#### F9: Shot History
- **Description**: Review previous shots
- **Requirements**:
  - Scrollable list of session shots
  - Tap to replay trajectory
  - Key metrics per shot
- **Acceptance Criteria**:
  - All shots recorded
  - Smooth replay

#### F10: Multiple Environments
- **Description**: Different range settings
- **Requirements**:
  - Marina/Coastal (default)
  - Mountain
  - Desert
  - Links
- **Acceptance Criteria**:
  - Each environment polished
  - Quick switching

#### F11: Settings & Preferences
- **Description**: Configurable options
- **Requirements**:
  - Graphics quality
  - Units (yards/meters)
  - Conditions (temp, elevation, wind)
  - Connection settings
  - Audio volume
- **Acceptance Criteria**:
  - Settings persist
  - Platform-appropriate UI

### P2 - Nice to Have

#### F12: Demo/Test Mode
- **Description**: Simulated shots without GC2
- **Requirements**:
  - Generate realistic test shots
  - Great for demos
- **Acceptance Criteria**:
  - Believable shots

#### F13: Dispersion View
- **Description**: Top-down shot pattern
- **Requirements**:
  - Overhead camera
  - Landing spot markers
  - Dispersion ellipse
- **Acceptance Criteria**:
  - Clear visualization

#### F14: Audio
- **Description**: Immersive sound
- **Requirements**:
  - Ball landing sounds
  - Ambient environment
  - UI feedback
- **Acceptance Criteria**:
  - High quality audio

#### F15: Haptic Feedback (Mobile)
- **Description**: Tactile response
- **Requirements**:
  - Vibration on ball landing (iPad, Android)
- **Acceptance Criteria**:
  - Subtle, not annoying

---

## User Flows

### Flow 1: iPad/Android - Direct USB
1. User connects GC2 to tablet via USB-C
2. System shows permission dialog (platform-specific)
3. User grants permission
4. App detects GC2, shows "Connected"
5. User hits a shot
6. Ball flight visualizes in 3D
7. Data bar shows results
8. Shot added to history

### Flow 2: Mac - Direct USB
1. User connects GC2 to Mac
2. App detects GC2 automatically (libusb)
3. User hits a shot
4. Beautiful visualization
5. No companion app needed

### Flow 3: Any Platform - GSPro Mode
1. User selects "GSPro" mode
2. Enters GSPro PC IP address
3. App connects via TCP
4. User hits a shot
5. Shot sent to GSPro AND visualized locally
6. Best of both worlds

### Flow 4: Offline Practice (Mobile)
1. User takes tablet outdoors (no WiFi)
2. Connects GC2 via USB
3. Opens app in Open Range mode
4. Practices with full visualization
5. No network required

---

## Visual Design

### Quality Tiers

**High Quality (Mac, iPad Pro M1+)**
- Full HDR lighting
- Real-time shadows (soft)
- Screen-space reflections
- Volumetric fog/atmosphere
- High-resolution textures (2K)
- Anti-aliasing (MSAA 4x)
- Post-processing (bloom, color grading)

**Medium Quality (iPad Air, High-end Android)**
- HDR lighting
- Hard shadows
- Planar reflections (water only)
- Standard fog
- Medium textures (1K)
- Anti-aliasing (FXAA)
- Reduced post-processing

**Low Quality (Mid-range Android)**
- Standard lighting
- No real-time shadows (baked)
- No reflections
- Simple fog
- Low textures (512px)
- No anti-aliasing
- Minimal post-processing

### Responsive UI

**iPad/Large Tablet (10"+)**
- Full GSPro-style layout
- Side panels visible
- Large data tiles

**Small Tablet (8-10")**
- Compact data bar
- Collapsible panels
- Slightly smaller tiles

**Mac (Windowed)**
- Resizable window
- Adapts like tablet sizes
- Keyboard shortcuts

---

## Technical Constraints

### Unity Version
- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP)
- IL2CPP scripting backend

### Platform Requirements

| Platform | Min OS | Min Hardware | USB Support |
|----------|--------|--------------|-------------|
| macOS | 11.0 | Intel i5 / M1 | libusb |
| iPad | 16.0 | M1 chip | DriverKit |
| Android | 8.0 | USB Host capable | USB Host API |

### Native Plugin Requirements

**macOS Plugin**
- Objective-C/Swift
- libusb integration
- Bundled as .bundle

**iPad Plugin**
- Swift + DriverKit
- System extension
- Requires entitlements

**Android Plugin**
- Java/Kotlin
- Uses Android USB Host API
- AAR library format

---

## Out of Scope (v2.0)

- iPhone support (Apple restriction on DriverKit)
- Full course simulation
- Multiplayer/online
- VR support (future v3.0)
- Club fitting analysis
- Video recording (future)

---

## Timeline

### Phase 1: Core Unity App (Weeks 1-3)
- Unity project setup with URP
- Cross-platform architecture
- 3D environment (Marina)
- Physics engine (C# port)
- Ball visualization

### Phase 2: macOS USB Plugin (Weeks 3-4)
- Native Objective-C plugin
- libusb integration
- Unity bridge
- Testing with GC2

### Phase 3: Android USB Plugin (Weeks 4-5)
- Java native plugin
- USB Host API integration
- Unity bridge
- Testing on tablets

### Phase 4: iPad USB Plugin (Weeks 5-7)
- DriverKit entitlement request
- Swift driver implementation
- Unity bridge
- Testing on iPad Pro

### Phase 5: UI & Polish (Weeks 7-9)
- Complete UI implementation
- Quality tier optimization
- Settings and preferences
- GSPro mode integration

### Phase 6: Testing & Release (Weeks 9-11)
- Cross-platform testing
- Performance optimization
- App store submissions
- Documentation

---

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| iPad DriverKit approval delayed | High | Medium | Start process early; have Mac+Android ready |
| Unity mobile performance issues | Medium | Low | Aggressive quality tiers; profiling |
| USB plugin complexity | Medium | Medium | Incremental development; fallback to TCP |
| Different Unity behavior per platform | Medium | Low | CI/CD testing on all platforms |
| App Store rejection (iPad) | Medium | Low | Follow guidelines; appeal if needed |

---

## Success Criteria

### Launch Criteria
- [ ] Identical physics on all platforms (validated)
- [ ] 60 FPS on iPad Pro M1
- [ ] 30+ FPS on Samsung Galaxy Tab S8
- [ ] 120 FPS on M1 Mac
- [ ] USB working on all three platforms
- [ ] All P0 features complete
- [ ] Apps approved for distribution

### Post-Launch Metrics (60 days)
- User rating > 4.5/5 (all platforms)
- < 20 crash reports total
- 60%+ prefer Unity version over previous
- Feature parity maintained across platforms

---

## Migration Path

### From Existing Apps

**GC2 Connect Desktop (Python) Users**
- Can continue using Python app → TCP → Unity
- Or switch to native USB in Unity app
- No forced migration

**GC2 Connect Mobile (React Native) Users**
- Replace with Unity app
- Same or better functionality
- Much better visuals

---

## Appendix

### Comparison: React Native vs Unity for Mobile

| Aspect | React Native + WebView | Unity |
|--------|------------------------|-------|
| Visual Quality | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| Performance | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Code Sharing | ⭐⭐⭐ (limited) | ⭐⭐⭐⭐⭐ (95%+) |
| USB Integration | ⭐⭐⭐ (native modules) | ⭐⭐⭐ (native plugins) |
| Dev Experience | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| App Size | ⭐⭐⭐⭐⭐ (small) | ⭐⭐⭐ (larger) |
| Battery | ⭐⭐⭐⭐ | ⭐⭐⭐ |

**Verdict**: Unity wins on visual quality and code sharing, which are priorities for this project.

### Related Documents
- Open Range Unity macOS PRD/TRD
- GC2 Connect Mobile PRD/TRD (legacy)
- GC2 USB Protocol Specification
- DriverKit Implementation Guide
