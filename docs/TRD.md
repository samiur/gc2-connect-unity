# Technical Requirements Document (TRD)
# Open Range Unity - Cross-Platform Edition

## Overview

### Document Purpose
This document defines the technical architecture for a unified Unity application that runs on macOS, iPad, and Android with native USB support for the Foresight GC2 launch monitor.

### Version
2.0.0

### Last Updated
December 2024

---

## System Architecture

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                        Open Range Unity (Cross-Platform)                          │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                     Shared C# Code (95% of codebase)                        │ │
│  │                                                                              │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐   │ │
│  │  │   Physics   │ │Visualization│ │     UI      │ │     Services        │   │ │
│  │  │   Engine    │ │   System    │ │   System    │ │                     │   │ │
│  │  │             │ │             │ │             │ │ • ShotProcessor     │   │ │
│  │  │ • Trajectory│ │ • BallCtrl  │ │ • DataBar   │ │ • SessionManager    │   │ │
│  │  │ • Aero      │ │ • TrailFX   │ │ • Panels    │ │ • SettingsManager   │   │ │
│  │  │ • Ground    │ │ • Camera    │ │ • Responsive│ │ • GSProClient       │   │ │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────┘   │ │
│  │                                                                              │ │
│  └──────────────────────────────────┬──────────────────────────────────────────┘ │
│                                     │                                            │
│  ┌──────────────────────────────────┴──────────────────────────────────────────┐ │
│  │                    GC2 Connection Abstraction Layer                          │ │
│  │                                                                              │ │
│  │  public interface IGC2Connection {                                          │ │
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
│  │                       │ │ Plugin        │ │ Plugin           │               │
│  │  ┌─────────────────┐  │ │               │ │                  │               │
│  │  │ GC2MacPlugin.mm │  │ │ ┌───────────┐ │ │ ┌──────────────┐ │               │
│  │  │ (Objective-C)   │  │ │ │DriverKit │ │ │ │GC2Android.kt │ │               │
│  │  │                 │  │ │ │Extension │ │ │ │(Kotlin)      │ │               │
│  │  │ Uses libusb     │  │ │ └───────────┘ │ │ │              │ │               │
│  │  └─────────────────┘  │ │ ┌───────────┐ │ │ │Uses USB Host │ │               │
│  │                       │ │ │Swift      │ │ │ │API           │ │               │
│  │  Bundled in .bundle   │ │ │Bridge     │ │ │ └──────────────┘ │               │
│  └───────────┬───────────┘ │ └───────────┘ │ │                  │               │
│              │             └───────┬───────┘ └────────┬─────────┘               │
└──────────────┼─────────────────────┼──────────────────┼──────────────────────────┘
               │                     │                  │
               ▼                     ▼                  ▼
        ┌────────────┐        ┌────────────┐     ┌────────────┐
        │    GC2     │        │    GC2     │     │    GC2     │
        │  (USB-A)   │        │  (USB-C)   │     │  (USB-C)   │
        └────────────┘        └────────────┘     └────────────┘
              Mac                  iPad              Android
```

---

## Project Structure

```
OpenRangeUnity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── ShotProcessor.cs
│   │   │   ├── SessionManager.cs
│   │   │   ├── SettingsManager.cs
│   │   │   └── PlatformManager.cs
│   │   │
│   │   ├── GC2/
│   │   │   ├── IGC2Connection.cs           # Interface
│   │   │   ├── GC2ConnectionFactory.cs     # Platform factory
│   │   │   ├── GC2Protocol.cs              # Protocol parsing
│   │   │   ├── GC2ShotData.cs              # Data model
│   │   │   │
│   │   │   ├── Platforms/
│   │   │   │   ├── MacOS/
│   │   │   │   │   └── GC2MacConnection.cs
│   │   │   │   ├── iOS/
│   │   │   │   │   └── GC2iPadConnection.cs
│   │   │   │   └── Android/
│   │   │   │       └── GC2AndroidConnection.cs
│   │   │   │
│   │   │   └── Network/
│   │   │       └── GC2TCPConnection.cs     # Fallback/GSPro mode
│   │   │
│   │   ├── Physics/
│   │   │   ├── PhysicsConstants.cs
│   │   │   ├── Aerodynamics.cs
│   │   │   ├── TrajectorySimulator.cs
│   │   │   ├── GroundPhysics.cs
│   │   │   ├── AtmosphericModel.cs
│   │   │   └── UnitConversions.cs
│   │   │
│   │   ├── Visualization/
│   │   │   ├── BallController.cs
│   │   │   ├── TrajectoryRenderer.cs
│   │   │   ├── LandingMarker.cs
│   │   │   ├── CameraController.cs
│   │   │   └── EnvironmentManager.cs
│   │   │
│   │   ├── UI/
│   │   │   ├── UIManager.cs
│   │   │   ├── ShotDataBar.cs
│   │   │   ├── ClubDataPanel.cs
│   │   │   ├── SessionInfoPanel.cs
│   │   │   ├── ClubSelector.cs
│   │   │   ├── SettingsPanel.cs
│   │   │   ├── ConnectionStatusUI.cs
│   │   │   └── ResponsiveLayout.cs
│   │   │
│   │   ├── Network/
│   │   │   └── GSProClient.cs
│   │   │
│   │   └── Utilities/
│   │       ├── MainThreadDispatcher.cs
│   │       └── PlatformUtils.cs
│   │
│   ├── Plugins/
│   │   ├── macOS/
│   │   │   ├── GC2MacPlugin.bundle/
│   │   │   │   └── Contents/
│   │   │   │       ├── MacOS/
│   │   │   │       │   └── GC2MacPlugin
│   │   │   │       └── Info.plist
│   │   │   └── libusb.dylib
│   │   │
│   │   ├── iOS/
│   │   │   ├── GC2iOSPlugin.framework/
│   │   │   └── GC2Driver.dext/             # DriverKit extension
│   │   │
│   │   └── Android/
│   │       └── GC2AndroidPlugin.aar
│   │
│   ├── Prefabs/
│   │   ├── Ball/
│   │   ├── Environment/
│   │   ├── UI/
│   │   └── Effects/
│   │
│   ├── Scenes/
│   │   ├── Bootstrap.unity                 # Initial loading
│   │   ├── MainMenu.unity
│   │   └── Ranges/
│   │       ├── Marina.unity
│   │       ├── Mountain.unity
│   │       └── Links.unity
│   │
│   ├── Materials/
│   ├── Textures/
│   ├── Models/
│   ├── Audio/
│   │
│   └── StreamingAssets/
│       └── Settings/
│
├── Packages/
│   └── manifest.json
│
├── NativePlugins/                          # Native source code
│   ├── macOS/
│   │   ├── GC2MacPlugin/
│   │   │   ├── GC2MacPlugin.mm
│   │   │   ├── GC2MacPlugin.h
│   │   │   └── GC2MacPlugin.xcodeproj
│   │   └── README.md
│   │
│   ├── iOS/
│   │   ├── GC2iOSPlugin/
│   │   │   ├── GC2iOSPlugin.swift
│   │   │   └── GC2iOSPlugin.xcodeproj
│   │   ├── GC2Driver/
│   │   │   ├── GC2Driver.swift
│   │   │   ├── GC2UserClient.swift
│   │   │   └── Info.plist
│   │   └── README.md
│   │
│   └── Android/
│       ├── GC2AndroidPlugin/
│       │   ├── src/main/kotlin/
│       │   │   └── com/openrange/gc2/
│       │   │       ├── GC2Plugin.kt
│       │   │       ├── GC2Device.kt
│       │   │       └── GC2Protocol.kt
│       │   ├── build.gradle
│       │   └── AndroidManifest.xml
│       └── README.md
│
└── ProjectSettings/
```

---

## Native Plugin Specifications

### Interface Definition (C#)

```csharp
// GC2/IGC2Connection.cs
namespace OpenRange.GC2
{
    /// <summary>
    /// Platform-agnostic interface for GC2 communication
    /// </summary>
    public interface IGC2Connection
    {
        /// <summary>Connection state</summary>
        bool IsConnected { get; }
        
        /// <summary>Device information (if connected)</summary>
        GC2DeviceInfo DeviceInfo { get; }
        
        /// <summary>Fired when a valid shot is received</summary>
        event Action<GC2ShotData> OnShotReceived;
        
        /// <summary>Fired on connection state change</summary>
        event Action<bool> OnConnectionChanged;
        
        /// <summary>Fired on errors</summary>
        event Action<string> OnError;
        
        /// <summary>Attempt to connect to GC2</summary>
        Task<bool> ConnectAsync();
        
        /// <summary>Disconnect from GC2</summary>
        void Disconnect();
        
        /// <summary>Check if GC2 is available (pre-connection check)</summary>
        bool IsDeviceAvailable();
    }
    
    public class GC2DeviceInfo
    {
        public string SerialNumber;
        public string FirmwareVersion;
        public bool HasHMT;
    }
    
    public class GC2ShotData
    {
        public int ShotId;
        public DateTime Timestamp;
        
        // Ball data
        public float BallSpeed;        // mph
        public float LaunchAngle;      // degrees (vertical)
        public float Direction;        // degrees (horizontal)
        public float TotalSpin;        // rpm
        public float BackSpin;         // rpm
        public float SideSpin;         // rpm
        public float SpinAxis;         // degrees
        
        // Club data (HMT only)
        public bool HasClubData;
        public float ClubSpeed;        // mph
        public float Path;             // degrees
        public float AttackAngle;      // degrees
        public float FaceToTarget;     // degrees
        public float DynamicLoft;      // degrees
        public float Lie;              // degrees
    }
}
```

### Factory Pattern for Platform Selection

```csharp
// GC2/GC2ConnectionFactory.cs
namespace OpenRange.GC2
{
    public static class GC2ConnectionFactory
    {
        public static IGC2Connection Create()
        {
#if UNITY_EDITOR
            // In editor, use TCP for testing
            return new GC2TCPConnection();
#elif UNITY_STANDALONE_OSX
            return new GC2MacConnection();
#elif UNITY_IOS
            return new GC2iPadConnection();
#elif UNITY_ANDROID
            return new GC2AndroidConnection();
#else
            // Fallback to TCP
            return new GC2TCPConnection();
#endif
        }
        
        public static IGC2Connection CreateTCP(string host = "127.0.0.1", int port = 8888)
        {
            return new GC2TCPConnection(host, port);
        }
    }
}
```

---

### macOS Native Plugin

#### GC2MacPlugin.h

```objectivec
// NativePlugins/macOS/GC2MacPlugin/GC2MacPlugin.h

#ifndef GC2MacPlugin_h
#define GC2MacPlugin_h

#import <Foundation/Foundation.h>

// Callback types
typedef void (*GC2ShotCallback)(const char* jsonData);
typedef void (*GC2ConnectionCallback)(bool connected);
typedef void (*GC2ErrorCallback)(const char* error);

#ifdef __cplusplus
extern "C" {
#endif

// Plugin functions
bool GC2Mac_Initialize(void);
void GC2Mac_Shutdown(void);
bool GC2Mac_IsDeviceAvailable(void);
bool GC2Mac_Connect(void);
void GC2Mac_Disconnect(void);
bool GC2Mac_IsConnected(void);
void GC2Mac_SetShotCallback(GC2ShotCallback callback);
void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback);
void GC2Mac_SetErrorCallback(GC2ErrorCallback callback);

#ifdef __cplusplus
}
#endif

