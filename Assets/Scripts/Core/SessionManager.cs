// ABOUTME: Service that tracks practice session state and shot history.
// ABOUTME: Provides statistics calculation, shot retrieval, and session lifecycle events.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OpenRange.GC2;
using OpenRange.Physics;

namespace OpenRange.Core
{
    /// <summary>
    /// Tracks session state including shot history, statistics, and session lifecycle.
    /// Listens to ShotProcessor events and maintains the session record.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        [Header("Session Configuration")]
        [SerializeField] private int _maxHistorySize = 500;
        [SerializeField] private bool _enableDebugLogging = false;

        private DateTime _sessionStartTime;
        private bool _isActive;
        private readonly List<SessionShot> _shotHistory = new List<SessionShot>();
        private int _totalShots;

        // Cached statistics
        private float _averageBallSpeed;
        private float _averageCarryDistance;
        private float _longestCarry;
        private SessionShot _bestShot;

        /// <summary>Maximum number of shots to retain in history.</summary>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set => _maxHistorySize = Mathf.Max(1, value);
        }

        /// <summary>When the current session started.</summary>
        public DateTime SessionStartTime => _sessionStartTime;

        /// <summary>Time elapsed since session started.</summary>
        public TimeSpan ElapsedTime => _isActive ? DateTime.UtcNow - _sessionStartTime : TimeSpan.Zero;

        /// <summary>Total shots recorded in this session.</summary>
        public int TotalShots => _totalShots;

        /// <summary>Whether a session is currently active.</summary>
        public bool IsActive => _isActive;

        /// <summary>Number of shots currently in history.</summary>
        public int ShotCount => _shotHistory.Count;

        /// <summary>Average ball speed across all shots in session (mph).</summary>
        public float AverageBallSpeed => _averageBallSpeed;

        /// <summary>Average carry distance across all shots in session (yards).</summary>
        public float AverageCarryDistance => _averageCarryDistance;

        /// <summary>Longest carry distance in session (yards).</summary>
        public float LongestCarry => _longestCarry;

        /// <summary>Shot with the longest carry distance.</summary>
        public SessionShot BestShot => _bestShot;

        /// <summary>Fired when a new session starts.</summary>
        public event Action OnSessionStarted;

        /// <summary>Fired when the current session ends.</summary>
        public event Action OnSessionEnded;

        /// <summary>Fired when a shot is recorded to the session.</summary>
        public event Action<SessionShot> OnShotRecorded;

        /// <summary>Fired when session statistics are recalculated.</summary>
        public event Action OnStatisticsUpdated;

        private void Start()
        {
            SubscribeToShotProcessor();
        }

        private void OnDestroy()
        {
            UnsubscribeFromShotProcessor();
        }

        /// <summary>
        /// Start a new practice session.
        /// </summary>
        public void StartNewSession()
        {
            if (_isActive)
            {
                EndSession();
            }

            _sessionStartTime = DateTime.UtcNow;
            _isActive = true;
            _totalShots = 0;
            _shotHistory.Clear();
            ResetStatistics();

            if (_enableDebugLogging)
            {
                Debug.Log($"SessionManager: New session started at {_sessionStartTime:HH:mm:ss}");
            }

            OnSessionStarted?.Invoke();
        }

        /// <summary>
        /// End the current session.
        /// </summary>
        public void EndSession()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;

            if (_enableDebugLogging)
            {
                Debug.Log($"SessionManager: Session ended - {_totalShots} shots, " +
                          $"duration: {ElapsedTime:hh\\:mm\\:ss}");
            }

            OnSessionEnded?.Invoke();
        }

        /// <summary>
        /// Record a shot to the session history.
        /// </summary>
        /// <param name="shot">Raw shot data from GC2.</param>
        /// <param name="result">Physics simulation result.</param>
        public void RecordShot(GC2ShotData shot, ShotResult result)
        {
            if (shot == null || result == null)
            {
                if (_enableDebugLogging)
                {
                    Debug.LogWarning("SessionManager: Cannot record null shot or result");
                }
                return;
            }

            // Auto-start session if not active
            if (!_isActive)
            {
                StartNewSession();
            }

            _totalShots++;

            var sessionShot = new SessionShot
            {
                ShotData = shot,
                Result = result,
                Timestamp = DateTime.UtcNow,
                ShotNumber = _totalShots
            };

            // Add to history
            _shotHistory.Add(sessionShot);

            // Trim history if needed
            while (_shotHistory.Count > _maxHistorySize)
            {
                _shotHistory.RemoveAt(0);
            }

            if (_enableDebugLogging)
            {
                Debug.Log($"SessionManager: Shot #{_totalShots} recorded - " +
                          $"Carry: {result.CarryDistance:F1} yds, Speed: {shot.BallSpeed:F1} mph");
            }

            // Update statistics
            RecalculateStatistics();

            OnShotRecorded?.Invoke(sessionShot);
        }

        /// <summary>
        /// Get a shot from history by index (0 = oldest in history).
        /// </summary>
        /// <param name="index">Index into the history list.</param>
        /// <returns>The session shot, or null if index is out of range.</returns>
        public SessionShot GetShot(int index)
        {
            if (index < 0 || index >= _shotHistory.Count)
            {
                return null;
            }

            return _shotHistory[index];
        }

        /// <summary>
        /// Get the most recent shot.
        /// </summary>
        /// <returns>The latest session shot, or null if no shots recorded.</returns>
        public SessionShot GetLatestShot()
        {
            if (_shotHistory.Count == 0)
            {
                return null;
            }

            return _shotHistory[_shotHistory.Count - 1];
        }

        /// <summary>
        /// Get all shots in the current session.
        /// </summary>
        /// <returns>Read-only list of all session shots.</returns>
        public IReadOnlyList<SessionShot> GetAllShots()
        {
            return _shotHistory.AsReadOnly();
        }

        /// <summary>
        /// Get recent shots (most recent first).
        /// </summary>
        /// <param name="count">Maximum number of shots to return.</param>
        /// <returns>List of recent shots, most recent first.</returns>
        public List<SessionShot> GetRecentShots(int count)
        {
            count = Mathf.Min(count, _shotHistory.Count);
            var recent = new List<SessionShot>(count);

            for (int i = _shotHistory.Count - 1; i >= _shotHistory.Count - count; i--)
            {
                recent.Add(_shotHistory[i]);
            }

            return recent;
        }

        /// <summary>
        /// Clear all shot history but keep session active.
        /// </summary>
        public void ClearHistory()
        {
            _shotHistory.Clear();
            _totalShots = 0;
            ResetStatistics();

            if (_enableDebugLogging)
            {
                Debug.Log("SessionManager: History cleared");
            }

            OnStatisticsUpdated?.Invoke();
        }

        /// <summary>
        /// Subscribe to ShotProcessor events.
        /// </summary>
        private void SubscribeToShotProcessor()
        {
            var shotProcessor = FindObjectOfType<ShotProcessor>();
            if (shotProcessor != null)
            {
                shotProcessor.OnShotProcessed += HandleShotProcessed;

                if (_enableDebugLogging)
                {
                    Debug.Log("SessionManager: Subscribed to ShotProcessor events");
                }
            }
            else if (_enableDebugLogging)
            {
                Debug.LogWarning("SessionManager: ShotProcessor not found, will not auto-record shots");
            }
        }

        /// <summary>
        /// Unsubscribe from ShotProcessor events.
        /// </summary>
        private void UnsubscribeFromShotProcessor()
        {
            var shotProcessor = FindObjectOfType<ShotProcessor>();
            if (shotProcessor != null)
            {
                shotProcessor.OnShotProcessed -= HandleShotProcessed;
            }
        }

        /// <summary>
        /// Handle shot processed event from ShotProcessor.
        /// </summary>
        private void HandleShotProcessed(GC2ShotData shot, ShotResult result)
        {
            RecordShot(shot, result);
        }

        /// <summary>
        /// Reset statistics to defaults.
        /// </summary>
        private void ResetStatistics()
        {
            _averageBallSpeed = 0f;
            _averageCarryDistance = 0f;
            _longestCarry = 0f;
            _bestShot = null;
        }

        /// <summary>
        /// Recalculate all statistics from shot history.
        /// </summary>
        private void RecalculateStatistics()
        {
            if (_shotHistory.Count == 0)
            {
                ResetStatistics();
                OnStatisticsUpdated?.Invoke();
                return;
            }

            float totalSpeed = 0f;
            float totalCarry = 0f;
            float maxCarry = 0f;
            SessionShot best = null;

            foreach (var shot in _shotHistory)
            {
                totalSpeed += shot.ShotData.BallSpeed;
                totalCarry += shot.Result.CarryDistance;

                if (shot.Result.CarryDistance > maxCarry)
                {
                    maxCarry = shot.Result.CarryDistance;
                    best = shot;
                }
            }

            _averageBallSpeed = totalSpeed / _shotHistory.Count;
            _averageCarryDistance = totalCarry / _shotHistory.Count;
            _longestCarry = maxCarry;
            _bestShot = best;

            OnStatisticsUpdated?.Invoke();
        }
    }

    /// <summary>
    /// A recorded shot in a session with metadata.
    /// </summary>
    [Serializable]
    public class SessionShot
    {
        /// <summary>Raw shot data from GC2.</summary>
        public GC2ShotData ShotData;

        /// <summary>Physics simulation result.</summary>
        public ShotResult Result;

        /// <summary>When the shot was recorded.</summary>
        public DateTime Timestamp;

        /// <summary>Shot number in the session (1-based).</summary>
        public int ShotNumber;

        public override string ToString()
        {
            return $"Shot #{ShotNumber}: {ShotData?.BallSpeed:F1} mph → " +
                   $"{Result?.CarryDistance:F1} yds carry";
        }
    }
}
