# GC2 Connect Unity - Implementation Plan

## Project Overview

Cross-platform driving range simulator connecting to Foresight GC2 launch monitor via USB.
Platforms: macOS (Intel + Apple Silicon), iPad (M1+ with DriverKit), Android tablets (USB Host API).

## Current State

**Test Count**: 1600+ EditMode tests passing
**Phases Complete**: 1-9.5 (except iPad plugin), plus UI refinement
**Current Phase**: 7.5 - UI Refinement (Touch Support)
**Next Prompt**: 60 (Test Shot Button for Touch Devices)

## Phase Breakdown

| Phase | Focus | Prompts | Status |
|-------|-------|---------|--------|
| 1-3 | Core Services & Physics | 1-3 | ✅ Complete |
| 4 | Scenes & Bootstrap | 4-5 | ✅ Complete |
| 5 | Visualization | 6-11 | ✅ Complete |
| 5.5 | Ground Physics | 32-34 | ✅ Complete |
| 5.6 | Ball Ready Indicator | 35 | ✅ Complete |
| 6 | UI System | 12-17 | ✅ Complete |
| 6.5 | GSPro Buffer | 42 | ✅ Complete |
| 7 | TCP/Network & macOS Plugin | 18-22 | ✅ Complete |
| 7.5 | UI Refinement | 43-46, 60 | 🔄 In Progress |
| 8 | macOS Build & Release | 36-37 | ✅ Complete |
| 9 | Android Native Plugin | 23-25 | ✅ Complete |
| 9.5 | Android Build | 39 | ✅ Complete |
| 10 | iPad Native Plugin | 26-28 | ⏳ Deferred |
| 11 | Quality & Polish | 29-31 | ⏳ Pending |
| 12 | Mobile Builds | 38, 40 | ⏳ Pending |
| 13 | CI/CD | 41 | ⏳ Pending |
| 14 | Visual Enhancements | 47-54 | ⏳ Pending |
| 15 | Bridge Mode | 55-59 | ⏳ Pending |

---

## Completed Prompts (One-Line Summary)

| Prompt | Title | PR |
|--------|-------|-----|
| 1 | ShotProcessor Service | #1 |
| 2 | SessionManager Service | #2 |
| 3 | SettingsManager | #4 |
| 4 | BootstrapLoader & Scenes | #6 |
| 5 | PlatformManager & QualityManager | #8 |
| 6 | Golf Ball Prefab | #9 |
| 7 | BallController & BallSpinner | #11 |
| 8 | TrajectoryRenderer | #13 |
| 9 | CameraController | #15 |
| 10 | Landing Effects | #17 |
| 11 | Marina Environment | #19 |
| 12 | UIManager & Layout | #21 |
| 13 | Shot Data Bar | #23 |
| 14 | Club Data Panel | #25 |
| 15 | Connection Status UI | #27 |
| 16 | Session Info Panel | #29 |
| 17 | Settings Panel | #31 |
| 18 | Device Status Interface | #39 |
| 18b | TCP Connection | #41 |
| 19 | GSPro Client | #43 |
| 20 | macOS Plugin Structure | #45 |
| 21 | macOS USB Read Loop | #47 |
| 22 | macOS C# Bridge (tested with hardware) | #49 |
| 23 | Android Plugin Project | #65 |
| 24 | Android Plugin Implementation | #66 |
| 25 | Android C# Bridge | #67 |
| 32 | Spin-Dependent Bounce | #33 |
| 33 | Improved Roll Model | #35 |
| 34 | Physics Validation | #37 |
| 35 | Ball Ready Indicator | #51 |
| 36 | macOS Build Script | #61 |
| 37 | Code Signing & Notarization | #63 |
| 39 | Android Build Configuration | #69 |
| 42 | GSPro Buffer Management | #53 |
| 43 | GSPro Mode Panel Fixes | #55 |
| 44 | Connection Panel Fixes | #57 |
| 45 | Settings Dropdown Fixes | #58 |
| 46 | Test Shot Panel | #59 |

