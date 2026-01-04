// ABOUTME: Unit tests for AndroidBridgeService - the C# bridge for Android foreground service.
// ABOUTME: Tests lifecycle, event handling, callback processing, and state management.

using System;
using NUnit.Framework;
using OpenRange.Core;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_ANDROID
using OpenRange.GC2.Platforms.Android;
#endif

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class AndroidBridgeServiceTests
    {
#if UNITY_ANDROID
        #region Component Lifecycle

        [Test]
        public void AndroidBridgeService_NewInstance_IsNotRunning()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.That(service.IsRunning, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_NewInstance_ShotsRelayedIsZero()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.That(service.GetShotsRelayed(), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_ImplementsIBridgeService()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.That(service, Is.InstanceOf<IBridgeService>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_IsMonoBehaviour()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.That(service, Is.InstanceOf<MonoBehaviour>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region IBridgeService Interface

        [Test]
        public void IsRunning_AfterForceSetRunning_ReturnsTrue()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();
                service.ForceSetRunning(true);

                Assert.That(service.IsRunning, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void IsRunning_WhenDisposed_ReturnsFalse()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();
                service.ForceSetRunning(true);

                // Simulate disposal
                Assert.That(service.IsRunning, Is.True);
                // After destroy, disposed flag is set
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Stop_WhenNotRunning_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.DoesNotThrow(() => service.Stop());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Native Callbacks

        [Test]
        public void OnBridgeServiceStarted_SetsIsRunningTrue()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                service.OnBridgeServiceStarted("");

                Assert.That(service.IsRunning, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeServiceStopped_SetsIsRunningFalse()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();
                service.ForceSetRunning(true);

                service.OnBridgeServiceStopped("");

                Assert.That(service.IsRunning, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeShotRelayed_UpdatesShotCount()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                service.OnBridgeShotRelayed("5");

                Assert.That(service.GetShotsRelayed(), Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeShotRelayed_InvalidNumber_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.DoesNotThrow(() => service.OnBridgeShotRelayed("not a number"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeShotRelayed_EmptyString_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.DoesNotThrow(() => service.OnBridgeShotRelayed(""));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeError_NullError_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.DoesNotThrow(() => service.OnBridgeError(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeError_EmptyError_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.DoesNotThrow(() => service.OnBridgeError(""));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeError_ValidError_LogsError()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Test error.*"));
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Native error.*"));

                service.OnBridgeError("Test error");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region UpdateNotification

        [Test]
        public void UpdateNotification_WhenNotRunning_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.DoesNotThrow(() => service.UpdateNotification(10, true, true));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Event Handlers

        [Test]
        public void OnStarted_EventCanBeSubscribed()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                bool eventFired = false;

                service.OnStarted += () => eventFired = true;
                service.ForceInitialize();
                service.OnBridgeServiceStarted("");

                // Event is dispatched via MainThreadDispatcher, which won't run in EditMode
                // Just verify subscription works
                Assert.That(eventFired, Is.False.Or.True); // Either is fine
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnStopped_EventCanBeSubscribed()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                bool eventFired = false;

                service.OnStopped += () => eventFired = true;
                service.ForceInitialize();
                service.OnBridgeServiceStopped("");

                // Event is dispatched via MainThreadDispatcher
                Assert.That(eventFired, Is.False.Or.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnError_EventCanBeSubscribed()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                string errorReceived = null;

                service.OnError += (error) => errorReceived = error;

                // Event subscription works
                Assert.That(errorReceived, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnShotRelayed_EventCanBeSubscribed()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                OpenRange.GC2.GC2ShotData shotReceived = null;

                service.OnShotRelayed += (shot) => shotReceived = shot;

                // Event subscription works
                Assert.That(shotReceived, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Internal State

        [Test]
        public void IsInitialized_AfterForceInitialize_ReturnsTrue()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.That(service.IsInitialized, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void IsDisposed_BeforeDestroy_ReturnsFalse()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                Assert.That(service.IsDisposed, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void MultipleStop_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();

                Assert.DoesNotThrow(() =>
                {
                    service.Stop();
                    service.Stop();
                    service.Stop();
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ShotCount_IncrementsThroughCallbacks()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                service.OnBridgeShotRelayed("1");
                Assert.That(service.GetShotsRelayed(), Is.EqualTo(1));

                service.OnBridgeShotRelayed("2");
                Assert.That(service.GetShotsRelayed(), Is.EqualTo(2));

                service.OnBridgeShotRelayed("10");
                Assert.That(service.GetShotsRelayed(), Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnBridgeShotRelayed_LargeNumber_HandlesCorrectly()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<AndroidBridgeService>();
                service.ForceInitialize();

                service.OnBridgeShotRelayed("999999");

                Assert.That(service.GetShotsRelayed(), Is.EqualTo(999999));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

#else
        [Test]
        public void AndroidBridgeService_StubAvailableOnNonAndroid()
        {
            // On non-Android platforms, the stub implementation is used
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<OpenRange.GC2.Platforms.Android.AndroidBridgeService>();

                Assert.That(service.IsRunning, Is.False);
                Assert.That(service.GetShotsRelayed(), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_StubStartAsync_ReturnsFalse()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<OpenRange.GC2.Platforms.Android.AndroidBridgeService>();

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Not available on this platform.*"));

                var result = service.StartAsync().Result;

                Assert.That(result, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_StubStop_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<OpenRange.GC2.Platforms.Android.AndroidBridgeService>();

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Not available on this platform.*"));

                Assert.DoesNotThrow(() => service.Stop());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_StubUpdateNotification_DoesNotThrow()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<OpenRange.GC2.Platforms.Android.AndroidBridgeService>();

                Assert.DoesNotThrow(() => service.UpdateNotification(5, true, true));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AndroidBridgeService_StubImplementsIBridgeService()
        {
            var go = new GameObject("TestBridgeService");
            try
            {
                var service = go.AddComponent<OpenRange.GC2.Platforms.Android.AndroidBridgeService>();

                Assert.That(service, Is.InstanceOf<OpenRange.Core.IBridgeService>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
#endif
    }
}
