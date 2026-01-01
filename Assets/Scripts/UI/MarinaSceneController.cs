// ABOUTME: Controller for the Marina driving range scene.
// ABOUTME: Handles scene-specific UI and navigation back to main menu.

using UnityEngine;
using UnityEngine.UI;
using OpenRange.Core;

namespace OpenRange.UI
{
    /// <summary>
    /// Controller for the Marina driving range scene.
    /// Manages scene-specific UI and navigation.
    /// </summary>
    public class MarinaSceneController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _backButton;

        private void Start()
        {
            SetupButtonListeners();
            InitializeScene();
        }

        private void SetupButtonListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }
        }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        private void InitializeScene()
        {
            Debug.Log("MarinaSceneController: Marina range initialized");

            // Start a new session when entering the range
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewSession();
            }
        }

        private void OnBackClicked()
        {
            Debug.Log("MarinaSceneController: Returning to main menu...");

            // End the current session when leaving the range
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndSession();
            }

            SceneLoader.LoadMainMenu();
        }
    }
}
