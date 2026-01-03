// ABOUTME: Stub implementation of the macOS GC2 USB plugin using libusb.
// ABOUTME: Returns false for device operations; actual USB code in Prompt 21.

#import "GC2MacPlugin.h"
#import "libusb.h"
#import <dispatch/dispatch.h>

// =============================================================================
// Forward Declaration for Unity's UnitySendMessage
// =============================================================================

// UnitySendMessage is provided by Unity at runtime. When building the plugin
// standalone, we declare it as a weak symbol that will be resolved when the
// plugin is loaded into Unity. If not running in Unity, the symbol will be NULL.
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg) __attribute__((weak));

// Wrapper to safely call UnitySendMessage only if available
static void SafeUnitySendMessage(const char* obj, const char* method, const char* msg) {
    if (UnitySendMessage != NULL) {
        UnitySendMessage(obj, method, msg);
    } else {
        // Not running in Unity - just log
        NSLog(@"[GC2MacPlugin] UnitySendMessage not available: %s.%s(%s)", obj, method, msg);
    }
}

// =============================================================================
// Internal State
// =============================================================================

static libusb_context *g_usbContext = NULL;
static libusb_device_handle *g_deviceHandle = NULL;
static dispatch_queue_t g_readQueue = NULL;
static BOOL g_isRunning = NO;
static NSMutableString *g_dataBuffer = nil;
static NSString *g_unityCallbackObject = nil;
static NSString *g_deviceSerial = nil;
static NSString *g_firmwareVersion = nil;

// Callbacks for non-Unity usage
static GC2ShotCallback g_shotCallback = NULL;
static GC2ConnectionCallback g_connectionCallback = NULL;
static GC2ErrorCallback g_errorCallback = NULL;
static GC2DeviceStatusCallback g_deviceStatusCallback = NULL;

// Last device status for deduplication
static BOOL g_lastIsReady = NO;
static BOOL g_lastBallDetected = NO;

#pragma mark - Internal Helpers

static void LogInfo(NSString *message) {
    NSLog(@"[GC2MacPlugin] %@", message);
}

static void LogError(NSString *message) {
    NSLog(@"[GC2MacPlugin ERROR] %@", message);
}

/// Send message to Unity via UnitySendMessage
static void SendToUnity(NSString *methodName, NSString *message) {
    if (g_unityCallbackObject && g_unityCallbackObject.length > 0) {
        // Must be called from main thread for Unity
        if ([NSThread isMainThread]) {
            SafeUnitySendMessage(
                [g_unityCallbackObject UTF8String],
                [methodName UTF8String],
                [message UTF8String]
            );
        } else {
            dispatch_async(dispatch_get_main_queue(), ^{
                SafeUnitySendMessage(
                    [g_unityCallbackObject UTF8String],
                    [methodName UTF8String],
                    [message UTF8String]
                );
            });
        }
    }
}

/// Notify listeners of a shot
static void NotifyShot(NSDictionary *shotData) {
    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:shotData
                                                       options:0
                                                         error:&error];
    if (jsonData) {
        NSString *json = [[NSString alloc] initWithData:jsonData
                                               encoding:NSUTF8StringEncoding];
        LogInfo([NSString stringWithFormat:@"Shot received: %@", json]);

        if (g_shotCallback) {
            dispatch_async(dispatch_get_main_queue(), ^{
                g_shotCallback([json UTF8String]);
            });
        }

        SendToUnity(@"OnNativeShotReceived", json);
    }
}

/// Notify listeners of connection state change
static void NotifyConnection(BOOL connected) {
    NSString *value = connected ? @"true" : @"false";
    LogInfo([NSString stringWithFormat:@"Connection changed: %@", value]);

    if (g_connectionCallback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            g_connectionCallback([value UTF8String]);
        });
    }

    SendToUnity(@"OnNativeConnectionChanged", value);
}

/// Notify listeners of device status change (0M messages)
static void NotifyDeviceStatus(BOOL isReady, BOOL ballDetected) {
    // Deduplicate status updates
    if (isReady == g_lastIsReady && ballDetected == g_lastBallDetected) {
        return;
    }
    g_lastIsReady = isReady;
    g_lastBallDetected = ballDetected;

    NSDictionary *status = @{
        @"isReady": @(isReady),
        @"ballDetected": @(ballDetected)
    };

    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:status
                                                       options:0
                                                         error:&error];
    if (jsonData) {
        NSString *json = [[NSString alloc] initWithData:jsonData
                                               encoding:NSUTF8StringEncoding];
        LogInfo([NSString stringWithFormat:@"Device status: %@", json]);

        if (g_deviceStatusCallback) {
            dispatch_async(dispatch_get_main_queue(), ^{
                g_deviceStatusCallback([json UTF8String]);
            });
        }

        SendToUnity(@"OnNativeDeviceStatus", json);
    }
}