#endif
```

#### GC2MacPlugin.mm

```objectivec
// NativePlugins/macOS/GC2MacPlugin/GC2MacPlugin.mm

#import "GC2MacPlugin.h"
#import <libusb.h>
#import <dispatch/dispatch.h>

#define GC2_VENDOR_ID  0x2C79
#define GC2_PRODUCT_ID 0x0110

static libusb_context *ctx = NULL;
static libusb_device_handle *handle = NULL;
static dispatch_queue_t readQueue = NULL;
static bool isRunning = false;

static GC2ShotCallback shotCallback = NULL;
static GC2ConnectionCallback connectionCallback = NULL;
static GC2ErrorCallback errorCallback = NULL;

// Protocol parsing
static NSMutableString *dataBuffer = nil;

bool GC2Mac_Initialize(void) {
    int r = libusb_init(&ctx);
    if (r < 0) {
        if (errorCallback) errorCallback("Failed to initialize libusb");
        return false;
    }
    
    dataBuffer = [[NSMutableString alloc] init];
    readQueue = dispatch_queue_create("com.openrange.gc2.read", DISPATCH_QUEUE_SERIAL);
    return true;
}

void GC2Mac_Shutdown(void) {
    GC2Mac_Disconnect();
    if (ctx) {
        libusb_exit(ctx);
        ctx = NULL;
    }
}

