# Native USB Plugin Implementation Guide
# GC2 Connect Unity

## Document Info
| Field | Value |
|-------|-------|
| Version | 1.0.0 |
| Last Updated | December 2024 |

---

## 1. Overview

This document provides implementation details for the native USB plugins that enable GC2 communication on each platform.

### Plugin Matrix

| Platform | Language | Framework | Output Format |
|----------|----------|-----------|---------------|
| macOS | Objective-C | libusb | .bundle |
| iPad | Swift | DriverKit | .framework + .dext |
| Android | Kotlin | USB Host API | .aar |

---

## 2. Common Interface

All plugins implement the same logical interface, called from C# via platform-specific bridges.

### 2.1 C# Interface

```csharp
// Assets/Scripts/GC2/IGC2Connection.cs
namespace OpenRange.GC2
{
    public interface IGC2Connection
    {
        bool IsConnected { get; }
        GC2DeviceInfo DeviceInfo { get; }
        
        event Action<GC2ShotData> OnShotReceived;
        event Action<bool> OnConnectionChanged;
        event Action<string> OnError;
        
        bool IsDeviceAvailable();
        Task<bool> ConnectAsync();
        void Disconnect();
    }
}
```

### 2.2 Native Plugin API

All native plugins expose these functions:

```
Initialize(callbackObject: string) -> void
Shutdown() -> void
IsDeviceAvailable() -> bool
Connect() -> bool
Disconnect() -> void
IsConnected() -> bool

// Callbacks (via UnitySendMessage or delegates)
OnNativeShotReceived(json: string)
OnNativeConnectionChanged(connected: string)  // "true" or "false"
OnNativeError(error: string)
```

---

## 3. macOS Plugin (libusb)

### 3.1 Project Structure

```
NativePlugins/macOS/GC2MacPlugin/
├── GC2MacPlugin.h
├── GC2MacPlugin.mm
├── GC2MacPlugin.xcodeproj/
├── libusb/
│   ├── libusb.h
│   └── libusb-1.0.26.dylib
└── README.md
```

### 3.2 Header File

```objectivec
// GC2MacPlugin.h
#ifndef GC2MacPlugin_h
#define GC2MacPlugin_h

#import <Foundation/Foundation.h>

#ifdef __cplusplus
extern "C" {
#endif

// Callback function types
typedef void (*GC2ShotCallback)(const char* jsonData);
typedef void (*GC2ConnectionCallback)(const char* connected);
typedef void (*GC2ErrorCallback)(const char* error);

// Plugin lifecycle
void GC2Mac_Initialize(const char* callbackObject);
void GC2Mac_Shutdown(void);

// Device operations
bool GC2Mac_IsDeviceAvailable(void);
bool GC2Mac_Connect(void);
void GC2Mac_Disconnect(void);
bool GC2Mac_IsConnected(void);

// Callback registration (for non-Unity usage)
void GC2Mac_SetShotCallback(GC2ShotCallback callback);
void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback);
void GC2Mac_SetErrorCallback(GC2ErrorCallback callback);

#ifdef __cplusplus
}
#endif

#endif
```

### 3.3 Implementation

