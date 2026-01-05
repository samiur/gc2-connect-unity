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
        // Note: Some GC2 firmware sends "SHOT" instead of "SHOT_ID"
        const val FIELD_SHOT_ID = "SHOT_ID"
        const val FIELD_SHOT = "SHOT"  // Alias for SHOT_ID
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
     * Note: Using object with String constants instead of enum to avoid
     * Kotlin 1.9+ EnumEntriesKt dependency which isn't bundled with Unity Android.
     */
    object MessageType {
        const val SHOT = "SHOT"
        const val STATUS = "STATUS"
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
    fun processData(data: String, onMessage: (String, String) -> Unit) {
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

        // Check for message terminator - just clear buffer, don't force finalize.
        // Shot finalization happens in processShotLine when BACK_RPM/SIDE_RPM are received.
        // The GC2 sends two 0H messages per shot:
        // 1. First 0H: ball data (speed, angle, etc.) - no spin
        // 2. Second 0H: spin data (BACK_RPM, SIDE_RPM)
        // We must wait for the second message before emitting the shot.
        if (lineBuffer.contains(MESSAGE_TERMINATOR)) {
            lineBuffer.clear()
        }
    }

    /**
     * Processes a single line of data.
     */
    private fun processLine(line: String, onMessage: (String, String) -> Unit) {
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
     *
     * The GC2 may send fields in various formats:
     * - Space-separated: "SPEED_MPH=148.9 ELEVATION_DEG=11.7"
     * - Concatenated: "BACK_RPM=3095.SIDE_RPM=-419."
     * - Single field: "SHOT=1"
     *
     * We use regex to extract all KEY=VALUE pairs reliably.
     */
    private fun processShotLine(line: String, onMessage: (String, String) -> Unit) {
        // Regex to match KEY=VALUE pairs
        // KEY: uppercase letter followed by uppercase letters, digits, or underscores
        // VALUE: anything that's not part of the next KEY= pattern (non-greedy until next uppercase sequence + =)
        val keyValuePattern = Regex("""([A-Z][A-Z0-9_]*)=([^=]*?)(?=[A-Z][A-Z0-9_]*=|$)""")

        val matches = keyValuePattern.findAll(line)

        for (match in matches) {
            val key = match.groupValues[1].trim()
            var value = match.groupValues[2].trim()

            // Remove trailing period if present (GC2 sends "3095." format)
            if (value.endsWith(".") && value.length > 1 && value[value.length - 2].isDigit()) {
                value = value.dropLast(1)
            }

            Log.v(TAG, "Parsed field: $key = $value")

            // Check for new shot (different SHOT_ID or SHOT)
            // GC2 may use either "SHOT_ID" or "SHOT" depending on firmware
            val isShotIdField = (key == FIELD_SHOT_ID || key == FIELD_SHOT)
            if (isShotIdField && value != lastShotId) {
                // Finalize previous shot if exists
                finalizeShotIfReady(onMessage)

                // Start new shot
                currentShotData.clear()
                lastShotId = value
                Log.d(TAG, "New shot detected: ID=$value")
            }

            // Normalize SHOT to SHOT_ID for consistent internal handling
            val normalizedKey = if (key == FIELD_SHOT) FIELD_SHOT_ID else key
            currentShotData[normalizedKey] = value
        }

        // Check if shot is complete (has spin data)
        if (currentShotData.containsKey(FIELD_BACK_RPM) &&
            currentShotData.containsKey(FIELD_SIDE_RPM)) {
            Log.d(TAG, "Shot complete - has BACK_RPM and SIDE_RPM, finalizing...")
            finalizeShotIfReady(onMessage)
        } else {
            Log.d(TAG, "Shot incomplete - waiting for spin data. " +
                    "Has BACK_RPM: ${currentShotData.containsKey(FIELD_BACK_RPM)}, " +
                    "Has SIDE_RPM: ${currentShotData.containsKey(FIELD_SIDE_RPM)}, " +
                    "Current fields: ${currentShotData.keys}")
        }
    }

    /**
     * Processes a status line (0M prefix).
     */
    private fun processStatusLine(line: String, onMessage: (String, String) -> Unit) {
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
    private fun finalizeShotIfReady(onMessage: (String, String) -> Unit) {
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
     *
     * Shot is only valid if:
     * - SPEED_MPH is present and in range (1.1-250 mph)
     * - BACK_RPM and SIDE_RPM are present (spin data required)
     * - Spin is not zero (misread) or 2222 (error code)
     */
    private fun isValidShot(data: Map<String, String>): Boolean {
        // Check for required fields
        val speed = data[FIELD_SPEED_MPH]?.toFloatOrNull() ?: run {
            Log.d(TAG, "Missing SPEED_MPH")
            return false
        }

        // Spin data is REQUIRED - must be present (not just non-zero)
        val backSpinStr = data[FIELD_BACK_RPM]
        val sideSpinStr = data[FIELD_SIDE_RPM]

        if (backSpinStr == null || sideSpinStr == null) {
            Log.d(TAG, "Missing spin data - BACK_RPM=$backSpinStr, SIDE_RPM=$sideSpinStr")
            return false
        }

        // Parse as double first since GC2 may send "3095." format (trailing decimal)
        val backSpin = backSpinStr.toDoubleOrNull()?.toInt() ?: run {
            Log.d(TAG, "Invalid BACK_RPM value: $backSpinStr")
            return false
        }

        val sideSpin = sideSpinStr.toDoubleOrNull()?.toInt() ?: run {
            Log.d(TAG, "Invalid SIDE_RPM value: $sideSpinStr")
            return false
        }

        // Speed range check
        if (speed < MIN_BALL_SPEED_MPH || speed > MAX_BALL_SPEED_MPH) {
            Log.d(TAG, "Invalid speed: $speed mph (range: $MIN_BALL_SPEED_MPH-$MAX_BALL_SPEED_MPH)")
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
     * Note: Android's JSONObject.put() requires double, not Float. Using toDoubleOrNull().
     */
    private fun convertToJson(data: Map<String, String>): String? {
        return try {
            val json = JSONObject()

            // Required fields (map GC2 names to C# property names)
            // Note: Must use Double, not Float - Android JSONObject has put(String, double) not put(String, Float)
            json.put("ShotId", data[FIELD_SHOT_ID]?.toIntOrNull() ?: 0)
            json.put("BallSpeed", data[FIELD_SPEED_MPH]?.toDoubleOrNull() ?: 0.0)
            json.put("LaunchAngle", data[FIELD_ELEVATION_DEG]?.toDoubleOrNull() ?: 0.0)
            json.put("LaunchDirection", data[FIELD_AZIMUTH_DEG]?.toDoubleOrNull() ?: 0.0)
            json.put("TotalSpin", data[FIELD_SPIN_RPM]?.toDoubleOrNull() ?: 0.0)
            json.put("BackSpin", data[FIELD_BACK_RPM]?.toDoubleOrNull() ?: 0.0)
            json.put("SideSpin", data[FIELD_SIDE_RPM]?.toDoubleOrNull() ?: 0.0)
            json.put("SpinAxis", data[FIELD_SPIN_AXIS]?.toDoubleOrNull() ?: 0.0)

            // Timestamp
            json.put("Timestamp", System.currentTimeMillis())

            // Optional HMT club data
            val hasClubData = data.containsKey(FIELD_CLUBSPEED_MPH)
            json.put("HasClubData", hasClubData)

            if (hasClubData) {
                json.put("ClubSpeed", data[FIELD_CLUBSPEED_MPH]?.toDoubleOrNull() ?: 0.0)
                json.put("ClubPath", data[FIELD_HPATH_DEG]?.toDoubleOrNull() ?: 0.0)
                json.put("AttackAngle", data[FIELD_VPATH_DEG]?.toDoubleOrNull() ?: 0.0)
                json.put("FaceToTarget", data[FIELD_FACE_T_DEG]?.toDoubleOrNull() ?: 0.0)
                json.put("DynamicLoft", data[FIELD_LOFT_DEG]?.toDoubleOrNull() ?: 0.0)
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
