// ABOUTME: GC2 protocol parser for Android.
// ABOUTME: Parses KEY=VALUE format messages and converts to JSON for Unity.

package com.openrange.gc2

import android.util.Log
import org.json.JSONObject

/**
 * Parser for the GC2 USB text protocol.
 *
 * The GC2 sends data in KEY=VALUE format with newline separators.
 * Message types are identified by prefix:
 * - "0H" prefix: Shot data (speed, angle, spin, etc.)
 * - "0M" prefix: Device status (FLAGS, BALLS for ready state)
 *
 * Messages are terminated by "\n\t" (newline + tab).
 *
 * Shot data arrives across multiple packets and must be accumulated
 * until BACK_RPM and SIDE_RPM are received to ensure complete data.
 */
class GC2Protocol {

    companion object {
        private const val TAG = "GC2Protocol"

        /** Message prefix for shot data */
        const val SHOT_MESSAGE_PREFIX = "0H"

        /** Message prefix for device status */
        const val STATUS_MESSAGE_PREFIX = "0M"

        /** Message terminator indicating complete message */
        const val MESSAGE_TERMINATOR = "\n\t"

        /** Minimum valid ball speed in mph (putts) */
        const val MIN_BALL_SPEED_MPH = 1.1f

        /** Maximum valid ball speed in mph */
        const val MAX_BALL_SPEED_MPH = 250f

        /** Error pattern indicating a misread */
        const val MISREAD_SPIN_VALUE = 2222

        // GC2 field names (0H shot data)
        const val FIELD_SHOT_ID = "SHOT_ID"
        const val FIELD_SPEED_MPH = "SPEED_MPH"
        const val FIELD_ELEVATION_DEG = "ELEVATION_DEG"
        const val FIELD_AZIMUTH_DEG = "AZIMUTH_DEG"
        const val FIELD_SPIN_RPM = "SPIN_RPM"
        const val FIELD_BACK_RPM = "BACK_RPM"
        const val FIELD_SIDE_RPM = "SIDE_RPM"
        const val FIELD_SPIN_AXIS = "SPIN_AXIS"

        // HMT fields (optional club data)
        const val FIELD_CLUBSPEED_MPH = "CLUBSPEED_MPH"
        const val FIELD_HPATH_DEG = "HPATH_DEG"
        const val FIELD_VPATH_DEG = "VPATH_DEG"
        const val FIELD_FACE_T_DEG = "FACE_T_DEG"
        const val FIELD_LOFT_DEG = "LOFT_DEG"

        // GC2 status fields (0M status data)
        const val FIELD_FLAGS = "FLAGS"
        const val FIELD_BALLS = "BALLS"
    }

    /**
     * Message types that can be parsed.
     */
    enum class MessageType {
        SHOT,
        STATUS
    }

    // Buffer for accumulating multi-packet messages
    private val lineBuffer = StringBuilder()

    // Current shot data being accumulated
    private val currentShotData = mutableMapOf<String, String>()
    private var lastShotId: String? = null

    /**
     * Processes incoming data and calls the callback when complete messages are parsed.
     *
     * @param data Raw string data from USB
     * @param onMessage Callback invoked with message type and JSON data
     */
    fun processData(data: String, onMessage: (MessageType, String) -> Unit) {
        lineBuffer.append(data)

        // Process complete lines
        while (true) {
            val newlineIndex = lineBuffer.indexOf('\n')
            if (newlineIndex < 0) break

            val line = lineBuffer.substring(0, newlineIndex).trim()
            lineBuffer.delete(0, newlineIndex + 1)

            if (line.isEmpty()) continue

            processLine(line, onMessage)
        }

        // Check for message terminator
        if (lineBuffer.contains(MESSAGE_TERMINATOR)) {
            // Message complete, finalize any pending shot
            finalizeShotIfReady(onMessage)
            lineBuffer.clear()
        }
    }

    /**
     * Processes a single line of data.
     */
    private fun processLine(line: String, onMessage: (MessageType, String) -> Unit) {
        when {
            line.startsWith(SHOT_MESSAGE_PREFIX) -> {
                processShotLine(line.removePrefix(SHOT_MESSAGE_PREFIX).trim(), onMessage)
            }
            line.startsWith(STATUS_MESSAGE_PREFIX) -> {
                processStatusLine(line.removePrefix(STATUS_MESSAGE_PREFIX).trim(), onMessage)
            }
            else -> {
                // May be continuation of shot data without prefix
                if (line.contains("=")) {
                    processShotLine(line, onMessage)
                }
            }
        }
    }

    /**
     * Processes a shot data line (0H prefix).
     */
    private fun processShotLine(line: String, onMessage: (MessageType, String) -> Unit) {
        // Parse KEY=VALUE pairs
        val parts = line.split(",", " ").filter { it.contains("=") }

        for (part in parts) {
            val keyValue = part.split("=", limit = 2)
            if (keyValue.size == 2) {
                val key = keyValue[0].trim()
                val value = keyValue[1].trim()

                // Check for new shot (different SHOT_ID)
                if (key == FIELD_SHOT_ID && value != lastShotId) {
                    // Finalize previous shot if exists
                    finalizeShotIfReady(onMessage)

                    // Start new shot
                    currentShotData.clear()
                    lastShotId = value
                }

                currentShotData[key] = value
            }
        }

        // Check if shot is complete (has spin data)
        if (currentShotData.containsKey(FIELD_BACK_RPM) &&
            currentShotData.containsKey(FIELD_SIDE_RPM)) {
            finalizeShotIfReady(onMessage)
        }
    }

