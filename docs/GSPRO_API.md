# GSPro Open Connect API v1 Specification
# GC2 Connect Unity

## Document Info
| Field | Value |
|-------|-------|
| Version | 1.1.0 |
| API Version | GSPro Open Connect v1 |
| Last Updated | January 2025 |

---

## 1. Overview

GSPro Open Connect is a TCP-based API that allows launch monitors to send shot data to GSPro golf simulation software. This document describes the protocol for integration with GC2 Connect Unity.

### Connection Details
```
Protocol: TCP
Default Port: 921
Format: JSON (no newline delimiter required)
Direction: Client (GC2 Connect) → Server (GSPro)
```

### Critical Implementation Notes

| Requirement | Details |
|-------------|---------|
| TCP_NODELAY | **CRITICAL**: Must set `NoDelay = true` to disable Nagle's algorithm. Without this, messages may be buffered and delayed. |
| Response Handling | Shot data gets responses; heartbeats and status updates do **NOT** get responses. Don't block waiting! |
| Graceful Shutdown | GSPro doesn't handle abrupt disconnections well. Always close sockets properly. |

---

## 2. Connection Flow

```
┌─────────────────┐                    ┌─────────────────┐
│  GC2 Connect    │                    │     GSPro       │
│  (Client)       │                    │    (Server)     │
└────────┬────────┘                    └────────┬────────┘
         │                                      │
         │  1. TCP Connect to port 921          │
         │─────────────────────────────────────>│
         │                                      │
         │  2. Connection established           │
         │<─────────────────────────────────────│
         │                                      │
         │  3. Send heartbeat (optional)        │
         │─────────────────────────────────────>│
         │                                      │
         │  4. Player hits shot on GC2          │
         │                                      │
         │  5. Send shot data (JSON)            │
         │─────────────────────────────────────>│
         │                                      │
         │  6. GSPro processes shot             │
         │                                      │
         │  7. Repeat for each shot             │
         │                                      │
```

---

## 3. Message Format

### 3.1 Shot Message Structure

```json
{
  "DeviceID": "GC2 Connect Unity",
  "Units": "Yards",
  "ShotNumber": 1,
  "APIversion": "1",
  "BallData": {
    "Speed": 150.5,
    "SpinAxis": 5.2,
    "TotalSpin": 2800.0,
    "BackSpin": 2750.0,
    "SideSpin": 450.0,
    "HLA": -2.5,
    "VLA": 12.3
  },
  "ClubData": {
    "Speed": 105.2,
    "AngleOfAttack": -3.5,
    "FaceToTarget": 1.2,
    "Lie": 0.5,
    "Loft": 28.5,
    "Path": 2.1,
    "SpeedAtImpact": 104.8,
    "VerticalFaceImpact": 0.3,
    "HorizontalFaceImpact": -0.1,
    "ClosureRate": 450.0
  },
  "ShotDataOptions": {
    "ContainsBallData": true,
    "ContainsClubData": true,
    "LaunchMonitorIsReady": true,
    "LaunchMonitorBallDetected": true,
    "IsHeartBeat": false
  }
}
```

### 3.2 Field Descriptions

#### Root Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| DeviceID | string | Yes | Identifier for the launch monitor |
| Units | string | Yes | "Yards" or "Meters" |
| ShotNumber | int | Yes | Sequential shot number (1-based) |
| APIversion | string | Yes | Always "1" for v1 API |
| BallData | object | Yes* | Ball flight data |
| ClubData | object | No | Club data (HMT only) |
| ShotDataOptions | object | Yes | Metadata about the shot |

#### BallData Fields

| Field | Type | Unit | Description |
|-------|------|------|-------------|
| Speed | float | mph | Ball speed off clubface |
| SpinAxis | float | degrees | Spin axis tilt (+ = fade/slice) |
| TotalSpin | float | rpm | Total spin rate |
| BackSpin | float | rpm | Backspin component |
| SideSpin | float | rpm | Sidespin component (+ = fade/slice) |
| HLA | float | degrees | Horizontal launch angle (+ = right) |
| VLA | float | degrees | Vertical launch angle (up from ground) |

#### ClubData Fields (HMT Only)

| Field | Type | Unit | Description |
|-------|------|------|-------------|
| Speed | float | mph | Club head speed |
| AngleOfAttack | float | degrees | Attack angle (+ = up) |
| FaceToTarget | float | degrees | Face angle at impact (+ = open) |
| Lie | float | degrees | Lie angle |
| Loft | float | degrees | Dynamic loft at impact |
| Path | float | degrees | Club path (+ = in-to-out) |
| SpeedAtImpact | float | mph | Speed at moment of impact |
| VerticalFaceImpact | float | inches | Impact point vertical |
| HorizontalFaceImpact | float | inches | Impact point horizontal |
| ClosureRate | float | deg/sec | Face closure rate |