```objectivec
// GC2MacPlugin.mm
#import "GC2MacPlugin.h"
#import "libusb.h"
#import <dispatch/dispatch.h>

#define GC2_VENDOR_ID  0x2C79
#define GC2_PRODUCT_ID 0x0110
#define EP_IN 0x81
#define BUFFER_SIZE 512
#define READ_TIMEOUT_MS 100

// State
static libusb_context *usbContext = NULL;
static libusb_device_handle *deviceHandle = NULL;
static dispatch_queue_t readQueue = NULL;
static BOOL isRunning = NO;
static NSMutableString *dataBuffer = nil;
static NSString *unityCallbackObject = nil;

// Callbacks
static GC2ShotCallback shotCallback = NULL;
static GC2ConnectionCallback connectionCallback = NULL;
static GC2ErrorCallback errorCallback = NULL;

#pragma mark - Unity Communication

static void SendToUnity(NSString *methodName, NSString *message) {
    if (unityCallbackObject) {
        // Unity's UnitySendMessage - must be called from main thread
        dispatch_async(dispatch_get_main_queue(), ^{
            UnitySendMessage(
                [unityCallbackObject UTF8String],
                [methodName UTF8String],
                [message UTF8String]
            );
        });
    }
}

static void NotifyShot(NSDictionary *shotData) {
    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:shotData options:0 error:&error];
    if (jsonData) {
        NSString *json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        
        if (shotCallback) {
            dispatch_async(dispatch_get_main_queue(), ^{
                shotCallback([json UTF8String]);
            });
        }
        
        SendToUnity(@"OnNativeShotReceived", json);
    }
}

static void NotifyConnection(BOOL connected) {
    NSString *value = connected ? @"true" : @"false";
    
    if (connectionCallback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            connectionCallback([value UTF8String]);
        });
    }
    
    SendToUnity(@"OnNativeConnectionChanged", value);
}

static void NotifyError(NSString *error) {
    if (errorCallback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            errorCallback([error UTF8String]);
        });
    }
    
    SendToUnity(@"OnNativeError", error);
}

#pragma mark - Protocol Parsing

static NSDictionary* ParseGC2Protocol(NSString *data) {
    NSMutableDictionary *values = [NSMutableDictionary dictionary];
    
    NSArray *lines = [data componentsSeparatedByString:@"\n"];
    for (NSString *line in lines) {
        NSString *trimmed = [line stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
        if (trimmed.length == 0) continue;
        
        NSArray *parts = [trimmed componentsSeparatedByString:@"="];
        if (parts.count == 2) {
            values[parts[0]] = parts[1];
        }
    }
    
    // Validate required field
    if (!values[@"SPEED_MPH"]) return nil;
    
    // Build result
    return @{
        @"shotId": @([values[@"SHOT_ID"] intValue]),
        @"ballSpeed": @([values[@"SPEED_MPH"] floatValue]),
        @"launchAngle": @([values[@"ELEVATION_DEG"] floatValue]),
        @"direction": @([values[@"AZIMUTH_DEG"] floatValue]),
        @"totalSpin": @([values[@"SPIN_RPM"] floatValue]),
        @"backSpin": @([values[@"BACK_RPM"] floatValue]),
        @"sideSpin": @([values[@"SIDE_RPM"] floatValue]),
        @"spinAxis": @([values[@"SPIN_AXIS_DEG"] floatValue]),
        @"hasClubData": @([values[@"HMT"] intValue] == 1),
        @"clubSpeed": @([values[@"CLUBSPEED_MPH"] floatValue]),
        @"path": @([values[@"HPATH_DEG"] floatValue]),
        @"attackAngle": @([values[@"VPATH_DEG"] floatValue]),
        @"faceToTarget": @([values[@"FACE_T_DEG"] floatValue]),
        @"dynamicLoft": @([values[@"LOFT_DEG"] floatValue]),
        @"lie": @([values[@"LIE_DEG"] floatValue])
    };
}

static void ProcessBuffer(void) {
    NSRange range;
    while ((range = [dataBuffer rangeOfString:@"\n\n"]).location != NSNotFound) {
        NSString *message = [dataBuffer substringToIndex:range.location];
        [dataBuffer deleteCharactersInRange:NSMakeRange(0, range.location + 2)];
        
        NSDictionary *shot = ParseGC2Protocol(message);
        if (shot) {
            NotifyShot(shot);
        }
    }
}

#pragma mark - USB Read Loop

static void ReadLoop(void) {
    unsigned char buffer[BUFFER_SIZE];
    int transferred;
    
    while (isRunning && deviceHandle) {
        int result = libusb_bulk_transfer(deviceHandle, EP_IN, buffer, 
                                          BUFFER_SIZE, &transferred, READ_TIMEOUT_MS);
        
        if (result == 0 && transferred > 0) {
            NSString *data = [[NSString alloc] initWithBytes:buffer 
                                                      length:transferred 
                                                    encoding:NSUTF8StringEncoding];
            if (data) {
                [dataBuffer appendString:data];
                ProcessBuffer();
            }
        } else if (result != LIBUSB_ERROR_TIMEOUT && result != 0) {
            // Error occurred
            NotifyError([NSString stringWithFormat:@"USB read error: %d", result]);
            break;
        }
    }
    
    // Clean disconnect
    isRunning = NO;
    NotifyConnection(NO);
}

#pragma mark - Public API

void GC2Mac_Initialize(const char* callbackObject) {
    if (callbackObject) {
        unityCallbackObject = [NSString stringWithUTF8String:callbackObject];
    }
    
    int result = libusb_init(&usbContext);
    if (result < 0) {
        NotifyError(@"Failed to initialize libusb");
        return;
    }
    
    dataBuffer = [[NSMutableString alloc] init];
    readQueue = dispatch_queue_create("com.openrange.gc2.read", DISPATCH_QUEUE_SERIAL);
    
    NSLog(@"GC2MacPlugin initialized");
}

void GC2Mac_Shutdown(void) {
    GC2Mac_Disconnect();
    
    if (usbContext) {
        libusb_exit(usbContext);
        usbContext = NULL;
    }
    
    dataBuffer = nil;
    readQueue = nil;
    unityCallbackObject = nil;
    
    NSLog(@"GC2MacPlugin shutdown");
}

bool GC2Mac_IsDeviceAvailable(void) {
    if (!usbContext) return false;
    
    libusb_device **list;
    ssize_t count = libusb_get_device_list(usbContext, &list);
    bool found = false;
    
    for (ssize_t i = 0; i < count; i++) {
        struct libusb_device_descriptor desc;
        if (libusb_get_device_descriptor(list[i], &desc) == 0) {
            if (desc.idVendor == GC2_VENDOR_ID && desc.idProduct == GC2_PRODUCT_ID) {
                found = true;
                break;
            }
        }
    }
    
    libusb_free_device_list(list, 1);
    return found;
}

bool GC2Mac_Connect(void) {
    if (deviceHandle) return true;  // Already connected
    
    deviceHandle = libusb_open_device_with_vid_pid(usbContext, GC2_VENDOR_ID, GC2_PRODUCT_ID);
    if (!deviceHandle) {
        NotifyError(@"GC2 device not found");
        return false;
    }
    
    // Detach kernel driver if attached
    if (libusb_kernel_driver_active(deviceHandle, 0) == 1) {
        libusb_detach_kernel_driver(deviceHandle, 0);
    }
    
    // Claim interface
    int result = libusb_claim_interface(deviceHandle, 0);
    if (result < 0) {
        NotifyError(@"Failed to claim USB interface");
        libusb_close(deviceHandle);
        deviceHandle = NULL;
        return false;
    }
    
    // Start read loop
    isRunning = YES;
    [dataBuffer setString:@""];
    
    dispatch_async(readQueue, ^{
        ReadLoop();
    });
    
    NotifyConnection(YES);
    NSLog(@"GC2 connected");
    return true;
}

void GC2Mac_Disconnect(void) {
    isRunning = NO;
    
    if (deviceHandle) {
        libusb_release_interface(deviceHandle, 0);
        libusb_close(deviceHandle);
        deviceHandle = NULL;
    }
    
    NotifyConnection(NO);
    NSLog(@"GC2 disconnected");
}

bool GC2Mac_IsConnected(void) {
    return deviceHandle != NULL && isRunning;
}

void GC2Mac_SetShotCallback(GC2ShotCallback callback) {
    shotCallback = callback;
}

void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback) {
    connectionCallback = callback;
}

void GC2Mac_SetErrorCallback(GC2ErrorCallback callback) {
    errorCallback = callback;
}
```