    /**
     * Processes a status line (0M prefix).
     */
    private fun processStatusLine(line: String, onMessage: (MessageType, String) -> Unit) {
        val statusData = mutableMapOf<String, String>()

        // Parse KEY=VALUE pairs
        val parts = line.split(",", " ").filter { it.contains("=") }
        for (part in parts) {
            val keyValue = part.split("=", limit = 2)
            if (keyValue.size == 2) {
                statusData[keyValue[0].trim()] = keyValue[1].trim()
            }
        }

        // Convert to device status JSON
        val flags = statusData[FIELD_FLAGS]?.toIntOrNull() ?: 0
        val balls = statusData[FIELD_BALLS]?.toIntOrNull() ?: 0

        val isReady = flags == 7  // Green light
        val ballDetected = balls > 0

        val json = JSONObject().apply {
            put("IsReady", isReady)
            put("BallDetected", ballDetected)
            put("RawFlags", flags)
            put("BallCount", balls)
        }

        onMessage(MessageType.STATUS, json.toString())
    }

    /**
     * Finalizes and sends the current shot if valid.
     */
    private fun finalizeShotIfReady(onMessage: (MessageType, String) -> Unit) {
        if (currentShotData.isEmpty()) return

        // Validate shot data
        if (!isValidShot(currentShotData)) {
            Log.d(TAG, "Shot rejected as invalid: $currentShotData")
            currentShotData.clear()
            return
        }

        // Convert to JSON matching GC2ShotData C# properties
        val json = convertToJson(currentShotData)
        if (json != null) {
            onMessage(MessageType.SHOT, json)
        }

        currentShotData.clear()
    }

    /**
     * Validates shot data for misreads and out-of-range values.
     */
    private fun isValidShot(data: Map<String, String>): Boolean {
        // Check for required fields
        val speed = data[FIELD_SPEED_MPH]?.toFloatOrNull() ?: return false
        val backSpin = data[FIELD_BACK_RPM]?.toIntOrNull()
        val sideSpin = data[FIELD_SIDE_RPM]?.toIntOrNull()

        // Speed range check
        if (speed < MIN_BALL_SPEED_MPH || speed > MAX_BALL_SPEED_MPH) {
            Log.d(TAG, "Invalid speed: $speed mph")
            return false
        }

        // Zero spin check (misread)
        if (backSpin == 0 && sideSpin == 0) {
            Log.d(TAG, "Zero spin detected - misread")
            return false
        }

        // 2222 error pattern check
        if (backSpin == MISREAD_SPIN_VALUE) {
            Log.d(TAG, "2222 error pattern detected - misread")
            return false
        }

        return true
    }

    /**
     * Converts parsed shot data to JSON matching Unity's GC2ShotData properties.
     */
    private fun convertToJson(data: Map<String, String>): String? {
        return try {
            val json = JSONObject()

            // Required fields (map GC2 names to C# property names)
            json.put("ShotId", data[FIELD_SHOT_ID]?.toIntOrNull() ?: 0)
            json.put("BallSpeed", data[FIELD_SPEED_MPH]?.toFloatOrNull() ?: 0f)
            json.put("LaunchAngle", data[FIELD_ELEVATION_DEG]?.toFloatOrNull() ?: 0f)
            json.put("LaunchDirection", data[FIELD_AZIMUTH_DEG]?.toFloatOrNull() ?: 0f)
            json.put("TotalSpin", data[FIELD_SPIN_RPM]?.toFloatOrNull() ?: 0f)
            json.put("BackSpin", data[FIELD_BACK_RPM]?.toFloatOrNull() ?: 0f)
            json.put("SideSpin", data[FIELD_SIDE_RPM]?.toFloatOrNull() ?: 0f)
            json.put("SpinAxis", data[FIELD_SPIN_AXIS]?.toFloatOrNull() ?: 0f)

            // Timestamp
            json.put("Timestamp", System.currentTimeMillis())

            // Optional HMT club data
            val hasClubData = data.containsKey(FIELD_CLUBSPEED_MPH)
            json.put("HasClubData", hasClubData)

            if (hasClubData) {
                json.put("ClubSpeed", data[FIELD_CLUBSPEED_MPH]?.toFloatOrNull() ?: 0f)
                json.put("ClubPath", data[FIELD_HPATH_DEG]?.toFloatOrNull() ?: 0f)
                json.put("AttackAngle", data[FIELD_VPATH_DEG]?.toFloatOrNull() ?: 0f)
                json.put("FaceToTarget", data[FIELD_FACE_T_DEG]?.toFloatOrNull() ?: 0f)
                json.put("DynamicLoft", data[FIELD_LOFT_DEG]?.toFloatOrNull() ?: 0f)
            }

            json.toString()
        } catch (e: Exception) {
            Log.e(TAG, "Failed to convert to JSON: ${e.message}")
            null
        }
    }

    /**
     * Resets the parser state.
     */
    fun reset() {
        lineBuffer.clear()
        currentShotData.clear()
        lastShotId = null
    }
}
