# Technical Requirements Document (TRD)
# GC2 Connect Unity - Cross-Platform Architecture

## Document Info
| Field | Value |
|-------|-------|
| Version | 2.0.0 |
| Last Updated | December 2024 |
| Author | Samiur Rahman |
| Status | Draft |

---

## 1. System Architecture

### 1.1 High-Level Overview

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                          GC2 Connect Unity Application                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                        Shared C# Code (95%)                                  │ │
│  │                                                                              │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐   │ │
│  │  │   Physics   │ │Visualization│ │     UI      │ │     Services        │   │ │
│  │  │   Engine    │ │   System    │ │   System    │ │                     │   │ │
│  │  │             │ │             │ │             │ │ • GameManager       │   │ │
│  │  │ •Trajectory │ │ • Ball      │ │ • DataBar   │ │ • ShotProcessor     │   │ │
│  │  │ •Aerodynamic│ │ • Trail     │ │ • Panels    │ │ • SessionManager    │   │ │
│  │  │ •Ground     │ │ • Camera    │ │ • Responsive│ │ • SettingsManager   │   │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────┘   │ │
│  │                                                                              │ │
│  └──────────────────────────────────┬──────────────────────────────────────────┘ │
│                                     │                                            │
│  ┌──────────────────────────────────┴──────────────────────────────────────────┐ │
│  │                     GC2 Connection Abstraction Layer                         │ │
│  │                                                                              │ │
│  │  interface IGC2Connection {                                                 │ │
│  │      bool IsConnected { get; }                                              │ │
│  │      event Action<GC2ShotData> OnShotReceived;                              │ │
│  │      Task<bool> ConnectAsync();                                             │ │
│  │      void Disconnect();                                                      │ │
│  │  }                                                                           │ │
│  └──────────────────────────────────┬──────────────────────────────────────────┘ │
│                 ┌───────────────────┼───────────────────┐                        │
│                 │                   │                   │                        │
│  ┌──────────────▼────────┐ ┌───────▼───────┐ ┌────────▼─────────┐               │
│  │   macOS USB Plugin    │ │ iPad USB      │ │ Android USB      │               │
│  │   (Objective-C)       │ │ Plugin        │ │ Plugin           │               │
│  │                       │ │ (Swift)       │ │ (Kotlin)         │               │
│  │   libusb wrapper      │ │ DriverKit     │ │ USB Host API     │               │
│  └───────────┬───────────┘ └───────┬───────┘ └────────┬─────────┘               │
└──────────────┼─────────────────────┼──────────────────┼──────────────────────────┘
               │                     │                  │
               ▼                     ▼                  ▼
        ┌────────────┐        ┌────────────┐     ┌────────────┐
        │    GC2     │        │    GC2     │     │    GC2     │
        │  (USB-A)   │        │  (USB-C)   │     │  (USB-C)   │
        └────────────┘        └────────────┘     └────────────┘
              Mac                  iPad              Android
```

### 1.2 Data Flow

```
GC2 Device
    │
    │ USB Bulk Transfer (text protocol)
    ▼
Native Plugin (platform-specific)
    │
    │ Parses protocol, creates JSON
    ▼
C# Bridge (GC2*Connection.cs)
    │
    │ Deserializes to GC2ShotData
    ▼
ShotProcessor
    │
    ├──────────────────────┐
    │                      │
    ▼                      ▼
TrajectorySimulator    GSProClient (optional)
    │                      │
    │ ShotResult           │ TCP JSON
    ▼                      ▼
BallController         GSPro (Windows)
    │
    │ Animation frames
    ▼
