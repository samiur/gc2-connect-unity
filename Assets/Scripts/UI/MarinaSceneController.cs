// ABOUTME: Controller for the Marina driving range scene.
// ABOUTME: Handles scene-specific UI, shot display, and navigation back to main menu.

using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Network;
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
        [SerializeField] private Button _settingsButton;
        [SerializeField] private ShotDataBar _shotDataBar;
        [SerializeField] private ClubDataPanel _clubDataPanel;

        [Header("Session Info UI")]
        [SerializeField] private SessionInfoPanel _sessionInfoPanel;
        [SerializeField] private ShotHistoryPanel _shotHistoryPanel;
        [SerializeField] private ShotDetailModal _shotDetailModal;

        [Header("Connection UI")]
        [SerializeField] private ConnectionStatusUI _connectionStatusUI;
        [SerializeField] private ConnectionPanel _connectionPanel;
        [SerializeField] private BallReadyIndicator _ballReadyIndicator;
        [SerializeField] private GSProModeUI _gsProModeUI;

        [Header("Settings UI")]
        [SerializeField] private SettingsPanel _settingsPanel;

        [Header("Visualization References")]
        [SerializeField] private BallController _ballController;
        [SerializeField] private TrajectoryRenderer _trajectoryRenderer;

        private ShotProcessor _shotProcessor;

        private void Start()
        {
            SetupButtonListeners();
            SetupConnectionUI();
            SetupSessionInfoUI();
            SetupGSProUI();
            InitializeScene();
            SubscribeToShotProcessor();
        }

        private void SetupButtonListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_connectionStatusUI != null)
            {
                _connectionStatusUI.OnClicked -= OnConnectionStatusClicked;
            }

            if (_sessionInfoPanel != null)
            {
                _sessionInfoPanel.OnExpandClicked -= OnSessionInfoExpandClicked;
            }

            if (_shotHistoryPanel != null)
            {
                _shotHistoryPanel.OnShotSelected -= OnHistoryShotSelected;
                _shotHistoryPanel.OnReplayRequested -= OnReplayRequested;
            }

            if (_shotDetailModal != null)
            {
                _shotDetailModal.OnReplayRequested -= OnReplayRequested;
            }

            if (_gsProModeUI != null)
            {
                _gsProModeUI.OnConnectClicked -= OnGSProConnectClicked;
                _gsProModeUI.OnDisconnectClicked -= OnGSProDisconnectClicked;
                _gsProModeUI.OnModeChanged -= OnGSProModeChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDeviceStatusChanged -= OnDeviceStatusChanged;
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

            if (_clubDataPanel != null)
            {
                _clubDataPanel.Clear();
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

            // Update club data panel (only shows if shot has HMT data)
            if (_clubDataPanel != null)
            {
                _clubDataPanel.UpdateDisplay(shotData);
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

        private void OnSettingsClicked()
        {
            Debug.Log("MarinaSceneController: Settings clicked");
            if (_settingsPanel != null)
            {
                _settingsPanel.Toggle();
            }
        }

        private void SetupConnectionUI()
        {
            // Wire up connection status click to show/hide panel
            if (_connectionStatusUI != null && _connectionPanel != null)
            {
                _connectionStatusUI.OnClicked += OnConnectionStatusClicked;
            }
        }

        private void OnConnectionStatusClicked()
        {
            if (_connectionPanel != null)
            {
                _connectionPanel.Toggle();
            }
        }

        private void SetupSessionInfoUI()
        {
            // Wire up session info panel expand click
            if (_sessionInfoPanel != null)
            {
                _sessionInfoPanel.OnExpandClicked += OnSessionInfoExpandClicked;

                // Set session manager reference
                if (GameManager.Instance != null)
                {
                    _sessionInfoPanel.SetSessionManager(GameManager.Instance.SessionManager);
                }
            }

            // Wire up shot history panel events
            if (_shotHistoryPanel != null)
            {
                _shotHistoryPanel.OnShotSelected += OnHistoryShotSelected;
                _shotHistoryPanel.OnReplayRequested += OnReplayRequested;

                // Set session manager reference
                if (GameManager.Instance != null)
                {
                    _shotHistoryPanel.SetSessionManager(GameManager.Instance.SessionManager);
                }
            }

            // Wire up shot detail modal events
            if (_shotDetailModal != null)
            {
                _shotDetailModal.OnReplayRequested += OnReplayRequested;

                // Set session manager reference
                if (GameManager.Instance != null)
                {
                    _shotDetailModal.SetSessionManager(GameManager.Instance.SessionManager);
                }
            }
        }

        private void OnSessionInfoExpandClicked()
        {
            if (_shotHistoryPanel != null)
            {
                _shotHistoryPanel.Toggle();
            }
        }

        private void OnHistoryShotSelected(SessionShot shot)
        {
            if (_shotDetailModal != null && shot != null)
            {
                _shotDetailModal.Show(shot);
            }
        }

        private void OnReplayRequested(SessionShot shot)
        {
            if (shot?.Result == null) return;

            // Replay the shot visualization
            if (_ballController != null)
            {
                _ballController.PlayShot(shot.Result);
            }

            if (_trajectoryRenderer != null)
            {
                _trajectoryRenderer.ShowTrajectory(shot.Result);
            }

            // Update the data bar with the replayed shot data
            if (_shotDataBar != null && shot.ShotData != null)
            {
                _shotDataBar.UpdateDisplay(shot.ShotData, shot.Result);
            }

            if (_clubDataPanel != null && shot.ShotData != null)
            {
                _clubDataPanel.UpdateDisplay(shot.ShotData);
            }

            Debug.Log($"MarinaSceneController: Replaying shot #{shot.ShotNumber}");
        }

        private void SetupGSProUI()
        {
            if (_gsProModeUI == null) return;

            // Subscribe to UI events
            _gsProModeUI.OnConnectClicked += OnGSProConnectClicked;
            _gsProModeUI.OnDisconnectClicked += OnGSProDisconnectClicked;
            _gsProModeUI.OnModeChanged += OnGSProModeChanged;

            // Wire up the GSProClient from GameManager for status updates
            if (GameManager.Instance != null)
            {
                _gsProModeUI.SetClient(GameManager.Instance.GSProClient);

                // Initialize mode from current app mode
                _gsProModeUI.SetMode(GameManager.Instance.CurrentMode == AppMode.GSPro);

                // Initialize device ready state
                var status = GameManager.Instance.CurrentDeviceStatus;
                if (status.HasValue)
                {
                    _gsProModeUI.SetReadyState(status.Value.IsReady, status.Value.BallDetected);
                }

                // Subscribe to device status changes
                GameManager.Instance.OnDeviceStatusChanged += OnDeviceStatusChanged;
            }

            // Set default host/port from settings if available
            var settings = SettingsManager.Instance;
            if (settings != null)
            {
                _gsProModeUI.SetHostPort(settings.GSProHost, settings.GSProPort);
            }
        }

        private void OnGSProConnectClicked()
        {
            if (GameManager.Instance == null || _gsProModeUI == null) return;

            string host = _gsProModeUI.Host;
            int port = _gsProModeUI.Port;

            Debug.Log($"MarinaSceneController: GSPro connect requested to {host}:{port}");

            // Update connection state to show "Connecting..."
            _gsProModeUI.SetConnectionState(GSProConnectionState.Connecting);

            // Initiate connection through GameManager
            GameManager.Instance.ConnectToGSPro(host, port);

            // Wire up client for status updates after connection attempt
            _gsProModeUI.SetClient(GameManager.Instance.GSProClient);
        }

        private void OnGSProDisconnectClicked()
        {
            if (GameManager.Instance == null) return;

            Debug.Log("MarinaSceneController: GSPro disconnect requested");
            GameManager.Instance.DisconnectFromGSPro();
        }

        private void OnGSProModeChanged(bool isGSProMode)
        {
            if (GameManager.Instance == null) return;

            var newMode = isGSProMode ? AppMode.GSPro : AppMode.OpenRange;
            GameManager.Instance.SetMode(newMode);

            Debug.Log($"MarinaSceneController: Mode changed to {newMode}");
        }

        private void OnDeviceStatusChanged(GC2DeviceStatus status)
        {
            if (_gsProModeUI != null)
            {
                _gsProModeUI.SetReadyState(status.IsReady, status.BallDetected);
            }
        }
    }
}
