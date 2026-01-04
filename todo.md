# GC2 Connect Unity - Development Todo

## Current Status
**Phase**: 7.5 - UI Refinement (Complete)
**Last Updated**: 2026-01-04
**Next Prompt**: Phase 15 Bridge Mode (Prompts 55-59)
**Test Count**: 1600+ EditMode tests passing

## Progress Summary
✅ Physics: Carry, bounce, roll validated (PRs #3, #33, #35, #37)
✅ Protocol: 0H shot + 0M device status parsing (PR #39)
✅ GSPro: Client + buffer management (PRs #43, #53)
✅ UI: Prompts 12-17, 43-46, 60 complete (PRs #21-31, #55-59, #71)
✅ macOS: Plugin + build + signing (PRs #45-49, #61, #63)
✅ Android: Plugin + build (PRs #65-69)

## Priority Order
1. **Phase 15**: Bridge Mode (Prompts 55-59) - Moonlight + GSPro use case
2. **Phase 14**: Visual Enhancements (Prompts 47-54)
3. **Phase 11**: Quality & Polish (Prompts 29-31)
4. **Phase 10**: iPad Native Plugin (Prompts 26-28) - deferred

---

## Completed Phases (Summary Table)

| Phase | Prompts | PRs | Key Components |
|-------|---------|-----|----------------|
| 1-3: Core Services | 1-3 | #1, #2, #4 | ShotProcessor, SessionManager, SettingsManager |
| 4: Scenes/Bootstrap | 4-5 | #6, #8 | SceneLoader, BootstrapLoader, QualityManager |
| 5: Visualization | 6-11 | #9-19 | BallController, TrajectoryRenderer, CameraController, EffectsManager |
| 5.5: Ground Physics | 32-34 | #33, #35, #37 | Penner COR, spin-dependent roll, landing data |
| 5.6: Ball Ready | 35 | #51 | BallReadyIndicator with visual states |
| 6: UI System | 12-17 | #21-31 | ShotDataBar, ClubDataPanel, ConnectionStatus, SessionInfo, Settings |
| 6.5: GSPro | 42 | #53 | GSProResponse, buffer management |
| 7: TCP/Network | 18-19 | #39, #41, #43 | GC2TCPConnection, GSProClient, heartbeat |
| 7: macOS Plugin | 20-22 | #45, #47, #49 | GC2MacPlugin, libusb, GC2MacConnection ✅ TESTED |
| 7.5: UI Refinement | 43-46 | #55-59 | Panel fixes, dropdowns, TestShotPanel |
| 8: macOS Build | 36-37 | #61, #63 | build_macos.sh, sign_and_notarize.sh |
| 9: Android Plugin | 23-25 | #65-67 | GC2Plugin.kt, async UsbRequest, GC2AndroidConnection |
| 9.5: Android Build | 39 | #69 | build_android.sh, AndroidBuildSettings.cs |

---

## Incomplete Phases (Full Detail)

### Active: Prompt 60 - Test Shot Button (Touch Device Support)

- [ ] Add "Test Shot" button to Marina scene header
  - [ ] Position: Left side, below Settings button (Y: -70)
  - [ ] Size: 100x40 (same as Back/Settings)
  - [ ] Text: "Test Shot" or "Test"
- [ ] Wire button in MarinaSceneController
  - [ ] Add _testShotButton serialized field
  - [ ] Add click handler calling _testShotPanel.Toggle()
  - [ ] Add cleanup in OnDestroy()
- [ ] Update SceneGenerator.cs
  - [ ] Create TestShotButton via CreateButton()
  - [ ] Wire to controller via SerializedObject
- [ ] Regenerate Marina scene (make generate)
- [ ] Test on Android device without keyboard

---

### Phase 9.5: Android Testing (Remaining)

- [ ] **Testing Phase 1: Android Emulator (Mac)**
  - [ ] Test app launch and UI on emulator
  - [ ] Test GSPro TCP connection (emulator → Mac GSPro)
  - [ ] Test TestShotPanel firing simulated shots
  - [ ] Verify ball animation, trajectory, UI updates
- [ ] **Testing Phase 2: Real Android Device**
  - [x] Install APK via adb or side-loading ✅
  - [ ] Test USB-C OTG connection with GC2
  - [ ] Verify shot data flows through full pipeline
  - [ ] Test GSPro relay mode with real shots

---

### Phase 10: iPad Native Plugin (DriverKit) - DEFERRED

> **Note**: Requires Apple DriverKit entitlement approval (weeks).

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

### Phase 11: Quality & Polish

- [ ] **Prompt 29**: Integration Testing
  - [ ] Create Edit Mode tests (PhysicsTests, ProtocolTests, SettingsTests, DataModelTests)
  - [ ] Create Play Mode tests (SceneLoadTests, ShotFlowTests, UITests, SessionTests)
  - [ ] Physics validation tests (Driver 275yds±5%, 7-Iron 172yds±5%, Wedge 136yds±5%)
  - [ ] Test shot generator utility
  - [ ] Platform verification checklist

- [ ] **Prompt 30**: Quality Tier Polish
  - [ ] Complete URP asset configuration (Low/Medium/High)
  - [ ] Dynamic adjustment (FPS monitoring, auto-downgrade)
  - [ ] Environment adjustments per tier
  - [ ] UI adjustments per tier
  - [ ] Per-platform defaults
  - [ ] Visual polish verification
  - [ ] Tests

- [ ] **Prompt 31**: Final Polish and Documentation
  - [ ] Code cleanup (debug logs, comments, formatting)
  - [ ] Performance verification (profile each platform)
  - [ ] Build verification (macOS universal, iPad archive, Android APK/AAB)
  - [ ] README.md update
  - [ ] CLAUDE.md update
  - [ ] docs/DEVELOPMENT.md
  - [ ] Final regression tests

---

### Phase 12: Mobile Builds (Partial)

- [ ] **Prompt 38**: iOS Build Configuration [Future - requires iPad plugin]
  - [ ] Prerequisites: iPad native plugin complete (Prompts 26-28)
  - [ ] Configure Unity iOS Player Settings
  - [ ] Create Xcode export options plist
  - [ ] Create iOS build script
  - [ ] Document TestFlight submission process

- [x] **Prompt 39**: Android Build Configuration ✅ (PR #69)

- [ ] **Prompt 40**: Mobile Build Environment Setup
  - [ ] Document Xcode requirements [Future - after iOS build]
  - [ ] Document Android SDK requirements
  - [ ] Create combined mobile build script
  - [ ] Add mobile build targets to Makefile

---

### Phase 13: CI/CD Release Workflow

- [ ] **Prompt 41**: GitHub Actions Release Workflow
  - [ ] Create `.github/workflows/release.yml`
    - [ ] Trigger on version tags (v*)
    - [ ] Build matrix: macOS, iOS (future), Android (future)
    - [ ] Use `game-ci/unity-builder` for Unity builds
  - [ ] macOS release job
    - [ ] Build native plugin
    - [ ] Build Unity app (IL2CPP)
    - [ ] Code sign with secrets
    - [ ] Notarize with Apple
    - [ ] Package as DMG
    - [ ] Upload as release artifact
  - [ ] iOS release job [Future]
  - [ ] Android release job [Future]
  - [ ] Create GitHub Release with all artifacts
  - [ ] Document required secrets
  - [ ] Add release badge to README.md

---

### Phase 14: Visual Enhancements

Visual inspiration: ProceduralGolf (toon shaders), Super-Golf (tropical aesthetic), golf_simulator (custom shaders)

- [ ] **Prompt 47**: Stylized Skybox and Lighting Setup
  - [ ] Create StylizedSkybox.shader (procedural gradient, clouds, sun)
  - [ ] Create MarinaSkybox.mat with "Golden Hour" preset
  - [ ] Configure directional light, ambient, reflection probe
  - [ ] Create LightingSetupGenerator.cs editor tool
  - [ ] Quality tier integration (soft/hard/no shadows)
  - [ ] Unit tests

- [ ] **Prompt 48**: Enhanced Grass Shader with Wind Animation
  - [ ] Create StylizedGrass.shader (vertex displacement, wind)
  - [ ] Create WindController.cs singleton
  - [ ] Create grass material presets (Fairway, Rough, Green)
  - [ ] Create GrassShaderSetup.cs editor tool
  - [ ] Integration with SettingsManager wind settings
  - [ ] Unit tests

- [ ] **Prompt 49**: Water Shader with Foam and Reflections
  - [ ] Create StylizedWater.shader (scrolling normals, depth foam)
  - [ ] Create WaterController.cs (planar reflection, quality tiers)
  - [ ] Create water material presets (Ocean, Pond)
  - [ ] Create WaterSetupGenerator.cs editor tool
  - [ ] Document URP depth texture requirements
  - [ ] Unit tests

- [ ] **Prompt 50**: Post-Processing Volume Configuration
  - [ ] Create Volume Profiles (High/Medium/Low quality)
  - [ ] Configure Bloom, Color Adjustments, Vignette, SSAO
  - [ ] Create PostProcessingController.cs
  - [ ] Create PostProcessingSetupGenerator.cs editor tool
  - [ ] Scene integration with MarinaSceneController
  - [ ] Unit tests

- [ ] **Prompt 51**: Enhanced Ball Trail and Trajectory Visuals
  - [ ] Create TrailGlow.shader (additive, HDR, fade)
  - [ ] Create TrajectoryLine.shader (glow, optional dashes)
  - [ ] Create enhanced materials (trajectory, ball trail)
  - [ ] Create TrajectoryEnhancer.cs component
  - [ ] Shot quality color customization
  - [ ] Create TrajectoryVisualsGenerator.cs editor tool
  - [ ] Unit tests

- [ ] **Prompt 52**: Environment Props - Trees and Scenery
  - [ ] Create PalmTree, RockCluster, DistantMountain prefabs
  - [ ] Create Foliage and Rock materials with LOD
  - [ ] Create PropPlacer.cs and LODController.cs
  - [ ] Create EnvironmentPropsGenerator.cs editor tool
  - [ ] GPU instancing and batching
  - [ ] Unit tests

- [ ] **Prompt 53**: Toon/Outline Shader Option
  - [ ] Create ToonLit.shader (cel shading, rim lighting)
  - [ ] Create ToonOutline.shader (inverted hull)
  - [ ] Create toon material presets
  - [ ] Create ToonModeController.cs
  - [ ] Add "Visual Style" setting to Settings Panel
  - [ ] Create ToonMaterialGenerator.cs editor tool
  - [ ] Unit tests

- [ ] **Prompt 54**: Visual Polish and Integration
  - [ ] Create VisualManager.cs (coordinates all visual systems)
  - [ ] Update SceneGenerator.cs for visual systems
  - [ ] Quality tier polish and validation
  - [ ] Create VisualSystemValidator.cs editor tool
  - [ ] Visual presets (Day, Sunset, Overcast)
  - [ ] Performance validation (60 FPS targets)
  - [ ] Documentation updates
  - [ ] Integration and PlayMode tests

---

### Phase 15: Bridge Mode (Background GC2→GSPro Relay)

Use case: Moonlight streaming - run OpenRange on Android in background, stream GSPro from PC.

- [ ] **Prompt 55**: Bridge Mode Architecture
  - [ ] Design background service pattern for each platform
  - [ ] Define minimal UI for background operation
  - [ ] Create BridgeModeManager.cs

- [ ] **Prompt 56**: Android Foreground Service
  - [ ] Create GC2BridgeService.kt (foreground service)
  - [ ] Persistent notification with status
  - [ ] Wake lock management
  - [ ] Service lifecycle with Unity

- [ ] **Prompt 57**: iOS PiP Workaround
  - [ ] Research Picture-in-Picture as background workaround
  - [ ] Minimal video view to keep app active
  - [ ] Alternative: document iOS limitations

- [ ] **Prompt 58**: Bridge Mode UI
  - [ ] Minimal status overlay (connection, shots relayed)
  - [ ] Quick toggle between full app and bridge mode
  - [ ] Notification-based control

- [ ] **Prompt 59**: Bridge Mode Testing
  - [ ] Test with Moonlight streaming
  - [ ] Verify GSPro receives shots while app backgrounded
  - [ ] Battery impact assessment
  - [ ] Documentation

---

## Recent Issue Log

**2026-01-04**: Android app tested on real device - works! TestShotPanel inaccessible without keyboard → Added Prompt 60 for Test Shot button.

**2026-01-04**: Prompt 39 complete (PR #69). Android build infrastructure: AndroidBuildSettings.cs, build_android.sh, Makefile targets, docs/BUILD_ANDROID.md.

**2026-01-03**: Prompts 23-25 complete (PRs #65-67). Android native plugin with async UsbRequest, C# bridge with UnitySendMessage callbacks.

**2026-01-03**: Prompts 36-37 complete (PRs #61, #63). macOS build scripts with signing and notarization.

**2026-01-03**: Prompts 43-45 complete (PRs #55-58). UI refinement - GSPro panel, connection panel, settings dropdown fixes.
