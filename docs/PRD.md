# Product Requirements Document (PRD)
# GC2 Connect Unity - Cross-Platform Driving Range

## Document Info
| Field | Value |
|-------|-------|
| Version | 2.0.0 |
| Last Updated | December 2024 |
| Author | Samiur Rahman |
| Status | Draft |

---

## 1. Executive Summary

GC2 Connect Unity is a cross-platform driving range simulator that connects to the Foresight GC2 launch monitor via USB and visualizes ball flight with physics-accurate trajectories in a beautiful 3D environment. Built with Unity, it delivers consistent GSPro-quality visuals across macOS, iPad, and Android tablets.

### Value Proposition
- **For GC2 owners**: Practice without GSPro subscription or Windows PC
- **For Mac users**: Native solution without Parallels/Boot Camp
- **For mobile users**: True portable practice with tablet + GC2
- **For everyone**: Offline capability, no network required

---

## 2. Problem Statement

### Current Pain Points

| Pain Point | Description | Impact |
|------------|-------------|--------|
| GSPro dependency | Need $250/year subscription just for range | High cost |
| Windows requirement | GSPro only runs on Windows | Mac users excluded |
| Network dependency | Current solutions need WiFi | No outdoor practice |
| Setup complexity | Multiple apps, cables, configurations | User friction |
| Inconsistent quality | Different apps look/work differently | Poor experience |

### Opportunity
Create a single, beautiful, native application that:
- Connects directly to GC2 via USB (no companion app)
- Works offline (no network required)
- Runs on Mac, iPad, and Android
- Provides GSPro-level visual quality
- Is completely free

---

## 3. Goals & Success Metrics

### Primary Goals
1. **Unified Experience**: Same app, same visuals, all platforms
2. **Native USB**: Direct GC2 connection without intermediaries
3. **Offline First**: Full functionality without network
4. **Visual Quality**: Match or exceed GSPro driving range

### Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Physics accuracy | ±5% of Nathan model | Automated tests |
| Frame rate (Mac M1) | 120 FPS | Unity profiler |
| Frame rate (iPad) | 60 FPS | Unity profiler |
| Frame rate (Android) | 30+ FPS | Unity profiler |
| Shot latency | <100ms | Timestamp delta |
| App size | <200MB | Build output |
| User satisfaction | >4.5/5 | App store rating |

---

## 4. Target Users

### Primary: Home Simulator Owner
- **Profile**: Golf enthusiast, 30-55, owns GC2
- **Technical**: Low to moderate
- **Goal**: Quick practice without full sim setup
- **Quote**: "I just want to hit some balls, not boot up Windows"

### Secondary: Mac-Only User
- **Profile**: Apple ecosystem user, owns GC2
- **Technical**: Moderate
- **Goal**: Use GC2 without Windows
- **Quote**: "I bought a GC2 but can't use it with my Mac"

### Tertiary: Mobile Practicer
- **Profile**: Travels, practices outdoors, takes lessons
- **Technical**: Low
- **Goal**: Portable GC2 setup
- **Quote**: "I want to practice in my backyard with just my iPad"

---

## 5. Platform Requirements

### Supported Platforms

| Platform | Min Version | USB Support | Priority |
|----------|-------------|-------------|----------|
| macOS | 11.0 (Big Sur) | libusb | P0 |
| iPad | iPadOS 16.0, M1+ | DriverKit | P0 |
| Android | 8.0 (API 26) | USB Host | P0 |
| Windows | 10 | libusb | P1 (future) |
| iPhone | - | Not possible | N/A |

### Why No iPhone?
Apple restricts USB accessory communication on iPhone. DriverKit (required for custom USB devices) is only available on iPadOS and macOS, not iOS. This is a platform limitation, not a technical oversight.

---

## 6. Features & Requirements

### P0 - Must Have (MVP)

#### F1: Native USB Connection
Connect to GC2 directly via USB on all platforms.