3D Visualization + UI Update
```

---

## 2. Project Structure

```
gc2-connect-unity/
├── CLAUDE.md                           # Claude Code reference
├── README.md                           # Project readme
├── .gitignore
│
├── docs/
│   ├── PRD.md                          # Product requirements
│   ├── TRD.md                          # Technical requirements (this file)
│   ├── PHYSICS.md                      # Physics specification
│   ├── GSPRO_API.md                    # GSPro Open Connect API
│   ├── GC2_PROTOCOL.md                 # GC2 USB protocol
│   └── USB_PLUGINS.md                  # Native plugin guide
│
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          # Main app controller
│   │   │   ├── ShotProcessor.cs        # Shot handling
│   │   │   ├── SessionManager.cs       # Session state
│   │   │   ├── SettingsManager.cs      # Settings persistence
│   │   │   ├── QualityManager.cs       # Graphics quality
│   │   │   └── PlatformManager.cs      # Platform detection
│   │   │
│   │   ├── GC2/
│   │   │   ├── IGC2Connection.cs       # Interface
│   │   │   ├── GC2ConnectionFactory.cs # Factory
│   │   │   ├── GC2Protocol.cs          # Protocol parser
│   │   │   ├── GC2ShotData.cs          # Data model
│   │   │   │
│   │   │   └── Platforms/
│   │   │       ├── MacOS/
│   │   │       │   └── GC2MacConnection.cs
│   │   │       ├── iOS/
│   │   │       │   └── GC2iPadConnection.cs
│   │   │       ├── Android/
│   │   │       │   └── GC2AndroidConnection.cs
│   │   │       └── TCP/
│   │   │           └── GC2TCPConnection.cs
│   │   │
│   │   ├── Physics/
│   │   │   ├── PhysicsConstants.cs     # Constants and tables
│   │   │   ├── Aerodynamics.cs         # Cd/Cl calculations
│   │   │   ├── TrajectorySimulator.cs  # Main physics engine
│   │   │   ├── GroundPhysics.cs        # Bounce/roll
│   │   │   ├── AtmosphericModel.cs     # Air density
│   │   │   ├── UnitConversions.cs      # Unit helpers
│   │   │   ├── ShotResult.cs           # Result model
│   │   │   └── TrajectoryPoint.cs      # Point model
│   │   │
│   │   ├── Visualization/
│   │   │   ├── BallController.cs       # Ball animation
│   │   │   ├── TrajectoryRenderer.cs   # Path line
│   │   │   ├── LandingMarker.cs        # Landing indicator
│   │   │   ├── CameraController.cs     # Camera modes
│   │   │   └── EnvironmentManager.cs   # Environment switching
│   │   │
│   │   ├── UI/
│   │   │   ├── UIManager.cs            # UI controller
│   │   │   ├── ShotDataBar.cs          # Bottom bar
│   │   │   ├── DataTile.cs             # Individual data tile
│   │   │   ├── ClubDataPanel.cs        # HMT panel
│   │   │   ├── SessionInfoPanel.cs     # Top-left info
│   │   │   ├── ClubSelector.cs         # Club selection
│   │   │   ├── ModeSelector.cs         # Range/GSPro toggle
│   │   │   ├── SettingsPanel.cs        # Settings UI
│   │   │   ├── ConnectionStatusUI.cs   # Connection indicators
│   │   │   └── ResponsiveLayout.cs     # Screen adaptation
│   │   │
│   │   ├── Network/
│   │   │   └── GSProClient.cs          # GSPro TCP client
│   │   │
│   │   └── Utilities/
│   │       ├── MainThreadDispatcher.cs # Thread marshaling
│   │       ├── JsonHelper.cs           # JSON utilities
│   │       └── PlatformUtils.cs        # Platform helpers
│   │
│   ├── Plugins/
│   │   ├── macOS/
│   │   │   ├── GC2MacPlugin.bundle/    # Compiled plugin
│   │   │   └── libusb.dylib            # libusb library
│   │   │
│   │   ├── iOS/
│   │   │   ├── GC2iOSPlugin.framework/ # iOS framework
│   │   │   └── GC2Driver.dext/         # DriverKit extension
│   │   │
│   │   └── Android/
│   │       └── GC2AndroidPlugin.aar    # Android library
│   │
│   ├── Prefabs/
│   │   ├── Ball/
│   │   │   ├── GolfBall.prefab
│   │   │   └── BallTrail.prefab
│   │   ├── Environment/
│   │   │   ├── DistanceMarker.prefab
│   │   │   ├── TargetGreen.prefab
│   │   │   └── Flag.prefab
│   │   └── UI/
│   │       ├── DataTile.prefab
│   │       └── ShotHistoryItem.prefab
│   │
│   ├── Scenes/
│   │   ├── Bootstrap.unity             # Initial loading
│   │   ├── MainMenu.unity              # Menu screen
│   │   └── Ranges/
│   │       ├── Marina.unity            # Default range
│   │       ├── Mountain.unity
│   │       └── Links.unity
│   │
│   ├── Materials/
│   │   ├── Environment/
│   │   │   ├── Fairway.mat
│   │   │   ├── Rough.mat
│   │   │   ├── Green.mat
│   │   │   └── Water.mat
│   │   ├── Ball/
│   │   │   └── GolfBall.mat
│   │   └── UI/
│   │       └── PanelBackground.mat
│   │
│   ├── Textures/
│   ├── Models/
│   ├── Audio/
│   ├── Fonts/
│   │
│   ├── Settings/                       # URP settings
│   │   ├── URP-HighQuality.asset
│   │   ├── URP-MediumQuality.asset
│   │   └── URP-LowQuality.asset
│   │
│   └── StreamingAssets/
│       └── DefaultSettings.json
│
├── NativePlugins/                      # Native source code
│   ├── macOS/
│   │   └── GC2MacPlugin/
│   │       ├── GC2MacPlugin.h
│   │       ├── GC2MacPlugin.mm
│   │       ├── GC2MacPlugin.xcodeproj/
│   │       └── README.md
│   │
│   ├── iOS/
│   │   ├── GC2iOSPlugin/
│   │   │   ├── GC2iOSPlugin.swift
│   │   │   └── GC2iOSPlugin.xcodeproj/
│   │   ├── GC2Driver/
│   │   │   ├── GC2Driver.swift
│   │   │   ├── GC2UserClient.swift
│   │   │   ├── Info.plist
│   │   │   └── Entitlements.plist
│   │   └── README.md
│   │
│   └── Android/
│       └── GC2AndroidPlugin/
│           ├── src/main/kotlin/com/openrange/gc2/
│           │   ├── GC2Plugin.kt
│           │   ├── GC2Device.kt
│           │   └── GC2Protocol.kt
│           ├── src/main/AndroidManifest.xml
│           ├── build.gradle
│           └── README.md
│
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   ├── QualitySettings.asset
│   ├── GraphicsSettings.asset
│   └── TagManager.asset
│
├── Packages/
│   └── manifest.json
│
└── Editor/
    └── BuildConfiguration.cs           # Build scripts
