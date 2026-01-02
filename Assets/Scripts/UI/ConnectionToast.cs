// ABOUTME: Toast notification component for displaying connection status change messages.
// ABOUTME: Subscribes to GameManager.OnConnectionStateChanged and shows appropriate toast messages.

using System;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Toast notification for connection status changes.
    /// Shows a brief message when connection state changes.
    /// </summary>
    public class ConnectionToast : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Default toast display duration in seconds.
        /// </summary>
        public const float DefaultDuration = 3f;

        #endregion

        #region Serialized Fields

        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private ConnectionState _lastState = ConnectionState.Disconnected;
        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            // Subscribe to GameManager connection state changes if available
            if (GameManager.Instance != null && !_isSubscribed)
            {
                GameManager.Instance.OnConnectionStateChanged += OnConnectionStateChanged;
                _isSubscribed = true;

                // Set initial state (don't show toast for initial state)
                _lastState = GameManager.Instance.ConnectionState;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && _isSubscribed)
            {
                GameManager.Instance.OnConnectionStateChanged -= OnConnectionStateChanged;
                _isSubscribed = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a toast for the given connection state.
        /// </summary>
        /// <param name="state">The connection state to show a message for.</param>
        public void ShowForState(ConnectionState state)
        {
            string message = GetToastMessage(state);
            ToastType type = GetToastType(state);

            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = GetColorForType(type);
            }

            // Use UIManager to show the toast if available
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast(message, DefaultDuration, type);
            }
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(TextMeshProUGUI messageText, Image backgroundImage, CanvasGroup canvasGroup)
        {
            _messageText = messageText;
            _backgroundImage = backgroundImage;
            _canvasGroup = canvasGroup;
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Gets the toast message for a given connection state.
        /// </summary>
        public static string GetToastMessage(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => "GC2 Connected",
                ConnectionState.Connecting => "Connecting to GC2...",
                ConnectionState.Disconnected => "GC2 Disconnected",
                ConnectionState.DeviceNotFound => "No GC2 Device Found",
                ConnectionState.Failed => "Connection Failed",
                _ => "Connection Status Changed"
            };
        }

        /// <summary>
        /// Gets the toast type for a given connection state.
        /// </summary>
        public static ToastType GetToastType(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => ToastType.Success,
                ConnectionState.Connecting => ToastType.Info,
                ConnectionState.Disconnected => ToastType.Error,
                ConnectionState.DeviceNotFound => ToastType.Warning,
                ConnectionState.Failed => ToastType.Error,
                _ => ToastType.Info
            };
        }

        /// <summary>
        /// Determines whether a toast should be shown for a state change.
        /// </summary>
        /// <param name="state">The new connection state.</param>
        /// <returns>True if a toast should be shown.</returns>
        public static bool ShouldShowToast(ConnectionState state)
        {
            // Don't spam toasts for transient connecting state
            return state != ConnectionState.Connecting;
        }

        #endregion

        #region Private Methods

        private void OnConnectionStateChanged(ConnectionState state)
        {
            // Only show toast if state actually changed and we should show one
            if (state != _lastState && ShouldShowToast(state))
            {
                ShowForState(state);
            }

            _lastState = state;
        }

        private Color GetColorForType(ToastType type)
        {
            return type switch
            {
                ToastType.Success => UITheme.ToastSuccess,
                ToastType.Warning => UITheme.ToastWarning,
                ToastType.Error => UITheme.ToastError,
                _ => UITheme.ToastInfo
            };
        }

        #endregion
    }
}