---

## Incomplete Prompts (Full Detail)

### Prompt 60: Test Shot Button (Touch Device Support)

```text
Add a "Test Shot" button to the Marina scene header for touch device access to TestShotPanel.

Context: The TestShotPanel can currently only be opened via keyboard shortcut (T key). On Android tablets and iPads without physical keyboards, users cannot access the test shot functionality.

Problem:
- TestShotPanel exists and works (Prompt 46, PR #59)
- Toggle via "T" key works on desktop
- No UI button to open the panel on touch devices
- Android testers cannot verify visualization without GC2

Solution:
Add a "Test Shot" button on the left side of the Marina header, under the existing "Back" and "Settings" buttons.

Files to modify:
- Assets/Editor/SceneGenerator.cs - Add Test Shot button creation
- Assets/Scripts/UI/MarinaSceneController.cs - Add button click handler

Requirements:

1. Add Test Shot button in SceneGenerator.cs:
   - Position: Left side, below Settings button
   - Anchor: Top-left (0, 1) - (0, 1)
   - Y position: -70 (below Settings at -20, accounting for 40px height + 10px gap)
   - Size: 100x40 (same as Back/Settings buttons)
   - Text: "Test Shot" or "Test"
   - Name: "TestShotButton"

2. Wire button to controller:
   - Add _testShotButton field to MarinaSceneController
   - Wire in SceneGenerator via SerializedObject
   - Add click handler in SetupButtonListeners()
   - Add cleanup in OnDestroy()

3. Click handler:
   - Call _testShotPanel.Toggle() if panel is not null

Layout after change:
[< Back] [Settings] [Test Shot]    ...    [Connection Status]

This is a small, focused change with minimal risk.
```

---

### Prompt 26: iPad Plugin Structure (DriverKit)

```text
Create the iPad native plugin project structure.

Context: iPad requires DriverKit for USB access. This is more complex and requires Apple entitlements.

Important: DriverKit requires entitlements from Apple which may take weeks.

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
   - Targets: GC2iOSPlugin.framework, GC2Driver.dext

4. Entitlements:
   - com.apple.developer.driverkit
   - com.apple.developer.driverkit.transport.usb
   - USB VID/PID in array

5. Info.plist for driver:
   - IOKitPersonalities
   - USB matching by VID/PID

Create placeholder implementation that returns "DriverKit not configured" error.
```

---

### Prompt 27: iPad DriverKit Implementation

```text
Implement the DriverKit extension for iPad USB.

Note: Full implementation may not be testable until Apple approves entitlements.

Implement GC2Driver/ (DriverKit extension):

1. GC2Driver.swift (IOUserService):
   - IOUSBHostDevice matching
   - Start()/Stop()
   - USB configuration, interface claiming, endpoint management

2. GC2UserClient.swift:
   - IOUserClient subclass
   - ExternalMethod dispatch
   - Methods: Connect, Disconnect, Read (async)
   - Shared memory for data transfer

3. Async read implementation:
   - AsyncCompletion for USB transfers
   - Buffer management
   - Notify client of new data

Implement GC2iOSPlugin/:
   - Check driver availability
   - IOServiceGetMatchingService()
   - IOUserClientConnection
   - Parse protocol, Unity callbacks

Document user flow for enabling extension in Settings.
```

---

### Prompt 28: iPad C# Bridge

```text
Create the C# wrapper for the iPad native plugin.

Create Assets/Scripts/GC2/Platforms/iOS/GC2iPadConnection.cs:

1. Native function imports with [DllImport("__Internal")]
2. Callback handling with [AOT.MonoPInvokeCallback]
3. Implement IGC2Connection
4. DriverKit state handling (not installed, needs approval, active)
5. User guidance for extension activation
6. Conditional compilation: #if UNITY_IOS && !UNITY_EDITOR

Create Assets/Scripts/UI/DriverKitSetupUI.cs:
- Panel explaining DriverKit setup for iPad
- Steps for user to enable
- Troubleshooting tips

Update GC2ConnectionFactory for UNITY_IOS.
```