```

---

## 3. Core Components

### 3.1 IGC2Connection Interface

```csharp
// Assets/Scripts/GC2/IGC2Connection.cs
namespace OpenRange.GC2
{
    public interface IGC2Connection
    {
        /// <summary>Whether currently connected to GC2</summary>
        bool IsConnected { get; }
        
        /// <summary>Device info if connected</summary>
        GC2DeviceInfo DeviceInfo { get; }
        
        /// <summary>Fired when shot data received</summary>
        event Action<GC2ShotData> OnShotReceived;
        
        /// <summary>Fired on connection state change</summary>
        event Action<bool> OnConnectionChanged;
        
        /// <summary>Fired on errors</summary>
        event Action<string> OnError;
        
        /// <summary>Check if GC2 device is present</summary>
        bool IsDeviceAvailable();
        
        /// <summary>Connect to GC2</summary>
        Task<bool> ConnectAsync();
        
        /// <summary>Disconnect from GC2</summary>
        void Disconnect();
    }
}
```

### 3.2 GC2ShotData Model

```csharp
// Assets/Scripts/GC2/GC2ShotData.cs
namespace OpenRange.GC2
{
    [System.Serializable]
    public class GC2ShotData
    {
        // Metadata
        public int ShotId;
        public long Timestamp;
        
        // Ball data (always present)
        public float BallSpeed;        // mph
        public float LaunchAngle;      // degrees (vertical)
        public float Direction;        // degrees (horizontal, + = right)
        public float TotalSpin;        // rpm
        public float BackSpin;         // rpm
        public float SideSpin;         // rpm (+ = right/fade)
        public float SpinAxis;         // degrees
        
        // Club data (HMT only)
        public bool HasClubData;
        public float ClubSpeed;        // mph
        public float Path;             // degrees (+ = in-to-out)
        public float AttackAngle;      // degrees (+ = up)
        public float FaceToTarget;     // degrees (+ = open)
        public float DynamicLoft;      // degrees
        public float Lie;              // degrees
    }
    
