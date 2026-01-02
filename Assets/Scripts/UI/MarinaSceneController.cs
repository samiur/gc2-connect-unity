// ABOUTME: Controller for the Marina driving range scene.
// ABOUTME: Handles scene-specific UI, shot display, and navigation back to main menu.

using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;
using OpenRange.Visualization;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private ShotDataBar _shotDataBar;

        [Header("Visualization References")]
        [SerializeField] private BallController _ballController;
        [SerializeField] private TrajectoryRenderer _trajectoryRenderer;

        private ShotProcessor _shotProcessor;

        private void Start()
        {
            SetupButtonListeners();
            InitializeScene();
            SubscribeToShotProcessor();
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

            UnsubscribeFromShotProcessor();
        }

        private void InitializeScene()
        {
            Debug.Log("MarinaSceneController: Marina range initialized");

            // Start a new session when entering the range
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewSession();
            }

            // Clear any previous shot data on scene entry
            if (_shotDataBar != null)
            {
                _shotDataBar.Clear();
            }
        }

        private void SubscribeToShotProcessor()
        {
            if (GameManager.Instance != null)
            {
                _shotProcessor = GameManager.Instance.ShotProcessor;
                if (_shotProcessor != null)
                {
                    _shotProcessor.OnShotProcessed += OnShotProcessed;
                    Debug.Log("MarinaSceneController: Subscribed to ShotProcessor events");
                }
            }
        }

        private void UnsubscribeFromShotProcessor()
        {
            if (_shotProcessor != null)
            {
                _shotProcessor.OnShotProcessed -= OnShotProcessed;
            }
        }

        private void OnShotProcessed(GC2ShotData shotData, ShotResult result)
        {
            // Update UI
            if (_shotDataBar != null)
            {
                _shotDataBar.UpdateDisplay(shotData, result);
            }

            // Trigger ball animation
            if (_ballController != null)
            {
                _ballController.PlayShot(result);
            }

            // Show trajectory
            if (_trajectoryRenderer != null)
            {
                _trajectoryRenderer.ShowTrajectory(result);
            }

            Debug.Log($"MarinaSceneController: Shot processed - Carry: {result.CarryDistance:F1}yd, Total: {result.TotalDistance:F1}yd");
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
