// ABOUTME: Main entry point for the GC2 Android Plugin.
// ABOUTME: Provides singleton access to GC2 USB communication from Unity.

package com.openrange.gc2

import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbManager
import android.os.Build
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

        /** Action for USB permission intent */
        private const val ACTION_USB_PERMISSION = "com.openrange.gc2.USB_PERMISSION"

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

    private var appContext: Context? = null
    private var callbackGameObject: String? = null
    private var gc2Device: GC2Device? = null
    private var isInitialized = false
    private var pendingConnectionContext: Context? = null
    private var receiversRegistered = false

    // -------------------------------------------------------------------------
    // BroadcastReceivers for USB events
    // -------------------------------------------------------------------------

    /**
     * BroadcastReceiver for USB permission responses.
     * Handles the result of UsbManager.requestPermission().
     */
    private val usbPermissionReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            if (intent.action == ACTION_USB_PERMISSION) {
                synchronized(this@GC2Plugin) {
                    val device = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        intent.getParcelableExtra(UsbManager.EXTRA_DEVICE, UsbDevice::class.java)
                    } else {
                        @Suppress("DEPRECATION")
                        intent.getParcelableExtra(UsbManager.EXTRA_DEVICE)
                    }

                    if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                        Log.i(TAG, "USB permission granted for device: ${device?.deviceName}")
                        device?.let { openDevice(it) }
                    } else {
                        Log.w(TAG, "USB permission denied for device: ${device?.deviceName}")
                        sendError("USB permission denied by user")
                    }

                    pendingConnectionContext = null
                }
            }
        }
    }

    /**
     * BroadcastReceiver for USB device attach events.
     * Auto-connects when a GC2 device is attached.
     */
    private val usbAttachReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            if (intent.action == UsbManager.ACTION_USB_DEVICE_ATTACHED) {
                val device = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    intent.getParcelableExtra(UsbManager.EXTRA_DEVICE, UsbDevice::class.java)
                } else {
                    @Suppress("DEPRECATION")
                    intent.getParcelableExtra(UsbManager.EXTRA_DEVICE)
                }

                if (device != null && isGC2Device(device)) {
                    Log.i(TAG, "GC2 device attached: ${device.deviceName}")

                    // Auto-connect if initialized
                    if (isInitialized) {
                        appContext?.let { ctx ->
                            connect(ctx)
                        }
                    }
                }
            }
        }
    }

    /**
     * BroadcastReceiver for USB device detach events.
     * Disconnects and notifies Unity when the device is removed.
     */
    private val usbDetachReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            if (intent.action == UsbManager.ACTION_USB_DEVICE_DETACHED) {
                val device = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    intent.getParcelableExtra(UsbManager.EXTRA_DEVICE, UsbDevice::class.java)
                } else {
                    @Suppress("DEPRECATION")
                    intent.getParcelableExtra(UsbManager.EXTRA_DEVICE)
                }

                if (device != null && isGC2Device(device)) {
                    Log.i(TAG, "GC2 device detached: ${device.deviceName}")
                    disconnect()
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /**
     * Initializes the plugin with the Unity activity context.
     *
     * @param context The Unity activity context (UnityPlayer.currentActivity)
     * @param gameObjectName The name of the Unity GameObject to receive callbacks
     */
    fun initialize(context: Context, gameObjectName: String) {
        Log.d(TAG, "Initializing GC2Plugin with callback object: $gameObjectName")

        this.appContext = context.applicationContext
        this.callbackGameObject = gameObjectName
        this.isInitialized = true

        // Register broadcast receivers
        registerReceivers(context)

        Log.i(TAG, "GC2Plugin initialized successfully")
    }

    /**
     * Shuts down the plugin and releases resources.
     */
    fun shutdown() {
        Log.d(TAG, "Shutting down GC2Plugin")

        disconnect()
        unregisterReceivers()
        appContext = null
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
        val ctx = appContext ?: run {
            Log.w(TAG, "isDeviceAvailable called before initialization")
            return false
        }

        val usbManager = ctx.getSystemService(Context.USB_SERVICE) as? UsbManager
        if (usbManager == null) {
            Log.e(TAG, "USB Manager not available")
            return false
        }

        val device = findGC2Device(usbManager)
        return device != null
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

        // Already connected?
        if (gc2Device?.isConnected == true) {
            Log.d(TAG, "Already connected to GC2 device")
            return true
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

        Log.d(TAG, "Found GC2 device: ${device.deviceName}")

        // Check if we already have permission
        if (usbManager.hasPermission(device)) {
            Log.d(TAG, "Already have USB permission, opening device")
            openDevice(device)
            return true
        }

        // Request permission - result will be handled by usbPermissionReceiver
        Log.d(TAG, "Requesting USB permission")
        pendingConnectionContext = context

        val permissionIntent = PendingIntent.getBroadcast(
            context,
            0,
            Intent(ACTION_USB_PERMISSION),
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                PendingIntent.FLAG_MUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
            } else {
                PendingIntent.FLAG_UPDATE_CURRENT
            }
        )

        usbManager.requestPermission(device, permissionIntent)
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
        return gc2Device?.getSerialNumber()
    }

    /**
     * Gets the firmware version of the connected GC2 device.
     *
     * Note: GC2 firmware version is not exposed via USB descriptors.
     * This would require a protocol-level query which is not yet implemented.
     *
     * @return Firmware version string, or null if not available
     */
    fun getFirmwareVersion(): String? {
        // GC2 firmware version is not available via USB descriptors.
        // Would need protocol-level query to retrieve this.
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

    /**
     * Registers BroadcastReceivers for USB events.
     */
    private fun registerReceivers(context: Context) {
        if (receiversRegistered) {
            Log.d(TAG, "Receivers already registered")
            return
        }

        val ctx = context.applicationContext

        // Register permission receiver
        val permissionFilter = IntentFilter(ACTION_USB_PERMISSION)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ctx.registerReceiver(usbPermissionReceiver, permissionFilter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            @Suppress("UnspecifiedRegisterReceiverFlag")
            ctx.registerReceiver(usbPermissionReceiver, permissionFilter)
        }

        // Register attach receiver
        val attachFilter = IntentFilter(UsbManager.ACTION_USB_DEVICE_ATTACHED)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ctx.registerReceiver(usbAttachReceiver, attachFilter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            @Suppress("UnspecifiedRegisterReceiverFlag")
            ctx.registerReceiver(usbAttachReceiver, attachFilter)
        }

        // Register detach receiver
        val detachFilter = IntentFilter(UsbManager.ACTION_USB_DEVICE_DETACHED)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ctx.registerReceiver(usbDetachReceiver, detachFilter, Context.RECEIVER_NOT_EXPORTED)
        } else {
            @Suppress("UnspecifiedRegisterReceiverFlag")
            ctx.registerReceiver(usbDetachReceiver, detachFilter)
        }

        receiversRegistered = true
        Log.d(TAG, "USB BroadcastReceivers registered")
    }

    /**
     * Unregisters BroadcastReceivers for USB events.
     */
    private fun unregisterReceivers() {
        if (!receiversRegistered) {
            return
        }

        appContext?.let { ctx ->
            try {
                ctx.unregisterReceiver(usbPermissionReceiver)
                ctx.unregisterReceiver(usbAttachReceiver)
                ctx.unregisterReceiver(usbDetachReceiver)
                Log.d(TAG, "USB BroadcastReceivers unregistered")
            } catch (e: Exception) {
                Log.w(TAG, "Error unregistering receivers: ${e.message}")
            }
        }

        receiversRegistered = false
    }

    /**
     * Checks if the given USB device is a GC2 launch monitor.
     */
    private fun isGC2Device(device: UsbDevice): Boolean {
        return device.vendorId == GC2_VENDOR_ID && device.productId == GC2_PRODUCT_ID
    }

    /**
     * Opens a connection to the GC2 device after permission is granted.
     */
    private fun openDevice(device: UsbDevice) {
        val ctx = appContext ?: run {
            Log.e(TAG, "Cannot open device: context is null")
            sendError("Internal error: context is null")
            return
        }

        val usbManager = ctx.getSystemService(Context.USB_SERVICE) as? UsbManager
        if (usbManager == null) {
            Log.e(TAG, "Cannot open device: USB Manager not available")
            sendError("USB Manager not available")
            return
        }

        // Close any existing connection
        gc2Device?.close()
        gc2Device = null

        // Open the USB device connection
        val connection = usbManager.openDevice(device)
        if (connection == null) {
            Log.e(TAG, "Failed to open USB device connection")
            sendError("Failed to open USB device")
            return
        }

        // Create and open our device wrapper
        val gc2 = GC2Device(device, connection, this)
        if (!gc2.open()) {
            Log.e(TAG, "Failed to initialize GC2 device")
            connection.close()
            sendError("Failed to initialize GC2 device")
            return
        }

        gc2Device = gc2
        Log.i(TAG, "GC2 device connected successfully")
        sendConnectionChanged(true)
    }
}