    [System.Serializable]
    public class GC2DeviceInfo
    {
        public string SerialNumber;
        public string FirmwareVersion;
        public bool HasHMT;
    }
}
```

### 3.3 GC2ConnectionFactory

```csharp
// Assets/Scripts/GC2/GC2ConnectionFactory.cs
namespace OpenRange.GC2
{
    public static class GC2ConnectionFactory
    {
        public static IGC2Connection Create()
        {
#if UNITY_EDITOR
            return new GC2TCPConnection();  // TCP for editor testing
#elif UNITY_STANDALONE_OSX
            return new GC2MacConnection();
#elif UNITY_IOS
            return new GC2iPadConnection();
#elif UNITY_ANDROID
            return new GC2AndroidConnection();
#else
            return new GC2TCPConnection();  // Fallback
#endif
        }
        
        public static IGC2Connection CreateTCP(string host, int port)
        {
            return new GC2TCPConnection(host, port);
        }
    }
}
```

### 3.4 ShotProcessor

```csharp
// Assets/Scripts/Core/ShotProcessor.cs
namespace OpenRange.Core
{
    public class ShotProcessor : MonoBehaviour
    {
        [SerializeField] private BallController _ballController;
        [SerializeField] private ShotDataBar _dataBar;
        [SerializeField] private SessionManager _sessionManager;
        
        private IGC2Connection _gc2Connection;
        private TrajectorySimulator _simulator;
        private GSProClient _gsproClient;
        private AppMode _mode = AppMode.OpenRange;
        
        public event Action<ShotResult> OnShotProcessed;
        
        private void Start()
        {
            _gc2Connection = GC2ConnectionFactory.Create();
            _gc2Connection.OnShotReceived += HandleShot;
            _gc2Connection.ConnectAsync();
            
            _simulator = new TrajectorySimulator();
        }
        
        private void HandleShot(GC2ShotData shotData)
        {
            // Run physics
            var result = _simulator.Simulate(
                shotData.BallSpeed,
                shotData.LaunchAngle,
                shotData.Direction,
                shotData.BackSpin,
                shotData.SideSpin
            );
            
            // Update UI
            _dataBar.UpdateDisplay(shotData, result);
            _sessionManager.RecordShot(shotData, result);
            
            // Visualize
            _ballController.PlayShot(result);
            
            // Send to GSPro if in that mode
            if (_mode == AppMode.GSPro && _gsproClient != null)
            {
                _gsproClient.SendShot(shotData);
            }
            
            OnShotProcessed?.Invoke(result);
        }
        
        public void SetMode(AppMode mode)
        {
            _mode = mode;
        }
    }
    
    public enum AppMode { OpenRange, GSPro }
}
```

---

## 4. Physics Engine

See `docs/PHYSICS.md` for complete specification.

### 4.1 Key Classes

```csharp
// TrajectorySimulator - Main entry point
public ShotResult Simulate(float ballSpeedMph, float vlaDeg, float hlaDeg, 
                           float backspinRpm, float sidespinRpm)

// Aerodynamics - Coefficient calculations  
public static float GetDragCoefficient(float reynolds)
public static float GetLiftCoefficient(float spinFactor)
public static float CalculateAirDensity(float tempF, float elevationFt, ...)

// GroundPhysics - Ground interaction
public static (Vector3 pos, Vector3 vel, float spin) Bounce(...)
public static (Vector3 pos, Vector3 vel, float spin, Phase phase) RollStep(...)
```

### 4.2 Validation Requirements

Physics must pass these tests:

| Test | Ball Speed | Launch | Spin | Expected Carry | Tolerance |
|------|------------|--------|------|----------------|-----------|
| Driver | 167 mph | 10.9° | 2686 rpm | 275 yds | ±5% |
| Driver | 160 mph | 11.0° | 3000 rpm | 259 yds | ±3% |
| 7-Iron | 120 mph | 16.3° | 7097 rpm | 172 yds | ±5% |
| Wedge | 102 mph | 24.2° | 9304 rpm | 136 yds | ±5% |

---

## 5. Native USB Plugins

See `docs/USB_PLUGINS.md` for complete implementation guide.

### 5.1 Plugin Matrix

| Platform | Language | Framework | Output |
|----------|----------|-----------|--------|
| macOS | Objective-C | libusb | .bundle |
| iPad | Swift | DriverKit | .framework + .dext |
| Android | Kotlin | USB Host API | .aar |

### 5.2 Plugin API (All Platforms)

```
Initialize(callbackObject: string) -> void
IsDeviceAvailable() -> bool
Connect() -> bool
Disconnect() -> void
IsConnected() -> bool

