# GC2 USB Protocol Specification
# GC2 Connect Unity

## Document Info
| Field | Value |
|-------|-------|
| Version | 1.0.0 |
| Device | Foresight GC2 Launch Monitor |
| Last Updated | December 2024 |

---

## 1. Overview

The Foresight GC2 communicates via USB using a simple text-based protocol. This document describes the USB interface and data format for reading shot data from the GC2.

---

## 2. USB Device Identification

### 2.1 Device Descriptors

```
Vendor ID:  0x2C79 (11385 decimal)
Product ID: 0x0110 (272 decimal)
Device Name: "Foresight GC2"
```

### 2.2 USB Configuration

```
Configuration: 1
Interface: 0
Endpoints:
  - EP 0x81: Bulk IN (device → host)
  - EP 0x02: Bulk OUT (host → device)
Max Packet Size: 64 bytes
```

---

## 3. Communication Protocol

### 3.1 Connection Sequence

```
1. Enumerate USB devices
2. Find device with VID=0x2C79, PID=0x0110
3. Open device
4. Claim interface 0
5. Begin reading from EP 0x81 (bulk transfers)
6. Parse incoming data for shot information
```

### 3.2 Data Flow

```
┌─────────────┐                    ┌─────────────┐
│    GC2      │                    │    Host     │
│  (Device)   │                    │ (Computer)  │
└──────┬──────┘                    └──────┬──────┘
       │                                  │
       │  [Shot detected by GC2]          │
       │                                  │
       │  Bulk IN: Shot data (text)       │
       │─────────────────────────────────>│
       │                                  │
       │  (May span multiple transfers)   │
       │─────────────────────────────────>│
       │                                  │
       │  (Terminated by double newline)  │
       │─────────────────────────────────>│
       │                                  │
```

### 3.3 Reading Strategy

```csharp
// Continuous read loop
while (connected)
{
    byte[] buffer = new byte[512];
    int bytesRead = BulkTransfer(EP_IN, buffer, timeout: 100ms);
    
    if (bytesRead > 0)
    {
        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        _buffer.Append(data);
        ProcessBuffer();
    }
}

void ProcessBuffer()
{
    // Look for message delimiter (double newline)
    int idx;
    while ((idx = _buffer.IndexOf("\n\n")) >= 0)
    {
        string message = _buffer.Substring(0, idx);
        _buffer.Remove(0, idx + 2);
        
        GC2ShotData shot = ParseMessage(message);
        if (shot != null)
        {
            OnShotReceived?.Invoke(shot);
        }
    }
}
```

---

## 4. Data Format

### 4.1 Message Structure

The GC2 sends shot data as key-value pairs, one per line:

```
KEY=VALUE
KEY=VALUE
...

```

Messages are terminated by a double newline (`\n\n`).

### 4.2 Ball Data Fields

| Key | Description | Unit | Example |
|-----|-------------|------|---------|
| SHOT_ID | Shot sequence number | - | 1 |
| SPEED_MPH | Ball speed | mph | 156.3 |
| ELEVATION_DEG | Vertical launch angle | degrees | 12.5 |
| AZIMUTH_DEG | Horizontal launch angle | degrees | -2.1 |
| SPIN_RPM | Total spin rate | rpm | 2850 |
| BACK_RPM | Backspin component | rpm | 2780 |
| SIDE_RPM | Sidespin component | rpm | 450 |
| SPIN_AXIS_DEG | Spin axis tilt | degrees | 8.5 |

### 4.3 Club Data Fields (HMT Only)

| Key | Description | Unit | Example |
|-----|-------------|------|---------|
| HMT | HMT data present flag | 0/1 | 1 |
| CLUBSPEED_MPH | Club head speed | mph | 105.2 |
| HPATH_DEG | Club path (horizontal) | degrees | 2.1 |
| VPATH_DEG | Attack angle (vertical) | degrees | -3.5 |
| FACE_T_DEG | Face to target angle | degrees | 1.2 |
| LOFT_DEG | Dynamic loft | degrees | 28.5 |
| LIE_DEG | Lie angle | degrees | 0.5 |

### 4.4 Example Messages

#### Ball Data Only (No HMT)