---

### Prompt 29: Integration Testing

```text
Create integration tests and wire everything together.

Create Assets/Tests/:

1. Edit Mode Tests:
   - PhysicsTests.cs - Validate physics accuracy
   - ProtocolTests.cs - GC2 protocol parsing
   - SettingsTests.cs - Settings persistence
   - DataModelTests.cs - GC2ShotData validation

2. Play Mode Tests:
   - SceneLoadTests.cs - Bootstrap → MainMenu → Marina
   - ShotFlowTests.cs - Full shot processing flow
   - UITests.cs - UI updates on shot
   - SessionTests.cs - Session tracking

3. Key validation tests:
   - Driver 167mph/10.9°/2686rpm → ~275 yards (±5%)
   - 7-iron 120mph/16.3°/7097rpm → ~172 yards (±5%)
   - Wedge 102mph/24.2°/9304rpm → ~136 yards (±5%)

Create TestShotGenerator utility with predefined and random shots.

Full flow verification:
1. Bootstrap loads → 2. Managers initialize → 3. Navigate to Marina →
4. Fire test shot → 5. Physics calculates → 6. Ball animates →
7. UI updates → 8. Session records
```

---

### Prompt 30: Quality Tier Polish

```text
Finalize quality tier system and visual polish.

Complete QualityManager implementation:

1. URP Asset Configuration:
   - Low: Render Scale 0.75, no shadows, no MSAA, no post-processing
   - Medium: Render Scale 1.0, hard shadows, MSAA 2x, basic post
   - High: Render Scale 1.0, soft shadows, MSAA 4x, full post

2. Dynamic adjustment:
   - Frame rate monitoring
   - Auto-downgrade if below target for 5 seconds
   - Notify user of change, manual override in settings

3. Environment adjustments:
   - Disable reflections on Low
   - Reduce particle counts
   - Lower LOD distances

4. Per-platform defaults:
   - Mac M1/iPad Pro M1: High
   - iPad Air: Medium
   - High-end Android: High
   - Mid Android: Medium
   - Low-end Android: Low

Visual polish: Ball visible at all distances, trajectory looks good, landing effects visible, UI readable.
```

---

### Prompt 31: Final Polish and Documentation

```text
Final polish, cleanup, and documentation.

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

Documentation:
- README.md - overview, build instructions, requirements
- CLAUDE.md - structure, conventions, workflow
- docs/DEVELOPMENT.md - setup, native plugins, testing

Final checklist:
- All P0 features complete
- Physics validated (±5%)
- 60 FPS on iPad Pro, 30+ FPS on mid-range Android
- USB works on all platforms (or documented limitations)
- No critical bugs
```

---

### Prompt 38: iOS Build Configuration [Future]

```text
[FUTURE - Requires DriverKit entitlements from Apple]

Create build configuration for iOS/iPad deployment.

Prerequisites:
- Apple Developer account with DriverKit entitlements
- Distribution certificates and provisioning profiles
- iPad with M1 chip or later

Create Scripts/build_ios.sh:
1. Pre-build: Verify Xcode 15+, provisioning profiles, entitlements
2. Native plugin: Build GC2iOSPlugin.framework and GC2Driver.dext
3. Unity iOS build: Generate Xcode project with -buildTarget iOS
4. Xcode modification: Add DriverKit extension target, configure signing
5. Archive and export

Create configs/ios/ExportOptions.plist for App Store and Ad Hoc.

Update Makefile: build-ios, archive-ios, export-ios
```

---

### Prompt 40: Mobile Build Environment Setup [Future]

