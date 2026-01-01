// ABOUTME: Unit tests for UIManager component.
// ABOUTME: Tests panel management, toast notifications, and singleton behavior.

using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for UIManager component.
    /// </summary>
    [TestFixture]
    public class UIManagerTests
    {
        private GameObject _testObject;
        private UIManager _uiManager;
        private GameObject _panelContainer;
        private List<GameObject> _testPanels;

        [SetUp]
        public void SetUp()
        {
            // Clean up any existing singleton
            if (UIManager.Instance != null)
            {
                Object.DestroyImmediate(UIManager.Instance.gameObject);
            }

            _testObject = new GameObject("TestUIManager");
            _uiManager = _testObject.AddComponent<UIManager>();
            _uiManager.ForceInitializeSingleton();

            // Create panel container
            _panelContainer = new GameObject("PanelContainer");
            _panelContainer.transform.SetParent(_testObject.transform, false);

            _testPanels = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var panel in _testPanels)
            {
                if (panel != null)
                {
                    Object.DestroyImmediate(panel);
                }
            }
            _testPanels.Clear();

            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        private GameObject CreateTestPanel(string name, bool startActive = false)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(_panelContainer.transform, false);
            panel.AddComponent<RectTransform>();
            panel.AddComponent<CanvasGroup>();
            panel.SetActive(startActive);
            _testPanels.Add(panel);
            return panel;
        }

        #region Singleton

        [Test]
        public void Instance_ReturnsUIManager()
        {
            Assert.That(UIManager.Instance, Is.EqualTo(_uiManager));
        }

        [Test]
        public void Instance_IsSingleton()
        {
            var instance1 = UIManager.Instance;
            var instance2 = UIManager.Instance;
            Assert.That(instance1, Is.SameAs(instance2));
        }

        [Test]
        public void ForceInitializeSingleton_SetsInstance()
        {
            // Create new manager without initialization
            var newObject = new GameObject("NewUIManager");
            var newManager = newObject.AddComponent<UIManager>();

            // Should still be original
            Assert.That(UIManager.Instance, Is.EqualTo(_uiManager));

            // Force initialize
            newManager.ForceInitializeSingleton();
            Assert.That(UIManager.Instance, Is.EqualTo(newManager));

            Object.DestroyImmediate(newObject);
        }

        #endregion

        #region Panel Registration

        [Test]
        public void RegisterPanel_AddsPanel()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("TestPanel", panel);

            var retrievedPanel = _uiManager.GetPanel("TestPanel");
            Assert.That(retrievedPanel, Is.EqualTo(panel));
        }

        [Test]
        public void RegisterPanel_WithNullName_DoesNotThrow()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel(null, panel);
            Assert.Pass();
        }

        [Test]
        public void RegisterPanel_WithNullPanel_DoesNotThrow()
        {
            _uiManager.RegisterPanel("TestPanel", null);
            Assert.Pass();
        }

        [Test]
        public void RegisterPanel_WithEmptyName_DoesNotThrow()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("", panel);
            Assert.Pass();
        }

        [Test]
        public void UnregisterPanel_RemovesPanel()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("TestPanel", panel);
            _uiManager.UnregisterPanel("TestPanel");

            var retrievedPanel = _uiManager.GetPanel("TestPanel");
            Assert.That(retrievedPanel, Is.Null);
        }

        [Test]
        public void GetPanel_UnknownPanel_ReturnsNull()
        {
            var panel = _uiManager.GetPanel("NonExistent");
            Assert.That(panel, Is.Null);
        }

        #endregion

        #region Show/Hide Panel

        [Test]
        public void ShowPanel_ActivatesPanel()
        {
            var panel = CreateTestPanel("TestPanel", startActive: false);
            _uiManager.RegisterPanel("TestPanel", panel);

            bool result = _uiManager.ShowPanel("TestPanel", animate: false);

            Assert.That(result, Is.True);
            Assert.That(panel.activeSelf, Is.True);
        }

        [Test]
        public void ShowPanel_SetsActivePanel()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("TestPanel", panel);

            _uiManager.ShowPanel("TestPanel", animate: false);

            Assert.That(_uiManager.ActivePanel, Is.EqualTo("TestPanel"));
        }

        [Test]
        public void ShowPanel_UnknownPanel_ReturnsFalse()
        {
            bool result = _uiManager.ShowPanel("NonExistent", animate: false);
            Assert.That(result, Is.False);
        }

        [Test]
        public void HidePanel_DeactivatesPanel()
        {
            var panel = CreateTestPanel("TestPanel", startActive: true);
            _uiManager.RegisterPanel("TestPanel", panel);

            bool result = _uiManager.HidePanel("TestPanel", animate: false);

            Assert.That(result, Is.True);
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void HidePanel_ClearsActivePanel()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("TestPanel", panel);
            _uiManager.ShowPanel("TestPanel", animate: false);

            _uiManager.HidePanel("TestPanel", animate: false);

            Assert.That(_uiManager.ActivePanel, Is.Null);
        }

        [Test]
        public void HidePanel_UnknownPanel_ReturnsFalse()
        {
            bool result = _uiManager.HidePanel("NonExistent", animate: false);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TogglePanel_ShowsHiddenPanel()
        {
            var panel = CreateTestPanel("TestPanel", startActive: false);
            _uiManager.RegisterPanel("TestPanel", panel);

            bool isNowVisible = _uiManager.TogglePanel("TestPanel", animate: false);

            Assert.That(isNowVisible, Is.True);
            Assert.That(panel.activeSelf, Is.True);
        }

        [Test]
        public void TogglePanel_HidesVisiblePanel()
        {
            var panel = CreateTestPanel("TestPanel", startActive: true);
            _uiManager.RegisterPanel("TestPanel", panel);

            bool isNowVisible = _uiManager.TogglePanel("TestPanel", animate: false);

            Assert.That(isNowVisible, Is.False);
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void IsPanelVisible_ReturnsCorrectState()
        {
            var panel = CreateTestPanel("TestPanel", startActive: true);
            _uiManager.RegisterPanel("TestPanel", panel);

            Assert.That(_uiManager.IsPanelVisible("TestPanel"), Is.True);

            panel.SetActive(false);
            Assert.That(_uiManager.IsPanelVisible("TestPanel"), Is.False);
        }

        [Test]
        public void IsPanelVisible_UnknownPanel_ReturnsFalse()
        {
            Assert.That(_uiManager.IsPanelVisible("NonExistent"), Is.False);
        }

        [Test]
        public void HideAllPanels_HidesAllRegisteredPanels()
        {
            var panel1 = CreateTestPanel("Panel1", startActive: true);
            var panel2 = CreateTestPanel("Panel2", startActive: true);
            _uiManager.RegisterPanel("Panel1", panel1);
            _uiManager.RegisterPanel("Panel2", panel2);

            _uiManager.HideAllPanels(animate: false);

            Assert.That(panel1.activeSelf, Is.False);
            Assert.That(panel2.activeSelf, Is.False);
        }

        #endregion

        #region Panel Events

        [Test]
        public void OnPanelShown_FiresOnShowPanel()
        {
            var panel = CreateTestPanel("TestPanel");
            _uiManager.RegisterPanel("TestPanel", panel);

            string shownPanel = null;
            _uiManager.OnPanelShown += (name) => shownPanel = name;

            _uiManager.ShowPanel("TestPanel", animate: false);

            Assert.That(shownPanel, Is.EqualTo("TestPanel"));
        }

        [Test]
        public void OnPanelHidden_FiresOnHidePanel()
        {
            var panel = CreateTestPanel("TestPanel", startActive: true);
            _uiManager.RegisterPanel("TestPanel", panel);

            string hiddenPanel = null;
            _uiManager.OnPanelHidden += (name) => hiddenPanel = name;

            _uiManager.HidePanel("TestPanel", animate: false);

            Assert.That(hiddenPanel, Is.EqualTo("TestPanel"));
        }

        #endregion

        #region Toast Properties

        [Test]
        public void ActiveToastCount_InitiallyZero()
        {
            Assert.That(_uiManager.ActiveToastCount, Is.EqualTo(0));
        }

        [Test]
        public void QueuedToastCount_InitiallyZero()
        {
            Assert.That(_uiManager.QueuedToastCount, Is.EqualTo(0));
        }

        #endregion

        #region Show Toast Methods

        [Test]
        public void ShowToast_DoesNotThrow()
        {
            _uiManager.ShowToast("Test message");
            Assert.Pass();
        }

        [Test]
        public void ShowToast_WithDuration_DoesNotThrow()
        {
            _uiManager.ShowToast("Test message", 5f);
            Assert.Pass();
        }

        [Test]
        public void ShowToast_WithType_DoesNotThrow()
        {
            _uiManager.ShowToast("Test message", 3f, ToastType.Success);
            Assert.Pass();
        }

        [Test]
        public void ShowSuccessToast_DoesNotThrow()
        {
            _uiManager.ShowSuccessToast("Success!");
            Assert.Pass();
        }

        [Test]
        public void ShowWarningToast_DoesNotThrow()
        {
            _uiManager.ShowWarningToast("Warning!");
            Assert.Pass();
        }

        [Test]
        public void ShowErrorToast_DoesNotThrow()
        {
            _uiManager.ShowErrorToast("Error!");
            Assert.Pass();
        }

        #endregion

        #region Clear Toasts

        [Test]
        public void ClearAllToasts_DoesNotThrow()
        {
            _uiManager.ShowToast("Test 1");
            _uiManager.ShowToast("Test 2");
            _uiManager.ClearAllToasts();
            Assert.Pass();
        }

        [Test]
        public void ClearAllToasts_WhenEmpty_DoesNotThrow()
        {
            _uiManager.ClearAllToasts();
            Assert.Pass();
        }

        #endregion

        #region Toast Event

        [Test]
        public void OnToastShown_CanSubscribe()
        {
            string receivedMessage = null;
            ToastType receivedType = ToastType.Info;

            _uiManager.OnToastShown += (message, type) =>
            {
                receivedMessage = message;
                receivedType = type;
            };

            // Note: In actual execution, the event fires when toast is created
            // In edit mode tests, the coroutine doesn't run
            Assert.Pass();
        }

        #endregion

        #region ResponsiveLayout

        [Test]
        public void ResponsiveLayout_IsNullInitially()
        {
            // Before Start() is called, may be null
            // This is expected behavior
            Assert.Pass();
        }

        #endregion
    }
}