bool GC2Mac_IsDeviceAvailable(void) {
    if (!ctx) return false;
    
    libusb_device **list;
    ssize_t count = libusb_get_device_list(ctx, &list);
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
    if (handle) return true; // Already connected
    
    handle = libusb_open_device_with_vid_pid(ctx, GC2_VENDOR_ID, GC2_PRODUCT_ID);
    if (!handle) {
        if (errorCallback) errorCallback("GC2 not found");
        return false;
    }
    
    // Claim interface
    int r = libusb_claim_interface(handle, 0);
    if (r < 0) {
        if (errorCallback) errorCallback("Failed to claim interface");
        libusb_close(handle);
        handle = NULL;
        return false;
    }
    
    isRunning = true;
    if (connectionCallback) connectionCallback(true);
    
    // Start read loop
    dispatch_async(readQueue, ^{
        unsigned char buffer[512];
        int transferred;
        
        while (isRunning && handle) {
            int r = libusb_bulk_transfer(handle, 0x81, buffer, sizeof(buffer), 
                                         &transferred, 100);
            
            if (r == 0 && transferred > 0) {
                NSString *data = [[NSString alloc] initWithBytes:buffer 
                                                          length:transferred 
                                                        encoding:NSUTF8StringEncoding];
                if (data) {
                    [dataBuffer appendString:data];
                    [self processBuffer];
                }
            } else if (r != LIBUSB_ERROR_TIMEOUT) {
                // Error occurred
                break;
            }
        }
        
        dispatch_async(dispatch_get_main_queue(), ^{
            if (connectionCallback) connectionCallback(false);
        });
    });
    
    return true;
}

void GC2Mac_Disconnect(void) {
    isRunning = false;
    
    if (handle) {
        libusb_release_interface(handle, 0);
        libusb_close(handle);
        handle = NULL;
    }
    
    if (connectionCallback) connectionCallback(false);
}

bool GC2Mac_IsConnected(void) {
    return handle != NULL && isRunning;
}