```text
[FUTURE - Supporting infrastructure for iOS and Android builds]

Create environment setup scripts and documentation.

Create Scripts/setup_mobile_env.sh:
1. iOS: Verify Xcode, simulators, code signing, DriverKit profile
2. Android: Install/update SDK, accept licenses, configure ANDROID_HOME
3. Unity license activation for CI

Create configs/mobile-requirements.txt with version requirements.

Update CLAUDE.md with "Mobile Development Setup" section.
Create docs/MOBILE_DEVELOPMENT.md with full setup guide.
```

---

### Prompt 41: GitHub Actions Release Workflow

```text
Create GitHub Actions workflow for automated testing, building, and releasing.

Create .github/workflows/release.yml:

1. Trigger on push to tags matching 'v*' or manual with version input

2. Job: test - Reuse existing test workflow, must pass first

3. Job: build-macos (runs-on: macos-latest)
   - Checkout with LFS
   - Install Unity via game-ci/unity-builder
   - Build native plugin
   - Build Unity project
   - Sign and notarize (using secrets)
   - Create DMG
   - Upload artifact

4. Job: build-ios [Conditional - skip if DriverKit not ready]

5. Job: build-android [Conditional - skip if native plugin not ready]

6. Job: create-release
   - Download all artifacts
   - Create GitHub Release with assets
   - Generate release notes from commits

Secrets required:
- UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD
- APPLE_TEAM_ID, APPLE_DEVELOPER_ID, APPLE_APP_PASSWORD
- APPLE_CERTIFICATE_BASE64, APPLE_CERTIFICATE_PASSWORD
- ANDROID_KEYSTORE_BASE64, ANDROID_KEYSTORE_PASSWORD

Create .github/workflows/build-check.yml for PR verification.
Add release badge to README.md, create docs/RELEASE_PROCESS.md.
```

---

## Phase 14: Visual Enhancements

Visual inspiration:
- **ProceduralGolf**: Toon shaders, water with foam, stylized skybox, outline rendering
- **Super-Golf**: Tropical island aesthetic, trail renderers, dramatic landscapes
- **golf_simulator**: Custom shaders (14% ShaderLab), beautiful landscapes

### Prompt 47: Stylized Skybox and Lighting Setup

```text
Create a dramatic skybox and lighting configuration.

Files to create:
- Assets/Shaders/Skybox/StylizedSkybox.shader
- Assets/Materials/Skybox/MarinaSkybox.mat
- Assets/Editor/LightingSetupGenerator.cs

Requirements:

1. StylizedSkybox.shader (URP compatible):
   - Procedural gradient sky (horizon to zenith)
   - Sun position parameter
   - Cloud layer using noise
   - Horizon fog/haze effect
   - HDR output for bloom
   - Properties: _TopColor, _HorizonColor, _SunColor, _SunSize, _CloudDensity, _CloudSpeed

2. MarinaSkybox.mat "Golden Hour" preset:
   - Top: Deep blue (0.1, 0.3, 0.8)
   - Horizon: Warm orange-pink (1.0, 0.6, 0.4)
   - Sun: Bright yellow-white (1.5, 1.4, 1.0) HDR

3. Lighting Configuration:
   - Directional light (soft shadows, 5500-6500K)
   - Ambient gradient mode
   - Reflection probe

4. LightingSetupGenerator.cs:
   - Menu: OpenRange > Lighting > Setup Marina Lighting
   - Creates/configures lights, skybox material, applies to scene

5. Quality tier integration:
   - High: Full skybox, soft shadows, reflection probe
   - Medium: Simplified skybox, hard shadows, no probe
   - Low: Solid color skybox, no shadows
```

---

### Prompt 48: Enhanced Grass Shader with Wind Animation