/// Notify listeners of an error
static void NotifyError(NSString *errorMessage) {
    LogError(errorMessage);

    if (g_errorCallback) {
        dispatch_async(dispatch_get_main_queue(), ^{
            g_errorCallback([errorMessage UTF8String]);
        });
    }

    SendToUnity(@"OnNativeError", errorMessage);
}

#pragma mark - Protocol Parsing (Placeholder)

/// Parse GC2 protocol data into shot dictionary
/// This is a placeholder - full implementation in Prompt 21
static NSDictionary* ParseGC2ShotData(NSString *data) {
    // Placeholder: just log that we received data
    LogInfo([NSString stringWithFormat:@"Parsing data: %@", data]);

    // For stub, return nil (no valid shot)
    return nil;
}

/// Process accumulated buffer for complete messages
static void ProcessBuffer(void) {
    // Placeholder - will be implemented in Prompt 21
    // Looks for message terminators (\n\t) and processes complete messages
}

#pragma mark - USB Read Loop (Placeholder)

/// USB read loop - runs on background queue
/// This is a placeholder - full implementation in Prompt 21
static void ReadLoop(void) {
    LogInfo(@"Read loop started (stub - not actually reading)");

    // Placeholder: In actual implementation, this would:
    // 1. libusb_interrupt_transfer() in a loop
    // 2. Parse 0H (shot) and 0M (status) messages
    // 3. Accumulate data until complete message received
    // 4. Call NotifyShot() or NotifyDeviceStatus()

    // For stub, just wait until stopped
    while (g_isRunning && g_deviceHandle) {
        [NSThread sleepForTimeInterval:0.1];
    }

    LogInfo(@"Read loop exited");
    g_isRunning = NO;
    NotifyConnection(NO);
}

#pragma mark - Public API: Lifecycle

void GC2Mac_Initialize(const char* callbackObject) {
    LogInfo(@"Initializing GC2MacPlugin");

    if (callbackObject) {
        g_unityCallbackObject = [NSString stringWithUTF8String:callbackObject];
        LogInfo([NSString stringWithFormat:@"Unity callback object: %@", g_unityCallbackObject]);
    }

    // Initialize libusb
    int result = libusb_init(&g_usbContext);
    if (result < 0) {
        NotifyError([NSString stringWithFormat:@"Failed to initialize libusb: %s",
                     libusb_error_name(result)]);
        return;
    }

    // Initialize state
    g_dataBuffer = [[NSMutableString alloc] init];
    g_readQueue = dispatch_queue_create("com.openrange.gc2.read", DISPATCH_QUEUE_SERIAL);
    g_lastIsReady = NO;
    g_lastBallDetected = NO;

    LogInfo(@"GC2MacPlugin initialized successfully");
}

void GC2Mac_Shutdown(void) {
    LogInfo(@"Shutting down GC2MacPlugin");

    // Disconnect if connected
    GC2Mac_Disconnect();

    // Cleanup libusb
    if (g_usbContext) {
        libusb_exit(g_usbContext);
        g_usbContext = NULL;
    }

    // Cleanup state
    g_dataBuffer = nil;
    g_readQueue = nil;
    g_unityCallbackObject = nil;
    g_deviceSerial = nil;
    g_firmwareVersion = nil;

    LogInfo(@"GC2MacPlugin shutdown complete");
}

#pragma mark - Public API: Device Operations

bool GC2Mac_IsDeviceAvailable(void) {
    if (!g_usbContext) {
        LogInfo(@"IsDeviceAvailable: libusb not initialized");
        return false;
    }

    libusb_device **list = NULL;
    ssize_t count = libusb_get_device_list(g_usbContext, &list);
    bool found = false;

    for (ssize_t i = 0; i < count; i++) {
        struct libusb_device_descriptor desc;
        if (libusb_get_device_descriptor(list[i], &desc) == 0) {
            if (desc.idVendor == GC2_VENDOR_ID && desc.idProduct == GC2_PRODUCT_ID) {
                found = true;
                LogInfo([NSString stringWithFormat:@"GC2 device found: VID=%04X, PID=%04X",
                         desc.idVendor, desc.idProduct]);
                break;
            }
        }
    }

    if (list) {
        libusb_free_device_list(list, 1);
    }

    if (!found) {
        LogInfo(@"GC2 device not found");
    }

    return found;
}

