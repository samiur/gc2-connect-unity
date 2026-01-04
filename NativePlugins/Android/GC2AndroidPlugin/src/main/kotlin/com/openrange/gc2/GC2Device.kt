// ABOUTME: USB device wrapper for GC2 communication on Android.
// ABOUTME: Uses async UsbRequest for continuous packet reading without loss.

package com.openrange.gc2

import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbEndpoint
import android.hardware.usb.UsbInterface
import android.hardware.usb.UsbRequest
import android.util.Log
import java.io.Closeable
import java.nio.ByteBuffer
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Wrapper class for USB communication with the GC2 device.
 *
 * This class handles the low-level USB operations including:
 * - Interface claiming
 * - Endpoint discovery (INTERRUPT IN at 0x82)
 * - Async data reading via UsbRequest (no packet loss)
 * - Protocol parsing via [GC2Protocol]
 *
 * ## Async USB Architecture
 *
 * The GC2 sends packets in rapid succession (~1-2ms apart), with 4+ packets
 * per shot burst. Synchronous reads can't keep up, causing kernel buffer
 * overflow and packet loss.
 *
 * Solution: Use Android's async UsbRequest API with multiple queued requests:
 * 1. Queue [NUM_USB_REQUESTS] requests at startup
 * 2. When one completes, copy data to processing queue, immediately re-queue
 * 3. Separate thread processes the queue through GC2Protocol
 *
 * This mirrors the async libusb pattern used in the macOS plugin.
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

        /** Buffer size for USB reads (64 bytes for interrupt transfers) */
        const val BUFFER_SIZE = 64

        /**
         * Number of async USB requests to keep queued.
         * With 4 requests queued, we can handle bursts of packets without loss.
         * Matches the macOS libusb implementation.
         */
        const val NUM_USB_REQUESTS = 4
    }

    private var usbInterface: UsbInterface? = null
    private var inEndpoint: UsbEndpoint? = null

    // Async USB request handling
    private val usbRequests = mutableListOf<UsbRequest>()
    private val requestBuffers = mutableMapOf<UsbRequest, ByteBuffer>()
    private var usbReaderThread: Thread? = null
    private var processorThread: Thread? = null

    // Thread-safe queue for passing data from USB reader to processor
    private val packetQueue = ConcurrentLinkedQueue<String>()

    // Atomic flag for clean shutdown
    private val running = AtomicBoolean(false)

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

        // Initialize async USB requests
        if (!initializeUsbRequests()) {
            Log.e(TAG, "Failed to initialize USB requests")
            connection.releaseInterface(iface)
            return false
        }

        isConnected = true
        running.set(true)

        // Start the reader and processor threads
        startThreads()

        Log.i(TAG, "GC2 device opened successfully with $NUM_USB_REQUESTS async requests")
        return true
    }

    /**
     * Closes the USB connection and stops reading.
     */
    override fun close() {
        Log.d(TAG, "Closing GC2 device")

        isConnected = false
        running.set(false)

        // Stop threads
        usbReaderThread?.interrupt()
        processorThread?.interrupt()

        // Wait for threads to finish (with timeout)
        try {
            usbReaderThread?.join(1000)
            processorThread?.join(1000)
        } catch (e: InterruptedException) {
            Log.w(TAG, "Interrupted while waiting for threads to stop")
        }

        usbReaderThread = null
        processorThread = null

        // Cancel and close all USB requests
        for (request in usbRequests) {
            try {
                request.cancel()
                request.close()
            } catch (e: Exception) {
                Log.w(TAG, "Error closing USB request: ${e.message}")
            }
        }
        usbRequests.clear()
        requestBuffers.clear()

        // Clear the packet queue
        packetQueue.clear()

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
     * Initializes async USB requests with buffers.
     *
     * Creates [NUM_USB_REQUESTS] UsbRequest objects, each with its own ByteBuffer.
     * These will be queued for continuous reading.
     */
    private fun initializeUsbRequests(): Boolean {
        val endpoint = inEndpoint ?: return false

        for (i in 0 until NUM_USB_REQUESTS) {
            val request = UsbRequest()

            if (!request.initialize(connection, endpoint)) {
                Log.e(TAG, "Failed to initialize USB request $i")
                // Clean up already created requests
                for (req in usbRequests) {
                    req.close()
                }
                usbRequests.clear()
                requestBuffers.clear()
                return false
            }

            // Allocate a direct ByteBuffer for this request
            val buffer = ByteBuffer.allocateDirect(BUFFER_SIZE)
            requestBuffers[request] = buffer
            usbRequests.add(request)

            Log.d(TAG, "Initialized USB request $i")
        }

        return true
    }

    /**
     * Starts the USB reader and packet processor threads.
     */
    private fun startThreads() {
        // Start the processor thread first so it's ready for data
        processorThread = Thread({
            Log.d(TAG, "Processor thread started")
            processorLoop()
            Log.d(TAG, "Processor thread finished")
        }, "GC2-Processor")
        processorThread?.start()

        // Start the USB reader thread
        usbReaderThread = Thread({
            Log.d(TAG, "USB reader thread started")
            usbReaderLoop()
            Log.d(TAG, "USB reader thread finished")
        }, "GC2-UsbReader")
        usbReaderThread?.start()
    }

    /**
     * USB reader loop using async UsbRequest.
     *
     * This thread:
     * 1. Queues all USB requests at startup
     * 2. Waits for any request to complete
     * 3. Copies data to the packet queue
     * 4. Immediately re-queues the request
     *
     * The key is that we always have multiple requests queued, so packets
     * arriving in rapid succession are captured without loss.
     */
    private fun usbReaderLoop() {
        // Queue all requests initially
        for (request in usbRequests) {
            val buffer = requestBuffers[request] ?: continue
            buffer.clear()
            buffer.limit(BUFFER_SIZE)  // Set limit for queue()
            if (!request.queue(buffer)) {
                Log.e(TAG, "Failed to queue initial USB request")
            }
        }

        Log.d(TAG, "Queued $NUM_USB_REQUESTS initial USB requests")

        while (running.get() && !Thread.currentThread().isInterrupted) {
            try {
                // Wait for any request to complete (blocking)
                val completedRequest = connection.requestWait()

                if (completedRequest == null) {
                    if (running.get()) {
                        Log.w(TAG, "requestWait returned null")
                    }
                    continue
                }

                // Get the buffer for this request
                val buffer = requestBuffers[completedRequest]
                if (buffer == null) {
                    Log.w(TAG, "No buffer found for completed request")
                    continue
                }

                // Read the data from the buffer
                val bytesRead = buffer.position()
                if (bytesRead > 0) {
                    // Convert to string
                    buffer.flip()
                    val bytes = ByteArray(bytesRead)
                    buffer.get(bytes)
                    val data = String(bytes, Charsets.UTF_8)

                    // Add to processing queue (non-blocking)
                    packetQueue.offer(data)

                    Log.v(TAG, "Received $bytesRead bytes, queue size: ${packetQueue.size}")
                }

                // Immediately re-queue the request for the next read
                buffer.clear()
                buffer.limit(BUFFER_SIZE)  // Set limit for queue()
                if (!completedRequest.queue(buffer)) {
                    if (running.get()) {
                        Log.e(TAG, "Failed to re-queue USB request")
                    }
                }
            } catch (e: Exception) {
                if (running.get()) {
                    Log.e(TAG, "USB reader error: ${e.message}")
                }
            }
        }

        Log.d(TAG, "USB reader loop exiting")
    }

    /**
     * Processor loop for handling received packets.
     *
     * This thread pulls packets from the queue and processes them through
     * the GC2Protocol parser. Runs independently of USB reading so we never
     * block packet reception.
     */
    private fun processorLoop() {
        while (running.get() && !Thread.currentThread().isInterrupted) {
            try {
                // Poll the queue (non-blocking)
                val data = packetQueue.poll()

                if (data != null) {
                    processReceivedData(data)
                } else {
                    // No data available, sleep briefly to avoid busy-waiting
                    Thread.sleep(1)
                }
            } catch (e: InterruptedException) {
                // Thread interrupted, exit gracefully
                break
            } catch (e: Exception) {
                if (running.get()) {
                    Log.e(TAG, "Processor error: ${e.message}")
                }
            }
        }

        // Process any remaining packets in the queue before exiting
        while (true) {
            val data = packetQueue.poll() ?: break
            try {
                processReceivedData(data)
            } catch (e: Exception) {
                Log.e(TAG, "Error processing remaining packet: ${e.message}")
            }
        }

        Log.d(TAG, "Processor loop exiting")
    }

    /**
     * Processes received data from the GC2.
     *
     * @param data Raw string data from USB transfer
     */
    private fun processReceivedData(data: String) {
        Log.v(TAG, "Processing: $data")

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
