// ABOUTME: macOS GC2 USB plugin using libusb for INTERRUPT IN endpoint communication.
// ABOUTME: Parses 0H (shot data) and 0M (device status) messages from the GC2 launch monitor.

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
// Protocol Constants
// =============================================================================

static const float kMinBallSpeedMph = 1.1f;      // Minimum putt speed
static const float kMaxBallSpeedMph = 250.0f;    // Maximum realistic speed
static const float kMisreadSpinValue = 2222.0f;  // Known GC2 error pattern
static const int kFlagsReady = 7;                // Device ready (green light)

// =============================================================================
// Internal State
// =============================================================================

static libusb_context *g_usbContext = NULL;
static libusb_device_handle *g_deviceHandle = NULL;
static dispatch_queue_t g_readQueue = NULL;
static BOOL g_isRunning = NO;
static NSMutableString *g_lineBuffer = nil;            // Buffer for incomplete lines
static NSMutableDictionary *g_shotAccumulator = nil;   // Accumulates shot fields across packets
static NSString *g_unityCallbackObject = nil;
static NSString *g_deviceSerial = nil;
static NSString *g_firmwareVersion = nil;
static int g_lastShotId = -1;                          // For duplicate detection
static NSString *g_currentMessageType = nil;           // Current message type (0H or 0M)

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

