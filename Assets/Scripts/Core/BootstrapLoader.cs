// ABOUTME: Bootstrap scene initializer that sets up all managers and loads the main menu.
// ABOUTME: Runs in the Bootstrap scene to initialize the application in the correct order.

using System.Collections;
using UnityEngine;
using OpenRange.Utilities;

namespace OpenRange.Core
{
    /// <summary>
    /// Initializes all core managers and loads the main menu scene.
    /// Attach to a GameObject in the Bootstrap scene.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float _minimumLoadTime = 0.5f;
        [SerializeField] private bool _enableDebugLogging = true;

        [Header("Manager References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private SettingsManager _settingsManager;

        private void Start()
        {
            StartCoroutine(InitializeAndLoad());
        }

        private IEnumerator InitializeAndLoad()
        {
            float startTime = Time.realtimeSinceStartup;

            Log("Bootstrap: Starting initialization...");

            // Step 1: Ensure MainThreadDispatcher exists (highest priority for thread safety)
            EnsureMainThreadDispatcher();
            yield return null;

            // Step 2: Initialize SettingsManager (needed by other managers)
            yield return InitializeSettingsManager();

            // Step 3: Initialize GameManager (depends on settings)
            yield return InitializeGameManager();

            // Step 4: Wait for minimum load time (for visual smoothness)
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < _minimumLoadTime)
            {
                yield return new WaitForSeconds(_minimumLoadTime - elapsed);
            }

            // Step 5: Load the main menu
            Log("Bootstrap: Loading main menu...");
            SceneLoader.LoadMainMenu();
        }

        private void EnsureMainThreadDispatcher()
        {
            // Accessing Instance will auto-create if needed
            var dispatcher = MainThreadDispatcher.Instance;
            if (dispatcher != null)
            {
                Log("Bootstrap: MainThreadDispatcher ready");
            }
            else
            {
                Debug.LogError("Bootstrap: Failed to initialize MainThreadDispatcher");
            }
        }

        private IEnumerator InitializeSettingsManager()
        {
            // Check if we have a reference, otherwise find it
            if (_settingsManager == null)
            {
                _settingsManager = FindAnyObjectByType<SettingsManager>();
            }

            if (_settingsManager == null)
            {
                Debug.LogError("Bootstrap: SettingsManager not found in scene");
                yield break;
            }

            // Wait for settings to be initialized
            int attempts = 0;
            while (!_settingsManager.IsInitialized && attempts < 100)
            {
                yield return null;
                attempts++;
            }

            if (_settingsManager.IsInitialized)
            {
                Log("Bootstrap: SettingsManager initialized");
            }
            else
            {
                Debug.LogWarning("Bootstrap: SettingsManager initialization timed out");
            }
        }

        private IEnumerator InitializeGameManager()
        {
            // Check if we have a reference, otherwise find it
            if (_gameManager == null)
            {
                _gameManager = FindAnyObjectByType<GameManager>();
            }

            if (_gameManager == null)
            {
                Debug.LogError("Bootstrap: GameManager not found in scene");
                yield break;
            }

            // GameManager initializes in Awake/Start, just verify it exists
            if (GameManager.Instance != null)
            {
                Log("Bootstrap: GameManager initialized");
            }
            else
            {
                Debug.LogWarning("Bootstrap: GameManager.Instance is null after initialization");
            }

            yield return null;
        }

        private void Log(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log(message);
            }
        }
    }
}
