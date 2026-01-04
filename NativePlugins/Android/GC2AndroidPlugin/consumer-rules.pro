# ABOUTME: Consumer ProGuard rules for apps using the GC2 Android Plugin.
# ABOUTME: These rules are automatically applied when the AAR is included in an app.

# Keep the public API
-keep class com.openrange.gc2.GC2Plugin {
    public *;
}
-keep class com.openrange.gc2.GC2Plugin$Companion {
    public *;
}