#### ShotDataOptions Fields

| Field | Type | Description |
|-------|------|-------------|
| ContainsBallData | bool | True if BallData is present |
| ContainsClubData | bool | True if ClubData is present |
| LaunchMonitorIsReady | bool | Device is ready for shots |
| LaunchMonitorBallDetected | bool | Ball detected on tee |
| IsHeartBeat | bool | True if this is a heartbeat message |

---

## 4. Heartbeat Messages

Heartbeat messages keep the connection alive and report device status.

### 4.1 Heartbeat Format

```json
{
  "DeviceID": "GC2 Connect Unity",
  "Units": "Yards",
  "ShotNumber": 0,
  "APIversion": "1",
  "BallData": {},
  "ShotDataOptions": {
    "ContainsBallData": false,
    "ContainsClubData": false,
    "LaunchMonitorIsReady": true,
    "LaunchMonitorBallDetected": false,
    "IsHeartBeat": true
  }
}
```

### 4.2 Heartbeat Timing

- Send heartbeat every 1-2 seconds when no shots
- Skip heartbeat immediately before/after a shot
- GSPro may disconnect if no messages for ~10 seconds

---

## 5. Response Handling

### 5.1 Response Behavior by Message Type

| Message Type | Response Behavior |
|--------------|-------------------|
| Shot Data | Responds with status code (200, 201, 5xx) |
| Heartbeat | **No response** - do not wait! |
| Status Update | **No response** - do not wait! |

**Critical:** If you wait for a response to heartbeat or status messages, your connection will hang or timeout.

### 5.2 Buffer Management

GSPro may buffer responses and send them together, causing JSON parsing errors:

```
{"Code":200,"Message":"OK"}{"Code":200,"Message":"OK"}
```

**Solutions:**
1. Clear the receive buffer before sending shot data
2. Parse only the first JSON object from responses
3. Use a JSON decoder that handles partial reads

### 5.3 Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Success with player info |
| 5xx | Error |

### 5.4 Response with Player Info

GSPro sends player information in responses:

```json
{
    "Code": 201,
    "Message": "Player info",
    "Player": {
        "Handed": "RH",
        "Club": "DR",
        "DistanceToTarget": 450
    }
}
```

---

## 6. C# Implementation

### 6.1 GSProClient Class

```csharp
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace OpenRange.Network
{
    public class GSProClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _heartbeatCts;
        private int _shotNumber = 0;
        private bool _isConnected;
        
        public bool IsConnected => _isConnected && _client?.Connected == true;
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        public async Task<bool> ConnectAsync(string host, int port = 921)
        {
            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;  // Disable Nagle for lower latency
                
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _isConnected = true;
                _shotNumber = 0;
                
                // Start heartbeat
                _heartbeatCts = new CancellationTokenSource();
                _ = HeartbeatLoop(_heartbeatCts.Token);
                
                OnConnected?.Invoke();
                Debug.Log($"Connected to GSPro at {host}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }
        
        public void Disconnect()
        {
            _heartbeatCts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _isConnected = false;
            OnDisconnected?.Invoke();
        }
        
        public async Task SendShot(GC2ShotData shot)
        {
            if (!IsConnected) return;
            
            _shotNumber++;
            
            var message = new GSProMessage
            {
                DeviceID = "GC2 Connect Unity",
                Units = "Yards",
                ShotNumber = _shotNumber,
                APIversion = "1",
                BallData = new GSProBallData
                {
                    Speed = shot.BallSpeed,
                    SpinAxis = shot.SpinAxis,
                    TotalSpin = shot.TotalSpin,
                    BackSpin = shot.BackSpin,
                    SideSpin = shot.SideSpin,
                    HLA = shot.Direction,
                    VLA = shot.LaunchAngle
                },
                ShotDataOptions = new GSProShotOptions
                {
                    ContainsBallData = true,
                    ContainsClubData = shot.HasClubData,
                    LaunchMonitorIsReady = true,
                    LaunchMonitorBallDetected = true,
                    IsHeartBeat = false
                }
            };
            
            if (shot.HasClubData)
            {
                message.ClubData = new GSProClubData
                {
                    Speed = shot.ClubSpeed,
                    AngleOfAttack = shot.AttackAngle,
                    FaceToTarget = shot.FaceToTarget,
                    Lie = shot.Lie,
                    Loft = shot.DynamicLoft,
                    Path = shot.Path
                };
            }
            
            await SendMessage(message);
        }
        
        private async Task SendHeartbeat()
        {
            var message = new GSProMessage
            {
                DeviceID = "GC2 Connect Unity",
                Units = "Yards",
                ShotNumber = 0,
                APIversion = "1",
                BallData = new GSProBallData(),
                ShotDataOptions = new GSProShotOptions
                {
                    ContainsBallData = false,
                    ContainsClubData = false,
                    LaunchMonitorIsReady = true,
                    LaunchMonitorBallDetected = false,
                    IsHeartBeat = true
                }
            };
            
            await SendMessage(message);
        }
        
        private async Task SendMessage(GSProMessage message)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send error: {ex.Message}");
                Disconnect();
            }
        }
        
        private async Task HeartbeatLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                try
                {
                    await Task.Delay(2000, ct);
                    await SendHeartbeat();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        
        public void Dispose()
        {
            Disconnect();
        }
    }
}
```