// Callbacks to Unity (via UnitySendMessage or delegates)
OnNativeShotReceived(json: string)
OnNativeConnectionChanged(connected: bool)
OnNativeError(error: string)
```

### 5.3 GC2 USB Identifiers

```
Vendor ID:  0x2C79 (11385 decimal)
Product ID: 0x0110 (272 decimal)
```

---

## 6. UI System

### 6.1 Responsive Layout Strategy

```csharp
public class ResponsiveLayout : MonoBehaviour
{
    public enum ScreenCategory { Phone, Tablet, Desktop }
    
    public ScreenCategory GetCategory()
    {
        float diagonal = GetDiagonalInches();
        if (diagonal < 7) return ScreenCategory.Phone;      // Not supported
        if (diagonal < 13) return ScreenCategory.Tablet;
        return ScreenCategory.Desktop;
    }
    
    // Layout rules per category
    // Tablet: Side panels, medium data tiles
    // Desktop: Full panels, large data tiles
}
```

### 6.2 Data Bar Layout

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ BALL SPEED │DIRECTION│ ANGLE │BACK SPIN│SIDE SPIN│ APEX │OFFLINE│CARRY│RUN│TOTAL│
├────────────┼─────────┼───────┼─────────┼─────────┼──────┼───────┼─────┼───┼─────┤
│   104.5    │  L4.0   │ 24.0  │  4,121  │   R311  │ 30.7 │  L7.2 │150.0│4.6│154.6│
│    mph     │   deg   │  deg  │   rpm   │   rpm   │  yd  │   yd  │  yd │ yd│  yd │
└────────────┴─────────┴───────┴─────────┴─────────┴──────┴───────┴─────┴───┴─────┘
```

---

## 7. Quality Tier System

### 7.1 Tier Definitions

```csharp
public enum QualityTier { Low, Medium, High }

// Auto-detection logic
public QualityTier DetectOptimalTier()
{
#if UNITY_IOS
    // M1+ iPad = High
    if (IsAppleSilicon() && SystemInfo.systemMemorySize >= 8000)
        return QualityTier.High;
    return QualityTier.Medium;
    
#elif UNITY_ANDROID
    if (SystemInfo.graphicsMemorySize >= 4000) return QualityTier.High;
    if (SystemInfo.graphicsMemorySize >= 2000) return QualityTier.Medium;
    return QualityTier.Low;
    
#elif UNITY_STANDALONE_OSX
    if (IsAppleSilicon()) return QualityTier.High;
    return SystemInfo.graphicsMemorySize >= 2000 ? 
        QualityTier.High : QualityTier.Medium;
#endif
}
```

### 7.2 Tier Settings

| Setting | Low | Medium | High |
|---------|-----|--------|------|
| Target FPS | 30 | 60 | 120 |
| Shadows | Baked | Hard | Soft |
| Reflections | None | Planar | SSR |
| Textures | 512px | 1K | 2K |
| Anti-aliasing | None | FXAA | MSAA 4x |
| Post-processing | None | Basic | Full |

---

## 8. Network (GSPro Mode)

### 8.1 GSProClient

```csharp
public class GSProClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    
    public async Task<bool> ConnectAsync(string host, int port = 921)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        return true;
    }
    
    public async Task SendShot(GC2ShotData shot)
    {
        var message = new GSProShotMessage
        {
            DeviceID = "GC2 Connect Unity",
            Units = "Yards",
            ShotNumber = shot.ShotId,
            APIversion = "1",
            BallData = new GSProBallData
            {
                Speed = shot.BallSpeed,
                SpinAxis = shot.SpinAxis,
                TotalSpin = shot.TotalSpin,
                BackSpin = shot.BackSpin,
                SideSpin = shot.SideSpin,
                HLA = shot.Direction,
                VLA = shot.LaunchAngle
            },
            ShotDataOptions = new GSProShotOptions
            {
                ContainsBallData = true,
                ContainsClubData = shot.HasClubData
            }
        };
        
        if (shot.HasClubData)
        {
            message.ClubData = new GSProClubData
            {
                Speed = shot.ClubSpeed,
                AngleOfAttack = shot.AttackAngle,
                FaceToTarget = shot.FaceToTarget,
                Lie = shot.Lie,
                Loft = shot.DynamicLoft,
                Path = shot.Path
            };
        }
        
        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }
}
```

