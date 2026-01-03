// ABOUTME: C interface header for the macOS GC2 USB plugin using libusb.
// ABOUTME: Defines callback types and public API for Unity integration.

#ifndef GC2MacPlugin_h
#define GC2MacPlugin_h

#import <Foundation/Foundation.h>

#ifdef __cplusplus
extern "C" {
#endif

// =============================================================================
// Callback Function Types
// =============================================================================

/// Called when a shot is received from the GC2.
/// @param jsonData JSON string containing GC2ShotData fields
typedef void (*GC2ShotCallback)(const char* jsonData);

/// Called when connection state changes.
/// @param connected "true" or "false" string
typedef void (*GC2ConnectionCallback)(const char* connected);

/// Called when an error occurs.
/// @param error Error message string
typedef void (*GC2ErrorCallback)(const char* error);

/// Called when device status changes (0M messages).
/// @param jsonData JSON string with {"isReady": bool, "ballDetected": bool}
typedef void (*GC2DeviceStatusCallback)(const char* jsonData);

// =============================================================================
// Plugin Lifecycle
// =============================================================================

/// Initialize the plugin and libusb context.
/// @param callbackObject Name of the Unity GameObject to receive UnitySendMessage calls
void GC2Mac_Initialize(const char* callbackObject);

/// Shutdown the plugin and cleanup resources.
void GC2Mac_Shutdown(void);

// =============================================================================
// Device Operations
// =============================================================================

/// Check if a GC2 device is connected to USB.
/// @return true if device is found, false otherwise
bool GC2Mac_IsDeviceAvailable(void);

/// Attempt to connect to the GC2 device.
/// @return true if connection succeeded, false otherwise
bool GC2Mac_Connect(void);

/// Disconnect from the GC2 device.
void GC2Mac_Disconnect(void);

/// Check if currently connected to a GC2 device.
/// @return true if connected and read loop is running
bool GC2Mac_IsConnected(void);

// =============================================================================
// Device Information
// =============================================================================

/// Get the serial number of the connected device.
/// @return Serial number string or NULL if not connected
const char* GC2Mac_GetDeviceSerial(void);

/// Get the firmware version of the connected device.
/// @return Firmware version string or NULL if not connected
const char* GC2Mac_GetFirmwareVersion(void);

// =============================================================================
// Callback Registration (for non-Unity usage)
// =============================================================================

/// Set the callback for shot data.
/// @param callback Function to call when shot is received
void GC2Mac_SetShotCallback(GC2ShotCallback callback);

/// Set the callback for connection state changes.
/// @param callback Function to call when connection state changes
void GC2Mac_SetConnectionCallback(GC2ConnectionCallback callback);

/// Set the callback for errors.
/// @param callback Function to call when error occurs
void GC2Mac_SetErrorCallback(GC2ErrorCallback callback);

/// Set the callback for device status (0M messages).
/// @param callback Function to call when device status changes
void GC2Mac_SetDeviceStatusCallback(GC2DeviceStatusCallback callback);

// =============================================================================
// Constants
// =============================================================================

/// GC2 USB Vendor ID
#define GC2_VENDOR_ID  0x2C79

/// GC2 USB Product ID
#define GC2_PRODUCT_ID 0x0110

/// INTERRUPT IN endpoint for reading data
#define GC2_EP_IN 0x82

/// USB transfer buffer size (64-byte packets)
#define GC2_BUFFER_SIZE 64

/// Read timeout in milliseconds
#define GC2_READ_TIMEOUT_MS 100

#ifdef __cplusplus
}
#endif

#endif /* GC2MacPlugin_h */
