# iOS/iPad Native Plugin (DriverKit)

This folder contains the source code for the iPad GC2 USB plugin using DriverKit.

## Important

DriverKit requires Apple entitlement approval. You must:

1. Request DriverKit + USB Transport entitlement from Apple
2. Provide VID: 0x2C79 (11385) and PID: 0x0110 (272)
3. Wait for approval (1-4 weeks)

## Implementation

See `docs/USB_PLUGINS.md` for the complete implementation guide.

## Structure

```
GC2iOSPlugin/
├── GC2iOSPlugin.swift      # App-side bridge
└── GC2iOSPlugin.xcodeproj/

GC2Driver/
├── GC2Driver.swift         # DriverKit driver
├── GC2UserClient.swift     # App communication
├── Info.plist              # USB matching
└── Entitlements.plist      # DriverKit entitlements
```

## Building

Build in Xcode with DriverKit-enabled provisioning profile.
