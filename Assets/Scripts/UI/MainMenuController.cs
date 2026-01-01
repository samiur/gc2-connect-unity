// ABOUTME: Controller for the main menu scene with navigation and connection status.
// ABOUTME: Handles button clicks and displays GC2 connection state.

using UnityEngine;
using UnityEngine.UI;
using OpenRange.Core;

namespace OpenRange.UI
{
    /// <summary>
    /// Controller for the main menu scene.
    /// Handles navigation to the driving range and displays connection status.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _openRangeButton;
        [SerializeField] private Button _settingsButton;

        private void Start()
        {
            SetupButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (_openRangeButton != null)
            {
                _openRangeButton.onClick.AddListener(OnOpenRangeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_openRangeButton != null)
            {
                _openRangeButton.onClick.RemoveListener(OnOpenRangeClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }
        }

        private void OnOpenRangeClicked()
        {
            Debug.Log("MainMenuController: Opening Marina range...");
            SceneLoader.LoadMarina();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("MainMenuController: Settings clicked (not implemented)");
        }
    }
}
