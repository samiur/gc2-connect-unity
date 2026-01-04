// ABOUTME: Native GSPro TCP client for background operation.
// ABOUTME: Sends shot data and heartbeats to GSPro when Unity is paused.

package com.openrange.gc2

import android.os.Handler
import android.os.Looper
import android.util.Log
import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.io.PrintWriter
import java.net.InetSocketAddress
import java.net.Socket
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicInteger

/**
 * Native GSPro TCP client for Android.
 * Handles connection, heartbeats, and shot sending in background.
 */
class GSProClient {

    companion object {
        private const val TAG = "GSProClient"
        const val DEFAULT_PORT = 921
        private const val CONNECT_TIMEOUT_MS = 5000
        private const val HEARTBEAT_INTERVAL_MS = 2000L
    }

    private val executor = Executors.newSingleThreadExecutor()
    private val mainHandler = Handler(Looper.getMainLooper())

    private var socket: Socket? = null
    private var writer: PrintWriter? = null
    private var reader: BufferedReader? = null

    private val isConnected = AtomicBoolean(false)
    private val shotNumber = AtomicInteger(0)

    private var heartbeatRunnable: Runnable? = null
    private var launchMonitorIsReady = true
    private var launchMonitorBallDetected = false

    var host: String = ""
        private set
    var port: Int = DEFAULT_PORT
        private set

    /**
     * Check if connected to GSPro.
     */
    fun isConnected(): Boolean = isConnected.get()

    /**
     * Connect to GSPro server.
     */
    fun connect(host: String, port: Int = DEFAULT_PORT, callback: (Boolean) -> Unit) {
        this.host = host
        this.port = port

        executor.execute {
            try {
                Log.d(TAG, "Connecting to GSPro at $host:$port")

                val newSocket = Socket()
                newSocket.tcpNoDelay = true  // Disable Nagle's algorithm
                newSocket.connect(InetSocketAddress(host, port), CONNECT_TIMEOUT_MS)

                writer = PrintWriter(newSocket.getOutputStream(), true)
                reader = BufferedReader(InputStreamReader(newSocket.getInputStream()))
                socket = newSocket

                isConnected.set(true)
                shotNumber.set(0)

                Log.i(TAG, "Connected to GSPro")

                // Start heartbeat
                startHeartbeat()

                mainHandler.post { callback(true) }

            } catch (e: Exception) {
                Log.e(TAG, "Failed to connect to GSPro: ${e.message}")
                isConnected.set(false)
                mainHandler.post { callback(false) }
            }
        }
    }

    /**
     * Disconnect from GSPro server.
     */
    fun disconnect() {
        Log.d(TAG, "Disconnecting from GSPro")

        stopHeartbeat()

        executor.execute {
            try {
                writer?.close()
                reader?.close()
                socket?.close()
            } catch (e: Exception) {
                Log.e(TAG, "Error closing socket: ${e.message}")
            } finally {
                writer = null
                reader = null
                socket = null
                isConnected.set(false)
            }
        }
    }

    /**
     * Send a shot to GSPro.
     */
    fun sendShot(
        ballSpeed: Float,
        launchAngle: Float,
        direction: Float,
        totalSpin: Float,
        backSpin: Float,
        sideSpin: Float,
        spinAxis: Float
    ) {
        if (!isConnected.get()) {
            Log.w(TAG, "Cannot send shot - not connected")
            return
        }

        val currentShotNumber = shotNumber.incrementAndGet()

        val message = createShotMessage(
            shotNumber = currentShotNumber,
            ballSpeed = ballSpeed,
            launchAngle = launchAngle,
            direction = direction,
            totalSpin = totalSpin,
            backSpin = backSpin,
            sideSpin = sideSpin,
            spinAxis = spinAxis
        )

        executor.execute {
            try {
                writer?.println(message)
                Log.d(TAG, "Sent shot #$currentShotNumber")
            } catch (e: Exception) {
                Log.e(TAG, "Failed to send shot: ${e.message}")
                isConnected.set(false)
            }
        }
    }

    /**
     * Update device status for heartbeats.
     */
    fun updateDeviceStatus(isReady: Boolean, ballDetected: Boolean) {
        launchMonitorIsReady = isReady
        launchMonitorBallDetected = ballDetected
    }

    private fun startHeartbeat() {
        stopHeartbeat()

        heartbeatRunnable = object : Runnable {
            override fun run() {
                sendHeartbeat()
                mainHandler.postDelayed(this, HEARTBEAT_INTERVAL_MS)
            }
        }

        mainHandler.postDelayed(heartbeatRunnable!!, HEARTBEAT_INTERVAL_MS)
        Log.d(TAG, "Heartbeat started")
    }

    private fun stopHeartbeat() {
        heartbeatRunnable?.let {
            mainHandler.removeCallbacks(it)
            heartbeatRunnable = null
            Log.d(TAG, "Heartbeat stopped")
        }
    }

    private fun sendHeartbeat() {
        if (!isConnected.get()) return

        val message = createHeartbeatMessage()

        executor.execute {
            try {
                writer?.println(message)
                Log.v(TAG, "Sent heartbeat")
            } catch (e: Exception) {
                Log.e(TAG, "Failed to send heartbeat: ${e.message}")
                isConnected.set(false)
            }
        }
    }

    private fun createShotMessage(
        shotNumber: Int,
        ballSpeed: Float,
        launchAngle: Float,
        direction: Float,
        totalSpin: Float,
        backSpin: Float,
        sideSpin: Float,
        spinAxis: Float
    ): String {
        // Note: JSONObject.put() requires Double, not Float
        val ballData = JSONObject().apply {
            put("Speed", ballSpeed.toDouble())
            put("SpinAxis", spinAxis.toDouble())
            put("TotalSpin", totalSpin.toDouble())
            put("BackSpin", backSpin.toDouble())
            put("SideSpin", sideSpin.toDouble())
            put("HLA", direction.toDouble())
            put("VLA", launchAngle.toDouble())
        }

        val options = JSONObject().apply {
            put("ContainsBallData", true)
            put("ContainsClubData", false)
            put("LaunchMonitorIsReady", launchMonitorIsReady)
            put("LaunchMonitorBallDetected", launchMonitorBallDetected)
            put("IsHeartBeat", false)
        }

        val message = JSONObject().apply {
            put("DeviceID", "GC2 Connect Unity")
            put("Units", "Yards")
            put("ShotNumber", shotNumber)
            put("APIversion", "1")
            put("BallData", ballData)
            put("ShotDataOptions", options)
        }

        return message.toString()
    }

    private fun createHeartbeatMessage(): String {
        val ballData = JSONObject().apply {
            put("Speed", 0)
            put("SpinAxis", 0)
            put("TotalSpin", 0)
            put("BackSpin", 0)
            put("SideSpin", 0)
            put("HLA", 0)
            put("VLA", 0)
        }

        val options = JSONObject().apply {
            put("ContainsBallData", false)
            put("ContainsClubData", false)
            put("LaunchMonitorIsReady", launchMonitorIsReady)
            put("LaunchMonitorBallDetected", launchMonitorBallDetected)
            put("IsHeartBeat", true)
        }

        val message = JSONObject().apply {
            put("DeviceID", "GC2 Connect Unity")
            put("Units", "Yards")
            put("ShotNumber", 0)
            put("APIversion", "1")
            put("BallData", ballData)
            put("ShotDataOptions", options)
        }

        return message.toString()
    }
}
