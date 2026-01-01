# Quick Start Guide - Running OpenRange in Development

## Prerequisites
- Unity 6000.3.2f1 installed via Unity Hub
- macOS (Intel or Apple Silicon)

## First-Time Setup

### Step 0: Configure URP Render Pipeline (Critical!)

**If materials appear pink/magenta, this step was missed.**

1. Open **Edit > Project Settings > Graphics**
2. Find **"Default Render Pipeline"** at the top
3. Click the circle picker (⊙) and select a URP asset:
   - If available: `URP-HighQuality` or `URP-MediumQuality`
   - If none exist: Run **OpenRange > Create URP Quality Assets** first, then return here

### Step 1: Create Ball Tag

1. Open **Edit > Project Settings > Tags and Layers**
2. Expand **Tags** section
3. Click **+** and add a tag named `Ball`

### Step 2: Generate Prefabs

Run these menu commands in Unity:

1. **OpenRange > Create Golf Ball Prefab**
2. **OpenRange > Create Trajectory Line Prefab**
3. **OpenRange > Create Camera Rig Prefab**

### Step 3: Generate Scenes

Run this menu command:

- **OpenRange > Generate All Scenes**

This creates:
- `Assets/Scenes/Bootstrap.unity` - Initialization scene
- `Assets/Scenes/MainMenu.unity` - Title screen
- `Assets/Scenes/Ranges/Marina.unity` - Main driving range (includes GolfBall, TrajectoryLine, CameraRig)

### Step 4: Configure Build Settings

Run this menu command:

- **OpenRange > Update Build Settings**

This sets the correct scene order for builds.

---

## Running the App

### Option A: Full App Flow (Recommended)
1. Open `Assets/Scenes/Bootstrap.unity`
2. Press **Play**
3. App loads MainMenu automatically
4. Click **"Open Range"** to enter Marina driving range

### Option B: Direct to Range (Development)
1. Open `Assets/Scenes/Ranges/Marina.unity`
2. Press **Play**
3. Open **OpenRange > Test Shot Window**
4. Select a preset (Driver, 7-Iron, Wedge) and click **Fire Test Shot**

---

## Test Shot Window Controls

| Control | Description |
|---------|-------------|
| Preset dropdown | Quick selection: Driver, 7-Iron, Wedge, Hook, Slice |
| Ball Speed | 50-200 mph |
| Launch Angle | 0-45 degrees |
| Azimuth | -20 to +20 degrees (left/right) |
| Back Spin | 0-12000 rpm |
| Side Spin | -3000 to +3000 rpm |
| Fire Test Shot | Sends shot through full pipeline |
| Reset Ball | Returns ball to tee position |

---

## Expected Behavior

When you fire a test shot:
1. ShotProcessor validates the shot data
2. TrajectorySimulator calculates the ball flight (Nathan model physics)
3. BallController animates the ball through the trajectory
4. TrajectoryRenderer draws the flight path (white-to-cyan gradient)
5. CameraController follows the ball (auto-switches to Follow mode)
6. Ball lands, rolls, and stops at final position

Console will log:
- Shot validation status
- Carry distance, total distance, apex height
- Flight time and landing position

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| All materials pink/magenta | URP not configured - see Step 0 above |
| "Tag: Ball is not defined" | Create "Ball" tag - see Step 1 above |
| "Scene couldn't be loaded" error | Run **OpenRange > Update Build Settings** |
| "Enter Play Mode to fire test shots" | Press Play before using Test Shot Window |
| "No ShotProcessor found" | Start from Bootstrap.unity, or regenerate Marina scene |
| Ball doesn't move | Check Console for errors; ensure prefabs are generated |
| No trajectory line visible | Run **OpenRange > Create Trajectory Line Prefab**, then regenerate Marina |
| Camera doesn't follow ball | Camera auto-switches to Follow mode during flight |

### URP Pink Material Fix (Detailed)

If you see pink/magenta materials:

1. **Check Graphics Settings**:
   - Edit > Project Settings > Graphics
   - "Default Render Pipeline" must NOT be "None"

2. **Create URP Assets if missing**:
   - Run **OpenRange > Create URP Quality Assets**
   - This creates Low/Medium/High quality pipeline assets

3. **Assign the asset**:
   - In Graphics settings, click ⊙ next to Default Render Pipeline
   - Select `URP-HighQuality` (or Medium/Low)

4. **Regenerate scenes**:
   - Run **OpenRange > Generate Marina Scene** to get proper materials

---

## Development Tips

- Use **Window > Analysis > Console** to see debug logs
- Shot results are logged with carry distance, total distance, apex height
- Camera modes: Static (default), Follow (during flight), TopDown, FreeOrbit
- Ball animation has phases: Idle → Flight → Bounce → Roll → Stopped
- Test different shots using presets: Driver (~275 yds), 7-Iron (~172 yds), Wedge (~136 yds)

---

## Running Tests

```bash
# Run all tests via Makefile
make test

# Run EditMode tests only
make test-edit

# Run physics validation tests
make test-physics
```

Or in Unity: **Window > General > Test Runner**