void processBuffer(void) {
    // Look for complete shot data (ends with specific marker)
    NSRange range = [dataBuffer rangeOfString:@"\n\n"];
    while (range.location != NSNotFound) {
        NSString *message = [dataBuffer substringToIndex:range.location];
        [dataBuffer deleteCharactersInRange:NSMakeRange(0, range.location + 2)];
        
        // Parse and convert to JSON
        NSDictionary *shotData = parseGC2Protocol(message);
        if (shotData) {
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:shotData options:0 error:nil];
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            
            dispatch_async(dispatch_get_main_queue(), ^{
                if (shotCallback) shotCallback([jsonString UTF8String]);
            });
        }
        
        range = [dataBuffer rangeOfString:@"\n\n"];
    }
}

NSDictionary* parseGC2Protocol(NSString *data) {
    // Parse key=value pairs
    NSMutableDictionary *result = [NSMutableDictionary dictionary];
    
    NSArray *lines = [data componentsSeparatedByString:@"\n"];
    for (NSString *line in lines) {
        NSArray *parts = [line componentsSeparatedByString:@"="];
        if (parts.count == 2) {
            result[parts[0]] = parts[1];
        }
    }
    
    // Validate and transform
    if (!result[@"SPEED_MPH"]) return nil;
    
    return @{
        @"shotId": result[@"SHOT_ID"] ?: @"0",
        @"ballSpeed": result[@"SPEED_MPH"] ?: @"0",
        @"launchAngle": result[@"ELEVATION_DEG"] ?: @"0",
        @"direction": result[@"AZIMUTH_DEG"] ?: @"0",
        @"totalSpin": result[@"SPIN_RPM"] ?: @"0",
        @"backSpin": result[@"BACK_RPM"] ?: @"0",
        @"sideSpin": result[@"SIDE_RPM"] ?: @"0",
        @"hasHMT": result[@"HMT"] ?: @"0",
        @"clubSpeed": result[@"CLUBSPEED_MPH"] ?: @"0",
        @"path": result[@"HPATH_DEG"] ?: @"0",
        @"attackAngle": result[@"VPATH_DEG"] ?: @"0",
        @"faceToTarget": result[@"FACE_T_DEG"] ?: @"0",
        @"dynamicLoft": result[@"LOFT_DEG"] ?: @"0"
    };
}

void GC2Mac_SetShotCallback(GC2ShotCallback callback) { shotCallback = callback; }
void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback) { connectionCallback = callback; }
void GC2Mac_SetErrorCallback(GC2ErrorCallback callback) { errorCallback = callback; }
```

#### C# Bridge (macOS)

```csharp
// GC2/Platforms/MacOS/GC2MacConnection.cs
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace OpenRange.GC2.Platforms.MacOS
{
    public class GC2MacConnection : IGC2Connection
    {
        // Native imports
        [DllImport("GC2MacPlugin")]
        private static extern bool GC2Mac_Initialize();
        
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
        
        // Callbacks
        private delegate void ShotCallbackDelegate(string jsonData);
        private delegate void ConnectionCallbackDelegate(bool connected);
        private delegate void ErrorCallbackDelegate(string error);
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_SetShotCallback(ShotCallbackDelegate callback);
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_SetConnectionCallback(ConnectionCallbackDelegate callback);
        
        [DllImport("GC2MacPlugin")]
        private static extern void GC2Mac_SetErrorCallback(ErrorCallbackDelegate callback);
        
        // Instance fields
        private static GC2MacConnection _instance;
        private ShotCallbackDelegate _shotCallback;
        private ConnectionCallbackDelegate _connectionCallback;
        private ErrorCallbackDelegate _errorCallback;
        
        public bool IsConnected => GC2Mac_IsConnected();
        public GC2DeviceInfo DeviceInfo { get; private set; }
        
        public event Action<GC2ShotData> OnShotReceived;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;
        
        public GC2MacConnection()
        {
            _instance = this;
            
            // Initialize native plugin
            if (!GC2Mac_Initialize())
            {
                Debug.LogError("Failed to initialize GC2 Mac plugin");
                return;
            }
            
            // Set up callbacks (prevent GC)
            _shotCallback = OnNativeShotReceived;
            _connectionCallback = OnNativeConnectionChanged;
            _errorCallback = OnNativeError;
            
            GC2Mac_SetShotCallback(_shotCallback);
            GC2Mac_SetConnectionCallback(_connectionCallback);
            GC2Mac_SetErrorCallback(_errorCallback);
        }
        
        ~GC2MacConnection()
        {
            GC2Mac_Shutdown();
        }
        
        public bool IsDeviceAvailable() => GC2Mac_IsDeviceAvailable();
        
        public Task<bool> ConnectAsync()
        {
            return Task.FromResult(GC2Mac_Connect());
        }
        
        public void Disconnect()
        {
            GC2Mac_Disconnect();
        }
        
        // Native callback handlers
        [AOT.MonoPInvokeCallback(typeof(ShotCallbackDelegate))]
        private static void OnNativeShotReceived(string jsonData)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var shot = JsonConvert.DeserializeObject<GC2ShotData>(jsonData);
                    _instance?.OnShotReceived?.Invoke(shot);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse shot data: {ex.Message}");
                }
            });
        }
        
        [AOT.MonoPInvokeCallback(typeof(ConnectionCallbackDelegate))]
        private static void OnNativeConnectionChanged(bool connected)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                _instance?.OnConnectionChanged?.Invoke(connected);
            });
        }
        
        [AOT.MonoPInvokeCallback(typeof(ErrorCallbackDelegate))]
        private static void OnNativeError(string error)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.LogError($"GC2 Error: {error}");
                _instance?.OnError?.Invoke(error);
            });
        }
    }
}
```

---

### Android Native Plugin

#### GC2Plugin.kt

```kotlin
// NativePlugins/Android/GC2AndroidPlugin/src/main/kotlin/com/openrange/gc2/GC2Plugin.kt