bool GC2Mac_Connect(void) {
    if (!g_usbContext) {
        NotifyError(@"Cannot connect: libusb not initialized");
        return false;
    }

    if (g_deviceHandle) {
        LogInfo(@"Already connected");
        return true;
    }

    LogInfo(@"Attempting to connect to GC2...");

    // Open device
    g_deviceHandle = libusb_open_device_with_vid_pid(g_usbContext,
                                                      GC2_VENDOR_ID,
                                                      GC2_PRODUCT_ID);
    if (!g_deviceHandle) {
        NotifyError(@"GC2 device not found or cannot be opened");
        return false;
    }

    // Detach kernel driver if active (macOS typically doesn't need this)
    if (libusb_kernel_driver_active(g_deviceHandle, 0) == 1) {
        LogInfo(@"Detaching kernel driver");
        int result = libusb_detach_kernel_driver(g_deviceHandle, 0);
        if (result < 0) {
            LogInfo([NSString stringWithFormat:@"Note: Could not detach kernel driver: %s",
                     libusb_error_name(result)]);
            // Continue anyway - might not be necessary on macOS
        }
    }

    // Claim interface
    int result = libusb_claim_interface(g_deviceHandle, 0);
    if (result < 0) {
        NotifyError([NSString stringWithFormat:@"Failed to claim USB interface: %s",
                     libusb_error_name(result)]);
        libusb_close(g_deviceHandle);
        g_deviceHandle = NULL;
        return false;
    }

    // Read device serial number (placeholder - will be implemented fully in Prompt 21)
    unsigned char serialBuffer[256];
    // Try to get serial number descriptor (index usually 3)
    int len = libusb_get_string_descriptor_ascii(g_deviceHandle, 3, serialBuffer, sizeof(serialBuffer));
    if (len > 0) {
        g_deviceSerial = [NSString stringWithUTF8String:(char*)serialBuffer];
        LogInfo([NSString stringWithFormat:@"Device serial: %@", g_deviceSerial]);
    }

    // Start read loop
    g_isRunning = YES;
    [g_dataBuffer setString:@""];

    dispatch_async(g_readQueue, ^{
        ReadLoop();
    });

    NotifyConnection(YES);
    LogInfo(@"GC2 connected successfully");

    return true;
}

void GC2Mac_Disconnect(void) {
    LogInfo(@"Disconnecting from GC2");

    // Stop read loop
    g_isRunning = NO;

    if (g_deviceHandle) {
        libusb_release_interface(g_deviceHandle, 0);
        libusb_close(g_deviceHandle);
        g_deviceHandle = NULL;
    }

    g_deviceSerial = nil;
    g_firmwareVersion = nil;
    g_lastIsReady = NO;
    g_lastBallDetected = NO;

    NotifyConnection(NO);
    LogInfo(@"GC2 disconnected");
}

bool GC2Mac_IsConnected(void) {
    return g_deviceHandle != NULL && g_isRunning;
}

#pragma mark - Public API: Device Information

const char* GC2Mac_GetDeviceSerial(void) {
    if (g_deviceSerial) {
        return [g_deviceSerial UTF8String];
    }
    return NULL;
}

const char* GC2Mac_GetFirmwareVersion(void) {
    if (g_firmwareVersion) {
        return [g_firmwareVersion UTF8String];
    }
    return NULL;
}

#pragma mark - Public API: Callback Registration

void GC2Mac_SetShotCallback(GC2ShotCallback callback) {
    g_shotCallback = callback;
    LogInfo(@"Shot callback registered");
}

void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback) {
    g_connectionCallback = callback;
    LogInfo(@"Connection callback registered");
}

void GC2Mac_SetErrorCallback(GC2ErrorCallback callback) {
    g_errorCallback = callback;
    LogInfo(@"Error callback registered");
}

void GC2Mac_SetDeviceStatusCallback(GC2DeviceStatusCallback callback) {
    g_deviceStatusCallback = callback;
    LogInfo(@"Device status callback registered");
}