See `docs/GSPRO_API.md` for complete protocol specification.

---

## 9. Build Configuration

### 9.1 Platform Settings

**macOS:**
```
Target: StandaloneOSX
Architecture: Universal (x64 + ARM64)
Scripting Backend: IL2CPP
API Compatibility: .NET Standard 2.1
Min OS: macOS 11.0
Notarization: Required
```

**iPad:**
```
Target: iOS
Architecture: ARM64
Scripting Backend: IL2CPP
Target Device: iPad Only
Min OS: iPadOS 16.0
Entitlements: DriverKit USB
```

**Android:**
```
Target: Android
Architecture: ARM64 + ARMv7
Scripting Backend: IL2CPP
Min SDK: API 26 (Android 8.0)
Target SDK: API 33
USB Host: Required in manifest
```

### 9.2 Build Script

```csharp
// Editor/BuildConfiguration.cs
public static class BuildConfiguration
{
    [MenuItem("Build/All Platforms")]
    public static void BuildAll()
    {
        BuildMacOS();
        BuildAndroid();
        BuildiOS();  // Xcode project only
    }
    
    [MenuItem("Build/macOS")]
    public static void BuildMacOS()
    {
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2);
        BuildPipeline.BuildPlayer(
            GetScenes(),
            "Builds/macOS/GC2Connect.app",
            BuildTarget.StandaloneOSX,
            BuildOptions.None
        );
    }
    
    // ... other platforms
}
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

```csharp
[Test]
public void Physics_DriverShot_CarryWithinTolerance()
{
    var sim = new TrajectorySimulator();
    var result = sim.Simulate(167f, 10.9f, 0f, 2686f, 0f);
    Assert.AreEqual(275f, result.CarryDistance, 275f * 0.05f);
}

[Test]
public void Protocol_ParsesValidShot()
{
    var raw = "SHOT_ID=1\nSPEED_MPH=150.5\nELEVATION_DEG=12.3\n...";
    var shot = GC2Protocol.Parse(raw);
    Assert.AreEqual(150.5f, shot.BallSpeed, 0.01f);
}
```

### 10.2 Integration Tests

1. USB connection on each platform
2. Full shot flow (receive → physics → visualize)
3. GSPro relay mode
4. Settings persistence

### 10.3 Device Test Matrix

| Platform | Device | Purpose |
|----------|--------|---------|
| macOS | M1 MacBook Air | Performance baseline |
| macOS | Intel iMac | Intel compatibility |
| iPad | iPad Pro 11" M1 | Primary tablet |
| iPad | iPad Pro 12.9" M2 | Large screen |
| Android | Samsung Tab S8+ | High-end Android |
| Android | Pixel Tablet | Stock Android |
| Android | Budget tablet | Low-end performance |

---

## 11. Performance Budgets

| Metric | Mac (M1) | iPad Pro | Android High | Android Mid |
|--------|----------|----------|--------------|-------------|
| FPS | 120 | 60 | 60 | 30 |
| Draw Calls | <100 | <80 | <80 | <50 |
| Triangles | <500K | <300K | <300K | <100K |
| Memory | <500MB | <300MB | <300MB | <200MB |
| Physics | <10ms | <20ms | <20ms | <30ms |
| App Size | <200MB | <150MB | <150MB | <100MB |

---

## 12. Dependencies

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

## 13. Security Considerations

1. **USB Permissions**: Follow platform guidelines for USB access
2. **Network**: GSPro connection is local network only
3. **Settings**: Stored locally, no sensitive data
4. **No Cloud**: No data leaves the device

---

## 14. Future Considerations

### v2.1
- Windows support
- Additional environments
- Shot replay system
- Club fitting mode

### v3.0
- VR support (Quest, Vision Pro)
- Multiplayer/online
- Cloud sync
- AI coaching
