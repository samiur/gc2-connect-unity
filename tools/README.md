# GC2 Connect Development Tools

Testing tools for debugging USB packet handling and GSPro integration.

## Tools

### gc2_simulator.py - GC2 Launch Monitor Simulator

Simulates the GC2's USB packet behavior over TCP:
- Sends packets in 64-byte chunks with realistic timing (1-2ms apart)
- Sends early readings (incomplete, no spin) then final readings (complete)
- Supports configurable timing to stress test packet handling

```bash
# Start simulator on port 5555
uv run tools/gc2_simulator.py

# With custom timing (faster packets = more stress)
uv run tools/gc2_simulator.py --packet-delay-ms 0.5

# Commands:
#   driver  - Fire a driver shot
#   7iron   - Fire a 7-iron shot
#   wedge   - Fire a wedge shot
#   status  - Send device status
#   burst N - Fire N shots rapidly
#   quit    - Exit
```

### mock_gspro_server.py - Mock GSPro Server

Simulates GSPro's Open Connect API:
- Listens on port 921
- Accepts JSON shot messages
- Logs received spin data (to verify it arrives correctly)
- Sends realistic response JSON

```bash
# Start mock GSPro on port 921
uv run tools/mock_gspro_server.py

# With custom response delay
uv run tools/mock_gspro_server.py --delay-ms 100
```

### packet_loss_test.py - Stress Test

Tests for packet loss with extreme timing:
- Sends packets with configurable timing (down to 0.1ms)
- Uses unique spin values per shot for tracking
- Reports statistics

```bash
# Run stress test (10 shots, 0.5ms between packets)
uv run tools/packet_loss_test.py --shots 10 --packet-delay-ms 0.5
```

## Testing Workflow

### 1. Test GC2 → Unity Flow (TCP simulation)

```bash
# Terminal 1: Start GC2 simulator
uv run tools/gc2_simulator.py

# Terminal 2: Start Unity
# - Open GC2 Test Window (OpenRange > GC2 Test Window)
# - Set Mode: Client
# - Set Host: localhost, Port: 5555
# - Click Connect

# In simulator, type: driver
# Watch Unity console for shot data
```

### 2. Test Unity → GSPro Flow

```bash
# Terminal 1: Start mock GSPro
uv run tools/mock_gspro_server.py

# Terminal 2: Start Unity
# - Toggle to GSPro mode
# - Enter Host: localhost, Port: 921
# - Click Connect

# Terminal 3: Start GC2 simulator
uv run tools/gc2_simulator.py

# Connect Unity to GC2 simulator (see above)
# Fire shots from simulator
# Watch mock GSPro for spin data - should NOT be 3500/0
```

### 3. Stress Test for Packet Loss

```bash
# Terminal 1: Start mock GSPro
uv run tools/mock_gspro_server.py

# Terminal 2: Start Unity in GSPro mode, connected to mock GSPro

# Terminal 3: Run stress test
uv run tools/packet_loss_test.py --shots 20 --packet-delay-ms 0.5

# Compare spin values in mock GSPro output to expected values
# If values are 3500/0 or missing, packet loss is occurring
```

## Expected Output

### Good (spin data arrives):
```
SHOT #1 RECEIVED
  Speed:      165.3 mph
  BackSpin:   2501 rpm    ← Unique value
  SideSpin:   -199 rpm    ← Unique value
  ✓ Spin data looks valid
```

### Bad (packet loss / 3500 issue):
```
SHOT #1 RECEIVED
  Speed:      165.3 mph
  BackSpin:   3500 rpm    ← GSPro default!
  SideSpin:   0 rpm       ← GSPro default!
  ⚠️  DEFAULT SPIN DETECTED (3500/0) - Spin data missing!
```

## Diagnosing the 3500 RPM Issue

If spin shows as 3500 in GSPro:

1. **Check native plugin logs** - Are BACK_RPM/SIDE_RPM in USB packets?
2. **Check GC2MacConnection logs** - Is spin in parsed JSON?
3. **Check GSProClient logs** - Is spin in outgoing JSON?

The issue is wherever spin data disappears in the pipeline.
