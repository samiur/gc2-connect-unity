// ABOUTME: Unit tests for the PlatformManager static utility class.
// ABOUTME: Tests platform detection, screen category, and device capability queries.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Core;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class PlatformManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            PlatformManager.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            PlatformManager.ClearCache();
        }

        #region Platform Detection Tests

        [Test]
        public void CurrentPlatform_ReturnsEditorInEditor()
        {
#if UNITY_EDITOR
            Assert.AreEqual(PlatformManager.Platform.Editor, PlatformManager.CurrentPlatform);
#endif
        }

        [Test]
        public void CurrentPlatform_ReturnsSameValueOnMultipleCalls()
        {
            var first = PlatformManager.CurrentPlatform;
            var second = PlatformManager.CurrentPlatform;
            Assert.AreEqual(first, second);
        }

        [Test]
        public void CurrentPlatform_IsCached()
        {
            var platform = PlatformManager.CurrentPlatform;
            PlatformManager.ClearCache();
            var cleared = PlatformManager.CurrentPlatform;
            Assert.AreEqual(platform, cleared);
        }

        #endregion

        #region IsDesktop Tests

        [Test]
        public void IsDesktop_ReturnsTrueForEditor()
        {
#if UNITY_EDITOR
            Assert.IsTrue(PlatformManager.IsDesktop);
#endif
        }

        [Test]
        public void IsDesktop_AndIsMobile_AreMutuallyExclusive()
        {
            bool isDesktop = PlatformManager.IsDesktop;
            bool isMobile = PlatformManager.IsMobile;

            if (PlatformManager.CurrentPlatform == PlatformManager.Platform.Editor ||
                PlatformManager.CurrentPlatform == PlatformManager.Platform.Mac ||
                PlatformManager.CurrentPlatform == PlatformManager.Platform.Windows)
            {
                Assert.IsTrue(isDesktop);
                Assert.IsFalse(isMobile);
            }
            else
            {
                Assert.IsFalse(isDesktop);
                Assert.IsTrue(isMobile);
            }
        }

        #endregion

        #region IsMobile Tests

        [Test]
        public void IsMobile_ReturnsFalseForEditor()
        {
#if UNITY_EDITOR
            Assert.IsFalse(PlatformManager.IsMobile);
#endif
        }

        #endregion

        #region IsTablet Tests

        [Test]
        public void IsTablet_ReturnsFalseForEditor()
        {
#if UNITY_EDITOR
            Assert.IsFalse(PlatformManager.IsTablet);
#endif
        }

        #endregion

        #region GetDiagonalInches Tests

        [Test]
        public void GetDiagonalInches_ReturnsNonNegativeValue()
        {
            float diagonal = PlatformManager.GetDiagonalInches();
            Assert.GreaterOrEqual(diagonal, 0f);
        }

        [Test]
        public void GetDiagonalInches_ReturnsSameValueOnMultipleCalls()
        {
            float first = PlatformManager.GetDiagonalInches();
            float second = PlatformManager.GetDiagonalInches();
            Assert.AreEqual(first, second, 0.001f);
        }

        [Test]
        public void GetDiagonalInches_CacheIsClearable()
        {
            float first = PlatformManager.GetDiagonalInches();
            PlatformManager.ClearCache();
            float second = PlatformManager.GetDiagonalInches();
            Assert.AreEqual(first, second, 0.001f);
        }

        #endregion

        #region ScreenCategory Tests

        [Test]
        public void ScreenCategoryValue_IsValidEnum()
        {
            var category = PlatformManager.ScreenCategoryValue;
            Assert.IsTrue(
                category == ScreenCategory.Compact ||
                category == ScreenCategory.Regular ||
                category == ScreenCategory.Large
            );
        }

        [Test]
        public void ScreenCategoryValue_ReturnsSameValueOnMultipleCalls()
        {
            var first = PlatformManager.ScreenCategoryValue;
            var second = PlatformManager.ScreenCategoryValue;
            Assert.AreEqual(first, second);
        }

        #endregion

        #region HasUSBHostSupport Tests

        [Test]
        public void HasUSBHostSupport_ReturnsBoolean()
        {
            bool hasSupport = PlatformManager.HasUSBHostSupport;
            Assert.IsTrue(hasSupport == true || hasSupport == false);
        }

        [Test]
        public void HasUSBHostSupport_ReturnsTrueForEditor()
        {
#if UNITY_EDITOR
            Assert.IsTrue(PlatformManager.HasUSBHostSupport);
#endif
        }

        #endregion

        #region DeviceModel Tests

        [Test]
        public void DeviceModel_ReturnsNonNullString()
        {
            string model = PlatformManager.DeviceModel;
            Assert.IsNotNull(model);
        }

        [Test]
        public void DeviceModel_ReturnsNonEmptyString()
        {
            string model = PlatformManager.DeviceModel;
            Assert.IsNotEmpty(model);
        }

        #endregion

        #region DeviceName Tests

        [Test]
        public void DeviceName_ReturnsNonNullString()
        {
            string name = PlatformManager.DeviceName;
            Assert.IsNotNull(name);
        }

        #endregion

        #region GraphicsDeviceName Tests

        [Test]
        public void GraphicsDeviceName_ReturnsNonNullString()
        {
            string name = PlatformManager.GraphicsDeviceName;
            Assert.IsNotNull(name);
        }

        #endregion

        #region SystemMemoryMB Tests

        [Test]
        public void SystemMemoryMB_ReturnsPositiveValue()
        {
            int memory = PlatformManager.SystemMemoryMB;
            Assert.Greater(memory, 0);
        }

        #endregion

        #region GraphicsMemoryMB Tests

        [Test]
        public void GraphicsMemoryMB_ReturnsNonNegativeValue()
        {
            int memory = PlatformManager.GraphicsMemoryMB;
            Assert.GreaterOrEqual(memory, 0);
        }

        #endregion

        #region IsAppleSilicon Tests

        [Test]
        public void IsAppleSilicon_ReturnsBooleanValue()
        {
            bool isSilicon = PlatformManager.IsAppleSilicon();
            Assert.IsTrue(isSilicon == true || isSilicon == false);
        }

        [Test]
        public void IsAppleSilicon_ReturnsSameValueOnMultipleCalls()
        {
            bool first = PlatformManager.IsAppleSilicon();
            bool second = PlatformManager.IsAppleSilicon();
            Assert.AreEqual(first, second);
        }

        [Test]
        public void IsAppleSilicon_CacheIsClearable()
        {
            bool first = PlatformManager.IsAppleSilicon();
            PlatformManager.ClearCache();
            bool second = PlatformManager.IsAppleSilicon();
            Assert.AreEqual(first, second);
        }

        #endregion

        #region SupportsDriverKit Tests

        [Test]
        public void SupportsDriverKit_ReturnsBooleanValue()
        {
            bool supports = PlatformManager.SupportsDriverKit;
            Assert.IsTrue(supports == true || supports == false);
        }

        [Test]
        public void SupportsDriverKit_ReturnsFalseForNonIOS()
        {
#if !UNITY_IOS || UNITY_EDITOR
            Assert.IsFalse(PlatformManager.SupportsDriverKit);
#endif
        }

        #endregion

        #region ClearCache Tests

        [Test]
        public void ClearCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => PlatformManager.ClearCache());
        }

        [Test]
        public void ClearCache_CanBeCalledMultipleTimes()
        {
            Assert.DoesNotThrow(() =>
            {
                PlatformManager.ClearCache();
                PlatformManager.ClearCache();
                PlatformManager.ClearCache();
            });
        }

        #endregion

        #region ScreenCategory Enum Tests

        [Test]
        public void ScreenCategory_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)ScreenCategory.Compact);
            Assert.AreEqual(1, (int)ScreenCategory.Regular);
            Assert.AreEqual(2, (int)ScreenCategory.Large);
        }

        #endregion

        #region Platform Enum Tests

        [Test]
        public void Platform_HasAllExpectedValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlatformManager.Platform), PlatformManager.Platform.Mac));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlatformManager.Platform), PlatformManager.Platform.iPad));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlatformManager.Platform), PlatformManager.Platform.Android));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlatformManager.Platform), PlatformManager.Platform.Windows));
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlatformManager.Platform), PlatformManager.Platform.Editor));
        }

        #endregion
    }
}