**Requirements:**
- Auto-detect GC2 (VID: 0x2C79, PID: 0x0110)
- Handle platform-specific permissions
- Display connection status clearly
- Graceful disconnect/reconnect handling
- No dropped shots

**Acceptance Criteria:**
- [ ] GC2 detected within 3 seconds
- [ ] Permission flow clear on each platform
- [ ] Status updates in real-time
- [ ] Reconnects automatically if cable unplugged/replugged

#### F2: 3D Driving Range Environment
Beautiful, GSPro-quality range visualization.

**Requirements:**
- Coastal/marina setting (primary environment)
- High-quality lighting and shadows
- Distance markers (50, 100, 150, 200, 250, 300 yards)
- Target greens at key distances
- Decorative elements (boats, mountains, clouds)
- Quality tiers for different hardware

**Acceptance Criteria:**
- [ ] Visually comparable to GSPro driving range
- [ ] Maintains target frame rate per platform
- [ ] No visible artifacts or pop-in
- [ ] Quality auto-adjusts to hardware

#### F3: Physics-Accurate Ball Flight
Realistic trajectory based on launch data.

**Requirements:**
- Nathan model physics engine
- WSU aerodynamic coefficients (Cd/Cl tables)
- Spin effects (draw/fade, rise/drop)
- Atmospheric corrections (temp, elevation, humidity)
- Wind effects (optional)

**Acceptance Criteria:**
- [ ] Carry distances within ±5% of validation data
- [ ] Spin effects visually correct
- [ ] Same results on all platforms

#### F4: Ball Animation
Smooth, satisfying ball flight visualization.

**Requirements:**
- High-quality golf ball model
- Visible spin rotation
- Trajectory trail/tracer
- Landing impact effect
- Bounce and roll animation

**Acceptance Criteria:**
- [ ] 60 FPS animation (30 on low-end Android)
- [ ] Ball clearly visible throughout flight
- [ ] Natural-looking movement

#### F5: Shot Data Display
GSPro-style comprehensive data presentation.

**Requirements:**
- Bottom bar: Ball Speed, Direction, Angle, Back Spin, Side Spin, Apex, Offline, Carry, Run, Total
- Right panel (HMT): Path, Attack Angle, D.Loft, Face to Target
- Top-left: Session time, shot count
- Responsive layout for different screens

**Acceptance Criteria:**
- [ ] All metrics update immediately
- [ ] Readable on all screen sizes
- [ ] Matches GSPro visual style

#### F6: Platform-Appropriate Input
Natural controls for each platform.

**Requirements:**
- Touch gestures on mobile (pinch zoom, pan)
- Mouse/keyboard on Mac
- Large touch targets on tablets
- Consistent interaction patterns

**Acceptance Criteria:**
- [ ] Controls feel native to each platform
- [ ] No accidental inputs
- [ ] Camera manipulation intuitive

### P1 - Should Have

#### F7: GSPro Relay Mode
Send shots to GSPro in addition to local visualization.

**Requirements:**
- Mode selector: "Open Range" vs "GSPro"
- TCP connection to GSPro (port 921)
- GSPro Open Connect API v1 format
- Queue shots if temporarily disconnected

**Acceptance Criteria:**
- [ ] Shots appear in GSPro within 150ms
- [ ] Mode switch is instant
- [ ] Connection status clearly shown

#### F8: Shot History
Review previous shots in session.

**Requirements:**
- Scrollable list of all session shots
- Key metrics per shot
- Tap to replay trajectory
- Session totals/averages

**Acceptance Criteria:**
- [ ] All shots recorded
- [ ] Replay animation smooth
- [ ] Performance good with 100+ shots

#### F9: Club Selector
Visual club selection for test mode.

**Requirements:**
- Club bag visualization
- Common clubs: DR, 3W, 5i-PW, SW
- Tee/ground toggle
- Affects test shot parameters

