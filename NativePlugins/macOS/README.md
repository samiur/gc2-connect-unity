# macOS Native Plugin

This folder contains the source code for the macOS GC2 USB plugin.

## Implementation

See `docs/USB_PLUGINS.md` for the complete implementation guide.

## Files to Create

- `GC2MacPlugin.h` - Header file
- `GC2MacPlugin.mm` - Objective-C implementation
- `GC2MacPlugin.xcodeproj/` - Xcode project

## Dependencies

- libusb 1.0.26 or later

## Building

```bash
xcodebuild -project GC2MacPlugin.xcodeproj -scheme GC2MacPlugin -configuration Release build
```

Output: `GC2MacPlugin.bundle` → Copy to `Assets/Plugins/macOS/`