package com.openrange.gc2

import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.*
import com.unity3d.player.UnityPlayer
import org.json.JSONObject
import java.nio.charset.Charset

class GC2Plugin {
    companion object {
        private const val ACTION_USB_PERMISSION = "com.openrange.gc2.USB_PERMISSION"
        private const val GC2_VENDOR_ID = 0x2C79
        private const val GC2_PRODUCT_ID = 0x0110
        
        private var instance: GC2Plugin? = null
        
        @JvmStatic
        fun getInstance(): GC2Plugin {
            if (instance == null) {
                instance = GC2Plugin()
            }
            return instance!!
        }
    }
    
    private var usbManager: UsbManager? = null
    private var device: UsbDevice? = null
    private var connection: UsbDeviceConnection? = null
    private var endpoint: UsbEndpoint? = null
    private var isRunning = false
    private var readThread: Thread? = null
    private val dataBuffer = StringBuilder()
    
    // Unity callback object and method names
    private var callbackObject: String = "GC2Manager"
    
    fun initialize(context: Context, callbackObj: String) {
        callbackObject = callbackObj
        usbManager = context.getSystemService(Context.USB_SERVICE) as UsbManager
        
        // Register USB permission receiver
        val filter = IntentFilter(ACTION_USB_PERMISSION)
        filter.addAction(UsbManager.ACTION_USB_DEVICE_ATTACHED)
        filter.addAction(UsbManager.ACTION_USB_DEVICE_DETACHED)
        context.registerReceiver(usbReceiver, filter)
    }
    
    fun isDeviceAvailable(): Boolean {
        return findGC2Device() != null
    }
    
    fun connect(context: Context): Boolean {
        device = findGC2Device()
        if (device == null) {
            sendError("GC2 not found")
            return false
        }
        
        // Request permission if needed
        if (!usbManager!!.hasPermission(device)) {
            val permissionIntent = PendingIntent.getBroadcast(
                context, 0, 
                Intent(ACTION_USB_PERMISSION),
                PendingIntent.FLAG_IMMUTABLE
            )
            usbManager!!.requestPermission(device, permissionIntent)
            return false // Will connect after permission granted
        }
        
        return openDevice()
    }
    
    fun disconnect() {
        isRunning = false
        readThread?.interrupt()
        readThread = null
        connection?.close()
        connection = null
        device = null
        
        sendConnectionChanged(false)
    }
    
    fun isConnected(): Boolean {
        return connection != null && isRunning
    }
    
    private fun findGC2Device(): UsbDevice? {
        return usbManager?.deviceList?.values?.find { 
            it.vendorId == GC2_VENDOR_ID && it.productId == GC2_PRODUCT_ID 
        }
    }
    
    private fun openDevice(): Boolean {
        try {
            connection = usbManager!!.openDevice(device)
            if (connection == null) {
                sendError("Failed to open device")
                return false
            }
            
            // Find the bulk IN endpoint
            val intf = device!!.getInterface(0)
            connection!!.claimInterface(intf, true)
            
            for (i in 0 until intf.endpointCount) {
                val ep = intf.getEndpoint(i)
                if (ep.type == UsbConstants.USB_ENDPOINT_XFER_BULK &&
                    ep.direction == UsbConstants.USB_DIR_IN) {
                    endpoint = ep
                    break
                }
            }
            
            if (endpoint == null) {
                sendError("No bulk IN endpoint found")
                connection!!.close()
                connection = null
                return false
            }
            
            // Start read thread
            isRunning = true
            readThread = Thread { readLoop() }
            readThread!!.start()
            
            sendConnectionChanged(true)
            return true
            
        } catch (e: Exception) {
            sendError("Connection error: ${e.message}")
            return false
        }
    }
    
    private fun readLoop() {
        val buffer = ByteArray(512)
        
        while (isRunning && connection != null) {
            val bytesRead = connection!!.bulkTransfer(endpoint, buffer, buffer.size, 100)
            
            if (bytesRead > 0) {
                val data = String(buffer, 0, bytesRead, Charset.forName("UTF-8"))
                dataBuffer.append(data)
                processBuffer()
            }
        }
    }
    
