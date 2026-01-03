# GC2 macOS Native Plugin

Native USB plugin for communicating with the Foresight GC2 launch monitor on macOS.

## Overview

This plugin uses [libusb](https://libusb.info/) to communicate with the GC2 device via USB. It provides a C interface that can be called from Unity via P/Invoke.

## Prerequisites

- **macOS 11.0+** (Big Sur or later)
- **Xcode 14+** with Command Line Tools
- **libusb** (install via Homebrew)

### Install libusb

```bash
brew install libusb
```

## Building

### Quick Build

From the project root:
```bash
./NativePlugins/macOS/build_mac_plugin.sh
```

This will:
1. Build the plugin for your current architecture
2. Copy the bundle to `Assets/Plugins/macOS/`
3. Copy the libusb dylib alongside the bundle

### Manual Build

Using Xcode:
1. Open `GC2MacPlugin/GC2MacPlugin.xcodeproj`
2. Select Release configuration
3. Build (⌘B)
4. Copy `build/Release/GC2MacPlugin.bundle` to `Assets/Plugins/macOS/`
5. Copy `libusb/lib/libusb-1.0.dylib` to `Assets/Plugins/macOS/`

Using command line:
```bash
cd GC2MacPlugin
xcodebuild -scheme GC2MacPlugin -configuration Release build
```

## Project Structure

```
NativePlugins/macOS/
├── GC2MacPlugin/
│   ├── GC2MacPlugin.h         # Public C interface
│   ├── GC2MacPlugin.mm        # Objective-C++ implementation
│   ├── GC2MacPlugin.xcodeproj/
│   └── libusb/
│       ├── include/
│       │   └── libusb.h       # libusb header
│       └── lib/
│           └── libusb-1.0.dylib  # libusb dynamic library
├── build_mac_plugin.sh        # Build script
└── README.md                  # This file
```

## API Reference

### Lifecycle

```c
// Initialize the plugin with Unity callback object name
void GC2Mac_Initialize(const char* callbackObject);

// Shutdown and cleanup resources
void GC2Mac_Shutdown(void);
```

### Device Operations

```c
// Check if a GC2 device is connected
bool GC2Mac_IsDeviceAvailable(void);

// Connect to the GC2 device
bool GC2Mac_Connect(void);

// Disconnect from the GC2 device
void GC2Mac_Disconnect(void);

// Check if currently connected
bool GC2Mac_IsConnected(void);
```

### Device Information

```c
// Get device serial number (or NULL if not connected)
const char* GC2Mac_GetDeviceSerial(void);

// Get firmware version (or NULL if not available)
const char* GC2Mac_GetFirmwareVersion(void);
```

### Callbacks

The plugin calls Unity via `UnitySendMessage` with the following methods:

| Method | Parameter | Description |
|--------|-----------|-------------|
| `OnNativeShotReceived` | JSON string | Shot data from GC2 |
| `OnNativeConnectionChanged` | `"true"` or `"false"` | Connection state |
| `OnNativeError` | Error message | Error occurred |
| `OnNativeDeviceStatus` | JSON `{isReady, ballDetected}` | Device status |

For non-Unity usage, you can register callbacks directly:

```c
void GC2Mac_SetShotCallback(GC2ShotCallback callback);
void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback);
void GC2Mac_SetErrorCallback(GC2ErrorCallback callback);
void GC2Mac_SetDeviceStatusCallback(GC2DeviceStatusCallback callback);
```

## Unity Integration

### Plugin Placement

After building, the Unity project should have:
```
Assets/Plugins/macOS/
├── GC2MacPlugin.bundle
└── libusb-1.0.dylib
```

### Plugin Settings in Unity

1. Select `GC2MacPlugin.bundle` in Project window
2. Set Platform: Standalone, macOS only
3. Set CPU: Universal (if available) or your architecture

### C# Bridge

The C# bridge is in `Assets/Scripts/GC2/Platforms/MacOS/GC2MacConnection.cs` and handles:
- DllImport declarations
- Callback routing via UnitySendMessage
- JSON parsing of shot data

## GC2 USB Details

| Parameter | Value |
|-----------|-------|
| Vendor ID | `0x2C79` (11385) |
| Product ID | `0x0110` (272) |
| Endpoint | `0x82` (INTERRUPT IN) |
| Buffer Size | 64 bytes |
| Read Timeout | 100 ms |

## Troubleshooting

### Plugin not loading

1. Check Console for errors
2. Verify bundle is in correct location
3. Check code signing (may need to notarize for distribution)

### libusb not found

```bash
# Check if libusb is installed
brew list libusb

# If not, install it
brew install libusb

# Rebuild the plugin
./NativePlugins/macOS/build_mac_plugin.sh
```

### Device not detected

1. Check USB cable connection
2. Verify GC2 is powered on
3. Check System Information > USB for device
4. Try different USB port (avoid hubs)

### Architecture mismatch

If you see architecture errors:
```bash
# Check your Mac's architecture
uname -m

# Check the dylib architecture
file Assets/Plugins/macOS/libusb-1.0.dylib
```

For universal builds, you need libusb compiled for both architectures.

## Current Limitations

This is a **stub implementation** for Prompt 20. The following are not yet implemented:

- [ ] Actual USB data reading (Prompt 21)
- [ ] Full protocol parsing (Prompt 21)
- [ ] Shot data callback (Prompt 21)
- [ ] Device status monitoring (Prompt 21)

## License

Part of the GC2 Connect Unity project.
