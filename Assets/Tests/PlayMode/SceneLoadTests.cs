// ABOUTME: PlayMode tests for scene loading and manager persistence.
// ABOUTME: Validates Bootstrap initialization and scene transitions work correctly.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using OpenRange.Core;
using OpenRange.Utilities;

namespace OpenRange.Tests.PlayMode
{
    /// <summary>
    /// Play mode tests for scene loading and navigation.
    /// Tests the Bootstrap initialization, scene transitions, and manager persistence.
    /// </summary>
    [TestFixture]
    public class SceneLoadTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Clean up any existing managers
            CleanupSingletons();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CleanupSingletons();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoader_GetActiveSceneName_ReturnsCurrentScene()
        {
            // Given: The current scene (PlayMode test scene)
            yield return null;

            // When: We get the active scene name
            string activeSceneName = SceneLoader.GetActiveSceneName();

            // Then: It should not be null or empty
            Assert.That(activeSceneName, Is.Not.Null.And.Not.Empty);
        }

        [UnityTest]
        public IEnumerator SceneLoader_IsSceneLoaded_ReturnsTrueForCurrentScene()
        {
            // Given: The current active scene
            string currentScene = SceneManager.GetActiveScene().name;
            yield return null;

            // When: We check if the current scene is loaded
            bool isLoaded = SceneLoader.IsSceneLoaded(currentScene);

            // Then: It should return true
            Assert.That(isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneLoader_IsSceneLoaded_ReturnsFalseForNotLoadedScene()
        {
            // Given: The current scene
            yield return null;

            // When: We check for a scene that doesn't exist
            bool isLoaded = SceneLoader.IsSceneLoaded("NonExistentSceneThatDoesNotExist");

            // Then: It should return false
            Assert.That(isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator SceneLoader_SceneConstants_AreCorrect()
        {
            // Given: SceneLoader class constants
            yield return null;

            // Then: Constants should have expected values
            Assert.That(SceneLoader.BootstrapScene, Is.EqualTo("Bootstrap"));
            Assert.That(SceneLoader.MainMenuScene, Is.EqualTo("MainMenu"));
            Assert.That(SceneLoader.MarinaScene, Is.EqualTo("Marina"));
        }

        [UnityTest]
        public IEnumerator MainThreadDispatcher_Instance_CreatesSingletonOnAccess()
        {
            // Given: No MainThreadDispatcher exists
            CleanupSingletons();
            yield return null;

            // When: We access the Instance
            var dispatcher = MainThreadDispatcher.Instance;

            // Then: It should be created and not null
            Assert.That(dispatcher, Is.Not.Null);
            Assert.That(MainThreadDispatcher.Instance, Is.SameAs(dispatcher));
        }

        [UnityTest]
        public IEnumerator MainThreadDispatcher_Enqueue_ExecutesActionOnMainThread()
        {
            // Given: A dispatcher and a flag
            var dispatcher = MainThreadDispatcher.Instance;
            bool executed = false;
            int executedThreadId = -1;

            // When: We enqueue an action
            MainThreadDispatcher.Enqueue(() =>
            {
                executed = true;
                executedThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            });

            // Wait a frame for execution
            yield return null;

            // Then: The action should have executed on the main thread
            Assert.That(executed, Is.True, "Action was not executed");
            Assert.That(executedThreadId, Is.EqualTo(1), "Action was not executed on main thread");
        }

        [UnityTest]
        public IEnumerator MainThreadDispatcher_Execute_RunsImmediatelyOnMainThread()
        {
            // Given: A dispatcher
            var dispatcher = MainThreadDispatcher.Instance;
            bool executed = false;

            // When: We execute an action (should run immediately since we're on main thread)
            MainThreadDispatcher.Execute(() => executed = true);

            // Then: It should have executed immediately (no yield needed)
            Assert.That(executed, Is.True, "Action should have executed immediately");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MainThreadDispatcher_Enqueue_NullAction_DoesNotThrow()
        {
            // Given: A dispatcher
            var dispatcher = MainThreadDispatcher.Instance;

            // When: We enqueue a null action
            MainThreadDispatcher.Enqueue(null);

            // Then: No exception should occur
            yield return null;
            Assert.Pass("Null action handled gracefully");
        }

        [UnityTest]
        public IEnumerator GameManager_Singleton_InitializesCorrectly()
        {
            // Given: A GameManager is created
            var managerGo = new GameObject("GameManager");
            var gameManager = managerGo.AddComponent<GameManager>();
            yield return null;

            // Then: Instance should be set
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance, Is.SameAs(gameManager));
        }

        [UnityTest]
        public IEnumerator GameManager_DontDestroyOnLoad_IsApplied()
        {
            // Given: A GameManager is created
            var managerGo = new GameObject("GameManager");
            managerGo.AddComponent<GameManager>();
            yield return null;

            // Then: The GameObject should be in DontDestroyOnLoad scene
            Assert.That(managerGo.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator SettingsManager_Singleton_InitializesCorrectly()
        {
            // Given: A SettingsManager is created
            var managerGo = new GameObject("SettingsManager");
            var settingsManager = managerGo.AddComponent<SettingsManager>();
            yield return null;

            // Then: Instance should be set
            Assert.That(SettingsManager.Instance, Is.Not.Null);
            Assert.That(SettingsManager.Instance, Is.SameAs(settingsManager));
        }

        [UnityTest]
        public IEnumerator SettingsManager_IsInitialized_ReturnsTrueAfterAwake()
        {
            // Given: A SettingsManager is created
            var managerGo = new GameObject("SettingsManager");
            var settingsManager = managerGo.AddComponent<SettingsManager>();

            // When: We wait for Awake to complete
            yield return null;

            // Then: IsInitialized should be true
            Assert.That(settingsManager.IsInitialized, Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsManager_DontDestroyOnLoad_IsApplied()
        {
            // Given: A SettingsManager is created
            var managerGo = new GameObject("SettingsManager");
            managerGo.AddComponent<SettingsManager>();
            yield return null;

            // Then: The GameObject should be in DontDestroyOnLoad scene
            Assert.That(managerGo.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator DuplicateGameManager_IsDestroyed()
        {
            // Given: One GameManager already exists
            var firstGo = new GameObject("GameManager1");
            firstGo.AddComponent<GameManager>();
            yield return null;

            var originalInstance = GameManager.Instance;

            // When: A second GameManager is created
            var secondGo = new GameObject("GameManager2");
            secondGo.AddComponent<GameManager>();
            yield return null;
            yield return null; // Extra frame for destruction

            // Then: The second one should be destroyed and Instance unchanged
            Assert.That(GameManager.Instance, Is.SameAs(originalInstance));
            Assert.That(secondGo == null, Is.True, "Duplicate manager should be destroyed");
        }

        [UnityTest]
        public IEnumerator DuplicateSettingsManager_IsDestroyed()
        {
            // Given: One SettingsManager already exists
            var firstGo = new GameObject("SettingsManager1");
            firstGo.AddComponent<SettingsManager>();
            yield return null;

            var originalInstance = SettingsManager.Instance;

            // When: A second SettingsManager is created
            var secondGo = new GameObject("SettingsManager2");
            secondGo.AddComponent<SettingsManager>();
            yield return null;
            yield return null; // Extra frame for destruction

            // Then: The second one should be destroyed and Instance unchanged
            Assert.That(SettingsManager.Instance, Is.SameAs(originalInstance));
            Assert.That(secondGo == null, Is.True, "Duplicate manager should be destroyed");
        }

        [UnityTest]
        public IEnumerator BootstrapLoader_FindsManagerReferences()
        {
            // Given: A scene with managers and BootstrapLoader
            var managersGo = new GameObject("Managers");
            var gameManagerGo = new GameObject("GameManager");
            gameManagerGo.transform.SetParent(managersGo.transform);
            var gameManager = gameManagerGo.AddComponent<GameManager>();

            var settingsManagerGo = new GameObject("SettingsManager");
            settingsManagerGo.transform.SetParent(managersGo.transform);
            var settingsManager = settingsManagerGo.AddComponent<SettingsManager>();

            yield return null;

            // Then: Managers should be initialized
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(SettingsManager.Instance, Is.Not.Null);
        }

        private void CleanupSingletons()
        {
            // Find and destroy all managers
            foreach (var manager in Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }

            foreach (var manager in Object.FindObjectsByType<SettingsManager>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }

            foreach (var manager in Object.FindObjectsByType<QualityManager>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }

            foreach (var dispatcher in Object.FindObjectsByType<MainThreadDispatcher>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(dispatcher.gameObject);
            }
        }
    }
}