    private fun processBuffer() {
        var idx = dataBuffer.indexOf("\n\n")
        while (idx >= 0) {
            val message = dataBuffer.substring(0, idx)
            dataBuffer.delete(0, idx + 2)
            
            val shotData = parseGC2Protocol(message)
            if (shotData != null) {
                sendShot(shotData.toString())
            }
            
            idx = dataBuffer.indexOf("\n\n")
        }
    }
    
    private fun parseGC2Protocol(data: String): JSONObject? {
        val values = mutableMapOf<String, String>()
        
        data.split("\n").forEach { line ->
            val parts = line.split("=")
            if (parts.size == 2) {
                values[parts[0]] = parts[1]
            }
        }
        
        // Validate
        if (!values.containsKey("SPEED_MPH")) return null
        
        return JSONObject().apply {
            put("shotId", values["SHOT_ID"] ?: "0")
            put("ballSpeed", values["SPEED_MPH"]?.toFloatOrNull() ?: 0f)
            put("launchAngle", values["ELEVATION_DEG"]?.toFloatOrNull() ?: 0f)
            put("direction", values["AZIMUTH_DEG"]?.toFloatOrNull() ?: 0f)
            put("totalSpin", values["SPIN_RPM"]?.toFloatOrNull() ?: 0f)
            put("backSpin", values["BACK_RPM"]?.toFloatOrNull() ?: 0f)
            put("sideSpin", values["SIDE_RPM"]?.toFloatOrNull() ?: 0f)
            put("hasHMT", values["HMT"] == "1")
            put("clubSpeed", values["CLUBSPEED_MPH"]?.toFloatOrNull() ?: 0f)
            put("path", values["HPATH_DEG"]?.toFloatOrNull() ?: 0f)
            put("attackAngle", values["VPATH_DEG"]?.toFloatOrNull() ?: 0f)
            put("faceToTarget", values["FACE_T_DEG"]?.toFloatOrNull() ?: 0f)
            put("dynamicLoft", values["LOFT_DEG"]?.toFloatOrNull() ?: 0f)
        }
    }
    
    private val usbReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            when (intent.action) {
                ACTION_USB_PERMISSION -> {
                    synchronized(this) {
                        val device = intent.getParcelableExtra<UsbDevice>(UsbManager.EXTRA_DEVICE)
                        if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                            openDevice()
                        } else {
                            sendError("USB permission denied")
                        }
                    }
                }
                UsbManager.ACTION_USB_DEVICE_DETACHED -> {
                    val detachedDevice = intent.getParcelableExtra<UsbDevice>(UsbManager.EXTRA_DEVICE)
                    if (detachedDevice?.vendorId == GC2_VENDOR_ID) {
                        disconnect()
                    }
                }
            }
        }
    }
    
    // Unity callbacks
    private fun sendShot(json: String) {
        UnityPlayer.UnitySendMessage(callbackObject, "OnNativeShotReceived", json)
    }
    
    private fun sendConnectionChanged(connected: Boolean) {
        UnityPlayer.UnitySendMessage(callbackObject, "OnNativeConnectionChanged", 
            if (connected) "true" else "false")
    }
    
    private fun sendError(error: String) {
        UnityPlayer.UnitySendMessage(callbackObject, "OnNativeError", error)
    }
}
```

#### C# Bridge (Android)

```csharp
// GC2/Platforms/Android/GC2AndroidConnection.cs
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenRange.GC2.Platforms.Android
{
    public class GC2AndroidConnection : MonoBehaviour, IGC2Connection
    {
        private AndroidJavaObject _plugin;
        private static GC2AndroidConnection _instance;
        
        public bool IsConnected => _plugin?.Call<bool>("isConnected") ?? false;
        public GC2DeviceInfo DeviceInfo { get; private set; }
        
        public event Action<GC2ShotData> OnShotReceived;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;
        
        private void Awake()
        {
            _instance = this;
            
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var pluginClass = new AndroidJavaClass("com.openrange.gc2.GC2Plugin"))
                {
                    _plugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
                }
                
                using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _plugin.Call("initialize", activity, gameObject.name);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Android GC2 plugin: {ex.Message}");
            }
#endif
        }
        
        public bool IsDeviceAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _plugin?.Call<bool>("isDeviceAvailable") ?? false;
#else
            return false;
#endif
        }
        
        public Task<bool> ConnectAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return Task.FromResult(_plugin?.Call<bool>("connect", activity) ?? false);
            }
#else
            return Task.FromResult(false);
#endif
        }
        
        public void Disconnect()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _plugin?.Call("disconnect");
#endif
        }
        
        // Called from native code via UnitySendMessage
        public void OnNativeShotReceived(string json)
        {
            try
            {
                var shot = JsonUtility.FromJson<GC2ShotData>(json);
                OnShotReceived?.Invoke(shot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse shot: {ex.Message}");
            }
        }
        
        public void OnNativeConnectionChanged(string connected)
        {
            OnConnectionChanged?.Invoke(connected == "true");
        }
        
        public void OnNativeError(string error)
        {
            Debug.LogError($"GC2 Android Error: {error}");
            OnError?.Invoke(error);
        }
    }
}
```

---

### iPad Native Plugin (DriverKit)

The iPad implementation requires DriverKit, which is more complex. Here's the structure:

#### GC2iOSPlugin.swift (App-side bridge)

```swift
// NativePlugins/iOS/GC2iOSPlugin/GC2iOSPlugin.swift