```text
Create stylized grass shader with wind animation.

Files to create:
- Assets/Shaders/Environment/StylizedGrass.shader
- Assets/Scripts/Visualization/WindController.cs
- Assets/Editor/GrassShaderSetup.cs

Requirements:

1. StylizedGrass.shader (URP Lit-based):
   - Wind animation via vertex displacement
   - Global wind direction, strength, turbulence
   - Height-based influence (tips move more)
   - Tip color tint, shadow color tint
   - Properties: _BaseColor, _TipColor, _WindDirection, _WindSpeed, _WindTurbulence, _GrassHeight

2. WindController.cs (singleton):
   - WindDirection, WindStrength, WindSpeed, Gusting
   - Updates Shader.SetGlobalVector("_GlobalWindDirection")
   - Integration with SettingsManager wind settings
   - Events: OnWindChanged

3. Material Presets:
   - FairwayGrassEnhanced: Bright green, subtle tip lightening, low wind effect
   - RoughGrass: Darker/yellower, more wind movement
   - GreenGrass: Very short, uniform, no wind effect

4. GrassShaderSetup.cs:
   - Menu: OpenRange > Materials > Create Grass Materials
   - Creates materials, applies to terrain, creates WindController

5. Performance: Shader LOD, disable wind on Low quality
```

---

### Prompt 49: Water Shader with Foam and Reflections

```text
Create stylized water shader for hazards and marina backdrop.

Files to create:
- Assets/Shaders/Environment/StylizedWater.shader
- Assets/Scripts/Visualization/WaterController.cs
- Assets/Editor/WaterSetupGenerator.cs

Requirements:

1. StylizedWater.shader:
   - Base color (deep/shallow blend by depth)
   - Scrolling normal maps (two directions, blended)
   - Depth-based foam at water-object intersection
   - Reflection: Planar (High) or cubemap fallback
   - Wave vertex animation (Gerstner or sine-based)
   - Properties: _ShallowColor, _DeepColor, _NormalMap1/2, _NormalScale, _NormalSpeed,
     _FoamColor, _FoamThreshold, _WaveAmplitude, _WaveFrequency, _ReflectionStrength

2. WaterController.cs:
   - Manages water plane instances
   - Planar reflection camera (High quality)
   - Quality tier switching

3. Material Presets:
   - OceanWater: Deep blue, larger waves
   - PondWater: Calmer, more transparency

4. WaterSetupGenerator.cs:
   - Menu: OpenRange > Materials > Create Water Materials
   - Creates materials and water planes

Document URP depth texture requirements.
```

---

### Prompt 50: Post-Processing Volume Configuration

```text
Create post-processing setup for visual polish.

Files to create:
- Assets/Settings/PostProcessing/HighQualityVolume.asset
- Assets/Settings/PostProcessing/MediumQualityVolume.asset
- Assets/Settings/PostProcessing/LowQualityVolume.asset
- Assets/Scripts/Visualization/PostProcessingController.cs
- Assets/Editor/PostProcessingSetupGenerator.cs

Requirements:

1. Volume Profiles:
   - High: Bloom (threshold 0.9, intensity 0.5), Color Adjustments (saturation +10),
     Vignette (intensity 0.3), SSAO
   - Medium: Bloom (reduced), Color Adjustments, Vignette (lighter)
   - Low: Minimal or none

2. PostProcessingController.cs:
   - Singleton managing active volume
   - Responds to QualityManager tier changes
   - Events: OnPostProcessingChanged

3. PostProcessingSetupGenerator.cs:
   - Menu: OpenRange > Post Processing > Create Volume Profiles
   - Creates profiles with documented presets

4. Scene integration with MarinaSceneController
```

---

### Prompt 51: Enhanced Ball Trail and Trajectory Visuals

```text
Create enhanced visual effects for ball flight.

Files to create:
- Assets/Shaders/Effects/TrailGlow.shader
- Assets/Shaders/Effects/TrajectoryLine.shader
- Assets/Scripts/Visualization/TrajectoryEnhancer.cs
- Assets/Editor/TrajectoryVisualsGenerator.cs

Requirements:

1. TrailGlow.shader:
   - Additive blending
   - HDR color output for bloom interaction
   - Alpha fade over trail length
   - Configurable glow intensity

2. TrajectoryLine.shader:
   - Soft glow effect
   - Optional dashed pattern
   - Fade at ends

3. TrajectoryEnhancer.cs:
   - Attaches to TrajectoryRenderer
   - Manages trail/line materials
   - Shot quality color customization (green=good, red=mishit)

4. TrajectoryVisualsGenerator.cs:
   - Menu: OpenRange > Effects > Create Enhanced Trajectory Materials
   - Creates materials, updates prefabs
```