**Acceptance Criteria:**
- [ ] One-tap selection
- [ ] Visual feedback clear

#### F10: Multiple Environments
Different range settings.

**Requirements:**
- Marina/Coastal (default)
- Mountain
- Links/Ocean
- (Stretch) Indoor

**Acceptance Criteria:**
- [ ] Each environment polished
- [ ] Quick switching (<3 sec)

#### F11: Settings & Preferences
User configuration options.

**Requirements:**
- Units (yards/meters, mph/km/h)
- Graphics quality override
- Environmental conditions (temp, elevation)
- Audio volume
- GSPro connection settings

**Acceptance Criteria:**
- [ ] Settings persist between sessions
- [ ] Changes apply immediately

### P2 - Nice to Have

#### F12: Test/Demo Mode
Simulated shots without GC2.

#### F13: Dispersion View
Top-down shot pattern visualization.

#### F14: Audio
Ball landing, ambient sounds.

#### F15: Haptic Feedback
Vibration on ball landing (mobile).

#### F16: Session Statistics
Averages, trends, club comparisons.

---

## 7. User Flows

### Flow 1: First Launch (Any Platform)
```
1. User installs app
2. App shows welcome screen with platform-specific USB instructions
3. User connects GC2 via USB
4. Platform shows permission dialog
5. User grants permission
6. App shows "GC2 Connected" ✓
7. User hits a shot
8. Ball flight animates beautifully
9. Data bar shows results
```

### Flow 2: Typical Session
```
1. User opens app
2. GC2 auto-connects (previously approved)
3. Range loads in <3 seconds
4. User practices
5. All shots visualized and recorded
6. User reviews history (optional)
7. User closes app
```

### Flow 3: GSPro Mode
```
1. User taps mode selector → "GSPro"
2. User enters GSPro PC IP (first time only)
3. App connects to GSPro
4. User hits a shot
5. Shot visualizes locally AND appears in GSPro
6. Best of both worlds
```

### Flow 4: Offline Practice (Mobile)
```
1. User is outdoors (no WiFi)
2. Connects GC2 to tablet via USB-C
3. Opens app → already in Open Range mode
4. Practices with full visualization
5. No network ever needed
```

---

## 8. Visual Design

### Reference: GSPro Style
Based on GSPro's driving range aesthetic:

**Environment:**
- Waterfront marina setting
- Colorful sailboats, luxury yacht
- Mountains in distance
- Blue sky with cumulus clouds
- Manicured fairway with mowing patterns
- Target greens with flags

