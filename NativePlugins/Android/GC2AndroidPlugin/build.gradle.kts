// ABOUTME: Gradle build configuration for the GC2 Android Plugin.
// ABOUTME: Builds an AAR library for Unity Android integration with USB Host API support.

plugins {
    id("com.android.library") version "8.7.3"
    id("org.jetbrains.kotlin.android") version "2.0.21"
}

android {
    namespace = "com.openrange.gc2"
    compileSdk = 34

    defaultConfig {
        minSdk = 26

        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    // Output AAR to build/outputs/aar/
    libraryVariants.all {
        outputs.all {
            val output = this as com.android.build.gradle.internal.api.LibraryVariantOutputImpl
            output.outputFileName = "GC2AndroidPlugin-${name}.aar"
        }
    }
}

dependencies {
    // AndroidX Core for modern Android APIs
    implementation("androidx.core:core-ktx:1.15.0")

    // Coroutines for async USB operations
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")

    // JSON parsing (matches Unity's Newtonsoft.Json output format)
    implementation("org.json:json:20240303")

    // Testing
    testImplementation("junit:junit:4.13.2")
    androidTestImplementation("androidx.test.ext:junit:1.2.1")
    androidTestImplementation("androidx.test.espresso:espresso-core:3.6.1")
}