```
SHOT_ID=1
SPEED_MPH=156.3
ELEVATION_DEG=12.5
AZIMUTH_DEG=-2.1
SPIN_RPM=2850
BACK_RPM=2780
SIDE_RPM=450
SPIN_AXIS_DEG=8.5
HMT=0

```

#### With HMT Club Data

```
SHOT_ID=5
SPEED_MPH=120.5
ELEVATION_DEG=16.2
AZIMUTH_DEG=1.8
SPIN_RPM=7200
BACK_RPM=7100
SIDE_RPM=850
SPIN_AXIS_DEG=6.8
HMT=1
CLUBSPEED_MPH=87.5
HPATH_DEG=-2.1
VPATH_DEG=-4.2
FACE_T_DEG=0.8
LOFT_DEG=31.5
LIE_DEG=1.2

```

---

## 5. Protocol Parser

### 5.1 C# Implementation

```csharp
namespace OpenRange.GC2
{
    public static class GC2Protocol
    {
        /// <summary>
        /// Parse a GC2 shot message into structured data
        /// </summary>
        public static GC2ShotData Parse(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            
            var values = new Dictionary<string, string>();
            
            // Parse key=value pairs
            var lines = message.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                var parts = trimmed.Split('=');
                if (parts.Length == 2)
                {
                    values[parts[0]] = parts[1];
                }
            }
            
            // Validate required fields
            if (!values.ContainsKey("SPEED_MPH"))
                return null;
            
            // Build shot data
            var shot = new GC2ShotData
            {
                ShotId = GetInt(values, "SHOT_ID", 0),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                
                // Ball data
                BallSpeed = GetFloat(values, "SPEED_MPH"),
                LaunchAngle = GetFloat(values, "ELEVATION_DEG"),
                Direction = GetFloat(values, "AZIMUTH_DEG"),
                TotalSpin = GetFloat(values, "SPIN_RPM"),
                BackSpin = GetFloat(values, "BACK_RPM"),
                SideSpin = GetFloat(values, "SIDE_RPM"),
                SpinAxis = GetFloat(values, "SPIN_AXIS_DEG"),
                
                // HMT data
                HasClubData = GetInt(values, "HMT", 0) == 1
            };
            
            if (shot.HasClubData)
            {
                shot.ClubSpeed = GetFloat(values, "CLUBSPEED_MPH");
                shot.Path = GetFloat(values, "HPATH_DEG");
                shot.AttackAngle = GetFloat(values, "VPATH_DEG");
                shot.FaceToTarget = GetFloat(values, "FACE_T_DEG");
                shot.DynamicLoft = GetFloat(values, "LOFT_DEG");
                shot.Lie = GetFloat(values, "LIE_DEG");
            }
            
            // Handle spin calculation fallback
            if (shot.BackSpin == 0 && shot.SideSpin == 0 && shot.TotalSpin > 0)
            {
                // Calculate from TotalSpin and SpinAxis
                float axisRad = shot.SpinAxis * Mathf.Deg2Rad;
                shot.BackSpin = shot.TotalSpin * Mathf.Cos(axisRad);
                shot.SideSpin = shot.TotalSpin * Mathf.Sin(axisRad);
            }
            
            return shot;
        }
        
        private static float GetFloat(Dictionary<string, string> values, string key, float defaultValue = 0f)
        {
            if (values.TryGetValue(key, out var str) && float.TryParse(str, out var value))
                return value;
            return defaultValue;
        }
        
        private static int GetInt(Dictionary<string, string> values, string key, int defaultValue = 0)
        {
            if (values.TryGetValue(key, out var str) && int.TryParse(str, out var value))
                return value;
            return defaultValue;
        }
    }
}
```

---

## 6. Sign Conventions

### 6.1 Angles

| Measurement | Positive | Negative |
|-------------|----------|----------|
| AZIMUTH_DEG (HLA) | Right of target | Left of target |
| ELEVATION_DEG (VLA) | Upward | N/A (always positive) |
| SPIN_AXIS_DEG | Fade/slice | Draw/hook |
| SIDE_RPM | Fade/slice | Draw/hook |
| HPATH_DEG (Path) | In-to-out | Out-to-in |
| VPATH_DEG (AoA) | Upward | Downward |
| FACE_T_DEG | Open | Closed |

### 6.2 Spin Direction