---

### Prompt 52: Environment Props - Trees and Scenery

```text
Create environment props for Marina scene.

Files to create:
- Assets/Prefabs/Environment/Props/PalmTree.prefab
- Assets/Prefabs/Environment/Props/RockCluster.prefab
- Assets/Prefabs/Environment/Props/DistantMountain.prefab
- Assets/Scripts/Visualization/PropPlacer.cs
- Assets/Scripts/Visualization/LODController.cs
- Assets/Editor/EnvironmentPropsGenerator.cs

Requirements:

1. Prop Prefabs:
   - PalmTree: Simple geometry, wind animation, multiple LODs
   - RockCluster: Low-poly rocks, various sizes
   - DistantMountain: Silhouette for backdrop

2. Materials:
   - Foliage: Stylized, wind-reactive
   - Rock: Simple lit, color variations

3. PropPlacer.cs:
   - Editor tool for placing props
   - Random rotation/scale variation
   - Terrain snapping

4. LODController.cs:
   - Manages LOD transitions
   - Distance-based culling
   - Quality tier integration

5. GPU instancing and batching for performance

6. EnvironmentPropsGenerator.cs:
   - Menu: OpenRange > Props > Create All Props
```

---

### Prompt 53: Toon/Outline Shader Option

```text
Create optional toon rendering mode.

Files to create:
- Assets/Shaders/Toon/ToonLit.shader
- Assets/Shaders/Toon/ToonOutline.shader
- Assets/Scripts/Visualization/ToonModeController.cs
- Assets/Editor/ToonMaterialGenerator.cs

Requirements:

1. ToonLit.shader:
   - Cel shading with configurable steps
   - Rim lighting for edge highlight
   - Specular highlight bands
   - Stylized shadows

2. ToonOutline.shader:
   - Inverted hull technique for outlines
   - Configurable thickness and color
   - Camera distance scaling

3. ToonModeController.cs:
   - Toggle between realistic and toon modes
   - Material swapping
   - Outline camera setup

4. Add "Visual Style" setting to Settings Panel

5. ToonMaterialGenerator.cs:
   - Menu: OpenRange > Materials > Create Toon Materials
   - Creates toon versions of existing materials
```

---

### Prompt 54: Visual Polish and Integration

```text
Final visual system integration and polish.

Files to create:
- Assets/Scripts/Visualization/VisualManager.cs
- Assets/Editor/VisualSystemValidator.cs

Requirements:

1. VisualManager.cs:
   - Singleton coordinating all visual systems
   - Skybox, lighting, grass, water, post-processing, toon mode
   - Visual presets: Day (default), Sunset (warm), Overcast (soft)
   - Events: OnVisualPresetChanged

2. Update SceneGenerator.cs for visual systems

3. Quality tier validation and polish

4. VisualSystemValidator.cs:
   - Menu: OpenRange > Validation > Validate Visual Systems
   - Checks all materials assigned, shaders compile, performance targets met

5. Performance validation:
   - 60 FPS on High-tier devices
   - 30 FPS on Low-tier devices

6. Documentation updates
```

---

## Phase 15: Bridge Mode (Background Processing)

**Use Case**: Moonlight streaming - run OpenRange on Android tablet in background, stream GSPro from PC to tablet.

### Prompt 55: Bridge Mode Core Architecture

