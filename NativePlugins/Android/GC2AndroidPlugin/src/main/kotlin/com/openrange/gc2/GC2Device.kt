// ABOUTME: USB device wrapper for GC2 communication on Android.
// ABOUTME: Handles USB connection, interface claiming, and data reading.

package com.openrange.gc2

import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbEndpoint
import android.hardware.usb.UsbInterface
import android.util.Log
import java.io.Closeable

/**
 * Wrapper class for USB communication with the GC2 device.
 *
 * This class handles the low-level USB operations including:
 * - Interface claiming
 * - Endpoint discovery (INTERRUPT IN at 0x82)
 * - Data reading via interrupt transfers
 * - Protocol parsing via [GC2Protocol]
 *
 * @param device The USB device representing the GC2
 * @param connection The opened USB device connection
 * @param plugin Reference to the parent plugin for callbacks
 */
class GC2Device(
    private val device: UsbDevice,
    private val connection: UsbDeviceConnection,
    private val plugin: GC2Plugin
) : Closeable {

    companion object {
        private const val TAG = "GC2Device"

        /** GC2 uses INTERRUPT IN endpoint at address 0x82 */
        const val ENDPOINT_ADDRESS = 0x82

        /** USB interface index for the GC2 */
        const val INTERFACE_INDEX = 0

        /** Timeout for interrupt transfers in milliseconds */
        const val TRANSFER_TIMEOUT_MS = 100

        /** Buffer size for USB reads (64 bytes for interrupt transfers) */
        const val BUFFER_SIZE = 64
    }

    private var usbInterface: UsbInterface? = null
    private var inEndpoint: UsbEndpoint? = null
    private var readThread: Thread? = null

    @Volatile
    var isConnected: Boolean = false
        private set

    private val protocol = GC2Protocol()

    /**
     * Opens the USB interface and starts reading data.
     *
     * @return true if successfully opened, false otherwise
     */
    fun open(): Boolean {
        Log.d(TAG, "Opening GC2 device: ${device.deviceName}")

        // Find and claim the USB interface
        if (device.interfaceCount == 0) {
            Log.e(TAG, "Device has no interfaces")
            return false
        }

        usbInterface = device.getInterface(INTERFACE_INDEX)
        val iface = usbInterface ?: run {
            Log.e(TAG, "Failed to get interface at index $INTERFACE_INDEX")
            return false
        }

        if (!connection.claimInterface(iface, true)) {
            Log.e(TAG, "Failed to claim interface")
            return false
        }

        Log.d(TAG, "Claimed interface: ${iface.id}, endpoints: ${iface.endpointCount}")

        // Find the INTERRUPT IN endpoint (0x82)
        inEndpoint = findInterruptInEndpoint(iface)
        if (inEndpoint == null) {
            Log.e(TAG, "Failed to find INTERRUPT IN endpoint at 0x82")
            connection.releaseInterface(iface)
            return false
        }

        Log.d(TAG, "Found endpoint: address=0x${Integer.toHexString(inEndpoint!!.address)}, " +
                "type=${inEndpoint!!.type}, direction=${inEndpoint!!.direction}")

        isConnected = true

        // Start the read thread
        startReadThread()

        Log.i(TAG, "GC2 device opened successfully")
        return true
    }

    /**
     * Closes the USB connection and stops reading.
     */
    override fun close() {
        Log.d(TAG, "Closing GC2 device")

        isConnected = false

        // Stop the read thread
        readThread?.interrupt()
        readThread = null

        // Release the interface
        usbInterface?.let { iface ->
            connection.releaseInterface(iface)
        }
        usbInterface = null
        inEndpoint = null

        // Close the connection
        connection.close()

        Log.i(TAG, "GC2 device closed")
    }

    /**
     * Finds the INTERRUPT IN endpoint on the given interface.
     */
    private fun findInterruptInEndpoint(usbInterface: UsbInterface): UsbEndpoint? {
        for (i in 0 until usbInterface.endpointCount) {
            val endpoint = usbInterface.getEndpoint(i)

            // Check if this is an INTERRUPT IN endpoint
            // USB_ENDPOINT_XFER_INT = 3, USB_DIR_IN = 0x80
            val isInterrupt = endpoint.type == android.hardware.usb.UsbConstants.USB_ENDPOINT_XFER_INT
            val isIn = endpoint.direction == android.hardware.usb.UsbConstants.USB_DIR_IN

            Log.d(TAG, "Endpoint $i: address=0x${Integer.toHexString(endpoint.address)}, " +
                    "type=${endpoint.type}, direction=${endpoint.direction}, " +
                    "isInterrupt=$isInterrupt, isIn=$isIn")

            if (isInterrupt && isIn && endpoint.address == ENDPOINT_ADDRESS) {
                return endpoint
            }
        }
        return null
    }

    /**
     * Starts the background thread for reading USB data.
     */
    private fun startReadThread() {
        readThread = Thread({
            Log.d(TAG, "Read thread started")
            readLoop()
            Log.d(TAG, "Read thread finished")
        }, "GC2-ReadThread")

        readThread?.start()
    }

    /**
     * Main read loop for receiving data from the GC2.
     *
     * This runs on a background thread and performs interrupt transfers
     * to receive shot data and device status messages.
     */
    private fun readLoop() {
        val buffer = ByteArray(BUFFER_SIZE)
        val endpoint = inEndpoint ?: return

        while (isConnected && !Thread.currentThread().isInterrupted) {
            try {
                // Perform interrupt transfer
                val bytesRead = connection.bulkTransfer(
                    endpoint,
                    buffer,
                    BUFFER_SIZE,
                    TRANSFER_TIMEOUT_MS
                )

                if (bytesRead > 0) {
                    // Convert to string and process
                    val data = String(buffer, 0, bytesRead, Charsets.UTF_8)
                    processReceivedData(data)
                } else if (bytesRead < 0) {
                    // Error occurred - might be disconnect
                    if (isConnected) {
                        Log.w(TAG, "USB transfer error: $bytesRead")
                    }
                }
                // bytesRead == 0 means timeout, which is normal
            } catch (e: Exception) {
                if (isConnected) {
                    Log.e(TAG, "Read error: ${e.message}")
                }
            }
        }
    }

    /**
     * Processes received data from the GC2.
     *
     * @param data Raw string data from USB transfer
     */
    private fun processReceivedData(data: String) {
        // TODO: Implement full parsing in Prompt 24
        // This stub just logs the data

        Log.v(TAG, "Received: $data")

        // Forward to protocol parser
        protocol.processData(data) { messageType, jsonData ->
            when (messageType) {
                GC2Protocol.MessageType.SHOT -> {
                    Log.d(TAG, "Shot data received")
                    plugin.sendShotData(jsonData)
                }
                GC2Protocol.MessageType.STATUS -> {
                    Log.d(TAG, "Status data received")
                    plugin.sendDeviceStatus(jsonData)
                }
            }
        }
    }

    /**
     * Gets the device serial number.
     */
    fun getSerialNumber(): String? {
        return try {
            connection.serial
        } catch (e: Exception) {
            Log.w(TAG, "Failed to get serial number: ${e.message}")
            null
        }
    }
}