```
Backspin: Creates lift (ball rises then falls)
          Higher = higher trajectory, more stopping power

Sidespin: Creates lateral curve
          Positive = ball curves right (fade/slice for RH)
          Negative = ball curves left (draw/hook for RH)
```

---

## 7. Data Validation

### 7.1 Valid Ranges

| Field | Min | Max | Notes |
|-------|-----|-----|-------|
| BallSpeed | 10 | 220 | mph |
| LaunchAngle | -10 | 60 | degrees |
| Direction | -45 | 45 | degrees |
| TotalSpin | 0 | 15000 | rpm |
| BackSpin | 0 | 15000 | rpm |
| SideSpin | -5000 | 5000 | rpm |
| ClubSpeed | 10 | 150 | mph (HMT) |

### 7.2 Misread Detection

```csharp
public static bool IsValidShot(GC2ShotData shot)
{
    // Speed sanity check
    if (shot.BallSpeed < 10 || shot.BallSpeed > 220)
        return false;
    
    // Launch angle sanity
    if (shot.LaunchAngle < -10 || shot.LaunchAngle > 60)
        return false;
    
    // Zero spin with high speed is usually a misread
    if (shot.BallSpeed > 80 && shot.TotalSpin < 100)
        return false;
    
    // Spin axis must be reasonable
    if (Mathf.Abs(shot.SpinAxis) > 90)
        return false;
    
    return true;
}
```

---

## 8. Platform-Specific USB Access

### 8.1 macOS (libusb)

```c
// Initialize
libusb_init(&ctx);

// Find device
handle = libusb_open_device_with_vid_pid(ctx, 0x2C79, 0x0110);

// Claim interface
libusb_claim_interface(handle, 0);

// Read
int transferred;
libusb_bulk_transfer(handle, 0x81, buffer, sizeof(buffer), &transferred, timeout);
```

### 8.2 iPad (DriverKit)

```swift
// Match device in Info.plist
<key>idVendor</key>
<integer>11385</integer>
<key>idProduct</key>
<integer>272</integer>

// Read via IOUserClient
let data = try connection.read(timeout: 100)
```

### 8.3 Android (USB Host API)

```kotlin
// Find device
val device = usbManager.deviceList.values.find {
    it.vendorId == 0x2C79 && it.productId == 0x0110
}

// Open and claim
val connection = usbManager.openDevice(device)
connection.claimInterface(device.getInterface(0), true)

// Find bulk IN endpoint
val endpoint = device.getInterface(0).getEndpoint(0)  // EP 0x81

// Read
val buffer = ByteArray(512)
val bytesRead = connection.bulkTransfer(endpoint, buffer, buffer.size, 100)
```

---

## 9. Troubleshooting

### 9.1 Device Not Found

| Cause | Solution |
|-------|----------|
| GC2 not powered on | Turn on GC2 |
| USB cable issue | Try different cable |
| USB port issue | Try different port |
| Driver conflict | Check for other GC2 software |

### 9.2 No Data Received

| Cause | Solution |
|-------|----------|
| No ball on tee | Place ball on GC2 |
| GC2 in standby | Wake GC2 (move ball) |
| Wrong endpoint | Verify EP 0x81 |
| Interface not claimed | Claim interface 0 |

### 9.3 Garbled Data

| Cause | Solution |
|-------|----------|
| Buffer overflow | Increase buffer size |
| Encoding issue | Use UTF-8 |
| Partial reads | Accumulate until \n\n |

---

## 10. USB Debugging

### 10.1 macOS

```bash
# List USB devices
system_profiler SPUSBDataType | grep -A5 "GC2\|2C79"

# USB logging
sudo log stream --predicate 'subsystem == "com.apple.usb"'
```

### 10.2 Linux

```bash
# List USB devices
lsusb | grep 2c79

# Detailed info
lsusb -v -d 2c79:0110

# USB traffic (requires root)
sudo usbmon
```

### 10.3 Android

```bash
# ADB shell
adb shell lsusb

# Logcat USB events
adb logcat | grep -i usb
```

---

## 11. References

- Foresight Sports GC2 User Manual
- USB 2.0 Specification
- libusb Documentation
- Apple DriverKit Documentation
- Android USB Host Documentation
