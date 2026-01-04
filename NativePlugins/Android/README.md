# GC2 Android Plugin

Native Android plugin for communicating with the Foresight GC2 launch monitor via USB Host API.

## Requirements

- Android Studio (for development)
- Android SDK with:
  - Build Tools 34.0.0
  - Platform 34 (Android 14)
- Java 17+ (bundled with Android Studio)
- Device with USB Host support (Android 8.0+ / API 26+)

## Project Structure

```
NativePlugins/Android/
├── build_android_plugin.sh     # Build script
├── README.md                   # This file
└── GC2AndroidPlugin/
    ├── build.gradle.kts        # Gradle build configuration
    ├── settings.gradle.kts     # Gradle settings
    ├── gradle.properties       # Gradle properties
    ├── gradlew                 # Gradle wrapper script
    ├── gradle/wrapper/         # Gradle wrapper files
    └── src/main/
        ├── AndroidManifest.xml # USB host permission
        ├── res/xml/
        │   └── usb_device_filter.xml  # GC2 VID/PID filter
        └── kotlin/com/openrange/gc2/
            ├── GC2Plugin.kt    # Main entry point
            ├── GC2Device.kt    # USB device wrapper
            └── GC2Protocol.kt  # Protocol parser
```

## Building

### Using the Build Script (Recommended)

```bash
cd NativePlugins/Android
./build_android_plugin.sh
```

Options:
- `--debug` - Build debug variant instead of release
- `--clean` - Clean before building
- `--help` - Show help message

The script automatically:
1. Detects Android SDK and Java from Android Studio
2. Builds the AAR
3. Copies it to `Assets/Plugins/Android/GC2AndroidPlugin.aar`

### Manual Build

```bash
cd NativePlugins/Android/GC2AndroidPlugin
./gradlew assembleRelease
# Output: build/outputs/aar/GC2AndroidPlugin-release.aar
```

## Unity Integration

The AAR is automatically copied to `Assets/Plugins/Android/` by the build script.

### C# Bridge

The plugin is accessed from Unity via the `GC2AndroidConnection` class:

```csharp
// The connection is created automatically by GC2ConnectionFactory on Android
var connection = GC2ConnectionFactory.Create(); // Returns GC2AndroidConnection
connection.Connect();
connection.OnShotReceived += (shotData) => { ... };
connection.OnDeviceStatusChanged += (status) => { ... };
```

### Unity Callbacks

The plugin sends messages to Unity via `UnitySendMessage`:

| Method | Object | Message |
|--------|--------|---------|
| `OnNativeShotReceived` | GC2AndroidConnection | JSON shot data |
| `OnNativeDeviceStatus` | GC2AndroidConnection | JSON device status |
| `OnNativeConnectionChanged` | GC2AndroidConnection | "connected" or "disconnected" |
| `OnNativeError` | GC2AndroidConnection | Error message |

## API Reference

### GC2Plugin (Singleton)

```kotlin
// Get the singleton instance
val plugin = GC2Plugin.getInstance()

// Initialize with Unity context
plugin.initialize(activity, gameObjectName)

// Cleanup
plugin.shutdown()

// Device detection
val available = plugin.isDeviceAvailable()

// Connection management
plugin.connect(requestPermission = true)
plugin.disconnect()
val connected = plugin.isConnected()

// Device info
val serial = plugin.getDeviceSerial()
val firmware = plugin.getFirmwareVersion()
```

### GC2Protocol

Parses GC2 USB messages:

- **0H messages**: Shot data (speed, angle, spin, etc.)
- **0M messages**: Device status (ready state, ball detection)

Misread detection:
- Zero spin (BACK_RPM == 0 AND SIDE_RPM == 0)
- 2222 error pattern (BACK_RPM == 2222)
- Invalid speed (< 1.1 mph or > 250 mph)

## GC2 USB Protocol

| Property | Value |
|----------|-------|
| Vendor ID | 0x2C79 (11385) |
| Product ID | 0x0110 (272) |
| Endpoint | 0x82 (INTERRUPT IN) |
| Interface | 0 |
| Transfer Size | 64 bytes |

### Message Format

```
KEY=VALUE pairs separated by newlines
Terminated by \n\t (newline + tab)

Shot data (0H prefix):
SPEED_MPH=167.5
ELEVATION_DEG=10.9
AZIMUTH_DEG=-0.5
BACK_RPM=2686
SIDE_RPM=150
...

Status data (0M prefix):
FLAGS=7    # 7 = green light (ready)
BALLS=1    # Number of balls detected
```

## Troubleshooting

### Build Issues

**Java not found:**
The build script automatically uses Java bundled with Android Studio. If you have issues:
```bash
export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
```

**Android SDK not found:**
```bash
export ANDROID_HOME="$HOME/Library/Android/sdk"
```

### Runtime Issues

**USB permission denied:**
The plugin automatically requests USB permission when `connect(requestPermission = true)` is called. Ensure the device filter matches the GC2.

**Device not detected:**
- Check USB OTG cable (some phones require specific adapters)
- Verify USB Host mode is supported on the device
- Check Android version (requires 8.0+ / API 26+)

## License

Proprietary - OpenRange project
