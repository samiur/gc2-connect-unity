// ABOUTME: Toast notification component for displaying temporary messages.
// ABOUTME: Used by UIManager to show toast notifications with fade animations.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenRange.UI
{
    /// <summary>
    /// Toast notification component for displaying temporary messages.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private string _message;
        private float _duration;
        private ToastType _type;
        private TextMeshProUGUI _text;
        private Image _background;
        private CanvasGroup _canvasGroup;
        private bool _isShowing;
        private float _showTime;

        /// <summary>
        /// Fired when the toast is dismissed.
        /// </summary>
        public event Action OnDismissed;

        /// <summary>
        /// Initialize the toast.
        /// </summary>
        public void Initialize(string message, float duration, ToastType type)
        {
            _message = message;
            _duration = duration;
            _type = type;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _text = GetComponentInChildren<TextMeshProUGUI>();
            _background = GetComponent<Image>();

            if (_text != null)
            {
                _text.text = message;
            }

            if (_background != null)
            {
                _background.color = GetColorForType(type);
            }
        }

        /// <summary>
        /// Show the toast with animation.
        /// </summary>
        public void Show()
        {
            _isShowing = true;
            _showTime = Time.time;
            StartCoroutine(ShowAnimation());
        }

        /// <summary>
        /// Dismiss the toast immediately.
        /// </summary>
        public void Dismiss()
        {
            if (!_isShowing) return;
            _isShowing = false;
            StartCoroutine(HideAnimation());
        }

        private void Update()
        {
            if (_isShowing && Time.time - _showTime >= _duration)
            {
                Dismiss();
            }
        }

        private IEnumerator ShowAnimation()
        {
            float elapsed = 0f;
            float duration = UITheme.Animation.ToastSlide;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            float duration = UITheme.Animation.ToastSlide;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            OnDismissed?.Invoke();
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
    }
}
