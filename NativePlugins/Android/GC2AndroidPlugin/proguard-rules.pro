# ABOUTME: ProGuard rules for the GC2 Android Plugin release build.
# ABOUTME: Preserves classes needed for Unity reflection and USB callbacks.

# Keep the main plugin class and all its methods (called via reflection from Unity)
-keep class com.openrange.gc2.GC2Plugin {
    public static *** getInstance();
    public *** initialize(...);
    public *** shutdown();
    public *** isDeviceAvailable();
    public *** connect(...);
    public *** disconnect();
    public *** isConnected();
    public *** getDeviceSerial();
    public *** getFirmwareVersion();
}

# Keep companion object methods
-keep class com.openrange.gc2.GC2Plugin$Companion {
    public *** getInstance();
}

# Keep other classes that might be accessed
-keep class com.openrange.gc2.GC2Device { *; }
-keep class com.openrange.gc2.GC2Protocol { *; }
-keep class com.openrange.gc2.GC2Protocol$MessageType { *; }

# Keep JSON-related classes
-keep class org.json.** { *; }

# Keep Kotlin metadata for reflection
-keepattributes *Annotation*
-keepattributes Signature
-keepattributes Exceptions
