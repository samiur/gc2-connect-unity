# Android Native Plugin

This folder contains the source code for the Android GC2 USB plugin using USB Host API.

## Implementation

See `docs/USB_PLUGINS.md` for the complete implementation guide.

## Structure

```
GC2AndroidPlugin/
├── src/main/
│   ├── kotlin/com/openrange/gc2/
│   │   ├── GC2Plugin.kt
│   │   ├── GC2Device.kt
│   │   └── GC2Protocol.kt
│   ├── AndroidManifest.xml
│   └── res/xml/
│       └── usb_device_filter.xml
└── build.gradle
```

## Building

```bash
./gradlew assembleRelease
```

Output: `GC2AndroidPlugin-release.aar` → Copy to `Assets/Plugins/Android/`

## Requirements

- Android device with USB Host support
- minSdkVersion: 26 (Android 8.0)
