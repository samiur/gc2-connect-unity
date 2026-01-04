// ABOUTME: Android foreground service for Bridge Mode background operation.
// ABOUTME: Maintains USB connection and notification when app is backgrounded.

package com.openrange.gc2

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Binder
import android.os.Build
import android.os.IBinder
import android.os.PowerManager
import android.util.Log
import androidx.core.app.NotificationCompat

/**
 * Foreground service for Bridge Mode operation.
 *
 * This service keeps the app alive when backgrounded, maintaining the USB connection
 * to the GC2 and enabling shot relay to GSPro. Required for use cases like Moonlight
 * streaming where GSPro is streamed to the device while OpenRange runs in background.
 *
 * Features:
 * - Persistent notification with connection status and shot count
 * - Wake lock to prevent USB from sleeping
 * - Binder for activity communication
 */
class GC2BridgeService : Service() {

    companion object {
        private const val TAG = "GC2BridgeService"

        /** Notification channel ID for Android 8.0+ */
        const val NOTIFICATION_CHANNEL_ID = "gc2_bridge_channel"

        /** Notification ID for the foreground service */
        const val NOTIFICATION_ID = 1001

        /** Action to start the service */
        const val ACTION_START = "com.openrange.gc2.action.START_BRIDGE"

        /** Action to stop the service */
        const val ACTION_STOP = "com.openrange.gc2.action.STOP_BRIDGE"

        /** Action to update notification */
        const val ACTION_UPDATE_NOTIFICATION = "com.openrange.gc2.action.UPDATE_NOTIFICATION"

        /** Extra key for shots relayed count */
        const val EXTRA_SHOTS_RELAYED = "shots_relayed"

        /** Extra key for connection status */
        const val EXTRA_IS_CONNECTED = "is_connected"

        /** Extra key for GSPro connection status */
        const val EXTRA_GSPRO_CONNECTED = "gspro_connected"

        /** Extra key for whether to use connectedDevice service type (requires GC2 USB permission) */
        const val EXTRA_USE_CONNECTED_DEVICE = "use_connected_device"

        /** Wake lock tag */
        private const val WAKE_LOCK_TAG = "OpenRange::GC2BridgeWakeLock"

        @Volatile
        private var instance: GC2BridgeService? = null

        /**
         * Gets the running service instance, if any.
         */
        @JvmStatic
        fun getInstance(): GC2BridgeService? = instance

        /**
         * Checks if the service is currently running.
         */
        @JvmStatic
        fun isRunning(): Boolean = instance != null
    }

    /** Binder for activity communication */
    private val binder = BridgeBinder()

    /** Wake lock to keep USB alive */
    private var wakeLock: PowerManager.WakeLock? = null

    /** Current shots relayed count */
    private var shotsRelayed: Int = 0

    /** GC2 connection status */
    private var isGC2Connected: Boolean = false

    /** GSPro connection status */
    private var isGSProConnected: Boolean = false

    /** Callback for service events */
    private var callback: BridgeServiceCallback? = null

    /** Callback object name for Unity */
    private var unityCallbackObject: String? = null

    /**
     * Binder class for local binding.
     */
    inner class BridgeBinder : Binder() {
        /**
         * Gets the service instance.
         */
        fun getService(): GC2BridgeService = this@GC2BridgeService
    }

    /**
     * Callback interface for service events.
     */
    interface BridgeServiceCallback {
        fun onServiceStarted()
        fun onServiceStopped()
        fun onShotRelayed(shotCount: Int)
        fun onError(error: String)
    }

    override fun onCreate() {
        super.onCreate()
        Log.d(TAG, "Service onCreate")
        instance = this
        createNotificationChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.d(TAG, "onStartCommand: action=${intent?.action}")

        when (intent?.action) {
            ACTION_START -> startBridgeMode(intent)
            ACTION_STOP -> stopBridgeMode()
            ACTION_UPDATE_NOTIFICATION -> updateNotificationFromIntent(intent)
        }

        // Restart if killed
        return START_STICKY
    }

    override fun onBind(intent: Intent?): IBinder {
        Log.d(TAG, "Service onBind")
        return binder
    }

    override fun onDestroy() {
        Log.d(TAG, "Service onDestroy")
        releaseWakeLock()
        instance = null
        callback?.onServiceStopped()
        super.onDestroy()
    }

    /**
     * Sets the callback for service events.
     */
    fun setCallback(callback: BridgeServiceCallback?) {
        this.callback = callback
    }

    /**
     * Sets the Unity callback object name.
     */
    fun setUnityCallbackObject(objectName: String) {
        this.unityCallbackObject = objectName
    }

    /**
     * Updates the notification with current status.
     *
     * @param shotsRelayed Number of shots relayed
     * @param isGC2Connected Whether GC2 is connected
     * @param isGSProConnected Whether GSPro is connected
     */
    fun updateStatus(shotsRelayed: Int, isGC2Connected: Boolean, isGSProConnected: Boolean) {
        this.shotsRelayed = shotsRelayed
        this.isGC2Connected = isGC2Connected
        this.isGSProConnected = isGSProConnected
        updateNotification()
    }

    /**
     * Increments the shot count and updates notification.
     */
    fun incrementShotCount() {
        shotsRelayed++
        updateNotification()
        callback?.onShotRelayed(shotsRelayed)
        sendToUnity("OnBridgeShotRelayed", shotsRelayed.toString())
    }

    /**
     * Gets the current shots relayed count.
     */
    fun getShotsRelayed(): Int = shotsRelayed