### 6.2 Message Classes

```csharp
namespace OpenRange.Network
{
    [System.Serializable]
    public class GSProMessage
    {
        public string DeviceID;
        public string Units;
        public int ShotNumber;
        public string APIversion;
        public GSProBallData BallData;
        public GSProClubData ClubData;
        public GSProShotOptions ShotDataOptions;
    }
    
    [System.Serializable]
    public class GSProBallData
    {
        public float Speed;
        public float SpinAxis;
        public float TotalSpin;
        public float BackSpin;
        public float SideSpin;
        public float HLA;
        public float VLA;
    }
    
    [System.Serializable]
    public class GSProClubData
    {
        public float Speed;
        public float AngleOfAttack;
        public float FaceToTarget;
        public float Lie;
        public float Loft;
        public float Path;
        public float SpeedAtImpact;
        public float VerticalFaceImpact;
        public float HorizontalFaceImpact;
        public float ClosureRate;
    }
    
    [System.Serializable]
    public class GSProShotOptions
    {
        public bool ContainsBallData;
        public bool ContainsClubData;
        public bool LaunchMonitorIsReady;
        public bool LaunchMonitorBallDetected;
        public bool IsHeartBeat;
    }
}
```

---

## 7. Example Messages

### 7.1 Driver Shot (Ball Data Only)

```json
{
  "DeviceID": "GC2 Connect Unity",
  "Units": "Yards",
  "ShotNumber": 1,
  "APIversion": "1",
  "BallData": {
    "Speed": 167.0,
    "SpinAxis": -3.5,
    "TotalSpin": 2686.0,
    "BackSpin": 2680.0,
    "SideSpin": -164.0,
    "HLA": -1.2,
    "VLA": 10.9
  },
  "ShotDataOptions": {
    "ContainsBallData": true,
    "ContainsClubData": false,
    "LaunchMonitorIsReady": true,
    "LaunchMonitorBallDetected": true,
    "IsHeartBeat": false
  }
}
```

### 7.2 7-Iron Shot (With HMT Club Data)

```json
{
  "DeviceID": "GC2 Connect Unity",
  "Units": "Yards",
  "ShotNumber": 5,
  "APIversion": "1",
  "BallData": {
    "Speed": 120.0,
    "SpinAxis": 8.2,
    "TotalSpin": 7097.0,
    "BackSpin": 7020.0,
    "SideSpin": 1012.0,
    "HLA": 2.1,
    "VLA": 16.3
  },
  "ClubData": {
    "Speed": 87.5,
    "AngleOfAttack": -4.2,
    "FaceToTarget": 0.8,
    "Lie": 1.2,
    "Loft": 31.5,
    "Path": -2.1
  },
  "ShotDataOptions": {
    "ContainsBallData": true,
    "ContainsClubData": true,
    "LaunchMonitorIsReady": true,
    "LaunchMonitorBallDetected": true,
    "IsHeartBeat": false
  }
}
```

### 7.3 Heartbeat

```json
{
  "DeviceID": "GC2 Connect Unity",
  "Units": "Yards",
  "ShotNumber": 0,
  "APIversion": "1",
  "BallData": {},
  "ShotDataOptions": {
    "ContainsBallData": false,
    "ContainsClubData": false,
    "LaunchMonitorIsReady": true,
    "LaunchMonitorBallDetected": false,
    "IsHeartBeat": true
  }
}
```

---

## 8. Error Handling

### 8.1 Connection Errors