```text
Design and implement the core Bridge Mode architecture.

Files to create:
- Assets/Scripts/Core/BridgeModeManager.cs
- Assets/Scripts/Core/IBridgeService.cs
- Assets/Scripts/Network/GSProRelay.cs

Requirements:

1. BridgeModeManager.cs:
   - Singleton managing bridge mode state
   - States: Disabled, Active, Backgrounded
   - Events: OnBridgeModeStateChanged
   - Methods: EnableBridgeMode(), DisableBridgeMode()
   - Coordinates GC2 connection and GSPro relay
   - Shot counting and status tracking

2. IBridgeService interface:
   - Platform-specific background service abstraction
   - Start(), Stop(), IsRunning
   - OnShotRelayed callback

3. GSProRelay.cs:
   - Lightweight GSPro client for relay-only mode
   - No physics simulation needed
   - Just forward GC2 data to GSPro
   - Heartbeat maintenance

4. Integration points:
   - GameManager awareness of bridge mode
   - Reduced UI when backgrounded
```

---

### Prompt 56: Android Foreground Service

```text
Implement Android foreground service for background operation.

Files to create:
- NativePlugins/Android/GC2AndroidPlugin/src/main/java/com/openrange/GC2BridgeService.kt
- Assets/Scripts/GC2/Platforms/Android/AndroidBridgeService.cs

Requirements:

1. GC2BridgeService.kt (Foreground Service):
   - Extends Service with startForeground()
   - Persistent notification showing:
     * Connection status
     * Shots relayed count
     * Tap to return to app
   - PARTIAL_WAKE_LOCK for USB
   - Binder for Activity communication
   - Lifecycle management

2. AndroidManifest additions:
   - <service android:name=".GC2BridgeService" android:foregroundServiceType="connectedDevice"/>
   - FOREGROUND_SERVICE permission

3. AndroidBridgeService.cs (C# wrapper):
   - Implements IBridgeService
   - AndroidJavaObject calls to service
   - Start/stop service
   - Update notification

4. Service lifecycle:
   - Start when entering bridge mode
   - Stop on disable or explicit user action
   - Handle process death/restart
```

---

### Prompt 57: Android Bridge Mode C# Integration

```text
Integrate Android bridge service with Unity.

Files to modify:
- Assets/Scripts/Core/BridgeModeManager.cs
- Assets/Scripts/GC2/Platforms/Android/GC2AndroidConnection.cs

Requirements:

1. BridgeModeManager updates:
   - Platform-specific service instantiation
   - Android: AndroidBridgeService
   - iOS: (placeholder for Prompt 59)
   - Editor: MockBridgeService for testing

2. GC2AndroidConnection updates:
   - Support running in service context
   - Pass shots to GSProRelay when in bridge mode
   - Handle being backgrounded

3. Application lifecycle handling:
   - OnApplicationPause: Keep connection alive
   - OnApplicationFocus: Update UI state
   - Handle service reconnection

4. Battery optimization:
   - Request exemption from Doze mode
   - Document battery usage
```

---

### Prompt 58: Bridge Mode UI

```text
Create minimal UI for bridge mode operation.

Files to create:
- Assets/Scripts/UI/BridgeModeOverlay.cs
- Assets/Scripts/UI/BridgeModeToggle.cs
- Assets/Editor/BridgeModeUIGenerator.cs

Requirements:

1. BridgeModeOverlay.cs:
   - Minimal floating overlay when app foregrounded in bridge mode
   - Shows: Connection status, shots relayed, GSPro status
   - Tap to expand controls
   - Drag to reposition

2. BridgeModeToggle.cs:
   - Toggle in settings or main menu
   - "Enable Bridge Mode" with explanation
   - Warning about battery usage
   - GSPro connection config

3. Integration:
   - Add to Settings Panel
   - Add to MainMenu as quick-access button
   - Show overlay when returning from background

4. BridgeModeUIGenerator.cs:
   - Menu: OpenRange > Create Bridge Mode UI Prefabs
```

---

### Prompt 59: iPad Background Mode Research and Workarounds