    /**
     * Resets the shots relayed count.
     */
    fun resetShotCount() {
        shotsRelayed = 0
        updateNotification()
    }

    // -------------------------------------------------------------------------
    // Private methods
    // -------------------------------------------------------------------------

    private fun startBridgeMode(intent: Intent) {
        Log.i(TAG, "Starting Bridge Mode")

        // Get initial state from intent
        shotsRelayed = intent.getIntExtra(EXTRA_SHOTS_RELAYED, 0)
        isGC2Connected = intent.getBooleanExtra(EXTRA_IS_CONNECTED, false)
        isGSProConnected = intent.getBooleanExtra(EXTRA_GSPRO_CONNECTED, false)
        val useConnectedDevice = intent.getBooleanExtra(EXTRA_USE_CONNECTED_DEVICE, false)

        Log.d(TAG, "Bridge Mode: isGC2Connected=$isGC2Connected, useConnectedDevice=$useConnectedDevice")

        // Acquire wake lock
        acquireWakeLock()

        // Start as foreground service with notification
        val notification = buildNotification()

        // Android 14+ requires specifying foreground service type
        // Use connectedDevice when GC2 is connected (requires USB permission grant)
        // Use dataSync for GSPro testing without GC2
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.UPSIDE_DOWN_CAKE) {
            val serviceType = if (useConnectedDevice) {
                Log.d(TAG, "Using FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE")
                ServiceInfo.FOREGROUND_SERVICE_TYPE_CONNECTED_DEVICE
            } else {
                Log.d(TAG, "Using FOREGROUND_SERVICE_TYPE_DATA_SYNC")
                ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC
            }
            startForeground(NOTIFICATION_ID, notification, serviceType)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }

        Log.i(TAG, "Bridge Mode started")
        callback?.onServiceStarted()
        sendToUnity("OnBridgeServiceStarted", "")
    }

    private fun stopBridgeMode() {
        Log.i(TAG, "Stopping Bridge Mode")

        releaseWakeLock()
        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()

        Log.i(TAG, "Bridge Mode stopped")
        sendToUnity("OnBridgeServiceStopped", "")
    }

    private fun updateNotificationFromIntent(intent: Intent) {
        shotsRelayed = intent.getIntExtra(EXTRA_SHOTS_RELAYED, shotsRelayed)
        isGC2Connected = intent.getBooleanExtra(EXTRA_IS_CONNECTED, isGC2Connected)
        isGSProConnected = intent.getBooleanExtra(EXTRA_GSPRO_CONNECTED, isGSProConnected)
        updateNotification()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                NOTIFICATION_CHANNEL_ID,
                "Bridge Mode",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "Keeps GC2 connection alive for GSPro relay"
                setShowBadge(false)
            }

            val notificationManager = getSystemService(NotificationManager::class.java)
            notificationManager.createNotificationChannel(channel)
            Log.d(TAG, "Notification channel created")
        }
    }

    private fun buildNotification(): Notification {
        // Create intent to return to app
        val returnIntent = packageManager.getLaunchIntentForPackage(packageName)?.apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }

        val pendingIntentFlags = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        } else {
            PendingIntent.FLAG_UPDATE_CURRENT
        }

        val contentIntent = PendingIntent.getActivity(
            this,
            0,
            returnIntent,
            pendingIntentFlags
        )

        // Create stop action
        val stopIntent = Intent(this, GC2BridgeService::class.java).apply {
            action = ACTION_STOP
        }
        val stopPendingIntent = PendingIntent.getService(
            this,
            1,
            stopIntent,
            pendingIntentFlags
        )

        // Build status text
        val gc2Status = if (isGC2Connected) "GC2 Connected" else "GC2 Disconnected"
        val gsproStatus = if (isGSProConnected) "GSPro Connected" else "GSPro Disconnected"
        val contentText = "$gc2Status | $gsproStatus | Shots: $shotsRelayed"

        // Build title based on overall status
        val title = when {
            isGC2Connected && isGSProConnected -> "Bridge Mode Active"
            isGC2Connected -> "Waiting for GSPro..."
            else -> "Waiting for GC2..."
        }

        return NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(contentText)
            .setSmallIcon(android.R.drawable.ic_menu_share) // Use system icon, app can override
            .setOngoing(true)
            .setContentIntent(contentIntent)
            .addAction(
                android.R.drawable.ic_menu_close_clear_cancel,
                "Stop",
                stopPendingIntent
            )
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .build()
    }

    private fun updateNotification() {
        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.notify(NOTIFICATION_ID, buildNotification())
    }

    private fun acquireWakeLock() {
        if (wakeLock == null) {
            val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
            wakeLock = powerManager.newWakeLock(
                PowerManager.PARTIAL_WAKE_LOCK,
                WAKE_LOCK_TAG
            ).apply {
                // Acquire with timeout to prevent indefinite battery drain
                // 8 hours should be more than enough for any session
                acquire(8 * 60 * 60 * 1000L)
            }
            Log.d(TAG, "Wake lock acquired")
        }
    }

    private fun releaseWakeLock() {
        wakeLock?.let {
            if (it.isHeld) {
                it.release()
                Log.d(TAG, "Wake lock released")
            }
        }
        wakeLock = null
    }

    /**
     * Sends a message to Unity via UnitySendMessage.
     */
    private fun sendToUnity(methodName: String, message: String) {
        val gameObject = unityCallbackObject ?: return

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