**UI Panels:**
- Semi-transparent dark backgrounds (#1a1a2e)
- Clean sans-serif typography
- Green accent for headers (#2d5a27)
- White text on dark
- Coral red for "Total" distance (#ff6b6b)

**Data Bar Layout (Bottom):**
```
┌────────┬──────────┬───────┬──────────┬──────────┬───────┬─────────┬───────┬─────┬───────┐
│ BALL   │DIRECTION │ ANGLE │ BACK     │ SIDE     │ APEX  │ OFFLINE │ CARRY │ RUN │ TOTAL │
│ SPEED  │          │       │ SPIN     │ SPIN     │       │         │       │     │       │
├────────┼──────────┼───────┼──────────┼──────────┼───────┼─────────┼───────┼─────┼───────┤
│ 104.5  │   L4.0   │ 24.0  │  4,121   │   R311   │ 30.7  │   L7.2  │ 150.0 │ 4.6 │ 154.6 │
│  mph   │   deg    │  deg  │   rpm    │   rpm    │  yd   │    yd   │   yd  │ yd  │   yd  │
└────────┴──────────┴───────┴──────────┴──────────┴───────┴─────────┴───────┴─────┴───────┘
```

### Quality Tiers

| Tier | Devices | Features |
|------|---------|----------|
| High | Mac M1+, iPad Pro | Full shadows, reflections, post-processing, 2K textures |
| Medium | iPad Air, Good Android | Hard shadows, water reflections, 1K textures |
| Low | Mid-range Android | Baked shadows, simple water, 512px textures |

---

## 9. Technical Constraints

### Unity Configuration
- Version: 2022.3 LTS
- Render Pipeline: Universal (URP)
- Scripting Backend: IL2CPP
- API Level: .NET Standard 2.1

### Platform-Specific

| Platform | Build Target | Architecture | Notes |
|----------|--------------|--------------|-------|
| macOS | Standalone | Universal (x64 + ARM64) | Notarization required |
| iPad | iOS | ARM64 | DriverKit entitlements required |
| Android | Android | ARM64 + ARMv7 | USB Host manifest entry |

### Performance Budgets

| Platform | FPS | Draw Calls | Memory | Battery/hr |
|----------|-----|------------|--------|------------|
| Mac M1 | 120 | <100 | <500MB | N/A |
| iPad Pro | 60 | <80 | <300MB | <15% |
| Android High | 60 | <80 | <300MB | <20% |
| Android Mid | 30 | <50 | <200MB | <20% |

---

## 10. Out of Scope (v2.0)

- iPhone support (Apple platform restriction)
- Full course simulation
- Multiplayer/online features
- VR support (future v3.0)
- Club fitting analysis
- Video recording
- Apple Watch / Wear OS

---

## 11. Timeline

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| 1. Core App | Weeks 1-3 | Unity project, environment, physics, visualization |
| 2. macOS USB | Weeks 3-4 | Native plugin, integration, testing |
| 3. Android USB | Weeks 4-5 | Native plugin, integration, testing |
| 4. iPad USB | Weeks 5-7 | DriverKit driver, integration (depends on Apple) |
| 5. UI & Polish | Weeks 7-9 | Complete UI, quality tiers, settings |
| 6. Testing | Weeks 9-10 | Cross-platform testing, bug fixes |
| 7. Release | Weeks 10-11 | App store submissions, documentation |

**Total: ~11 weeks** (iPad timeline depends on Apple entitlement approval)

---

## 12. Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| iPad DriverKit approval delayed | High | Medium | Start early; have Mac+Android ready first |
| USB plugin complexity | Medium | Medium | Incremental development; TCP fallback |
| Unity performance on Android | Medium | Low | Aggressive quality tiers; profiling |
| App Store rejection | Medium | Low | Follow guidelines; appeal process |
| GC2 protocol changes | Low | Low | Protocol is stable; version checking |

---

## 13. Success Criteria

### Launch Criteria (All Required)
- [ ] Physics validated against Nathan model (±5%)
- [ ] 60+ FPS on iPad Pro M1
- [ ] 30+ FPS on Samsung Galaxy Tab S8
- [ ] USB working on Mac, iPad, Android
- [ ] All P0 features complete
- [ ] No critical bugs
- [ ] Apps approved for distribution

### Post-Launch (30 days)
- User rating >4.5/5
- <20 crash reports
- 50%+ users prefer over previous solution

---

## Appendix A: Competitive Analysis

| Solution | Platforms | Quality | USB Direct | Cost |
|----------|-----------|---------|------------|------|
| GSPro | Windows | ⭐⭐⭐⭐⭐ | No (SDK) | $250/yr |
| FSX Pro | Windows | ⭐⭐⭐⭐ | Yes | Included |
| E6 Connect | Windows | ⭐⭐⭐⭐ | No | $300/yr |
| **GC2 Connect Unity** | **Mac/iPad/Android** | **⭐⭐⭐⭐** | **Yes** | **Free** |

---

## Appendix B: Related Documents

- Technical Requirements Document (TRD): `docs/TRD.md`
- Physics Specification: `docs/PHYSICS.md`
- GSPro API Specification: `docs/GSPRO_API.md`
- GC2 Protocol Specification: `docs/GC2_PROTOCOL.md`
- USB Plugin Guide: `docs/USB_PLUGINS.md`