import Foundation
import IOKit
import os.log

@objc public class GC2iOSPlugin: NSObject {
    
    @objc public static let shared = GC2iOSPlugin()
    
    private var connection: IOUserClientConnection?
    private var isRunning = false
    private let readQueue = DispatchQueue(label: "com.openrange.gc2.read")
    private var dataBuffer = ""
    
    // Unity callback object name
    private var unityObject = "GC2Manager"
    
    @objc public func initialize(callbackObject: String) {
        unityObject = callbackObject
    }
    
    @objc public func isDeviceAvailable() -> Bool {
        // Check if DriverKit driver is available and device connected
        return findGC2Service() != nil
    }
    
    @objc public func connect() -> Bool {
        guard let service = findGC2Service() else {
            sendError("GC2 not found")
            return false
        }
        
        do {
            connection = try IOUserClientConnection(service: service)
            isRunning = true
            
            // Start async read
            readQueue.async { [weak self] in
                self?.readLoop()
            }
            
            sendConnectionChanged(true)
            return true
        } catch {
            sendError("Connection failed: \(error)")
            return false
        }
    }
    
    @objc public func disconnect() {
        isRunning = false
        connection?.close()
        connection = nil
        sendConnectionChanged(false)
    }
    
    @objc public func isConnected() -> Bool {
        return connection != nil && isRunning
    }
    
    private func findGC2Service() -> io_service_t? {
        let matching = IOServiceMatching("GC2Driver")
        var iterator: io_iterator_t = 0
        
        guard IOServiceGetMatchingServices(kIOMainPortDefault, matching, &iterator) == KERN_SUCCESS else {
            return nil
        }
        
        defer { IOObjectRelease(iterator) }
        
        let service = IOIteratorNext(iterator)
        return service != 0 ? service : nil
    }
    
    private func readLoop() {
        while isRunning {
            guard let data = connection?.read(timeout: 100) else {
                continue
            }
            
            if let string = String(data: data, encoding: .utf8) {
                dataBuffer += string
                processBuffer()
            }
        }
    }
    
    private func processBuffer() {
        while let range = dataBuffer.range(of: "\n\n") {
            let message = String(dataBuffer[..<range.lowerBound])
            dataBuffer = String(dataBuffer[range.upperBound...])
            
            if let shotData = parseGC2Protocol(message) {
                DispatchQueue.main.async {
                    self.sendShot(shotData)
                }
            }
        }
    }
    
    private func parseGC2Protocol(_ data: String) -> [String: Any]? {
        var values: [String: String] = [:]
        
        data.split(separator: "\n").forEach { line in
            let parts = line.split(separator: "=")
            if parts.count == 2 {
                values[String(parts[0])] = String(parts[1])
            }
        }
        
        guard values["SPEED_MPH"] != nil else { return nil }
        
        return [
            "shotId": values["SHOT_ID"] ?? "0",
            "ballSpeed": Float(values["SPEED_MPH"] ?? "0") ?? 0,
            "launchAngle": Float(values["ELEVATION_DEG"] ?? "0") ?? 0,
            "direction": Float(values["AZIMUTH_DEG"] ?? "0") ?? 0,
            "totalSpin": Float(values["SPIN_RPM"] ?? "0") ?? 0,
            "backSpin": Float(values["BACK_RPM"] ?? "0") ?? 0,
            "sideSpin": Float(values["SIDE_RPM"] ?? "0") ?? 0,
            "hasHMT": values["HMT"] == "1",
            "clubSpeed": Float(values["CLUBSPEED_MPH"] ?? "0") ?? 0,
            "path": Float(values["HPATH_DEG"] ?? "0") ?? 0,
            "attackAngle": Float(values["VPATH_DEG"] ?? "0") ?? 0,
            "faceToTarget": Float(values["FACE_T_DEG"] ?? "0") ?? 0,
            "dynamicLoft": Float(values["LOFT_DEG"] ?? "0") ?? 0
        ]
    }
    
    // Unity callbacks
    private func sendShot(_ data: [String: Any]) {
        guard let jsonData = try? JSONSerialization.data(withJSONObject: data),
              let json = String(data: jsonData, encoding: .utf8) else { return }
        
        UnitySendMessage(unityObject, "OnNativeShotReceived", json)
    }
    
    private func sendConnectionChanged(_ connected: Bool) {
        UnitySendMessage(unityObject, "OnNativeConnectionChanged", connected ? "true" : "false")
    }
    