### 3.4 Building the Plugin

```bash
# In Xcode project:
# 1. Set target: macOS, Universal (Intel + ARM64)
# 2. Add libusb as embedded framework
# 3. Build as .bundle

# Or command line:
xcodebuild -project GC2MacPlugin.xcodeproj \
           -scheme GC2MacPlugin \
           -configuration Release \
           ARCHS="x86_64 arm64" \
           build

# Copy to Unity
cp -r build/Release/GC2MacPlugin.bundle ../../../Assets/Plugins/macOS/
cp libusb/libusb-1.0.26.dylib ../../../Assets/Plugins/macOS/
```

### 3.5 C# Bridge

```csharp
// Assets/Scripts/GC2/Platforms/MacOS/GC2MacConnection.cs
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace OpenRange.GC2.Platforms.MacOS
{
    public class GC2MacConnection : MonoBehaviour, IGC2Connection
    {
        #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_Initialize(string callbackObject);
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_Shutdown();
        
        [DllImport("GC2MacPlugin")]
        private static extern bool GC2Mac_IsDeviceAvailable();
        
        [DllImport("GC2MacPlugin")]
        private static extern bool GC2Mac_Connect();
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_Disconnect();
        
        [DllImport("GC2MacPlugin")]
        private static extern bool GC2Mac_IsConnected();
        
        #endif
        
        public bool IsConnected => 
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            GC2Mac_IsConnected();
            #else
            false;
            #endif
        
        public GC2DeviceInfo DeviceInfo { get; private set; }
        
        public event Action<GC2ShotData> OnShotReceived;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;
        
        private void Awake()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            GC2Mac_Initialize(gameObject.name);
            #endif
        }
        
        private void OnDestroy()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            GC2Mac_Shutdown();
            #endif
        }
        
        public bool IsDeviceAvailable()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return GC2Mac_IsDeviceAvailable();
            #else
            return false;
            #endif
        }
        
        public Task<bool> ConnectAsync()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return Task.FromResult(GC2Mac_Connect());
            #else
            return Task.FromResult(false);
            #endif
        }
        
        public void Disconnect()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            GC2Mac_Disconnect();
            #endif
        }
        
        // Called from native via UnitySendMessage
        public void OnNativeShotReceived(string json)
        {
            try
            {
                var shot = JsonConvert.DeserializeObject<GC2ShotData>(json);
                OnShotReceived?.Invoke(shot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Shot parse error: {ex.Message}");
            }
        }
        
        public void OnNativeConnectionChanged(string connected)
        {
            OnConnectionChanged?.Invoke(connected == "true");
        }
        
        public void OnNativeError(string error)
        {
            Debug.LogError($"GC2 Mac Error: {error}");
            OnError?.Invoke(error);
        }
    }
}
```

