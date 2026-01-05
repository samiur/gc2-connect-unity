// ABOUTME: Android foreground service for Bridge Mode background operation.
// ABOUTME: Maintains USB connection, GSPro relay, and test shots when app is backgrounded.

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
import android.os.Handler
import android.os.IBinder
import android.os.Looper
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

        /** Action when app goes to background */
        const val ACTION_APP_BACKGROUNDED = "com.openrange.gc2.action.APP_BACKGROUNDED"

        /** Action when app returns to foreground */
        const val ACTION_APP_RESUMED = "com.openrange.gc2.action.APP_RESUMED"

        /** Action to send a shot to GSPro */
        const val ACTION_SEND_SHOT = "com.openrange.gc2.action.SEND_SHOT"

        /** Action to connect to GSPro */
        const val ACTION_CONNECT_GSPRO = "com.openrange.gc2.action.CONNECT_GSPRO"

        /** Action to disconnect from GSPro */
        const val ACTION_DISCONNECT_GSPRO = "com.openrange.gc2.action.DISCONNECT_GSPRO"

        // Shot data extras
        const val EXTRA_BALL_SPEED = "ball_speed"
        const val EXTRA_LAUNCH_ANGLE = "launch_angle"
        const val EXTRA_DIRECTION = "direction"
        const val EXTRA_TOTAL_SPIN = "total_spin"
        const val EXTRA_BACK_SPIN = "back_spin"
        const val EXTRA_SIDE_SPIN = "side_spin"
        const val EXTRA_SPIN_AXIS = "spin_axis"

        /** Extra key for shots relayed count */
        const val EXTRA_SHOTS_RELAYED = "shots_relayed"

        /** Extra key for connection status */
        const val EXTRA_IS_CONNECTED = "is_connected"

        /** Extra key for GSPro connection status */
        const val EXTRA_GSPRO_CONNECTED = "gspro_connected"

        /** Extra key for whether to use connectedDevice service type (requires GC2 USB permission) */
        const val EXTRA_USE_CONNECTED_DEVICE = "use_connected_device"

        /** Extra key for GSPro host address */
        const val EXTRA_GSPRO_HOST = "gspro_host"

        /** Extra key for GSPro port */
        const val EXTRA_GSPRO_PORT = "gspro_port"

        /** Extra key for test shot mode */
        const val EXTRA_TEST_SHOT_MODE = "test_shot_mode"

        /** Wake lock tag */
        private const val WAKE_LOCK_TAG = "OpenRange::GC2BridgeWakeLock"

        /** Test shot interval in milliseconds */
        private const val TEST_SHOT_INTERVAL_MS = 15000L

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

    /** Native GSPro client for background operation */
    private var gsProClient: GSProClient? = null

    /** Handler for test shot timer */
    private val mainHandler = Handler(Looper.getMainLooper())

    /** Test shot mode enabled */
    private var testShotModeEnabled: Boolean = false

    /** Whether app is currently in the background */
    private var isAppBackgrounded: Boolean = false

    /** Test shot runnable */
    private var testShotRunnable: Runnable? = null

    /** Current test shot index (cycles through presets) */
    private var testShotIndex: Int = 0

    /** Test shot presets: [ballSpeed, launchAngle, backSpin] */
    private val testShotPresets = arrayOf(
        floatArrayOf(167f, 10.9f, 2686f),  // Driver
        floatArrayOf(120f, 16.3f, 7097f),  // 7-Iron
        floatArrayOf(102f, 24.2f, 9304f)   // PW
    )

    private val testShotNames = arrayOf("Driver", "7-Iron", "PW")

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
            ACTION_APP_BACKGROUNDED -> onAppBackgrounded(intent)
            ACTION_APP_RESUMED -> onAppResumed()
            ACTION_SEND_SHOT -> sendShotFromIntent(intent)
            ACTION_CONNECT_GSPRO -> connectToGSProFromIntent(intent)
            ACTION_DISCONNECT_GSPRO -> disconnectFromGSProAction()
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

    /** Saved GSPro host for deferred connection */
    private var pendingGSProHost: String = ""

    /** Saved GSPro port for deferred connection */
    private var pendingGSProPort: Int = GSProClient.DEFAULT_PORT

    private fun startBridgeMode(intent: Intent) {
        Log.i(TAG, "Starting Bridge Mode")

        // Get initial state from intent
        shotsRelayed = intent.getIntExtra(EXTRA_SHOTS_RELAYED, 0)
        isGC2Connected = intent.getBooleanExtra(EXTRA_IS_CONNECTED, false)
        isGSProConnected = intent.getBooleanExtra(EXTRA_GSPRO_CONNECTED, false)
        val useConnectedDevice = intent.getBooleanExtra(EXTRA_USE_CONNECTED_DEVICE, false)

        // Get GSPro connection info - save for later when app is backgrounded
        pendingGSProHost = intent.getStringExtra(EXTRA_GSPRO_HOST) ?: "localhost"
        pendingGSProPort = intent.getIntExtra(EXTRA_GSPRO_PORT, GSProClient.DEFAULT_PORT)
        testShotModeEnabled = intent.getBooleanExtra(EXTRA_TEST_SHOT_MODE, false)

        Log.d(TAG, "Bridge Mode: isGC2Connected=$isGC2Connected, useConnectedDevice=$useConnectedDevice")
        Log.d(TAG, "GSPro: host=$pendingGSProHost, port=$pendingGSProPort, testShotMode=$testShotModeEnabled")

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

        // Connect to GSPro immediately - native service handles ALL GSPro communication on Android.
        // Unity does NOT connect to GSPro when Bridge Mode is enabled (prevents dual connection issues).
        if (pendingGSProHost.isNotEmpty()) {
            Log.d(TAG, "Connecting to GSPro immediately (native service handles all GSPro on Android)")
            connectToGSPro(pendingGSProHost, pendingGSProPort)
        } else {
            Log.w(TAG, "No GSPro host configured")
        }

        Log.i(TAG, "Bridge Mode started")
        callback?.onServiceStarted()
        sendToUnity("OnBridgeServiceStarted", "")
    }

    private fun stopBridgeMode() {
        Log.i(TAG, "Stopping Bridge Mode")

        // Stop test shots and disconnect GSPro
        stopTestShots()
        disconnectFromGSPro()

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

    // -------------------------------------------------------------------------
    // GSPro connection methods
    // -------------------------------------------------------------------------

    private fun connectToGSPro(host: String, port: Int) {
        Log.i(TAG, "Attempting to connect native GSPro client to $host:$port")

        gsProClient = GSProClient()
        gsProClient?.connect(host, port) { success ->
            if (success) {
                Log.i(TAG, "Native GSPro client connected successfully to $host:$port")
                isGSProConnected = true
                updateNotification()
                sendToUnity("OnBridgeGSProConnectionChanged", "true")

                // Note: Test shots are only started when app goes to background
                // via onAppBackgrounded(), not when GSPro first connects.
                // This ensures shots are only sent when the app is actually backgrounded.

                // If app is already backgrounded (race condition), start test shots now
                if (isAppBackgrounded && testShotModeEnabled && !isGC2Connected) {
                    Log.i(TAG, "App already backgrounded, starting test shots now")
                    startTestShots()
                }
            } else {
                Log.e(TAG, "Native GSPro client FAILED to connect to $host:$port")
                isGSProConnected = false
                updateNotification()
                sendToUnity("OnBridgeGSProConnectionChanged", "false")
            }
        }
    }

    /**
     * Handles ACTION_CONNECT_GSPRO intent from Unity.
     * Connects to GSPro with the host/port from the intent.
     */
    private fun connectToGSProFromIntent(intent: Intent?) {
        val host = intent?.getStringExtra(EXTRA_GSPRO_HOST) ?: pendingGSProHost
        val port = intent?.getIntExtra(EXTRA_GSPRO_PORT, GSProClient.DEFAULT_PORT) ?: pendingGSProPort

        Log.i(TAG, "Connecting to GSPro from Unity request: $host:$port")

        // Save for later reconnection if needed
        pendingGSProHost = host
        pendingGSProPort = port

        // Disconnect existing connection if any
        if (gsProClient?.isConnected() == true) {
            Log.d(TAG, "Disconnecting existing GSPro connection before reconnecting")
            gsProClient?.disconnect()
            gsProClient = null
            isGSProConnected = false
        }

        connectToGSPro(host, port)
    }

    /**
     * Handles ACTION_DISCONNECT_GSPRO intent from Unity.
     * Disconnects from GSPro.
     */
    private fun disconnectFromGSProAction() {
        Log.i(TAG, "Disconnecting from GSPro (Unity request)")
        disconnectFromGSPro()
        sendToUnity("OnBridgeGSProConnectionChanged", "false")
    }

    /**
     * Called when Unity app goes to background.
     * Starts test shots if conditions are met (GSPro already connected from startBridgeMode).
     *
     * @param intent The intent containing the current test_shot_mode setting.
     */
    private fun onAppBackgrounded(intent: Intent?) {
        // Update testShotModeEnabled from intent if provided (allows runtime setting changes)
        if (intent?.hasExtra(EXTRA_TEST_SHOT_MODE) == true) {
            testShotModeEnabled = intent.getBooleanExtra(EXTRA_TEST_SHOT_MODE, testShotModeEnabled)
        }

        Log.i(TAG, "App backgrounded")
        Log.i(TAG, "  testShotModeEnabled=$testShotModeEnabled")
        Log.i(TAG, "  isGC2Connected=$isGC2Connected")
        Log.i(TAG, "  isGSProConnected=$isGSProConnected")
        isAppBackgrounded = true

        // GSPro is already connected from startBridgeMode() - just start test shots if conditions are met
        if (testShotModeEnabled && !isGC2Connected && isGSProConnected) {
            Log.i(TAG, "Conditions met - starting test shots")
            startTestShots()
        } else {
            Log.d(TAG, "Not starting test shots: testShotMode=$testShotModeEnabled, gc2=$isGC2Connected, gspro=$isGSProConnected")
        }
    }

    /**
     * Called when Unity app returns to foreground.
     * Stops test shots but keeps GSPro connected (native service handles all GSPro on Android).
     */
    private fun onAppResumed() {
        Log.i(TAG, "App resumed - stopping test shots")
        isAppBackgrounded = false

        // Stop test shots only - keep GSPro connected (native service handles all GSPro on Android)
        stopTestShots()
    }

    private fun disconnectFromGSPro() {
        stopTestShots()
        gsProClient?.disconnect()
        gsProClient = null
        isGSProConnected = false
        Log.d(TAG, "Native GSPro client disconnected")
    }

    // -------------------------------------------------------------------------
    // Test shot methods
    // -------------------------------------------------------------------------

    private fun startTestShots() {
        if (testShotRunnable != null) {
            Log.d(TAG, "Test shots already running")
            return
        }

        Log.i(TAG, "Starting test shot mode (interval: ${TEST_SHOT_INTERVAL_MS}ms)")
        testShotIndex = 0

        // Send first shot immediately
        sendTestShot()

        // Schedule periodic shots
        testShotRunnable = object : Runnable {
            override fun run() {
                sendTestShot()
                mainHandler.postDelayed(this, TEST_SHOT_INTERVAL_MS)
            }
        }
        mainHandler.postDelayed(testShotRunnable!!, TEST_SHOT_INTERVAL_MS)
    }

    private fun stopTestShots() {
        testShotRunnable?.let {
            mainHandler.removeCallbacks(it)
            testShotRunnable = null
            Log.i(TAG, "Test shot mode stopped")
        }
    }

    private fun sendTestShot() {
        val client = gsProClient
        if (client == null || !client.isConnected()) {
            Log.w(TAG, "Cannot send test shot - GSPro not connected")
            return
        }

        val preset = testShotPresets[testShotIndex]
        val name = testShotNames[testShotIndex]
        testShotIndex = (testShotIndex + 1) % testShotPresets.size

        val ballSpeed = preset[0]
        val launchAngle = preset[1]
        val backSpin = preset[2]

        Log.i(TAG, "Sending test shot: $name ($ballSpeed mph)")

        // Set ball detected to true for test shots (GSPro may require this)
        client.updateDeviceStatus(isReady = true, ballDetected = true)

        client.sendShot(
            ballSpeed = ballSpeed,
            launchAngle = launchAngle,
            direction = 0f,
            totalSpin = backSpin,
            backSpin = backSpin,
            sideSpin = 0f,
            spinAxis = 0f
        )

        // Increment shot count and update notification
        shotsRelayed++
        updateNotification()
        sendToUnity("OnBridgeShotRelayed", shotsRelayed.toString())
    }

    /**
     * Sends a shot to GSPro directly from GC2Plugin.
     * Called when a shot is received from the GC2 USB device.
     * This ensures shots are relayed via native client even when app is backgrounded.
     *
     * @param ballSpeed Ball speed in mph
     * @param launchAngle Launch angle in degrees
     * @param direction Launch direction in degrees
     * @param totalSpin Total spin in rpm
     * @param backSpin Back spin in rpm
     * @param sideSpin Side spin in rpm
     * @param spinAxis Spin axis in degrees
     */
    fun sendShotToGSPro(
        ballSpeed: Float,
        launchAngle: Float,
        direction: Float,
        totalSpin: Float,
        backSpin: Float,
        sideSpin: Float,
        spinAxis: Float
    ) {
        val client = gsProClient
        if (client == null || !client.isConnected()) {
            Log.w(TAG, "Cannot send shot from GC2 - GSPro not connected")
            return
        }

        Log.i(TAG, "Sending GC2 shot to GSPro: $ballSpeed mph, $launchAngle° launch, $totalSpin rpm spin")

        // Set device status to ready with ball detected before sending shot
        client.updateDeviceStatus(isReady = true, ballDetected = true)

        client.sendShot(
            ballSpeed = ballSpeed,
            launchAngle = launchAngle,
            direction = direction,
            totalSpin = totalSpin,
            backSpin = backSpin,
            sideSpin = sideSpin,
            spinAxis = spinAxis
        )

        // Increment shot count and update notification
        shotsRelayed++
        isGC2Connected = true  // Shot came from GC2, so it's connected
        updateNotification()
        sendToUnity("OnBridgeShotRelayed", shotsRelayed.toString())
    }

    /**
     * Sends a shot to GSPro from Unity intent data.
     * Called when Unity wants to send a shot through the native client.
     * Unity sends shots here that it received from the GC2 USB device.
     */
    private fun sendShotFromIntent(intent: Intent) {
        val client = gsProClient
        if (client == null || !client.isConnected()) {
            Log.w(TAG, "Cannot send shot from Unity - GSPro not connected")
            sendToUnity("OnBridgeError", "GSPro not connected")
            return
        }

        val ballSpeed = intent.getFloatExtra(EXTRA_BALL_SPEED, 0f)
        val launchAngle = intent.getFloatExtra(EXTRA_LAUNCH_ANGLE, 0f)
        val direction = intent.getFloatExtra(EXTRA_DIRECTION, 0f)
        val totalSpin = intent.getFloatExtra(EXTRA_TOTAL_SPIN, 0f)
        val backSpin = intent.getFloatExtra(EXTRA_BACK_SPIN, 0f)
        val sideSpin = intent.getFloatExtra(EXTRA_SIDE_SPIN, 0f)
        val spinAxis = intent.getFloatExtra(EXTRA_SPIN_AXIS, 0f)

        Log.i(TAG, "Sending shot from Unity: $ballSpeed mph, $launchAngle° launch, $totalSpin rpm spin")

        client.sendShot(
            ballSpeed = ballSpeed,
            launchAngle = launchAngle,
            direction = direction,
            totalSpin = totalSpin,
            backSpin = backSpin,
            sideSpin = sideSpin,
            spinAxis = spinAxis
        )

        // Increment shot count and update notification
        // Shot came from Unity which received it from GC2, so GC2 is connected
        shotsRelayed++
        isGC2Connected = true
        updateNotification()
        sendToUnity("OnBridgeShotRelayed", shotsRelayed.toString())
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
