using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Utilities
{
    /// <summary>
    /// Dispatches actions to the Unity main thread.
    /// Required for callbacks from native plugins and async operations.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _actions = new Queue<Action>();
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Get or create the singleton instance.
        /// </summary>
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MainThreadDispatcher");
                    _instance = go.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Queue an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            
            lock (_lock)
            {
                _actions.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Execute an action on the main thread.
        /// If already on main thread, executes immediately.
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void Execute(Action action)
        {
            if (action == null) return;
            
            // Check if we're on the main thread
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
            {
                action();
            }
            else
            {
                Enqueue(action);
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            lock (_lock)
            {
                while (_actions.Count > 0)
                {
                    try
                    {
                        _actions.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"MainThreadDispatcher: Action failed - {ex.Message}");
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
