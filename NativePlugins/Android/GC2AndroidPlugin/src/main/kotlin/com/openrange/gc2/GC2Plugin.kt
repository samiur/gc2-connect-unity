// ABOUTME: Main entry point for the GC2 Android Plugin.
// ABOUTME: Provides singleton access to GC2 USB communication from Unity.

package com.openrange.gc2

import android.content.Context
import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbManager
import android.util.Log

/**
 * Main plugin class for GC2 USB communication on Android.
 *
 * This class provides the interface between Unity and the Android USB Host API.
 * It handles device discovery, permission requests, and data communication
 * with the Foresight GC2 launch monitor.
 *
 * Usage from Unity (via AndroidJavaObject):
 * ```csharp
 * var pluginClass = new AndroidJavaClass("com.openrange.gc2.GC2Plugin");
 * var plugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
 * plugin.Call("initialize", unityActivity, gameObjectName);
 * ```
 */
class GC2Plugin private constructor() {

    companion object {
        private const val TAG = "GC2Plugin"

        /** GC2 USB Vendor ID (Foresight Sports) */
        const val GC2_VENDOR_ID = 0x2C79  // 11385

        /** GC2 USB Product ID */
        const val GC2_PRODUCT_ID = 0x0110  // 272

        @Volatile
        private var instance: GC2Plugin? = null

        /**
         * Gets the singleton instance of the GC2 plugin.
         * Thread-safe double-checked locking pattern.
         */
        @JvmStatic
        fun getInstance(): GC2Plugin {
            return instance ?: synchronized(this) {
                instance ?: GC2Plugin().also { instance = it }
            }
        }
    }

    private var context: Context? = null
    private var callbackGameObject: String? = null
    private var gc2Device: GC2Device? = null
    private var isInitialized = false

    /**
     * Initializes the plugin with the Unity activity context.
     *
     * @param context The Unity activity context (UnityPlayer.currentActivity)
     * @param gameObjectName The name of the Unity GameObject to receive callbacks
     */
    fun initialize(context: Context, gameObjectName: String) {
        Log.d(TAG, "Initializing GC2Plugin with callback object: $gameObjectName")

        this.context = context.applicationContext
        this.callbackGameObject = gameObjectName
        this.isInitialized = true

        Log.i(TAG, "GC2Plugin initialized successfully")
    }

    /**
     * Shuts down the plugin and releases resources.
     */
    fun shutdown() {
        Log.d(TAG, "Shutting down GC2Plugin")

        disconnect()
        context = null
        callbackGameObject = null
        isInitialized = false

        Log.i(TAG, "GC2Plugin shutdown complete")
    }

    /**
     * Checks if a GC2 device is available (connected via USB).
     *
     * @return true if a GC2 device is connected, false otherwise
     */
    fun isDeviceAvailable(): Boolean {
        val ctx = context ?: run {
            Log.w(TAG, "isDeviceAvailable called before initialization")
            return false
        }

        val usbManager = ctx.getSystemService(Context.USB_SERVICE) as? UsbManager
        if (usbManager == null) {
            Log.e(TAG, "USB Manager not available")
            return false
        }

        val gc2Device = findGC2Device(usbManager)
        return gc2Device != null
    }

    /**
     * Attempts to connect to the GC2 device.
     *
     * This will trigger a permission request dialog if the user hasn't
     * already granted permission for this device.
     *
     * @param context The activity context for permission requests
     * @return true if connection initiated successfully, false otherwise
     */
    fun connect(context: Context): Boolean {
        Log.d(TAG, "Attempting to connect to GC2 device")

        if (!isInitialized) {
            Log.e(TAG, "Plugin not initialized. Call initialize() first.")
            sendError("Plugin not initialized")
            return false
        }

        val usbManager = context.getSystemService(Context.USB_SERVICE) as? UsbManager
        if (usbManager == null) {
            Log.e(TAG, "USB Manager not available")
            sendError("USB Manager not available")
            return false
        }

        val device = findGC2Device(usbManager)
        if (device == null) {
            Log.w(TAG, "GC2 device not found")
            sendError("GC2 device not connected")
            return false
        }

        // TODO: Implement permission request and connection in Prompt 24
        Log.d(TAG, "Found GC2 device: ${device.deviceName}")

        // Stub: Would create GC2Device and start reading
        sendConnectionChanged(true)
        return true
    }

    /**
     * Disconnects from the GC2 device.
     */
    fun disconnect() {
        Log.d(TAG, "Disconnecting from GC2 device")

        gc2Device?.close()
        gc2Device = null

        sendConnectionChanged(false)
    }

    /**
     * Checks if currently connected to a GC2 device.
     *
     * @return true if connected and reading data, false otherwise
     */
    fun isConnected(): Boolean {
        return gc2Device?.isConnected == true
    }

    /**
     * Gets the serial number of the connected GC2 device.
     *
     * @return Serial number string, or null if not connected
     */
    fun getDeviceSerial(): String? {
        // TODO: Implement in Prompt 24
        return null
    }

    /**
     * Gets the firmware version of the connected GC2 device.
     *
     * @return Firmware version string, or null if not connected
     */
    fun getFirmwareVersion(): String? {
        // TODO: Implement in Prompt 24
        return null
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /**
     * Finds the GC2 device among connected USB devices.
     */
    private fun findGC2Device(usbManager: UsbManager): UsbDevice? {
        for ((_, device) in usbManager.deviceList) {
            if (device.vendorId == GC2_VENDOR_ID && device.productId == GC2_PRODUCT_ID) {
                Log.d(TAG, "Found GC2 device: VID=${device.vendorId}, PID=${device.productId}")
                return device
            }
        }
        return null
    }

    /**
     * Sends a shot data callback to Unity.
     */
    internal fun sendShotData(jsonData: String) {
        sendToUnity("OnNativeShotReceived", jsonData)
    }

    /**
     * Sends a connection state change callback to Unity.
     */
    private fun sendConnectionChanged(connected: Boolean) {
        sendToUnity("OnNativeConnectionChanged", if (connected) "true" else "false")
    }

    /**
     * Sends a device status callback to Unity.
     */
    internal fun sendDeviceStatus(jsonData: String) {
        sendToUnity("OnNativeDeviceStatus", jsonData)
    }

    /**
     * Sends an error callback to Unity.
     */
    private fun sendError(error: String) {
        sendToUnity("OnNativeError", error)
    }

    /**
     * Sends a message to Unity via UnitySendMessage.
     *
     * Note: This uses reflection to call UnityPlayer.UnitySendMessage
     * since we don't have a direct dependency on Unity classes.
     */
    private fun sendToUnity(methodName: String, message: String) {
        val gameObject = callbackGameObject ?: run {
            Log.w(TAG, "No callback GameObject set, cannot send message: $methodName")
            return
        }

        try {
            val unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer")
            val sendMessageMethod = unityPlayerClass.getMethod(
                "UnitySendMessage",
                String::class.java,
                String::class.java,
                String::class.java
            )
            sendMessageMethod.invoke(null, gameObject, methodName, message)
            Log.d(TAG, "Sent to Unity: $methodName($message)")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send message to Unity: ${e.message}")
        }
    }
}