static void LogDebug(NSString *message) {
#ifdef DEBUG
    NSLog(@"[GC2MacPlugin DEBUG] %@", message);
#endif
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

#pragma mark - Protocol Parsing

/// Check if shot data represents a misread that should be rejected
static BOOL IsMisread(NSDictionary *shotData) {
    // Check for zero spin (camera couldn't track)
    NSNumber *backSpin = shotData[@"BACK_RPM"];
    NSNumber *sideSpin = shotData[@"SIDE_RPM"];
    if (backSpin && sideSpin) {
        if ([backSpin floatValue] == 0.0f && [sideSpin floatValue] == 0.0f) {
            LogInfo(@"Misread detected: zero spin");
            return YES;
        }
        // Check for 2222 error pattern
        if ([backSpin floatValue] == kMisreadSpinValue) {
            LogInfo(@"Misread detected: 2222 error pattern");
            return YES;
        }
    }

    // Check for unrealistic ball speed
    NSNumber *speed = shotData[@"SPEED_MPH"];
    if (speed) {
        float speedValue = [speed floatValue];
        if (speedValue < kMinBallSpeedMph || speedValue > kMaxBallSpeedMph) {
            LogInfo([NSString stringWithFormat:@"Misread detected: unrealistic speed %.1f mph", speedValue]);
            return YES;
        }
    }

    return NO;
}

/// Check if shot is a duplicate based on SHOT_ID
static BOOL IsDuplicate(int shotId) {
    if (shotId == g_lastShotId) {
        LogDebug([NSString stringWithFormat:@"Duplicate shot ID: %d", shotId]);
        return YES;
    }
    return NO;
}

/// Parse a single KEY=VALUE line and add to accumulator
static void ParseLine(NSString *line) {
    line = [line stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
    if (line.length == 0) {
        return;
    }

    // Check for message type markers
    if ([line isEqualToString:@"0H"]) {
        g_currentMessageType = @"0H";
        LogDebug(@"Message type: 0H (shot data)");
        return;
    }
    if ([line isEqualToString:@"0M"]) {
        g_currentMessageType = @"0M";
        LogDebug(@"Message type: 0M (device status)");
        return;
    }

    // Parse KEY=VALUE
    NSRange eqRange = [line rangeOfString:@"="];
    if (eqRange.location == NSNotFound) {
        return;
    }

    NSString *key = [[line substringToIndex:eqRange.location]
                     stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];
    NSString *value = [[line substringFromIndex:eqRange.location + 1]
                       stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];

    if (key.length == 0) {
        return;
    }

    // Convert to appropriate type
    id parsedValue;
    if ([key isEqualToString:@"SHOT_ID"] ||
        [key isEqualToString:@"TIME_SEC"] ||
        [key isEqualToString:@"MSEC_SINCE_CONTACT"] ||
        [key isEqualToString:@"FLAGS"] ||
        [key isEqualToString:@"BALLS"] ||
        [key isEqualToString:@"IS_LEFT"]) {
        parsedValue = @([value intValue]);
    } else if ([key isEqualToString:@"HMT"]) {
        parsedValue = @([value isEqualToString:@"1"]);
    } else if ([key isEqualToString:@"BALL1"]) {
        // Ball position is stored as string "x,y,z"
        parsedValue = value;
    } else {
        // Float values
        parsedValue = @([value floatValue]);
    }

    g_shotAccumulator[key] = parsedValue;
    LogDebug([NSString stringWithFormat:@"Parsed: %@ = %@", key, parsedValue]);
}

/// Build shot JSON from accumulated data
/// Field names must match GC2ShotData C# properties exactly for JsonUtility parsing
static NSDictionary* BuildShotJSON(void) {
    NSMutableDictionary *shot = [NSMutableDictionary dictionary];

    // Map GC2 field names to GC2ShotData property names (must match C# exactly)
    if (g_shotAccumulator[@"SHOT_ID"]) {
        shot[@"ShotId"] = g_shotAccumulator[@"SHOT_ID"];
    }
    // Add timestamp
    shot[@"Timestamp"] = @((long long)([[NSDate date] timeIntervalSince1970] * 1000));

    if (g_shotAccumulator[@"SPEED_MPH"]) {
        shot[@"BallSpeed"] = g_shotAccumulator[@"SPEED_MPH"];
    }
    if (g_shotAccumulator[@"ELEVATION_DEG"]) {
        shot[@"LaunchAngle"] = g_shotAccumulator[@"ELEVATION_DEG"];
    }
    if (g_shotAccumulator[@"AZIMUTH_DEG"]) {
        shot[@"Direction"] = g_shotAccumulator[@"AZIMUTH_DEG"];
    }
    if (g_shotAccumulator[@"BACK_RPM"]) {
        shot[@"BackSpin"] = g_shotAccumulator[@"BACK_RPM"];
    }
    if (g_shotAccumulator[@"SIDE_RPM"]) {
        shot[@"SideSpin"] = g_shotAccumulator[@"SIDE_RPM"];
    }
    if (g_shotAccumulator[@"SPIN_RPM"]) {
        shot[@"TotalSpin"] = g_shotAccumulator[@"SPIN_RPM"];
    }

    // Calculate spin axis from back/side spin if not provided
    if (g_shotAccumulator[@"BACK_RPM"] && g_shotAccumulator[@"SIDE_RPM"]) {
        float backSpin = [g_shotAccumulator[@"BACK_RPM"] floatValue];
        float sideSpin = [g_shotAccumulator[@"SIDE_RPM"] floatValue];
        if (backSpin != 0 || sideSpin != 0) {
            float spinAxis = atan2f(sideSpin, backSpin) * 180.0f / M_PI;
            shot[@"SpinAxis"] = @(spinAxis);
        }
    }

    // HMT data (club data)
    BOOL hasClubData = [g_shotAccumulator[@"HMT"] boolValue];
    shot[@"HasClubData"] = @(hasClubData);

    if (hasClubData) {
        if (g_shotAccumulator[@"CLUBSPEED_MPH"]) {
            shot[@"ClubSpeed"] = g_shotAccumulator[@"CLUBSPEED_MPH"];
        }
        if (g_shotAccumulator[@"HPATH_DEG"]) {
            shot[@"Path"] = g_shotAccumulator[@"HPATH_DEG"];
        }
        if (g_shotAccumulator[@"VPATH_DEG"]) {
            shot[@"AttackAngle"] = g_shotAccumulator[@"VPATH_DEG"];
        }
        if (g_shotAccumulator[@"FACE_T_DEG"]) {
            shot[@"FaceToTarget"] = g_shotAccumulator[@"FACE_T_DEG"];
        }
        if (g_shotAccumulator[@"LOFT_DEG"]) {
            shot[@"DynamicLoft"] = g_shotAccumulator[@"LOFT_DEG"];
        }
        if (g_shotAccumulator[@"LIE_DEG"]) {
            shot[@"Lie"] = g_shotAccumulator[@"LIE_DEG"];
        }
    }

    return shot;
}

/// Process completed 0H message (shot data)
static void ProcessShotMessage(void) {
    // Log all accumulated fields for diagnostics
    LogInfo(@"=== SHOT MESSAGE RECEIVED ===");
    LogInfo([NSString stringWithFormat:@"Accumulated fields (%lu):", (unsigned long)g_shotAccumulator.count]);
    for (NSString *key in [g_shotAccumulator.allKeys sortedArrayUsingSelector:@selector(compare:)]) {
        LogInfo([NSString stringWithFormat:@"  %@ = %@", key, g_shotAccumulator[key]]);
    }

    // Check if we have spin data (indicates complete shot)
    if (!g_shotAccumulator[@"BACK_RPM"] && !g_shotAccumulator[@"SIDE_RPM"]) {
        LogInfo(@"Shot message incomplete - waiting for spin data (BACK_RPM/SIDE_RPM not found)");
        return;
    }

    // Check for misreads
    if (IsMisread(g_shotAccumulator)) {
        LogInfo(@"Shot rejected: misread detected");
        [g_shotAccumulator removeAllObjects];
        return;
    }

    // Check for duplicates
    int shotId = [g_shotAccumulator[@"SHOT_ID"] intValue];
    if (IsDuplicate(shotId)) {
        LogDebug(@"Shot rejected: duplicate");
        [g_shotAccumulator removeAllObjects];
        return;
    }

    // Update last shot ID
    g_lastShotId = shotId;

    // Build and send shot JSON
    NSDictionary *shot = BuildShotJSON();

    // Log final JSON for diagnostics
    LogInfo(@"=== SENDING SHOT TO UNITY ===");
    for (NSString *key in [shot.allKeys sortedArrayUsingSelector:@selector(compare:)]) {
        LogInfo([NSString stringWithFormat:@"  %@ = %@", key, shot[key]]);
    }

    NotifyShot(shot);

    // Clear accumulator for next shot
    [g_shotAccumulator removeAllObjects];
}

/// Process completed 0M message (device status)
static void ProcessDeviceStatusMessage(void) {
    NSNumber *flags = g_shotAccumulator[@"FLAGS"];
    NSNumber *balls = g_shotAccumulator[@"BALLS"];

    if (!flags) {
        LogDebug(@"Device status message incomplete - no FLAGS");
        return;
    }

    BOOL isReady = ([flags intValue] == kFlagsReady);
    BOOL ballDetected = (balls && [balls intValue] > 0);

    NotifyDeviceStatus(isReady, ballDetected);

    // Clear 0M-specific fields but keep accumulator for potential 0H data
    [g_shotAccumulator removeObjectForKey:@"FLAGS"];
    [g_shotAccumulator removeObjectForKey:@"BALLS"];
    [g_shotAccumulator removeObjectForKey:@"BALL1"];
}

/// Process buffer for complete lines
static void ProcessBuffer(void) {
    while (YES) {
        // Look for newline
        NSRange newlineRange = [g_lineBuffer rangeOfString:@"\n"];
        if (newlineRange.location == NSNotFound) {
            break;
        }

        // Extract complete line
        NSString *line = [g_lineBuffer substringToIndex:newlineRange.location];
        [g_lineBuffer deleteCharactersInRange:NSMakeRange(0, newlineRange.location + 1)];

        // Check for message terminator (tab after newline indicates end of message)
        if ([g_lineBuffer hasPrefix:@"\t"]) {
            // Remove the tab
            [g_lineBuffer deleteCharactersInRange:NSMakeRange(0, 1)];

            // Parse the last line
            ParseLine(line);

            // Process the complete message
            if ([g_currentMessageType isEqualToString:@"0H"]) {
                ProcessShotMessage();
            } else if ([g_currentMessageType isEqualToString:@"0M"]) {
                ProcessDeviceStatusMessage();
            }

            g_currentMessageType = nil;
            continue;
        }

        // Parse this line
        ParseLine(line);

        // Check if SHOT_ID changed (indicates new shot)
        if (g_shotAccumulator[@"SHOT_ID"]) {
            int currentShotId = [g_shotAccumulator[@"SHOT_ID"] intValue];
            if (g_lastShotId >= 0 && currentShotId != g_lastShotId) {
                // New shot ID detected - process previous accumulated data if complete
                LogDebug([NSString stringWithFormat:@"New shot ID detected: %d (was %d)", currentShotId, g_lastShotId]);
            }
        }
    }
}

#pragma mark - USB Read Loop

/// USB read loop - runs on background queue
static void ReadLoop(void) {
    LogInfo(@"Read loop started");

    unsigned char buffer[GC2_BUFFER_SIZE];
    int transferred = 0;

    while (g_isRunning && g_deviceHandle) {
        // Read from INTERRUPT IN endpoint
        int result = libusb_interrupt_transfer(
            g_deviceHandle,
            GC2_EP_IN,
            buffer,
            GC2_BUFFER_SIZE,
            &transferred,
            GC2_READ_TIMEOUT_MS
        );

        if (result == 0 && transferred > 0) {
            // Data received - log raw packet for diagnostics
            NSMutableString *hexDump = [NSMutableString stringWithString:@"USB RAW ["];
            for (int i = 0; i < transferred; i++) {
                [hexDump appendFormat:@"%02X ", buffer[i]];
            }
            [hexDump appendString:@"]"];
            LogInfo(hexDump);

            // Data received - append to line buffer
            NSString *data = [[NSString alloc] initWithBytes:buffer
                                                      length:transferred
                                                    encoding:NSUTF8StringEncoding];
            if (data) {
                LogInfo([NSString stringWithFormat:@"USB STR: %@",
                    [data stringByReplacingOccurrencesOfString:@"\n" withString:@"\\n"]]);
                [g_lineBuffer appendString:data];
                ProcessBuffer();
            } else {
                LogDebug(@"Received non-UTF8 data, skipping");
            }
        } else if (result == LIBUSB_ERROR_TIMEOUT) {
            // Timeout is normal - continue reading
            continue;
        } else if (result == LIBUSB_ERROR_NO_DEVICE || result == LIBUSB_ERROR_IO) {
            // Device disconnected
            LogInfo([NSString stringWithFormat:@"USB read error: %s", libusb_error_name(result)]);
            break;
        } else if (result < 0) {
            // Other error
            LogError([NSString stringWithFormat:@"USB transfer error: %s", libusb_error_name(result)]);
            // Don't break on transient errors, continue trying
            [NSThread sleepForTimeInterval:0.01];
        }
    }

    LogInfo(@"Read loop exited");
    g_isRunning = NO;

    // Clean up state
    [g_lineBuffer setString:@""];
    [g_shotAccumulator removeAllObjects];
    g_currentMessageType = nil;

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
    g_lineBuffer = [[NSMutableString alloc] init];
    g_shotAccumulator = [[NSMutableDictionary alloc] init];
    g_readQueue = dispatch_queue_create("com.openrange.gc2.read", DISPATCH_QUEUE_SERIAL);
    g_lastIsReady = NO;
    g_lastBallDetected = NO;
    g_lastShotId = -1;
    g_currentMessageType = nil;

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
    g_lineBuffer = nil;
    g_shotAccumulator = nil;
    g_readQueue = nil;
    g_unityCallbackObject = nil;
    g_deviceSerial = nil;
    g_firmwareVersion = nil;
    g_currentMessageType = nil;

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

    // Read device serial number (descriptor index 3 typically contains serial)
    unsigned char serialBuffer[256];
    int len = libusb_get_string_descriptor_ascii(g_deviceHandle, 3, serialBuffer, sizeof(serialBuffer));
    if (len > 0) {
        g_deviceSerial = [NSString stringWithUTF8String:(char*)serialBuffer];
        LogInfo([NSString stringWithFormat:@"Device serial: %@", g_deviceSerial]);
    }

    // Try to get firmware version (descriptor index 2 might have product info)
    unsigned char versionBuffer[256];
    len = libusb_get_string_descriptor_ascii(g_deviceHandle, 2, versionBuffer, sizeof(versionBuffer));
    if (len > 0) {
        g_firmwareVersion = [NSString stringWithUTF8String:(char*)versionBuffer];
        LogInfo([NSString stringWithFormat:@"Device version: %@", g_firmwareVersion]);
    }

    // Start read loop
    g_isRunning = YES;
    [g_lineBuffer setString:@""];
    [g_shotAccumulator removeAllObjects];
    g_lastShotId = -1;
    g_currentMessageType = nil;

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

    // Give read loop time to exit
    [NSThread sleepForTimeInterval:0.2];

    if (g_deviceHandle) {
        libusb_release_interface(g_deviceHandle, 0);
        libusb_close(g_deviceHandle);
        g_deviceHandle = NULL;
    }

    g_deviceSerial = nil;
    g_firmwareVersion = nil;
    g_lastIsReady = NO;
    g_lastBallDetected = NO;
    g_lastShotId = -1;
    g_currentMessageType = nil;

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