---

## 4. Android Plugin

### 4.1 Project Structure

```
NativePlugins/Android/GC2AndroidPlugin/
├── src/main/
│   ├── kotlin/com/openrange/gc2/
│   │   ├── GC2Plugin.kt
│   │   ├── GC2Device.kt
│   │   └── GC2Protocol.kt
│   ├── AndroidManifest.xml
│   └── res/xml/
│       └── usb_device_filter.xml
├── build.gradle
└── README.md
```

### 4.2 AndroidManifest.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.openrange.gc2">
    
    <uses-feature android:name="android.hardware.usb.host" android:required="true" />
    
</manifest>
```

### 4.3 USB Device Filter

```xml
<!-- res/xml/usb_device_filter.xml -->
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <usb-device vendor-id="11385" product-id="272" />
</resources>
```

### 4.4 Kotlin Implementation

See TRD for complete implementation. Key points:

```kotlin
// GC2Plugin.kt
class GC2Plugin {
    companion object {
        private const val GC2_VENDOR_ID = 0x2C79
        private const val GC2_PRODUCT_ID = 0x0110
        
        @JvmStatic
        fun getInstance(): GC2Plugin { ... }
    }
    
    fun initialize(context: Context, callbackObject: String) { ... }
    fun isDeviceAvailable(): Boolean { ... }
    fun connect(context: Context): Boolean { ... }
    fun disconnect() { ... }
    fun isConnected(): Boolean { ... }
}
```

### 4.5 Building the Plugin

```bash
# In Android Studio or command line
cd NativePlugins/Android/GC2AndroidPlugin
./gradlew assembleRelease

# Copy to Unity
cp build/outputs/aar/GC2AndroidPlugin-release.aar \
   ../../../Assets/Plugins/Android/
```

---

## 5. iPad Plugin (DriverKit)

### 5.1 Project Structure

```
NativePlugins/iOS/
├── GC2iOSPlugin/
│   ├── GC2iOSPlugin.swift
│   ├── GC2iOSPlugin.h          # Bridging header
│   └── GC2iOSPlugin.xcodeproj/
├── GC2Driver/
│   ├── GC2Driver.swift         # DriverKit driver
│   ├── GC2UserClient.swift     # App communication
│   ├── Info.plist              # USB matching
│   └── Entitlements.plist
└── README.md
```

### 5.2 DriverKit Entitlements

```xml
<!-- GC2Driver/Entitlements.plist -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "...">
<plist version="1.0">
<dict>
    <key>com.apple.developer.driverkit</key>
    <true/>
    <key>com.apple.developer.driverkit.transport.usb</key>
    <array>
        <dict>
            <key>idVendor</key>
            <integer>11385</integer>
            <key>idProduct</key>
            <integer>272</integer>
        </dict>
    </array>