```text
Research and document iPad background operation options.

Context: iOS severely limits background operation. Research workarounds.

Potential approaches:

1. Picture-in-Picture (PiP):
   - Create minimal video view
   - Show connection status as "video"
   - Keeps app active while PiP visible
   - Limitation: PiP must be visible

2. Background Audio:
   - Play silent audio
   - Keeps app active but battery-intensive
   - May violate App Store guidelines

3. Background Processing:
   - BGTaskScheduler for periodic tasks
   - Not suitable for continuous USB
   - Only brief execution windows

4. Accessory Mode:
   - External Accessory framework
   - Requires MFi certification for USB
   - Not applicable for GC2

Files to create:
- docs/IPAD_BACKGROUND_MODE.md - Document findings
- Assets/Scripts/GC2/Platforms/iOS/iOSBridgeService.cs - Placeholder/best-effort implementation

Likely outcome: Document limitations and recommend macOS or Android for bridge mode.
```

---

## Appendix A: Testing Strategy

### Test Categories

1. **Edit Mode Tests** (no Unity runtime): Physics, protocol, validation, conversions
2. **Play Mode Tests** (Unity runtime): Scenes, components, UI, animation
3. **Integration Tests** (hardware optional): Full shot flow, network, USB

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
- com.unity.render-pipelines.universal: 14.0.8
- com.unity.textmeshpro: 3.0.6
- com.unity.inputsystem: 1.7.0
- com.unity.nuget.newtonsoft-json: 3.2.1

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

---

## Appendix D: Implementation Notes (Lessons Learned)

### Unity 6 Module System

When using Assembly Definitions, some Unity APIs require explicit module references in `Packages/manifest.json`:
```json
"com.unity.modules.particlesystem": "1.0.0"
```

For external assemblies like TextMeshPro, add to `.asmdef`:
```json
"references": ["Unity.TextMeshPro"]
```

### Scene Integration Pattern

When creating new components, ALL steps required:
1. Create component scripts
2. Create editor generator with menu items
3. Update scene controller with `[SerializeField]` field
4. Update SceneGenerator.cs to instantiate and wire via `SerializedObject`
5. Regenerate scene

**Common Mistake**: Creating prefab generator but forgetting steps 3-4.

### Coordinate System Conversion

| Axis | Physics (Trajectory) | Unity World |
|------|---------------------|-------------|
| X | Forward (yards) | Right (lateral) |
| Y | Height (feet) | Up (height) |
| Z | Lateral (yards) | Forward (distance) |

When converting TrajectoryPoint.Position to Unity Vector3, swap X and Z.

### Native Plugin IL2CPP Callbacks

`UnitySendMessage` does NOT work in IL2CPP. Use function pointer callbacks:
```csharp
[AOT.MonoPInvokeCallback(typeof(NativeShotCallback))]
private static void OnNativeShotCallbackStatic(string jsonData) { ... }
```

**JSON Field Names:** Native JSON must use exact C# property names.

### GC2 USB Protocol Summary

- **Endpoint**: INTERRUPT IN (0x82), NOT bulk
- **Message types**: `0H` = shot, `0M` = device status
- **Terminator**: `\n\t` = complete message
- **Accumulation**: Wait for `BACK_RPM`/`SIDE_RPM`
- **Misreads**: Reject zero spin, 2222 error, speed <1.1 or >250 mph
- **Device status**: FLAGS == 7 = ready, BALLS > 0 = ball detected

### Unity UI Programmatic Creation

**TMP_Dropdown**: Requires explicit ScrollRect wiring (`scrollRect.viewport`/`content`).

**Button raycastTarget**: Set `raycastTarget = false` on button text to prevent click interception.

**Unicode Fonts**: LiberationSans SDF lacks many symbols. Use Image components or Unity built-in sprites (`AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd")`).

### Batchmode Scene Generation

`EditorUtility.DisplayDialog()` returns `false` in batchmode. Check `Application.isBatchMode` to skip dialogs.