| Error | Cause | Action |
|-------|-------|--------|
| Connection refused | GSPro not running or wrong port | Show "Start GSPro and enable Open Connect" |
| Connection timeout | Network issue or firewall | Check network, show retry option |
| Connection reset | GSPro closed | Auto-reconnect after delay |

### 8.2 Reconnection Strategy

```csharp
private async Task ReconnectLoop()
{
    int attempt = 0;
    int[] delays = { 1000, 2000, 5000, 10000 };  // Backoff
    
    while (!_isConnected && attempt < 10)
    {
        int delay = delays[Math.Min(attempt, delays.Length - 1)];
        await Task.Delay(delay);
        
        if (await ConnectAsync(_lastHost, _lastPort))
        {
            return;  // Success
        }
        
        attempt++;
    }
    
    OnError?.Invoke("Failed to reconnect after 10 attempts");
}
```

---

## 9. Testing

### 9.1 Test with netcat

```bash
# Start a simple TCP server to see messages
nc -l 921

# GC2 Connect Unity should connect and send JSON
```

### 9.2 Test Shot Script

```csharp
[ContextMenu("Send Test Shot")]
public void SendTestShot()
{
    var testShot = new GC2ShotData
    {
        BallSpeed = 150f,
        LaunchAngle = 12f,
        Direction = 0f,
        TotalSpin = 3000f,
        BackSpin = 2900f,
        SideSpin = 500f,
        SpinAxis = 10f
    };
    
    _gsproClient.SendShot(testShot);
}
```

---

## 10. GSPro Configuration

### 10.1 Enable Open Connect in GSPro

1. Open GSPro
2. Go to Settings → Launch Monitor
3. Select "Open Connect" from the dropdown
4. Note the port (default: 921)
5. Ensure "Enable Open Connect" is checked

### 10.2 Firewall Rules

If connecting from a different machine:
- Allow inbound TCP on port 921
- Both Windows Firewall and any third-party firewall

---

## 11. Common Pitfalls

### 11.1 Waiting for Heartbeat Responses

**Problem:** Connection hangs or times out
**Cause:** Waiting for response to heartbeat/status messages
**Solution:** Don't await responses for non-shot messages

### 11.2 Buffered/Concatenated Responses

**Problem:** "Invalid JSON" or "Extra data" parsing errors
**Cause:** Multiple responses buffered together
**Solution:** Clear buffer before sending, parse only first JSON object

### 11.3 Nagle's Algorithm Delays

**Problem:** Messages appear delayed or batched together
**Cause:** TCP Nagle's algorithm buffering small packets
**Solution:** Set `NoDelay = true` (TCP_NODELAY socket option)

### 11.4 Abrupt Disconnection

**Problem:** GSPro shows "disconnected" state incorrectly after app closes
**Cause:** Socket closed without proper cleanup
**Solution:** Implement graceful shutdown, register cleanup handlers (OnApplicationQuit, etc.)

### 11.5 Unit Confusion

**Problem:** Values displayed incorrectly in GSPro
**Cause:** Wrong unit assumptions (m/s vs mph)
**Solution:** Always send ball speed in mph (not m/s)

**Note:** The GSPro Open Connect debug window may display incorrect values (appears to apply a unit conversion), but the actual simulator uses values correctly.

---

## 12. Testing Checklist

Before deploying, verify:

- [ ] Connection establishes successfully
- [ ] Initial heartbeat sent on connect
- [ ] Shot data receives 200 response
- [ ] Heartbeats don't block waiting for response
- [ ] Status updates don't block waiting for response
- [ ] Buffered responses handled correctly
- [ ] Graceful shutdown disconnects properly
- [ ] Ball speed displays correctly in GSPro

---

## 13. References

### Official Resources
- [GSPro Official Documentation](https://gsprogolf.com/openconnect)
- [GSPro Discord - Open Connect Channel](https://discord.gg/gspro)

### Reference Implementations
These implementations were studied for protocol details:
- [MLM2PRO-GSPro-Connector](https://github.com/springbok/MLM2PRO-GSPro-Connector)
- [gspro-connect](https://github.com/joebockhorst/gspro-connect)

---

## Summary

Building a reliable GSPro connector requires attention to:

1. **Socket configuration**: Set `NoDelay = true` (TCP_NODELAY) and proper timeouts
2. **Response handling**: Don't wait for heartbeat/status responses - they don't reply
3. **Buffer management**: Clear stale data before sending, parse first JSON object only
4. **Shutdown handling**: Register cleanup handlers for graceful disconnect
5. **Units**: Send all speeds in mph
