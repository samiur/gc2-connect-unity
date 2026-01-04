# GC2 Connect Unity - Implementation Plan

## Project Overview

Cross-platform driving range simulator connecting to Foresight GC2 launch monitor via USB.
Platforms: macOS (Intel + Apple Silicon), iPad (M1+ with DriverKit), Android tablets (USB Host API).

## Current State

**Test Count**: 1600+ EditMode tests passing
**Phases Complete**: 1-8, plus prompts 23, 32-35, 42-46
**Current Phase**: 9 - Android Native Plugin (Prompts 23-25)
**Next Prompt**: 24 (Android Plugin Implementation) or 25 (Android C# Bridge)

## Phase Breakdown

| Phase | Focus | Prompts | Status |
|-------|-------|---------|--------|
| 1-3 | Core Services & Physics | 1-3 | ✅ Complete |
| 4 | Scenes & Bootstrap | 4-5 | ✅ Complete |
| 5 | Visualization | 6-11 | ✅ Complete |
| 6 | UI System | 12-17 | ✅ Complete |
| 7 | GSPro & Native Plugins | 18-22, 32-35, 42-43 | ✅ Complete |
| 7.5 | UI Refinement | 43-46 | ✅ Complete |
| 8 | macOS Build & Release | 36-37 | ✅ Complete |
| 9 | Android Native Plugin | 23-25 | 🔄 In Progress (23 done) |
| 10 | iPad Native Plugin | 26-28 | ⏳ Pending |
| 11 | Quality & Polish | 29-31 | ⏳ Pending |
| 12-13 | Mobile Builds & CI/CD | 38-41 | ⏳ Future |
| 14 | Visual Enhancements | 47-54 | ⏳ Pending |
| 15 | Bridge Mode (Background) | 55-59 | ⏳ Pending |

---

## Completed Prompts (Summary)

### Phase 1-3: Core Services & Physics
- **Prompt 1**: ShotProcessor Service ✅ (PR #1) - Validates GC2ShotData, runs TrajectorySimulator
- **Prompt 2**: SessionManager Service ✅ (PR #2) - Tracks shots, history, statistics
- **Prompt 3**: SettingsManager ✅ (PR #4) - PlayerPrefs persistence for all settings

### Phase 4: Scenes & Bootstrap
- **Prompt 4**: BootstrapLoader ✅ (PR #6) - Scene loading, DontDestroyOnLoad managers
- **Prompt 5**: PlatformManager & QualityManager ✅ (PR #8) - Dynamic quality adjustment

### Phase 5: Visualization
- **Prompt 6**: Golf Ball Prefab ✅ (PR #9) - BallVisuals, materials, dimple texture
- **Prompt 7**: BallController & BallSpinner ✅ (PR #11) - Trajectory animation, spin rotation
- **Prompt 8**: TrajectoryRenderer ✅ (PR #13) - LineRenderer with quality tiers
- **Prompt 9**: CameraController ✅ (PR #15) - Follow, Orbit, Static, TopDown modes
- **Prompt 10**: Landing Effects ✅ (PR #17) - LandingMarker, ImpactEffect, EffectsManager
- **Prompt 11**: Marina Environment ✅ (PR #19) - DistanceMarker, TargetGreen, TeeMat

### Phase 6: UI System
- **Prompt 12**: UIManager & Layout ✅ (PR #21) - ResponsiveLayout, SafeAreaHandler, UITheme
- **Prompt 13**: Shot Data Bar ✅ (PR #23) - DataTile components, GSPro-style layout
- **Prompt 14**: Club Data Panel ✅ (PR #25) - HMT metrics, SwingPathIndicator, AttackAngleIndicator
- **Prompt 15**: Connection Status UI ✅ (PR #27) - Indicator, panel, toasts
- **Prompt 16**: Session Info Panel ✅ (PR #29) - ShotHistoryPanel, ShotDetailModal
- **Prompt 17**: Settings Panel ✅ (PR #31) - SettingToggle, SettingSlider, SettingDropdown

### Phase 7: GSPro & Native Plugins
- **Prompt 18**: IGC2Connection Update ✅ (PR #39) - GC2DeviceStatus, OnDeviceStatusChanged event
- **Prompt 18b**: TCP Connection ✅ (PR #41) - GC2TCPConnection, GC2TCPListener, GC2TestWindow
- **Prompt 19**: GSPro Client ✅ (PR #43) - GSProMessage, heartbeat, device readiness
- **Prompt 20**: macOS Plugin Header ✅ (PR #45) - GC2MacPlugin.h, Xcode project, build script
- **Prompt 21**: macOS USB Read Loop ✅ (PR #47) - libusb, 0H/0M parsing, misread detection
- **Prompt 22**: macOS C# Bridge ✅ (PR #49) - GC2MacConnection, IL2CPP callbacks, tested with hardware
- **Prompt 32**: Ground Physics - Bounce ✅ (PR #33) - Penner COR model, spin-dependent braking
- **Prompt 33**: Ground Physics - Roll ✅ (PR #35) - Spin-enhanced deceleration, spin-back
- **Prompt 34**: Physics Validation ✅ (PR #37) - TrackMan PGA data validation, landing data
- **Prompt 35**: Ball Ready Indicator ✅ (PR #51) - Visual states, pulse animation
- **Prompt 42**: GSPro Buffer Management ✅ (PR #53) - Response parsing, shot confirmation
- **Prompt 43**: GSPro Mode Panel Fixes ✅ (PR #55) - Layout constants, improved sizing
- **Prompt 44**: Connection Panel Fixes ✅ (PR #57) - Panel height, modal positioning, close button sizing
- **Prompt 45**: Settings Dropdown & UI Polish ✅ (PR #58) - Dropdown z-order, ScrollRect wiring, checkmark sprite, batchmode fix
- **Prompt 46**: Test Shot Panel ✅ (PR #59) - Runtime test shots without GC2 hardware

### Phase 8: macOS Build & Release
- **Prompt 36**: macOS Build Script ✅ (PR #61) - build_macos.sh, Makefile targets
- **Prompt 37**: Code Signing & Notarization ✅ (PR #63) - sign_and_notarize.sh, entitlements

### Phase 9: Android Native Plugin
- **Prompt 23**: Android Plugin Project ✅ (PR #65) - Kotlin/Gradle, async UsbRequest, GC2Protocol

---

## Incomplete Prompts (Full Detail)

### Prompt 44: Connection Panel and Settings Button Fixes ✅ (Completed in PR #57)

```text
Fix the Connection Status panel overflow and truncated Settings button.

Context: The Connection Status modal panel has content overflowing below the panel background, and the Settings button in the top-right is truncated.

Issues identified:
- Connection Panel content extends below panel background
- "USB", "Last Shot:", "No shots yet" text appears outside panel
- "Connect" button is almost entirely hidden at bottom of screen
- Close button (X) is tiny and hard to click
- Panel not vertically centered on screen
- No scrolling for overflow content
- Settings button shows "Sett" instead of "Settings"

Files to modify:
- Assets/Scripts/UI/ConnectionPanel.cs
- Assets/Editor/ConnectionStatusGenerator.cs
- Assets/Prefabs/UI/ConnectionPanel.prefab (regenerate)
- Assets/Scripts/UI/MarinaSceneController.cs (for Settings button)
- Assets/Editor/SceneGenerator.cs (Settings button sizing)

Requirements:

1. Fix Connection Panel height:
   - Calculate proper height to fit all content
   - Or use ScrollRect for content that doesn't fit
   - Ensure minimum height includes all sections:
     * Status header
     * Device info (serial, firmware)
     * Connection mode
     * Last shot time
     * Action buttons (Connect/Disconnect/Retry)
     * Close button

2. Fix panel positioning:
   - Center panel vertically and horizontally on screen
   - Add proper modal overlay (semi-transparent background)
   - Modal should dim/block interaction with elements behind

3. Improve Close button:
   - Increase size to 32x32px minimum (was tiny)
   - Position in top-right corner with proper padding
   - Clear X icon or "Close" text

4. Fix action buttons:
   - Ensure buttons are fully visible within panel
   - Proper spacing between buttons
   - Minimum button width (100px)

5. Add scroll support if needed:
   - If content exceeds max panel height, add ScrollRect
   - Show scrollbar indicator when content overflows

6. Fix Settings button:
   - Increase button width to fit "Settings" text
   - Or use icon-only button with tooltip
   - Ensure text doesn't truncate

7. Update prefab generator:
   - Fix sizing calculations
   - Test with various content lengths

Write/update unit tests for:
- Panel fits all content without overflow
- Close button is clickable (adequate size)
- Modal overlay blocks interaction
- Settings button text is not truncated
```

---

### Prompt 45: Settings Panel Dropdown and General UI Polish ✅ (PR #58 Complete)

```text
Fixed Settings panel dropdown issues and UI polish items.

Completed:
- ✅ Fixed dropdown z-order with Canvas.overrideSorting (sortingOrder = 100)
- ✅ Fixed dropdown ScrollRect (viewport/content wiring was missing)
- ✅ Fixed button click areas (raycastTarget = false on text)
- ✅ Fixed checkmark → Unity's built-in Checkmark.psd sprite (Unicode didn't render)
- ✅ Fixed X button from Unicode "✕" to simple "X" text
- ✅ Moved Settings button to left side (next to Back button)
- ✅ Fixed batchmode scene generation (DisplayDialog returns false in CLI)
- ✅ Dropdown item height 44px for accessibility
- ✅ Dropdown template height shows 4+ items
- ✅ Unit tests: 13 layout validation tests in SettingsPanelGeneratorTests.cs

Key Learnings:
1. TMP_Dropdown requires explicit ScrollRect wiring (scrollRect.viewport/content)
2. EditorUtility.DisplayDialog() returns false in batchmode - use Application.isBatchMode check
3. Unity built-in sprites via AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd")
4. LiberationSans SDF font lacks many Unicode symbols - use Image components instead
```

---

### Prompt 46: Test Shot Panel (Runtime UI)

```text
Create a runtime Test Shot Panel that allows users to fire simulated shots without GC2 hardware in built applications.

Context: The existing TestShotWindow is an Editor-only tool (Assets/Editor/). Users running built applications (macOS, iPad, Android) need a way to test the visualization system without connecting actual GC2 hardware. This is essential for:
- Demo/showcase mode at trade shows
- Development testing on devices
- User experience testing
- Verifying visualization works before hardware setup

Files to create:
- Assets/Scripts/UI/TestShotPanel.cs
- Assets/Editor/TestShotPanelGenerator.cs
- Assets/Tests/EditMode/TestShotPanelTests.cs
- Assets/Prefabs/UI/TestShotPanel.prefab (generated)

Requirements:

1. TestShotPanel.cs - Main panel component:
   - Slide-out panel from left side (opposite of settings)
   - Toggle via button or keyboard shortcut (T key)
   - Sections:
     * Quick Presets: Driver, 7-Iron, Wedge, Hook, Slice (one-tap buttons)
     * Ball Data: Speed, Launch Angle, Direction sliders
     * Spin Data: Backspin, Sidespin sliders
     * Club Data (optional toggle): Club Speed, Attack Angle, Face to Target, Path
   - "Fire Shot" button (large, prominent, green)
   - "Reset Ball" button
   - Environmental conditions toggle (use current SettingsManager values)
   - Event: OnTestShotFired(GC2ShotData)

2. UI Layout:
   - Width: 300px panel on left side
   - CanvasGroup for fade in/out animation
   - ScrollRect if content exceeds screen height
   - Consistent styling with UITheme constants
   - Use existing SettingSlider components for sliders
   - Use existing SettingToggle for toggles

3. Integration with ShotProcessor:
   - Create GC2ShotData from panel values
   - Call ShotProcessor.ProcessShot() to trigger full pipeline:
     * Ball animation (BallController)
     * Trajectory rendering (TrajectoryRenderer)
     * UI updates (ShotDataBar, ClubDataPanel)
     * Session recording (SessionManager)
   - Use environmental conditions from SettingsManager

4. Preset shots (match TestShotWindow values for consistency):
   - Driver: 167 mph, 10.9° launch, 2686 rpm backspin
   - 7-Iron: 120 mph, 16.3° launch, 7097 rpm backspin
   - Wedge: 102 mph, 24.2° launch, 9304 rpm backspin
   - Hook: 150 mph, 12° launch, 3000 rpm back, -1500 rpm side
   - Slice: 150 mph, 12° launch, 3000 rpm back, +1500 rpm side

5. Optional Club Data (for HMT testing):
   - Toggle to show/hide club data section
   - Club Speed (60-130 mph)
   - Attack Angle (-10 to +10 degrees)
   - Face to Target (-10 to +10 degrees)
   - Path (-15 to +15 degrees)
   - Dynamic Loft (5-50 degrees)
   - When enabled, sets shotData.HasClubData = true

6. Scene Integration:
   - Add test shot button to Marina scene header (next to Settings)
   - Button only visible in development builds OR when no GC2 connected
   - MarinaSceneController: Add _testShotPanel serialized field
   - SceneGenerator: Instantiate prefab and wire reference
   - Hide panel when GC2 is connected (real data takes precedence)

7. Keyboard Shortcuts (optional but nice):
   - T: Toggle panel visibility
   - D: Fire Driver preset
   - I: Fire 7-Iron preset
   - W: Fire Wedge preset
   - Space: Fire shot with current settings (when panel open)

8. TestShotPanelGenerator.cs - Editor tool:
   - Menu: OpenRange > Create Test Shot Panel Prefab
   - Creates full panel hierarchy with all UI components
   - Wires up SettingSlider references
   - Sets proper anchoring for left-side panel

Write comprehensive unit tests for:
- Panel show/hide toggle
- Preset values applied correctly
- GC2ShotData created with correct values
- Event fires when shot triggered
- Optional club data included when toggled
- Keyboard shortcut handling (if implemented)
- Integration with ShotProcessor via event
```

---

### Prompt 23: Android Plugin Project ✅ (Completed in PR #65)

```text
Created Android native plugin project with async USB implementation.

Completed:
- ✅ Gradle project (Kotlin 1.9.21, AGP 8.3.0, minSdk 26, targetSdk 34)
- ✅ AndroidManifest.xml with USB host permission
- ✅ USB device filter (VID 0x2C79, PID 0x0110)
- ✅ GC2Plugin.kt - Main entry point, singleton, Unity callbacks
- ✅ GC2Device.kt - Async UsbRequest with 4 queued requests
- ✅ GC2Protocol.kt - 0H/0M parsing, misread detection
- ✅ build_android_plugin.sh - Builds AAR to Assets/Plugins/Android/
- ✅ README.md with async USB architecture documentation

Key Implementation Detail:
The GC2 sends 4+ packets per shot burst at 1-2ms intervals.
Synchronous bulkTransfer() can't keep up, causing packet loss.

Solution: Async UsbRequest pattern matching macOS libusb:
- 4 queued UsbRequest objects for continuous reception
- ConcurrentLinkedQueue for thread-safe packet buffering
- Separate USB reader thread (I/O) and processor thread (parsing)
- Immediate re-queue on completion - no reception gaps
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
   - Find INTERRUPT IN endpoint (address 0x82, type USB_ENDPOINT_XFER_INT)
   - Note: NOT bulk endpoint - GC2 uses interrupt transfers

5. Read thread:
   - Thread with interrupt transfer loop (64-byte packets)
   - 100ms timeout
   - Multi-packet buffer management
   - Message type filtering (0H for shots, 0M for status)
   - Data accumulation until BACK_RPM/SIDE_RPM received

6. Protocol parsing (GC2Protocol.kt per GC2_PROTOCOL.md):
   - Filter lines by prefix: "0H" = shot data, "0M" = device status
   - For 0H messages:
     - Parse key=value format, accumulate across packets
     - Wait for spin components before finalizing shot
     - Detect new shots via SHOT_ID change
     - Handle timeouts (~500ms) if spin never arrives
     - Convert to JSONObject
   - For 0M messages:
     - Parse FLAGS and BALLS fields
     - FLAGS == 7 means ready (green light)
     - BALLS > 0 means ball detected
     - Forward status to Unity via onDeviceStatus callback

7. Device status callback:
   - onDeviceStatus(isReady: Boolean, ballDetected: Boolean)
   - Called when 0M message received with changed status
   - UnitySendMessage with JSON {"isReady": bool, "ballDetected": bool}

8. Misread detection:
   - Reject SPIN_RPM == 0
   - Reject BACK_RPM == 2222 (known error pattern)
   - Reject SPEED_MPH < 10 or > 250
   - Track SHOT_ID for duplicate filtering

9. Unity callbacks:
   - UnityPlayer.UnitySendMessage()
   - Main thread if needed

10. Device attach/detach:
   - BroadcastReceiver for USB_DEVICE_ATTACHED
   - BroadcastReceiver for USB_DEVICE_DETACHED
   - Auto-reconnect on reattach

11. GC2Device.kt:
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
   - OnNativeDeviceStatus(string json) - {"isReady": bool, "ballDetected": bool}
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

### Prompt 36: macOS Build Script and Configuration

```text
Create a comprehensive build script for macOS that handles native plugin compilation, Unity build, and app bundle creation.

Context: The macOS app needs to be distributed as a signed, notarized .app bundle or DMG. This requires building the native plugin, the Unity project, and packaging everything correctly.

Create/Update Makefile:

1. Add new build targets:
   - `make build-plugin` - Build native macOS plugin only
   - `make build-app` - Full macOS app build (plugin + Unity)
   - `make build-release` - Release build with version tagging
   - `make package` - Create DMG for distribution

2. Plugin build integration:
   - Call `NativePlugins/macOS/build_mac_plugin.sh`
   - Verify plugin builds successfully before Unity build
   - Copy plugin bundle and libusb.dylib to correct location

3. Unity build configuration:
   - Use `-buildOSXUniversalPlayer` for universal (Intel + Apple Silicon)
   - Configure player settings via `-executeMethod`
   - Set version number from git tag or parameter

Create Scripts/build_macos.sh:

1. Pre-build validation:
   - Check Unity version installed
   - Check Xcode installed
   - Check libusb available
   - Verify all required files exist

2. Native plugin build:
   - Build GC2MacPlugin.bundle
   - Verify bundle architecture (universal or native)
   - Copy to Assets/Plugins/macOS/

3. Unity project build:
   - Run EditMode tests first (fail fast)
   - Build player with IL2CPP
   - Output to Builds/macOS/OpenRange.app

4. Post-build steps:
   - Verify app bundle structure
   - Check native plugin is included
   - Verify libusb.dylib is bundled
   - Fix @rpath references if needed

5. Version management:
   - Read version from git tag or VERSION file
   - Update ProjectSettings/ProjectSettings.asset
   - Embed version in app bundle

Update CLAUDE.md:
- Add "Building for macOS" section with full instructions
- Document build requirements
- Explain build script options

Create docs/BUILD_MACOS.md:
- Detailed build instructions
- Troubleshooting guide
- Release checklist

Write verification:
- Build completes without errors
- App launches on clean macOS system
- Native plugin loads correctly
- GC2 device detection works (if hardware available)
```

---

### Prompt 37: macOS Code Signing and Notarization

```text
Add code signing and notarization to the macOS build process for distribution outside the App Store.

Context: macOS Gatekeeper requires apps to be signed and notarized for users to run them without security warnings.

Prerequisites:
- Apple Developer account ($99/year)
- Developer ID Application certificate
- Developer ID Installer certificate (for PKG)
- App-specific password for notarization

Create Scripts/sign_and_notarize.sh:

1. Code signing:
   - Sign libusb.dylib with hardened runtime
   - Sign GC2MacPlugin.bundle with hardened runtime
   - Sign OpenRange.app with:
     - --deep (sign all nested code)
     - --force (replace existing signatures)
     - --options runtime (enable hardened runtime)
     - --entitlements entitlements.plist
   - Verify signatures with codesign --verify --deep --strict

2. Entitlements (Scripts/entitlements.plist):
   - com.apple.security.cs.allow-unsigned-executable-memory (for IL2CPP)
   - com.apple.security.device.usb (for USB access)
   - com.apple.security.cs.disable-library-validation (for libusb)

3. Notarization:
   - Create ZIP of signed app
   - Submit to Apple: xcrun notarytool submit
   - Wait for completion: xcrun notarytool wait
   - Check result and handle errors
   - Staple ticket: xcrun stapler staple

4. Create DMG:
   - Use create-dmg or hdiutil
   - Include app and README
   - Sign and notarize DMG itself
   - Final output: OpenRange-{version}.dmg

5. Update Makefile targets:
   - `make sign` - Sign app bundle
   - `make notarize` - Submit for notarization and staple
   - `make dmg` - Create signed DMG
   - `make release-macos` - Full release pipeline (build → sign → notarize → dmg)

6. Environment variables for credentials:
   - APPLE_TEAM_ID
   - APPLE_DEVELOPER_ID
   - APPLE_APP_PASSWORD
   - KEYCHAIN_PASSWORD (for CI)

Create Scripts/setup_signing.sh:
- Import certificates from base64 env vars
- Create temporary keychain for CI
- Unlock keychain and set ACLs

Document:
- Code signing requirements
- Certificate management
- Troubleshooting notarization failures
- Common errors and solutions

Write verification:
- Signed app passes: codesign --verify --deep --strict
- Notarization succeeds: xcrun notarytool log shows "Accepted"
- DMG opens on fresh macOS without Gatekeeper warnings
```

---

### Prompt 38: iOS Build Configuration [Future]

```text
[FUTURE - Requires DriverKit entitlements from Apple]

Create build configuration and scripts for iOS/iPad deployment.

Context: The iPad version requires DriverKit extension for USB access. This prompt prepares the build infrastructure once entitlements are approved.

Prerequisites:
- Apple Developer account with DriverKit entitlements
- Distribution certificates and provisioning profiles
- iPad with M1 chip or later for testing

Create Scripts/build_ios.sh:

1. Pre-build:
   - Verify Xcode version (15+)
   - Check provisioning profiles
   - Verify DriverKit entitlements

2. Native plugin build:
   - Build GC2iOSPlugin.framework
   - Build GC2Driver.dext (system extension)
   - Verify entitlements in both

3. Unity iOS build:
   - Generate Xcode project: -buildTarget iOS
   - Apply iOS-specific settings
   - Include native plugin

4. Xcode project modification:
   - Add DriverKit extension target
   - Configure signing for both targets
   - Set deployment target (iOS 16+)

5. Archive and export:
   - xcodebuild archive
   - Export for App Store or Ad Hoc

Create configs/ios/ExportOptions.plist:
- App Store distribution settings
- Ad Hoc distribution settings

Update Makefile:
- `make build-ios` - Build iOS Xcode project
- `make archive-ios` - Create archive
- `make export-ios` - Export IPA

Note: This prompt is marked [Future] as it requires DriverKit entitlements which may take weeks to obtain from Apple.
```

---

### Prompt 39: Android Build Configuration [Future]

```text
[FUTURE - Requires Android native plugin completion (Prompts 23-25)]

Create build configuration and scripts for Android deployment.

Context: The Android version uses USB Host API which is standard Android functionality (no special permissions needed beyond manifest).

Prerequisites:
- Android SDK with API level 26+
- Android Studio or command-line tools
- Keystore for signing

Create Scripts/build_android.sh:

1. Pre-build validation:
   - Check Android SDK path
   - Verify Gradle version
   - Check keystore exists

2. Native plugin build:
   - Run ./gradlew assembleRelease in NativePlugins/Android/
   - Copy AAR to Assets/Plugins/Android/
   - Verify AAR contents

3. Unity Android build:
   - Configure player settings:
     - Minimum API: 26
     - Target API: 34
     - IL2CPP backend
     - ARM64 architecture
   - Build APK: -buildTarget Android
   - Or Build AAB: -exportAsGooglePlayBundle

4. Signing:
   - Sign with release keystore
   - Use zipalign for APK
   - Verify signature

5. Output:
   - OpenRange-{version}.apk (side-loading)
   - OpenRange-{version}.aab (Play Store)

Create configs/android/keystore.properties.template:
- Template for keystore configuration
- Never commit actual credentials

Update Makefile:
- `make build-android-plugin` - Build native AAR
- `make build-android` - Full Android build
- `make apk` - Create signed APK
- `make aab` - Create signed AAB for Play Store

Note: This prompt is marked [Future] as it depends on Android native plugin completion (Prompts 23-25).
```

---

### Prompt 40: Mobile Build Environment Setup [Future]

```text
[FUTURE - Supporting infrastructure for iOS and Android builds]

Create environment setup scripts and documentation for mobile builds.

Context: Developers and CI systems need consistent environment configuration for mobile builds.

Create Scripts/setup_mobile_env.sh:

1. iOS environment:
   - Verify Xcode installation
   - Install required simulators
   - Configure code signing
   - Setup DriverKit development profile

2. Android environment:
   - Install/update Android SDK
   - Accept licenses
   - Install required build tools
   - Configure ANDROID_HOME

3. Unity license activation:
   - Support for serial license
   - Support for Unity Plus/Pro floating license
   - Handle CI activation/deactivation

Create configs/mobile-requirements.txt:
- Minimum Xcode version
- Minimum Android SDK version
- Required Unity modules
- Recommended device specs

Update CLAUDE.md:
- Add "Mobile Development Setup" section
- Link to detailed docs

Create docs/MOBILE_DEVELOPMENT.md:
- Full environment setup guide
- Platform-specific notes
- Troubleshooting section

Note: This prompt is marked [Future] as it depends on iOS (Prompt 38) and Android (Prompt 39) build configurations.
```

---

### Prompt 41: GitHub Actions Release Workflow

```text
Create GitHub Actions workflow for automated testing, building, and releasing all platform binaries.

Context: Automate the release process to create consistent, tested builds for macOS, iOS, and Android whenever a release is tagged.

Create .github/workflows/release.yml:

1. Trigger configuration:
   - On push to tags matching 'v*' (e.g., v1.0.0)
   - Manual trigger with version input

2. Job: test
   - Reuse existing test workflow
   - Must pass before build jobs run

3. Job: build-macos (runs-on: macos-latest)
   - Checkout with LFS
   - Install Unity via game-ci/unity-builder
   - Build native plugin (requires Xcode)
   - Build Unity project
   - Sign and notarize (using secrets)
   - Create DMG
   - Upload artifact

4. Job: build-ios (runs-on: macos-latest) [Conditional]
   - Skip if DriverKit not ready
   - Build Xcode project
   - Archive and export
   - Sign with distribution certificate
   - Upload IPA artifact

5. Job: build-android (runs-on: ubuntu-latest) [Conditional]
   - Skip if native plugin not ready
   - Build native AAR
   - Build Unity project
   - Sign APK/AAB
   - Upload artifacts

6. Job: create-release (runs-on: ubuntu-latest)
   - Depends on all build jobs
   - Download all artifacts
   - Create GitHub Release
   - Upload release assets:
     - OpenRange-{version}-macOS.dmg
     - OpenRange-{version}-iOS.ipa (when ready)
     - OpenRange-{version}-Android.apk (when ready)
   - Generate release notes from commits

Secrets required:
- UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD
- APPLE_TEAM_ID, APPLE_DEVELOPER_ID, APPLE_APP_PASSWORD
- APPLE_CERTIFICATE_BASE64, APPLE_CERTIFICATE_PASSWORD
- ANDROID_KEYSTORE_BASE64, ANDROID_KEYSTORE_PASSWORD
- GITHUB_TOKEN (automatic)

Create .github/workflows/build-check.yml:
- Run on PR to verify builds complete
- Don't create releases, just verify

Update README.md:
- Add badges for CI status
- Document release process

Create docs/RELEASE_PROCESS.md:
- How to create a release
- Version numbering scheme
- Pre-release checklist
- Post-release verification

Write verification:
- Workflow runs on tag push
- All platform builds complete (or skip gracefully)
- GitHub Release created with correct assets
- Release notes generated correctly
```

---

## Phase 14: Visual Enhancements

Visual inspiration from:
- **ProceduralGolf** (SolomonBaarda): Toon shaders, water with foam, stylized skybox, outline rendering
- **Super-Golf** (jzgom067): Tropical island aesthetic, trail renderers, dramatic landscapes
- **golf_simulator** (JanWalsh91): Custom shaders (14% ShaderLab), beautiful landscapes

Key visual elements to implement:
1. Stylized skybox with volumetric clouds
2. Improved grass shader with wind animation
3. Enhanced water shader with foam and reflections
4. Toon/outline shader option for stylized mode
5. Improved ball trail with gradient glow
6. Post-processing effects (bloom, color grading, ambient occlusion)
7. Environment props (trees, rocks, scenery)
8. Dramatic lighting setup

---

### Prompt 47: Stylized Skybox and Lighting Setup

```text
Create a dramatic skybox and lighting configuration for the Marina driving range.

Context: Looking at reference projects like Super-Golf and ProceduralGolf, they use stylized skyboxes with volumetric-looking clouds that create a dramatic atmosphere. The current Marina scene likely uses default lighting. We need a cohesive visual atmosphere.

Inspiration: The SuperGolf screenshot shows:
- Blue sky with white fluffy clouds
- Dramatic mountain silhouettes
- Warm/cool color contrast
- Clear horizon line

Files to create:
- Assets/Shaders/Skybox/StylizedSkybox.shader
- Assets/Materials/Skybox/MarinaSkybox.mat
- Assets/Scripts/Visualization/DayNightCycle.cs (optional, for future expansion)
- Assets/Editor/LightingSetupGenerator.cs
- Assets/Tests/EditMode/SkyboxTests.cs

Requirements:

1. StylizedSkybox.shader (URP compatible):
   - Procedural gradient sky (horizon to zenith color blend)
   - Sun position parameter affecting sky color
   - Cloud layer using noise texture or procedural noise
   - Horizon fog/haze effect
   - HDR output for bloom compatibility
   - Properties:
     * _TopColor (HDR color for zenith)
     * _HorizonColor (HDR color for horizon)
     * _SunColor (HDR for sun disc)
     * _SunSize (float, sun disc size)
     * _CloudDensity (float, 0-1)
     * _CloudSpeed (float, animation speed)
     * _HorizonFogDensity (float)

2. MarinaSkybox.mat:
   - Use StylizedSkybox shader
   - Preset for "Golden Hour" look:
     * Top: Deep blue (0.1, 0.3, 0.8)
     * Horizon: Warm orange-pink (1.0, 0.6, 0.4)
     * Sun: Bright yellow-white (1.5, 1.4, 1.0) HDR
   - Alternative "Clear Day" preset values documented

3. Lighting Configuration:
   - Directional light as sun:
     * Soft shadows
     * Warm color temperature (5500-6500K)
     * Rotation matching skybox sun position
   - Ambient lighting:
     * Gradient mode (sky/equator/ground colors)
     * Sky color from skybox top
     * Ground color darker/cooler
   - Reflection probe for environmental reflections

4. LightingSetupGenerator.cs:
   - Menu: OpenRange > Lighting > Setup Marina Lighting
   - Creates/configures directional light
   - Sets RenderSettings for ambient
   - Creates skybox material if missing
   - Applies to current scene

5. Integration with QualityManager:
   - High: Full skybox, soft shadows, reflection probe
   - Medium: Simplified skybox, hard shadows, no probe
   - Low: Solid color skybox, no shadows

Write unit tests for:
- Shader compiles without errors
- Material properties are accessible
- Lighting generator creates expected objects
- Quality tier changes affect lighting settings
```

---

### Prompt 48: Enhanced Grass Shader with Wind Animation

```text
Create a stylized grass shader with wind animation for the Marina driving range terrain.

Context: The current grass materials are likely basic URP Lit materials. ProceduralGolf uses stylized toon rendering, and real golf courses have grass that moves in the wind, adding life to the scene.

Files to create:
- Assets/Shaders/Environment/StylizedGrass.shader
- Assets/Shaders/Environment/StylizedGrass.hlsl (include file)
- Assets/Materials/Environment/FairwayGrassEnhanced.mat
- Assets/Materials/Environment/RoughGrass.mat
- Assets/Materials/Environment/GreenGrass.mat
- Assets/Scripts/Visualization/WindController.cs
- Assets/Editor/GrassShaderSetup.cs
- Assets/Tests/EditMode/GrassShaderTests.cs

Requirements:

1. StylizedGrass.shader (URP Lit-based):
   - Base color with albedo texture support
   - Wind animation via vertex displacement:
     * Global wind direction (Vector3)
     * Wind strength parameter
     * Wind turbulence (noise-based variation)
     * Height-based influence (grass tips move more)
   - Color variation:
     * Tip color tint (lighter at grass tips)
     * Shadow color tint (darker in shadows)
   - Optional subsurface scattering approximation for backlit grass
   - Properties:
     * _BaseColor, _BaseMap
     * _TipColor (color at grass blade tips)
     * _WindDirection (Vector4, xyz = direction, w = strength)
     * _WindSpeed (animation speed)
     * _WindTurbulence (noise scale)
     * _GrassHeight (for vertex displacement scaling)

2. WindController.cs:
   - Singleton managing global wind parameters
   - Properties:
     * WindDirection (Vector3)
     * WindStrength (0-1 normalized)
     * WindSpeed (m/s for physics)
     * Gusting (bool, enables variation)
   - Updates Shader.SetGlobalVector("_GlobalWindDirection")
   - Optional: Gusting with Perlin noise variation
   - Integration with SettingsManager wind settings
   - Events: OnWindChanged

3. Material Presets:
   - FairwayGrassEnhanced: Short, well-maintained look
     * Bright green base
     * Subtle tip lightening
     * Low grass height (minimal wind effect)
   - RoughGrass: Longer, wilder grass
     * Darker/yellower green
     * More wind movement
     * Higher grass height
   - GreenGrass: Putting green
     * Very short, uniform
     * Minimal/no wind effect
     * Smooth, manicured look

4. GrassShaderSetup.cs:
   - Menu: OpenRange > Materials > Create Grass Materials
   - Creates all grass material presets
   - Applies to terrain if present
   - Creates WindController if missing

5. Performance considerations:
   - Shader LOD for quality tiers
   - Disable wind animation on Low quality
   - Vertex animation only (no geometry shader)

Write unit tests for:
- Shader compiles without errors
- WindController updates global shader properties
- Material presets have correct default values
- Quality tier affects wind animation toggle
```

---

### Prompt 49: Water Shader with Foam and Reflections

```text
Create a stylized water shader for water hazards and the marina backdrop.

Context: ProceduralGolf uses the "Simple Water Shader URP" by IgniteCoders which features scrolling normals, depth-based foam, and wave animation. Water adds significant visual interest and the Marina scene should have ocean/lake backdrop.

Files to create:
- Assets/Shaders/Environment/StylizedWater.shader
- Assets/Shaders/Environment/WaterFunctions.hlsl
- Assets/Materials/Environment/OceanWater.mat
- Assets/Materials/Environment/PondWater.mat
- Assets/Scripts/Visualization/WaterController.cs
- Assets/Editor/WaterSetupGenerator.cs
- Assets/Tests/EditMode/WaterShaderTests.cs

Requirements:

1. StylizedWater.shader (URP compatible):
   - Base water color (deep and shallow blend based on depth)
   - Scrolling normal maps for wave appearance:
     * Two normal maps scrolling in different directions
     * Blended for complex wave patterns
   - Depth-based effects (requires depth texture):
     * Shallow water lighter color
     * Foam line at water-object intersection
     * Transparency falloff
   - Reflection:
     * Planar reflection (High quality)
     * Cubemap fallback (Medium/Low)
     * Fresnel-based reflection intensity
   - Wave vertex animation:
     * Gerstner wave approximation or sine-based
     * Configurable amplitude and frequency
   - Properties:
     * _ShallowColor, _DeepColor
     * _NormalMap1, _NormalMap2
     * _NormalScale, _NormalSpeed1, _NormalSpeed2
     * _FoamColor, _FoamThreshold, _FoamSoftness
     * _WaveAmplitude, _WaveFrequency, _WaveSpeed
     * _ReflectionStrength, _FresnelPower

2. WaterController.cs:
   - Manages water plane instances
   - Planar reflection camera setup (High quality):
     * Creates reflection camera
     * Renders to RenderTexture
     * Passes texture to water material
   - Quality tier handling:
     * High: Planar reflection + foam + waves
     * Medium: Cubemap reflection + foam, reduced waves
     * Low: Solid color + basic scrolling, no foam

3. Material Presets:
   - OceanWater: Open water, darker blue, larger waves
   - PondWater: Calm water hazard, lighter, minimal waves

4. WaterSetupGenerator.cs:
   - Menu: OpenRange > Environment > Create Water Prefab
   - Creates water plane with material
   - Configures WaterController
   - Optional: Creates ocean backdrop plane for Marina

5. Normal map textures:
   - Create procedural normal map generation OR
   - Document how to import water normal textures
   - Include simple tiling normal in shader as fallback

6. Integration:
   - Depth texture must be enabled in URP asset
   - Document URP settings required

Write unit tests for:
- Shader compiles without errors
- WaterController quality tier switching
- Material depth fade works (mock depth)
- Foam threshold affects output
```

---

### Prompt 50: Post-Processing Volume Configuration

```text
Configure post-processing effects for cinematic visual quality.

Context: Modern golf games use post-processing to enhance visuals. ProceduralGolf mentions SSAO support, and the SuperGolf screenshot shows subtle bloom on the sky. URP's Volume system provides these effects.

Files to create:
- Assets/Settings/PostProcessing/MarinaVolume.asset
- Assets/Settings/PostProcessing/HighQualityProfile.asset
- Assets/Settings/PostProcessing/MediumQualityProfile.asset
- Assets/Settings/PostProcessing/LowQualityProfile.asset
- Assets/Scripts/Visualization/PostProcessingController.cs
- Assets/Editor/PostProcessingSetupGenerator.cs
- Assets/Tests/EditMode/PostProcessingTests.cs

Requirements:

1. Volume Profiles (URP Post-Processing):

   HighQualityProfile.asset:
   - Bloom:
     * Threshold: 0.9
     * Intensity: 0.5
     * Scatter: 0.7
     * Tint: Warm white
   - Color Adjustments:
     * Saturation: +10
     * Contrast: +5
     * Post Exposure: 0
   - Vignette:
     * Intensity: 0.2
     * Smoothness: 0.4
   - Ambient Occlusion (SSAO):
     * Intensity: 0.5
     * Radius: 0.3
   - Depth of Field (optional, for replay mode):
     * Mode: Bokeh
     * Focus distance: Ball position
   - Motion Blur (optional):
     * Intensity: 0.1 (only during fast camera moves)

   MediumQualityProfile.asset:
   - Bloom: Intensity 0.3, lower quality
   - Color Adjustments: Same as High
   - Vignette: Same
   - No SSAO, no DOF, no Motion Blur

   LowQualityProfile.asset:
   - Bloom: Disabled
   - Color Adjustments: Saturation +5 only
   - No other effects

2. PostProcessingController.cs:
   - Manages active Volume based on quality tier
   - Methods:
     * SetQualityTier(QualityTier) - swaps profile
     * SetDepthOfFieldFocus(Vector3 target) - for cinematic shots
     * EnableMotionBlur(bool) - toggle during fast transitions
   - References Volume component in scene
   - Integration with QualityManager.OnQualityTierChanged

3. MarinaVolume.asset:
   - Global Volume with default Medium profile
   - Priority 0 (base effects)

4. PostProcessingSetupGenerator.cs:
   - Menu: OpenRange > Post-Processing > Create Volume Profiles
   - Creates all profile assets
   - Menu: OpenRange > Post-Processing > Add Volume to Scene
   - Adds Global Volume to current scene with controller

5. URP Asset Configuration:
   - Document required URP settings:
     * Post-processing enabled
     * HDR enabled
     * Depth texture enabled (for SSAO)
   - Update existing URP quality assets if needed

6. Scene Integration:
   - SceneGenerator.cs: Add Volume to Marina scene
   - Wire PostProcessingController to MarinaSceneController

Write unit tests for:
- Profile assets created with expected overrides
- PostProcessingController quality switching
- Volume component properly configured
- Effects enabled/disabled per tier
```

---

### Prompt 51: Enhanced Ball Trail and Trajectory Visuals

```text
Enhance the ball trail and trajectory line visuals for more dramatic flight visualization.

Context: The SuperGolf screenshot shows a thin white trajectory line arcing through the sky. ProceduralGolf likely uses stylized trails. Current implementation uses basic LineRenderer and TrailRenderer. We want a more polished, glowing effect.

Files to create:
- Assets/Shaders/Effects/TrailGlow.shader
- Assets/Shaders/Effects/TrajectoryLine.shader
- Assets/Materials/Effects/EnhancedTrajectory.mat
- Assets/Materials/Effects/EnhancedBallTrail.mat
- Assets/Scripts/Visualization/TrajectoryEnhancer.cs
- Assets/Editor/TrajectoryVisualsGenerator.cs
- Assets/Tests/EditMode/EnhancedTrajectoryTests.cs

Requirements:

1. TrailGlow.shader (for ball trail):
   - Additive blending for glow effect
   - Gradient along trail length (bright at ball, fade out)
   - HDR color support for bloom interaction
   - Soft edges using alpha gradient
   - Properties:
     * _TrailColor (HDR color)
     * _GlowIntensity (multiplier for HDR)
     * _FadeLength (how quickly trail fades)
     * _TrailWidth
   - Vertex color support for gradient

2. TrajectoryLine.shader (for arc preview/history):
   - Dashed or solid line options
   - Glow effect (softer than trail)
   - Gradient along path (start to end color)
   - Optional: Animated dash movement
   - Properties:
     * _LineColor (HDR color)
     * _GlowIntensity
     * _DashLength, _GapLength (0 for solid)
     * _DashSpeed (animation speed)

3. Material Presets:
   - EnhancedTrajectory.mat:
     * White with subtle cyan tint (matching SuperGolf)
     * Low glow intensity (not overpowering)
     * Solid line (no dashes for actual flight)
   - EnhancedBallTrail.mat:
     * Bright white/cyan core
     * Higher HDR intensity for bloom
     * Quick fade (short trail length)

4. TrajectoryEnhancer.cs:
   - Component to enhance existing TrajectoryRenderer
   - Adds secondary "glow" LineRenderer behind main line
   - Configurable glow parameters
   - Quality tier support:
     * High: Glow + main line + HDR
     * Medium: Main line only, subtle glow
     * Low: Basic line, no glow
   - Optional: Trail particles along trajectory

5. Update existing components:
   - BallVisuals.cs: Use new trail material
   - TrajectoryRenderer.cs: Support for enhanced materials

6. TrajectoryVisualsGenerator.cs:
   - Menu: OpenRange > Visuals > Create Enhanced Trajectory Materials
   - Creates shaders and materials
   - Menu: OpenRange > Visuals > Upgrade Ball Trail
   - Updates existing BallTrail prefab/material

7. Color Customization:
   - Allow trajectory color to indicate shot quality:
     * Perfect shot: Gold/yellow glow
     * Good shot: White/cyan
     * Mishit: Orange/red tint
   - TrajectoryRenderer.SetShotQualityColor(ShotQuality)

Write unit tests for:
- Shaders compile without errors
- Materials have HDR colors
- TrajectoryEnhancer quality tier switching
- Color customization works
```

---

### Prompt 52: Environment Props - Trees and Scenery

```text
Add environmental scenery to the Marina driving range for visual interest.

Context: Super-Golf features dramatic mountain/island scenery. ProceduralGolf uses low-poly tree and rock assets. The Marina scene needs background scenery to feel like a real driving range location.

Files to create:
- Assets/Prefabs/Environment/Props/PalmTree.prefab
- Assets/Prefabs/Environment/Props/RockCluster.prefab
- Assets/Prefabs/Environment/Props/DistantMountain.prefab
- Assets/Prefabs/Environment/Props/FlagPole.prefab
- Assets/Materials/Environment/Foliage.mat
- Assets/Materials/Environment/Rock.mat
- Assets/Scripts/Visualization/PropPlacer.cs
- Assets/Scripts/Visualization/LODController.cs
- Assets/Editor/EnvironmentPropsGenerator.cs
- Assets/Tests/EditMode/EnvironmentPropsTests.cs

Requirements:

1. Low-Poly Prop Meshes (create procedurally or document asset sources):

   PalmTree.prefab:
   - Simple trunk (cylinder/cone)
   - 4-6 frond planes with alpha texture
   - LOD Group: High (full), Medium (fewer fronds), Low (billboard)
   - Optional: Frond wind animation via shader
   - Foliage material with alpha cutout

   RockCluster.prefab:
   - 3-5 rock meshes grouped
   - Low-poly stylized shapes
   - LOD Group: High (detailed), Low (simplified)
   - Rock material with subtle normal detail

   DistantMountain.prefab:
   - Large background silhouette mesh
   - Simplified geometry
   - Atmospheric fog shader (fades with distance)
   - Non-collidable (visual only)

   FlagPole.prefab:
   - Pole mesh + flag plane
   - Flag wind animation (vertex shader)
   - Target green flag appearance

2. Materials:
   - Foliage.mat: Alpha cutout, backface rendering, wind support
   - Rock.mat: Stylized color with subtle detail
   - Mountain.mat: Fog/distance fade shader

3. PropPlacer.cs:
   - Runtime prop placement manager
   - Spawns props around range perimeter
   - Respects exclusion zones (fairway, greens)
   - Quality tier adjustments:
     * High: Full prop density, all LODs
     * Medium: 50% density, force lower LODs
     * Low: 25% density, billboards only
   - Randomization: rotation, scale variation

4. LODController.cs:
   - Manages LOD distances based on quality tier
   - Aggressive culling for distant props
   - Integrates with Unity LOD system

5. EnvironmentPropsGenerator.cs:
   - Menu: OpenRange > Environment > Create Prop Prefabs
   - Creates basic procedural meshes for props
   - Menu: OpenRange > Environment > Place Marina Props
   - Runs PropPlacer to populate Marina scene
   - Saves placed props as scene objects

6. Marina Scene Layout:
   - Palm trees along sides of range
   - Rock clusters near boundaries
   - Distant mountains on horizon
   - Scattered small props (benches, signs)

7. Performance:
   - GPU instancing for repeated props
   - Occlusion culling friendly
   - Batching for static props

Write unit tests for:
- Prefabs have LODGroup components
- PropPlacer respects exclusion zones
- Quality tier affects prop density
- Materials use correct shaders
```

---

### Prompt 53: Toon/Outline Shader Option

```text
Create an optional toon shader mode for a stylized visual style inspired by ProceduralGolf.

Context: ProceduralGolf uses DELTation's URP Toon Shader for a cartoon-like appearance with rim lighting and outlines. This could be an alternative visual style option in Settings.

Files to create:
- Assets/Shaders/Toon/ToonLit.shader
- Assets/Shaders/Toon/ToonOutline.shader
- Assets/Shaders/Toon/ToonFunctions.hlsl
- Assets/Materials/Toon/ToonBall.mat
- Assets/Materials/Toon/ToonGrass.mat
- Assets/Materials/Toon/ToonEnvironment.mat
- Assets/Scripts/Visualization/ToonModeController.cs
- Assets/Editor/ToonMaterialGenerator.cs
- Assets/Tests/EditMode/ToonShaderTests.cs

Requirements:

1. ToonLit.shader (URP compatible):
   - Cel/toon shading with configurable steps:
     * 2-step color ramp (light/shadow)
     * Adjustable threshold and smoothness
   - Rim lighting (Fresnel effect):
     * HDR rim color for bloom
     * Rim power (thickness)
   - Specular highlights:
     * Hard cutoff specular
     * Optional anisotropic for stylized highlights
   - Properties:
     * _BaseColor, _BaseMap
     * _ShadowColor, _ShadowThreshold, _ShadowSmoothness
     * _RimColor (HDR), _RimPower, _RimThreshold
     * _SpecularColor, _SpecularSize, _SpecularSmoothness

2. ToonOutline.shader (Renderer Feature or second pass):
   - Inverted hull technique:
     * Render back faces only
     * Vertex extrusion along normals
     * Solid outline color
   - Properties:
     * _OutlineColor
     * _OutlineWidth
     * _OutlineZOffset (prevent z-fighting)
   - Clip space width option (consistent outline regardless of distance)

3. Material Presets:
   - ToonBall.mat: White with cyan rim, sharp specular
   - ToonGrass.mat: Green with darker green shadow, subtle rim
   - ToonEnvironment.mat: Configurable base for props

4. ToonModeController.cs:
   - Toggle between realistic and toon rendering modes
   - Methods:
     * SetToonMode(bool enabled)
     * IsToonModeEnabled
   - Material swapping:
     * Stores original material references
     * Swaps to toon variants when enabled
     * Restores originals when disabled
   - Integration with SettingsManager (new ToonMode setting)

5. ToonMaterialGenerator.cs:
   - Menu: OpenRange > Toon > Create Toon Materials
   - Creates toon variants of existing materials
   - Menu: OpenRange > Toon > Setup Outline Renderer Feature
   - Configures URP Renderer with outline feature

6. URP Renderer Feature Setup:
   - Document how to add outline renderer feature
   - Or create custom renderer feature for outlines

7. Settings Integration:
   - Add "Visual Style" dropdown to Settings Panel:
     * Realistic (default)
     * Stylized/Toon
   - SettingsManager: VisualStyle property
   - Persists across sessions

Write unit tests for:
- Toon shader compiles
- Outline shader compiles
- ToonModeController material swapping
- Settings persistence for visual style
```

---

### Prompt 54: Visual Polish and Integration

```text
Final visual polish pass integrating all new visual systems and ensuring they work together.

Context: Previous prompts created individual visual systems (skybox, grass, water, post-processing, trails, props, toon mode). This prompt integrates everything, fixes any conflicts, and adds final polish.

Files to create/modify:
- Assets/Scripts/Visualization/VisualManager.cs
- Assets/Editor/VisualSystemValidator.cs
- Assets/Tests/EditMode/VisualIntegrationTests.cs
- Assets/Tests/PlayMode/VisualSystemTests.cs

Requirements:

1. VisualManager.cs:
   - Central manager for all visual systems
   - Coordinates:
     * PostProcessingController
     * WindController
     * WaterController
     * ToonModeController
     * PropPlacer
     * LODController
   - Quality tier propagation to all systems
   - Initialization order management
   - Methods:
     * Initialize() - sets up all visual systems
     * SetQualityTier(QualityTier) - updates all systems
     * SetToonMode(bool) - coordinates toon switching
     * RefreshVisuals() - force update all systems

2. Scene Integration:
   - Update SceneGenerator.cs to include:
     * VisualManager creation
     * Skybox setup
     * Post-processing Volume
     * Environment props
     * Water plane (if Marina has water)
   - Update MarinaSceneController to reference VisualManager

3. Quality Tier Polish:
   - Verify all systems respond correctly to tier changes
   - Create quality presets that feel cohesive:
     * High: All effects, full props, reflections
     * Medium: Core effects, reduced props, no reflections
     * Low: Minimal effects, sparse props, simple shaders
   - Smooth transitions when quality changes

4. Performance Validation:
   - Target frame rates:
     * High: 60 FPS on M1 Mac
     * Medium: 60 FPS on mid-range devices
     * Low: 30 FPS on low-end devices
   - Profile and optimize if needed

5. VisualSystemValidator.cs:
   - Editor tool to validate visual setup
   - Menu: OpenRange > Visuals > Validate Visual Systems
   - Checks:
     * All required components present
     * Materials using correct shaders
     * Post-processing configured
     * Skybox assigned
     * LOD distances appropriate

6. Visual Presets:
   - Create named visual presets:
     * "Marina Day" (default)
     * "Marina Sunset"
     * "Marina Overcast"
   - Each preset configures: skybox colors, lighting, post-processing

7. Polish Items:
   - Ball visibility: Ensure ball is visible against all backgrounds
   - Trajectory contrast: Line visible against sky and ground
   - Landing marker visibility: Clear against all surfaces
   - Text readability: UI text readable with all lighting

8. Documentation:
   - Update CLAUDE.md with visual system architecture
   - Document shader properties and customization
   - List quality tier effects on each system

Write comprehensive tests:
- All visual managers initialize correctly
- Quality tier changes propagate to all systems
- Toon mode switches all materials correctly
- Visual presets apply expected settings
- Performance within targets per quality tier
```

---

## Phase 15: Bridge Mode (Background Processing)

This phase enables running the GC2 → GSPro bridge in the background on mobile devices, allowing users to simultaneously run Moonlight or other streaming apps to play GSPro remotely.

**Use Case**: User connects GC2 to Android tablet, enables bridge mode, then switches to Moonlight to stream GSPro from Windows PC. The OpenRange app continues relaying GC2 shots to GSPro even while in the background.

**Platform Considerations**:
- **Android**: Well-supported via Foreground Service with persistent notification
- **iOS/iPad**: Very restrictive; requires workarounds with documented limitations

---

### Prompt 55: Bridge Mode Core Architecture

```text
Create the core C# architecture for Bridge Mode - a minimal USB→GSPro relay that runs without visualization.

Context: When using the app as a GSPro bridge alongside a streaming app (like Moonlight), we don't need physics calculations, ball animation, or 3D visualization. We only need:
1. Read shot data from GC2 via USB
2. Send shot data to GSPro via TCP
3. Send heartbeat to GSPro every 2 seconds
4. Update device status (ball detected, ready state)

This prompt creates the Unity/C# side. Native platform integration comes in subsequent prompts.

Files to create:
- Assets/Scripts/Network/BridgeManager.cs
- Assets/Scripts/Network/BridgeState.cs
- Assets/Scripts/Network/IBridgeService.cs
- Assets/Tests/EditMode/BridgeManagerTests.cs

Requirements:

1. BridgeState.cs - Enumeration for bridge states:
   - Idle: Bridge not active
   - Connecting: Attempting to connect GC2 and GSPro
   - Active: Bridge running, relaying shots
   - Suspended: App backgrounded, native service handling relay
   - Error: Connection failed, with error message

2. IBridgeService.cs - Interface for platform-specific bridge services:
   - StartBridgeService() - Start native background service
   - StopBridgeService() - Stop native background service
   - IsBridgeServiceRunning - Check if service is active
   - UpdateNotification(int shotCount, string status) - Update persistent notification
   - OnServiceStatusChanged event

3. BridgeManager.cs - Main bridge controller:
   - Singleton pattern (DontDestroyOnLoad)
   - Properties:
     * State (BridgeState)
     * IsActive (bool)
     * ShotsRelayed (int)
     * GC2Connected (bool)
     * GSProConnected (bool)
     * LastShotTime (DateTime?)
   - Methods:
     * StartBridge() - Begin bridge mode
     * StopBridge() - End bridge mode
     * OnApplicationPause(bool paused) - Handle app lifecycle
   - Events:
     * OnStateChanged(BridgeState oldState, BridgeState newState)
     * OnShotRelayed(GC2ShotData shot, bool success)
     * OnError(string message)
   - Integration:
     * Uses existing IGC2Connection for USB
     * Uses existing GSProClient for TCP
     * Bypasses ShotProcessor/physics entirely
     * Only sends ball data to GSPro (no visualization)

4. Bridge Mode Flow:
   a. User taps "Start Bridge"
   b. BridgeManager.StartBridge() called
   c. Connect to GC2 via IGC2Connection
   d. Connect to GSPro via GSProClient
   e. Subscribe to IGC2Connection.OnShotReceived
   f. On shot received → GSProClient.SendShotAsync()
   g. Increment ShotsRelayed counter
   h. When app goes to background → signal native service
   i. Native service maintains connections
   j. When app returns → resume from service state

5. Minimal Resource Usage:
   - No TrajectorySimulator (skip physics)
   - No BallController (skip visualization)
   - No SessionManager recording (optional)
   - Only GSProClient heartbeat + shot forwarding

Write unit tests for:
- State transitions (Idle→Connecting→Active→Suspended→Active)
- Shot counting
- Error state handling
- Connection status aggregation
- Event firing
```

---

### Prompt 56: Android Foreground Service

```text
Create an Android Foreground Service that keeps the GC2→GSPro bridge alive when the app is backgrounded.

Context: Android kills background apps aggressively, but Foreground Services with persistent notifications are allowed to run indefinitely. This is the standard pattern for long-running tasks like music players, GPS tracking, and USB device communication.

Files to create:
- NativePlugins/Android/GC2AndroidPlugin/src/main/kotlin/com/openrange/gc2/BridgeService.kt
- NativePlugins/Android/GC2AndroidPlugin/src/main/kotlin/com/openrange/gc2/BridgeNotification.kt
- Update NativePlugins/Android/GC2AndroidPlugin/src/main/AndroidManifest.xml

Requirements:

1. AndroidManifest.xml updates:
   - Add permission: android.permission.FOREGROUND_SERVICE
   - Add permission: android.permission.FOREGROUND_SERVICE_CONNECTED_DEVICE (Android 14+)
   - Add permission: android.permission.WAKE_LOCK
   - Declare service:
     <service
         android:name=".BridgeService"
         android:enabled="true"
         android:exported="false"
         android:foregroundServiceType="connectedDevice" />

2. BridgeNotification.kt - Notification management:
   - Create notification channel "bridge_channel" on init
   - Channel name: "GC2 Bridge"
   - Importance: IMPORTANCE_LOW (no sound)
   - Methods:
     * createNotification(shotCount: Int, status: String): Notification
     * updateNotification(shotCount: Int, status: String)
   - Notification content:
     * Title: "GC2 Bridge Active"
     * Text: "Shots relayed: X • Status" (e.g., "Shots relayed: 5 • Connected")
     * Small icon: Use app icon or custom bridge icon
     * Ongoing: true (can't be swiped away)
     * Actions: "Stop Bridge" button

3. BridgeService.kt - Foreground Service:
   - Extends Service
   - Lifecycle:
     * onCreate(): Acquire wake lock, create notification
     * onStartCommand(): Start foreground with notification
     * onDestroy(): Release wake lock, cleanup
   - Wake lock:
     * PowerManager.PARTIAL_WAKE_LOCK
     * Tag: "openrange:bridge"
     * Keep CPU running when screen off
   - Integration with GC2Plugin:
     * Access GC2Plugin singleton
     * Delegate USB operations to existing GC2Device
     * Receive shots via callback
   - GSPro TCP handling:
     * Maintain TCP connection in service
     * Send heartbeat every 2 seconds (Handler + Runnable)
     * Forward shots from GC2Plugin callback
   - Unity callbacks:
     * OnBridgeStarted()
     * OnBridgeStopped()
     * OnShotRelayed(jsonData)
     * OnBridgeError(message)
   - Methods (called from Unity):
     * startBridge(gsproHost: String, gsproPort: Int)
     * stopBridge()
     * updateNotification(shotCount: Int, status: String)
     * isBridgeRunning(): Boolean

4. Service Lifecycle Handling:
   - START_STICKY: Restart if killed
   - Handle ACTION_STOP from notification button
   - Clean shutdown on stopBridge()
   - Handle USB device detach while in background

5. GSPro Connection in Service:
   - Reuse existing GSPro protocol from GC2Protocol.kt
   - Simple TCP socket connection
   - JSON message formatting matching GSProMessage
   - Heartbeat with device status (ball detected, ready)

6. Error Handling:
   - USB disconnect → update notification, attempt reconnect
   - GSPro disconnect → update notification, attempt reconnect
   - Low battery → warning in notification
   - Notify Unity of all state changes

Write verification:
- Service starts and shows notification
- Service survives app going to background
- Shots relay when screen is off
- Notification updates with shot count
- Service stops cleanly
```

---

### Prompt 57: Android Bridge Mode C# Integration

```text
Integrate the Android Foreground Service with Unity C# for seamless bridge mode operation.

Context: BridgeManager (from Prompt 55) needs to communicate with BridgeService (from Prompt 56) via JNI. When the app goes to background, the native service takes over; when it returns, Unity resumes control.

Files to create:
- Assets/Scripts/Network/Platforms/AndroidBridgeService.cs
- Assets/Tests/EditMode/AndroidBridgeServiceTests.cs

Files to modify:
- Assets/Scripts/Network/BridgeManager.cs (add platform integration)
- Assets/Scripts/GC2/Platforms/Android/GC2AndroidConnection.cs (add bridge hooks)

Requirements:

1. AndroidBridgeService.cs - Implements IBridgeService:
   - Conditional compilation: #if UNITY_ANDROID && !UNITY_EDITOR
   - AndroidJavaObject for BridgeService interaction
   - Methods:
     * StartBridgeService() - Start foreground service via Context.startForegroundService()
     * StopBridgeService() - Stop service via Context.stopService()
     * IsBridgeServiceRunning - Check via ActivityManager
     * UpdateNotification(shotCount, status) - Call service method
   - UnitySendMessage callbacks:
     * OnBridgeServiceStarted()
     * OnBridgeServiceStopped()
     * OnBridgeShotRelayed(string json)
     * OnBridgeError(string message)
   - Lifecycle handling:
     * OnApplicationPause(true) - Service takes over USB/TCP
     * OnApplicationPause(false) - Unity resumes, service provides state

2. Bridge Mode Handoff Protocol:
   When app goes to background:
   a. BridgeManager detects OnApplicationPause(true)
   b. BridgeManager.State → Suspended
   c. Native service already running, continues relay
   d. Unity stops processing (saves resources)

   When app returns to foreground:
   a. BridgeManager detects OnApplicationPause(false)
   b. Query service for current shot count, connection status
   c. Update BridgeManager state from service state
   d. BridgeManager.State → Active
   e. Resume UI updates

3. Update GC2AndroidConnection:
   - Add BridgeMode property (bool)
   - When BridgeMode = true:
     * Don't process shots in Unity (service handles it)
     * Forward connection events to BridgeManager
   - When BridgeMode = false:
     * Normal operation (shots processed in Unity)

4. BridgeManager Platform Integration:
   - Factory pattern for IBridgeService:
     * Android → AndroidBridgeService
     * iOS → iOSBridgeService (stub for now)
     * Editor → MockBridgeService (for testing)
   - OnApplicationPause integration
   - State synchronization with native service

5. Permission Handling:
   - Check FOREGROUND_SERVICE permission at runtime
   - Request battery optimization exemption (optional, improves reliability)
   - Handle permission denied gracefully

Write unit tests for:
- IBridgeService mock implementation
- State synchronization
- Platform service factory
- Lifecycle handling (pause/resume)
```

---

### Prompt 58: Bridge Mode UI

```text
Create a minimal Bridge Mode UI that shows connection status and allows users to start/stop the bridge.

Context: Bridge Mode needs a simple UI that:
- Shows clearly if the bridge is working
- Requires minimal interaction
- Works well when quickly switching between apps
- Provides quick access to launch streaming apps

Files to create:
- Assets/Scripts/UI/BridgeModePanel.cs
- Assets/Scripts/UI/BridgeStatusIndicator.cs
- Assets/Editor/BridgeModePanelGenerator.cs
- Assets/Tests/EditMode/BridgeModePanelTests.cs
- Assets/Prefabs/UI/BridgeModePanel.prefab (generated)

Requirements:

1. BridgeStatusIndicator.cs - Compact status display:
   - Visual states:
     * Inactive (gray): Bridge not running
     * Connecting (yellow pulse): Establishing connections
     * Active (green solid): Bridge running
     * Warning (orange): One connection lost
     * Error (red): Both connections lost
   - Small pill/badge design (~100x30px)
   - Icon + short text (e.g., "● Bridge Active")

2. BridgeModePanel.cs - Main panel:
   - Layout (vertical, ~300px wide, left side):
     * Header: "Bridge Mode" with close button
     * Section: Connection Status
       - GC2: [icon] Connected / Disconnected
       - GSPro: [icon] Connected / Disconnected
       - Shots relayed: [count]
       - Last shot: [time ago or "None"]
     * Section: Controls
       - Start/Stop Bridge button (large, prominent)
       - GSPro host/port (editable, defaults from SettingsManager)
     * Section: Quick Launch (Android only)
       - "Launch Moonlight" button
       - "Launch Steam Link" button
       - "Launch Parsec" button
     * Section: Battery Warning (conditional)
       - Show if battery < 20%
       - "Low battery - plug in for reliability"
   - Events:
     * OnBridgeStartRequested
     * OnBridgeStopRequested
     * OnAppLaunchRequested(string packageName)
   - Integration:
     * Subscribe to BridgeManager events
     * Update UI on state changes
     * Show toast on errors

3. Quick Launch Implementation (Android):
   - Use Application.OpenURL("package:com.limelight") pattern
   - Or use Intent via AndroidJavaObject
   - Package names:
     * Moonlight: com.limelight
     * Steam Link: com.valvesoftware.steamlink
     * Parsec: com.parsecgaming.parsec
   - Graceful handling if app not installed

4. BridgeModePanelGenerator.cs:
   - Menu: OpenRange > Create Bridge Mode Panel Prefab
   - Creates full panel hierarchy
   - Left-side anchoring

5. Scene Integration:
   - Add "Bridge Mode" button to Marina header (next to GSPro toggle)
   - Only visible when in GSPro mode
   - MarinaSceneController: Add _bridgeModePanel field
   - SceneGenerator: Instantiate and wire prefab

6. Visibility Logic:
   - Show Bridge Mode button only when:
     * GSPro mode is enabled
     * Platform supports background (Android for now)
   - Hide on platforms without background support

7. Accessibility:
   - Large touch targets (44px minimum)
   - Clear status colors with text labels
   - Readable contrast

Write unit tests for:
- Panel visibility logic
- Status indicator states
- Button enable/disable states
- Event firing
- Quick launch URL generation
```

---

### Prompt 59: iPad Background Mode Research and Workarounds

```text
Research iOS/iPad background processing options and implement the best available solution for bridge mode.

Context: iOS is notoriously restrictive about background processing. Unlike Android, there's no direct equivalent to Foreground Services. This prompt researches available options, documents limitations, and implements the best-effort solution.

Files to create:
- Assets/Scripts/Network/Platforms/iOSBridgeService.cs
- docs/IOS_BACKGROUND_LIMITATIONS.md

Files to modify:
- Assets/Scripts/UI/BridgeModePanel.cs (platform-specific messaging)

Research and Document:

1. iOS Background Mode Options (research each):

   a. Background Audio (com.apple.audio):
      - Play silent audio to keep app alive
      - Requires audio session category .playback
      - Risk: App Store rejection if detected as abuse
      - Battery impact: Minimal for silent audio
      - Reliability: Works but not intended use

   b. External Accessory Framework:
      - For MFi-certified accessories
      - GC2 is NOT MFi certified
      - Requires Apple approval and specific protocols
      - Conclusion: Not applicable

   c. Background Fetch:
      - iOS wakes app periodically (not guaranteed timing)
      - Maximum 30 seconds execution
      - Conclusion: Not suitable for continuous relay

   d. BGTaskScheduler (iOS 13+):
      - Background processing tasks
      - Executed opportunistically
      - Conclusion: Not suitable for real-time relay

   e. VoIP Push:
      - For voice/video calls
      - Would be rejected as abuse
      - Conclusion: Not applicable

   f. Location Updates (com.apple.location):
      - Continuous location tracking keeps app alive
      - Heavy battery drain
      - App Store scrutiny if no legitimate location use
      - Conclusion: Not recommended

2. Alternative Approaches:

   a. Picture-in-Picture (PiP) Mode:
      - Keep app visible in small window
      - User can use other apps while PiP shows status
      - Requires video content (could show status visualization)
      - Most legitimate solution for iPad
      - Implementation: AVPictureInPictureController

   b. Slide Over / Split View:
      - Use app in split screen with streaming app
      - App remains in foreground (not background)
      - Works well on iPad
      - No special implementation needed

   c. External Device (Mac/PC):
      - Run bridge on Mac (already working)
      - iPad connects to GC2, relays to Mac bridge
      - Mac handles GSPro connection
      - More complex but reliable

3. iOSBridgeService.cs Implementation:

   Implement Picture-in-Picture approach:
   - Create minimal video player showing status
   - Canvas with status text rendered to RenderTexture
   - AVPictureInPictureController to enter PiP
   - Methods:
     * StartBridgeService() - Enter PiP mode
     * StopBridgeService() - Exit PiP mode
     * IsBridgeServiceRunning - Check PiP state
     * UpdateNotification() - Update PiP content
   - Fallback: Show warning if PiP not available/supported

4. BridgeModePanel Platform Messaging:

   On iPad, show different UI:
   - Replace "Start Bridge" with "Start PiP Mode"
   - Info text: "Keep this window visible to maintain connection"
   - Or: "Use Split View with your streaming app"
   - Warning: "iOS may suspend the app if fully backgrounded"

5. Documentation (IOS_BACKGROUND_LIMITATIONS.md):

   - Honest explanation of iOS limitations
   - Comparison with Android capabilities
   - Recommended approaches (PiP, Split View)
   - Why true background mode isn't possible
   - Alternative workflows for users

6. Graceful Degradation:

   When iPad app is backgrounded without PiP:
   - Save bridge state
   - Attempt to reconnect on resume
   - Show notification of suspended time
   - Reconnect to GSPro automatically

Write unit tests for:
- Platform detection
- PiP availability check
- State persistence across suspend/resume
- Warning message display logic

Note: This prompt may result in documentation showing that true background mode is not feasible on iPad. That's a valid outcome - honest documentation is better than unreliable features.
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

---

## Appendix D: Implementation Notes (Lessons Learned)

### Unity 6 Module System

Unity 6 uses a modular package system. When using Assembly Definitions, some Unity APIs require explicit module references:

1. **Package Manifest** (`Packages/manifest.json`): Must include module dependencies:
   ```json
   "com.unity.modules.particlesystem": "1.0.0"
   ```

2. **Assembly Definition References**: For external assemblies like TextMeshPro:
   ```json
   "references": ["Unity.TextMeshPro"]
   ```

### Scene Integration Pattern

When creating new visual/UI components, ALL of these steps are required:

1. **Create component scripts** in `Assets/Scripts/Visualization/` or `Assets/Scripts/UI/`
2. **Create editor generator** in `Assets/Editor/` with menu items
3. **Update the scene controller** (e.g., `MarinaSceneController.cs`):
   - Add `[SerializeField] private YourComponent _yourComponent;`
   - Clear it in `InitializeScene()` if appropriate
   - Use it in event handlers like `OnShotProcessed()`
4. **Update SceneGenerator.cs** to:
   - Load and instantiate the prefab from Assets/Prefabs/
   - Position it (anchors, pivot, sizeDelta for UI)
   - Wire reference to scene controller via `SerializedObject.FindProperty("_yourComponent")`
5. **Regenerate scene** after creating prefabs: `OpenRange > Generate Marina Scene`

**Common Mistake**: Creating the prefab generator but forgetting steps 3-4.

### Coordinate System Conversion

| Axis | Physics (Trajectory) | Unity World |
|------|---------------------|-------------|
| X | Forward distance (yards) | Right (lateral) |
| Y | Height (feet) | Up (height) |
| Z | Lateral deviation (yards) | Forward (distance) |

When converting `TrajectoryPoint.Position` to Unity `Vector3`, swap X and Z.

### Native Plugin IL2CPP Callbacks

`UnitySendMessage` does NOT work in IL2CPP standalone builds. Use function pointer callbacks instead:

```csharp
// C# Side
private delegate void NativeShotCallback(string jsonData);

[DllImport(PluginName)]
private static extern void GC2Mac_SetShotCallback(NativeShotCallback callback);

[AOT.MonoPInvokeCallback(typeof(NativeShotCallback))]
private static void OnNativeShotCallbackStatic(string jsonData)
{
    if (s_instance != null) s_instance.OnNativeShotReceived(jsonData);
}
```

**JSON Field Names:** Native JSON must use exact C# property names (e.g., `BallSpeed` not `BallSpeedMph`).

### GC2 USB Protocol Summary

- **Endpoint**: INTERRUPT IN (0x82), NOT bulk
- **Message types**: `0H` = shot data, `0M` = device status
- **Terminator**: `\n\t` indicates complete message
- **Accumulation**: Wait for `BACK_RPM`/`SIDE_RPM` before processing
- **Misreads**: Reject zero spin, 2222 error, speed <1.1 or >250 mph
- **Device status**: FLAGS == 7 = ready, BALLS > 0 = ball detected

### Unity UI Programmatic Creation Lessons

**TMP_Dropdown ScrollRect Wiring:**
The dropdown template requires explicit ScrollRect wiring or items won't display:
```csharp
scrollRect.viewport = viewportRect;  // MUST be assigned!
scrollRect.content = contentRect;    // MUST be assigned!
```

**Button raycastTarget Issue:**
TextMeshProUGUI defaults to `raycastTarget = true`, intercepting clicks intended for the Button. Fix by setting `raycastTarget = false` on button text.

**Unicode Font Limitations:**
LiberationSans SDF (TMP default) lacks many Unicode symbols. Use Image components for symbols like checkmarks, or simple ASCII with bold styling (e.g., "X" instead of "✕").

**Unity Built-in Sprites:**
Access via `AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd")`. Available: Checkmark.psd, Background.psd, DropdownArrow.psd, Knob.psd, UISprite.psd, InputFieldBackground.psd.

### Batchmode Scene Generation Issue

`EditorUtility.DisplayDialog()` returns `false` in batchmode (CLI builds/tests), causing menu methods to exit without doing work. Always check `Application.isBatchMode` to skip dialogs:

```csharp
if (!Application.isBatchMode)
{
    if (!EditorUtility.DisplayDialog("Title", "Message", "OK", "Cancel"))
        return;  // User cancelled
}
// Proceed with generation...
```