    private func sendError(_ error: String) {
        UnitySendMessage(unityObject, "OnNativeError", error)
    }
}
```

---

## Quality Tier System

### QualityManager.cs

```csharp
// Core/QualityManager.cs
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OpenRange.Core
{
    public enum QualityTier
    {
        Low,
        Medium,
        High
    }
    
    public class QualityManager : MonoBehaviour
    {
        [SerializeField] private UniversalRenderPipelineAsset _lowQualityAsset;
        [SerializeField] private UniversalRenderPipelineAsset _mediumQualityAsset;
        [SerializeField] private UniversalRenderPipelineAsset _highQualityAsset;
        
        public QualityTier CurrentTier { get; private set; }
        
        private void Start()
        {
            // Auto-detect quality tier
            CurrentTier = DetectOptimalTier();
            ApplyQualityTier(CurrentTier);
        }
        
        public QualityTier DetectOptimalTier()
        {
#if UNITY_IOS
            // iPad Pro M1+ = High
            if (SystemInfo.processorType.Contains("Apple") && 
                SystemInfo.systemMemorySize >= 8000)
            {
                return QualityTier.High;
            }
            return QualityTier.Medium;
            
#elif UNITY_ANDROID
            // Check GPU tier
            if (SystemInfo.graphicsMemorySize >= 4000 &&
                SystemInfo.systemMemorySize >= 6000)
            {
                return QualityTier.High;
            }
            else if (SystemInfo.graphicsMemorySize >= 2000)
            {
                return QualityTier.Medium;
            }
            return QualityTier.Low;
            
#elif UNITY_STANDALONE_OSX
            // M1 = High, Intel with dedicated = High, Intel integrated = Medium
            if (SystemInfo.processorType.Contains("Apple"))
            {
                return QualityTier.High;
            }
            return SystemInfo.graphicsMemorySize >= 2000 ? 
                QualityTier.High : QualityTier.Medium;
#else
            return QualityTier.Medium;
#endif
        }
        
        public void ApplyQualityTier(QualityTier tier)
        {
            CurrentTier = tier;
            
            var asset = tier switch
            {
                QualityTier.Low => _lowQualityAsset,
                QualityTier.Medium => _mediumQualityAsset,
                QualityTier.High => _highQualityAsset,
                _ => _mediumQualityAsset
            };
            
            QualitySettings.renderPipeline = asset;
            
            // Additional settings per tier
            switch (tier)
            {
                case QualityTier.Low:
                    Application.targetFrameRate = 30;
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                    
                case QualityTier.Medium:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    break;
                    
                case QualityTier.High:
                    Application.targetFrameRate = 120;
                    QualitySettings.shadows = ShadowQuality.All;
                    break;
            }
        }
    }
}
```

---

## Build Configuration

### Platform Build Settings

```csharp
// Editor/BuildConfiguration.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

public static class BuildConfiguration
{
    [MenuItem("Build/macOS (Universal)")]
    public static void BuildMacOS()
    {
        var options = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = "Builds/macOS/OpenRange.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2); // Universal
        PlayerSettings.macOS.buildNumber = GetVersion();
        
        BuildPipeline.BuildPlayer(options);
    }
    
    [MenuItem("Build/iOS (iPad)")]
    public static void BuildiOS()
    {
        var options = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };
        
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
        PlayerSettings.iOS.targetOSVersionString = "16.0";
        PlayerSettings.iOS.buildNumber = GetVersion();
        
        BuildPipeline.BuildPlayer(options);
    }
    
    [MenuItem("Build/Android")]
    public static void BuildAndroid()
    {
        var options = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = "Builds/Android/OpenRange.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        
        BuildPipeline.BuildPlayer(options);
    }
    
    private static string[] GetScenes() => new[]
    {
        "Assets/Scenes/Bootstrap.unity",
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Ranges/Marina.unity"
    };
    
    private static string GetVersion() => "2.0.0";
}
#endif
```

---

## Performance Budgets

### Per-Platform Targets

| Metric | Mac (M1) | iPad Pro M1 | Android High | Android Mid |
|--------|----------|-------------|--------------|-------------|
| Target FPS | 120 | 60 | 60 | 30 |
| Draw Calls | < 100 | < 80 | < 80 | < 50 |
| Triangles | < 500K | < 300K | < 300K | < 100K |
| Texture Memory | 512MB | 256MB | 256MB | 128MB |
| Total Memory | 500MB | 300MB | 300MB | 200MB |
| App Size | 200MB | 150MB | 150MB | 100MB |

---

## Testing Matrix

| Platform | Device | Test Type |
|----------|--------|-----------|
| macOS | M1 MacBook Air | Performance, USB |
| macOS | Intel iMac | Performance, USB |
| iPad | iPad Pro M1 11" | Performance, USB, Battery |
| iPad | iPad Pro M2 12.9" | Performance, USB |
| Android | Samsung Tab S8+ | Performance, USB, Battery |
| Android | Pixel Tablet | Performance, USB |
| Android | Xiaomi Pad 6 | Performance, USB (budget test) |

---

## Future Considerations

### v2.1
- Windows support (native USB plugin)
- Additional range environments
- Shot replay and analysis

### v3.0
- VR support (Meta Quest, Vision Pro)
- Multiplayer/online modes
- Cloud sync and statistics
