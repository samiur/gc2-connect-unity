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
 * IMPORTANT: The GC2 sends TWO 0H messages per shot:
 * 1. Initial message (~128-180ms): Ball data with preliminary/default spin
 * 2. Final message (~800-1000ms): Same ball data with real spin values
 *
 * We use time-based accumulation:
 * - First message starts a pending shot with timestamp
 * - Subsequent messages with same SHOT_ID merge/override fields
 * - After SHOT_TIMEOUT_MS, we finalize with whatever data we have
 * - 0M status messages can arrive between shot messages and are handled separately
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

        /**
         * Time to wait for additional shot data before finalizing (ms).
         * GC2 sends initial message at ~128-180ms and final at ~800-1000ms,
         * so 1200ms gives buffer for the second message to arrive.
         */
        const val SHOT_TIMEOUT_MS = 1200L

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
        const val NONE = "NONE"
    }

    // Buffer for accumulating raw data across USB packets
    private val rawBuffer = StringBuilder()

    // Current message type we're accumulating (SHOT, STATUS, or NONE)
    private var currentMessageType = MessageType.NONE

    // Pending shot data with time-based accumulation
    private val pendingShotData = mutableMapOf<String, String>()
    private var pendingShotId: String? = null
    private var pendingShotTimestamp: Long = 0L

    // Current status data being accumulated (within a single 0M message)
    private val currentStatusData = mutableMapOf<String, String>()

    /**
     * Processes incoming data and calls the callback when complete messages are parsed.
     *
     * @param data Raw string data from USB
     * @param onMessage Callback invoked with message type and JSON data
     */
    fun processData(data: String, onMessage: (String, String) -> Unit) {
        // Check for pending shot timeout FIRST (before processing new data)
        checkPendingShotTimeout(onMessage)

        rawBuffer.append(data)

        // Process complete lines from the buffer
        while (true) {
            val newlineIndex = rawBuffer.indexOf('\n')
            if (newlineIndex < 0) break

            val line = rawBuffer.substring(0, newlineIndex).trim()
            rawBuffer.delete(0, newlineIndex + 1)

            // Check for message terminator (tab at start of line after newline)
            if (line == "\t" || line.isEmpty() && rawBuffer.startsWith("\t")) {
                // Message terminator found - finalize current message (status only)
                finalizeCurrentMessage(onMessage)
                if (rawBuffer.startsWith("\t")) {
                    rawBuffer.delete(0, 1)
                }
                continue
            }

            if (line.isEmpty()) continue

            processLine(line, onMessage)
        }

        // Check if buffer starts with tab (terminator without preceding newline in buffer)
        if (rawBuffer.startsWith("\t")) {
            finalizeCurrentMessage(onMessage)
            rawBuffer.delete(0, 1)
        }

        // Check timeout again after processing (in case we just started a new shot)
        checkPendingShotTimeout(onMessage)
    }

    /**
     * Checks if pending shot has timed out and should be finalized.
     */
    private fun checkPendingShotTimeout(onMessage: (String, String) -> Unit) {
        if (pendingShotData.isEmpty() || pendingShotTimestamp == 0L) return

        val elapsed = System.currentTimeMillis() - pendingShotTimestamp
        if (elapsed >= SHOT_TIMEOUT_MS) {
            Log.d(TAG, "Pending shot timeout after ${elapsed}ms - finalizing with ${pendingShotData.size} fields")
            finalizePendingShot(onMessage)
        }
    }

    /**
     * Processes a single line of data.
     */
    private fun processLine(line: String, onMessage: (String, String) -> Unit) {
        when {
            line.startsWith(SHOT_MESSAGE_PREFIX) -> {
                // Starting a new shot message - finalize any pending status
                if (currentMessageType == MessageType.STATUS) {
                    finalizeStatus(onMessage)
                }
                currentMessageType = MessageType.SHOT
                // Process any fields on the same line as 0H
                val remainder = line.removePrefix(SHOT_MESSAGE_PREFIX).trim()
                if (remainder.isNotEmpty()) {
                    accumulateShotFields(remainder, onMessage)
                }
            }
            line.startsWith(STATUS_MESSAGE_PREFIX) -> {
                // Starting a new status message - clear status buffer
                // Note: Status messages can arrive between shot messages, that's OK
                currentStatusData.clear()
                currentMessageType = MessageType.STATUS
                // Process any fields on the same line as 0M
                val remainder = line.removePrefix(STATUS_MESSAGE_PREFIX).trim()
                if (remainder.isNotEmpty()) {
                    accumulateStatusFields(remainder)
                }
            }
            else -> {
                // Continuation line - add to current message type
                if (line.contains("=")) {
                    when (currentMessageType) {
                        MessageType.SHOT -> accumulateShotFields(line, onMessage)
                        MessageType.STATUS -> accumulateStatusFields(line)
                        else -> {
                            // No message type set yet - probably orphaned data
                            Log.w(TAG, "Orphaned data line (no message type): $line")
                        }
                    }
                }
            }
        }
        // Note: Shot finalization is now time-based, not triggered here
    }

    /**
     * Finalizes the current message based on type.
     */
    private fun finalizeCurrentMessage(onMessage: (String, String) -> Unit) {
        when (currentMessageType) {
            MessageType.STATUS -> finalizeStatus(onMessage)
            MessageType.SHOT -> {
                // Don't finalize shot on terminator - use time-based finalization
                // The shot may span multiple 0H messages
                Log.d(TAG, "Message terminator - shot has ${pendingShotData.size} fields, " +
                        "waiting for timeout. Has BACK_RPM: ${pendingShotData.containsKey(FIELD_BACK_RPM)}")
            }
        }
        // Don't reset message type - shot data accumulates across messages
    }

    /**
     * Accumulates fields from a shot data line.
     * Uses regex to handle various GC2 formats:
     * - Space-separated: "SPEED_MPH=148.9 ELEVATION_DEG=11.7"
     * - Concatenated: "BACK_RPM=3095.SIDE_RPM=-419."
     * - Single field: "SHOT=1"
     *
     * Time-based accumulation:
     * - First message for a SHOT_ID starts the timer
     * - Subsequent messages with same SHOT_ID merge/override fields
     * - Different SHOT_ID finalizes previous and starts new
     */
    private fun accumulateShotFields(line: String, onMessage: (String, String) -> Unit) {
        // Regex to match KEY=VALUE pairs
        // KEY: uppercase letter followed by uppercase letters, digits, or underscores
        // VALUE: anything that's not part of the next KEY= pattern
        val keyValuePattern = Regex("""([A-Z][A-Z0-9_]*)=([^=]*?)(?=[A-Z][A-Z0-9_]*=|$)""")

        val matches = keyValuePattern.findAll(line)

        for (match in matches) {
            val key = match.groupValues[1].trim()
            var value = match.groupValues[2].trim()

            // Remove trailing period if present (GC2 sends "3095." format)
            if (value.endsWith(".") && value.length > 1 && value[value.length - 2].isDigit()) {
                value = value.dropLast(1)
            }

            // Skip status fields that might appear in shot messages (they come from 0M, not 0H)
            if (key == FIELD_FLAGS || key == FIELD_BALLS || key.startsWith("BALL")) {
                Log.v(TAG, "Skipping status field in shot context: $key = $value")
                continue
            }

            Log.v(TAG, "Shot field: $key = $value")

            // Check for new shot (different SHOT_ID or SHOT)
            val isShotIdField = (key == FIELD_SHOT_ID || key == FIELD_SHOT)
            if (isShotIdField && value != pendingShotId) {
                // New shot ID - finalize any pending shot first
                if (pendingShotData.isNotEmpty()) {
                    Log.d(TAG, "New shot ID=$value arrived, finalizing previous shot ID=$pendingShotId")
                    finalizePendingShot(onMessage)
                }
                // Start new pending shot
                pendingShotId = value
                pendingShotTimestamp = System.currentTimeMillis()
                Log.d(TAG, "New shot detected: ID=$value, timestamp=$pendingShotTimestamp")
            }

            // Normalize SHOT to SHOT_ID for consistent internal handling
            val normalizedKey = if (key == FIELD_SHOT) FIELD_SHOT_ID else key
            pendingShotData[normalizedKey] = value
        }
    }

    /**
     * Accumulates fields from a status data line.
     */
    private fun accumulateStatusFields(line: String) {
        // Parse KEY=VALUE pairs (simpler format for status)
        val keyValuePattern = Regex("""([A-Z][A-Z0-9_]*)=([^\s,]+)""")

        val matches = keyValuePattern.findAll(line)

        for (match in matches) {
            val key = match.groupValues[1].trim()
            val value = match.groupValues[2].trim()

            Log.v(TAG, "Status field: $key = $value")
            currentStatusData[key] = value
        }
    }

    /**
     * Finalizes and emits the current status message.
     */
    private fun finalizeStatus(onMessage: (String, String) -> Unit) {
        if (currentStatusData.isEmpty()) {
            Log.d(TAG, "No status data to finalize")
            return
        }

        // Convert to device status JSON
        val flags = currentStatusData[FIELD_FLAGS]?.toIntOrNull() ?: 0
        val balls = currentStatusData[FIELD_BALLS]?.toIntOrNull() ?: 0

        val isReady = flags == 7  // Green light
        val ballDetected = balls > 0

        Log.d(TAG, "Finalizing status: FLAGS=$flags, BALLS=$balls -> IsReady=$isReady, BallDetected=$ballDetected")

        val json = JSONObject().apply {
            put("IsReady", isReady)
            put("BallDetected", ballDetected)
            put("RawFlags", flags)
            put("BallCount", balls)
        }

        onMessage(MessageType.STATUS, json.toString())
        currentStatusData.clear()
    }

    /**
     * Finalizes and sends the pending shot if valid.
     * Called when timeout expires or new shot ID arrives.
     */
    private fun finalizePendingShot(onMessage: (String, String) -> Unit) {
        if (pendingShotData.isEmpty()) return

        val elapsed = System.currentTimeMillis() - pendingShotTimestamp
        Log.d(TAG, "Finalizing pending shot after ${elapsed}ms with ${pendingShotData.size} fields")

        // Validate shot data
        if (!isValidShot(pendingShotData)) {
            Log.d(TAG, "Shot rejected as invalid: $pendingShotData")
            clearPendingShot()
            return
        }

        // Convert to JSON matching GC2ShotData C# properties
        val json = convertToJson(pendingShotData)
        if (json != null) {
            Log.i(TAG, "Shot finalized successfully: ${pendingShotData[FIELD_SPEED_MPH]} mph, " +
                    "spin: ${pendingShotData[FIELD_BACK_RPM]}/${pendingShotData[FIELD_SIDE_RPM]}")
            onMessage(MessageType.SHOT, json)
        }

        clearPendingShot()
    }

    /**
     * Clears pending shot state.
     */
    private fun clearPendingShot() {
        pendingShotData.clear()
        pendingShotId = null
        pendingShotTimestamp = 0L
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
        val speed = data[FIELD_SPEED_MPH]?.toDoubleOrNull()?.toFloat() ?: run {
            Log.d(TAG, "Missing or invalid SPEED_MPH: ${data[FIELD_SPEED_MPH]}")
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
        // Also handle leading spaces like " 3500"
        val backSpin = backSpinStr.trim().toDoubleOrNull()?.toInt() ?: run {
            Log.d(TAG, "Invalid BACK_RPM value: '$backSpinStr'")
            return false
        }

        val sideSpin = sideSpinStr.trim().toDoubleOrNull()?.toInt() ?: run {
            Log.d(TAG, "Invalid SIDE_RPM value: '$sideSpinStr'")
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

        Log.d(TAG, "Shot validation passed: speed=$speed, backSpin=$backSpin, sideSpin=$sideSpin")
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
            // Also trim values to handle leading/trailing spaces
            json.put("ShotId", data[FIELD_SHOT_ID]?.trim()?.toIntOrNull() ?: 0)
            json.put("BallSpeed", data[FIELD_SPEED_MPH]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("LaunchAngle", data[FIELD_ELEVATION_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("LaunchDirection", data[FIELD_AZIMUTH_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("TotalSpin", data[FIELD_SPIN_RPM]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("BackSpin", data[FIELD_BACK_RPM]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("SideSpin", data[FIELD_SIDE_RPM]?.trim()?.toDoubleOrNull() ?: 0.0)
            json.put("SpinAxis", data[FIELD_SPIN_AXIS]?.trim()?.toDoubleOrNull() ?: 0.0)

            // Timestamp
            json.put("Timestamp", System.currentTimeMillis())

            // Optional HMT club data
            val hasClubData = data.containsKey(FIELD_CLUBSPEED_MPH)
            json.put("HasClubData", hasClubData)

            if (hasClubData) {
                json.put("ClubSpeed", data[FIELD_CLUBSPEED_MPH]?.trim()?.toDoubleOrNull() ?: 0.0)
                json.put("ClubPath", data[FIELD_HPATH_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
                json.put("AttackAngle", data[FIELD_VPATH_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
                json.put("FaceToTarget", data[FIELD_FACE_T_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
                json.put("DynamicLoft", data[FIELD_LOFT_DEG]?.trim()?.toDoubleOrNull() ?: 0.0)
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
        rawBuffer.clear()
        clearPendingShot()
        currentStatusData.clear()
        currentMessageType = MessageType.NONE
    }

    /**
     * Forces finalization of any pending shot.
     * Useful for testing or when disconnecting.
     */
    fun flush(onMessage: (String, String) -> Unit) {
        if (pendingShotData.isNotEmpty()) {
            Log.d(TAG, "Flush requested - finalizing pending shot")
            finalizePendingShot(onMessage)
        }
        if (currentStatusData.isNotEmpty()) {
            finalizeStatus(onMessage)
        }
    }
}