</dict>
</plist>
```

### 5.3 DriverKit Info.plist

```xml
<!-- GC2Driver/Info.plist -->
<key>IOKitPersonalities</key>
<dict>
    <key>GC2Driver</key>
    <dict>
        <key>CFBundleIdentifier</key>
        <string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
        <key>IOClass</key>
        <string>IOUserUSBHostDevice</string>
        <key>IOProviderClass</key>
        <string>IOUSBHostDevice</string>
        <key>idVendor</key>
        <integer>11385</integer>
        <key>idProduct</key>
        <integer>272</integer>
        <key>IOUserClass</key>
        <string>GC2Driver</string>
        <key>IOUserServerName</key>
        <string>com.openrange.GC2Driver</string>
    </dict>
</dict>
```

### 5.4 Requesting Entitlements

1. Go to [developer.apple.com/system-extensions](https://developer.apple.com/system-extensions/)
2. Request DriverKit + USB Transport entitlement
3. Provide:
   - Vendor ID: 0x2C79 (11385)
   - Product ID: 0x0110 (272)
   - Use case: "Golf launch monitor data reader"
4. Wait 1-4 weeks for approval
5. Create distribution profile with DriverKit template

---

## 6. Unity Integration

### 6.1 Plugin Placement

```
Assets/Plugins/
├── macOS/
│   ├── GC2MacPlugin.bundle
│   └── libusb-1.0.26.dylib
├── iOS/
│   ├── GC2iOSPlugin.framework
│   └── GC2Driver.dext
└── Android/
    └── GC2AndroidPlugin.aar
```

### 6.2 Platform Settings

**macOS Plugin (.bundle):**
- Platform: Standalone macOS
- CPU: Universal

**iOS Framework:**
- Platform: iOS
- Add to Embedded Binaries

**Android AAR:**
- Platform: Android
- Add to Gradle dependencies

### 6.3 Connection Factory

```csharp
// Assets/Scripts/GC2/GC2ConnectionFactory.cs
namespace OpenRange.GC2
{
    public static class GC2ConnectionFactory
    {
        public static IGC2Connection Create(GameObject host)
        {
#if UNITY_EDITOR
            return host.AddComponent<GC2TCPConnection>();
#elif UNITY_STANDALONE_OSX
            return host.AddComponent<GC2MacConnection>();
#elif UNITY_IOS
            return host.AddComponent<GC2iPadConnection>();
#elif UNITY_ANDROID
            return host.AddComponent<GC2AndroidConnection>();
#else
            return host.AddComponent<GC2TCPConnection>();
#endif
        }
    }
}
```

---

## 7. Testing

### 7.1 Editor Testing

Use TCP connection in editor:

```csharp
// Can connect to GC2 Connect Desktop running on same machine
var connection = GC2ConnectionFactory.CreateTCP("127.0.0.1", 8888);
```

### 7.2 Device Testing

1. Build for target platform
2. Connect GC2 via USB
3. Grant permissions when prompted
4. Hit a shot, verify data received

### 7.3 Test Without GC2

```csharp
// Inject test shots
public void InjectTestShot()
{
    var testShot = new GC2ShotData
    {
        BallSpeed = 150f,
        LaunchAngle = 12f,
        Direction = 0f,
        TotalSpin = 3000f,
        BackSpin = 2900f,
        SideSpin = 500f
    };
    
    _shotProcessor.HandleShot(testShot);
}
```

---

## 8. Troubleshooting

### 8.1 macOS

| Issue | Solution |
|-------|----------|
| Plugin not loading | Check .bundle is in Plugins/macOS/ |
| libusb not found | Ensure dylib is alongside bundle |
| Permission denied | Check app is signed/notarized |

### 8.2 Android

| Issue | Solution |
|-------|----------|
| Device not found | Check USB Host support |
| Permission denied | Handle permission flow |
| Plugin not loading | Verify AAR in Plugins/Android/ |

### 8.3 iPad

| Issue | Solution |
|-------|----------|
| Driver not loading | Check entitlements approved |
| Device not matched | Verify VID/PID in Info.plist |
| Framework error | Ensure framework embedded |
