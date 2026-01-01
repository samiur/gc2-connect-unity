// ABOUTME: Static utility class for scene transitions with sync and async loading.
// ABOUTME: Provides centralized scene navigation with progress callbacks.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenRange.Utilities;

namespace OpenRange.Core
{
    /// <summary>
    /// Static utility class for scene loading and transitions.
    /// Provides both synchronous and asynchronous scene loading with progress callbacks.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>Scene name for the bootstrap scene.</summary>
        public const string BootstrapScene = "Bootstrap";

        /// <summary>Scene name for the main menu.</summary>
        public const string MainMenuScene = "MainMenu";

        /// <summary>Scene name for the Marina driving range.</summary>
        public const string MarinaScene = "Marina";

        /// <summary>
        /// Load a scene synchronously by name.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="mode">Load mode (Single or Additive).</param>
        public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneLoader: Scene name cannot be null or empty");
                return;
            }

            Debug.Log($"SceneLoader: Loading scene '{sceneName}'");
            SceneManager.LoadScene(sceneName, mode);
        }

        /// <summary>
        /// Load a scene asynchronously with optional progress callback.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="progressCallback">Called with progress value (0-1) during load.</param>
        /// <param name="onComplete">Called when scene load completes.</param>
        /// <param name="mode">Load mode (Single or Additive).</param>
        /// <returns>The async operation for the scene load.</returns>
        public static AsyncOperation LoadSceneAsync(
            string sceneName,
            Action<float> progressCallback = null,
            Action onComplete = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneLoader: Scene name cannot be null or empty");
                return null;
            }

            Debug.Log($"SceneLoader: Loading scene '{sceneName}' asynchronously");

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
            if (asyncOp == null)
            {
                Debug.LogError($"SceneLoader: Failed to start async load for scene '{sceneName}'");
                return null;
            }

            // If we have callbacks, we need a coroutine runner
            if (progressCallback != null || onComplete != null)
            {
                // Use MainThreadDispatcher if available to track progress
                var dispatcher = MainThreadDispatcher.Instance;
                if (dispatcher != null)
                {
                    dispatcher.StartCoroutine(TrackProgress(asyncOp, progressCallback, onComplete));
                }
                else
                {
                    Debug.LogWarning("SceneLoader: No MainThreadDispatcher available for progress tracking");
                    asyncOp.completed += _ => onComplete?.Invoke();
                }
            }

            return asyncOp;
        }

        /// <summary>
        /// Load the main menu scene.
        /// </summary>
        public static void LoadMainMenu()
        {
            LoadScene(MainMenuScene);
        }

        /// <summary>
        /// Load the Marina driving range scene.
        /// </summary>
        public static void LoadMarina()
        {
            LoadScene(MarinaScene);
        }

        /// <summary>
        /// Load the main menu scene asynchronously.
        /// </summary>
        /// <param name="progressCallback">Called with progress value (0-1) during load.</param>
        /// <param name="onComplete">Called when scene load completes.</param>
        public static AsyncOperation LoadMainMenuAsync(
            Action<float> progressCallback = null,
            Action onComplete = null)
        {
            return LoadSceneAsync(MainMenuScene, progressCallback, onComplete);
        }

        /// <summary>
        /// Load the Marina driving range scene asynchronously.
        /// </summary>
        /// <param name="progressCallback">Called with progress value (0-1) during load.</param>
        /// <param name="onComplete">Called when scene load completes.</param>
        public static AsyncOperation LoadMarinaAsync(
            Action<float> progressCallback = null,
            Action onComplete = null)
        {
            return LoadSceneAsync(MarinaScene, progressCallback, onComplete);
        }

        /// <summary>
        /// Get the name of the currently active scene.
        /// </summary>
        /// <returns>The name of the active scene.</returns>
        public static string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Check if a scene is currently loaded.
        /// </summary>
        /// <param name="sceneName">Name of the scene to check.</param>
        /// <returns>True if the scene is loaded.</returns>
        public static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerator TrackProgress(
            AsyncOperation asyncOp,
            Action<float> progressCallback,
            Action onComplete)
        {
            while (!asyncOp.isDone)
            {
                progressCallback?.Invoke(asyncOp.progress);
                yield return null;
            }

            progressCallback?.Invoke(1f);
            onComplete?.Invoke();
        }
    }
}
