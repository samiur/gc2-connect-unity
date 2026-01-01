// ABOUTME: Detects screen size, orientation, and provides responsive layout utilities.
// ABOUTME: Fires events when layout changes and provides screen category detection.

using System;
using UnityEngine;

namespace OpenRange.UI
{
    /// <summary>
    /// Monitors screen size and orientation to provide responsive layout support.
    /// Fires events when the layout changes and provides utility methods for
    /// detecting screen category and safe area.
    /// </summary>
    public class ResponsiveLayout : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logLayoutChanges = false;

        private ScreenCategory _currentCategory;
        private ScreenOrientation _currentOrientation;
        private Vector2 _lastScreenSize;
        private Rect _lastSafeArea;

        /// <summary>
        /// Current screen category based on width.
        /// </summary>
        public ScreenCategory CurrentCategory => _currentCategory;

        /// <summary>
        /// Current screen orientation.
        /// </summary>
        public ScreenOrientation CurrentOrientation => _currentOrientation;

        /// <summary>
        /// Current screen width in pixels.
        /// </summary>
        public float ScreenWidth => Screen.width;

        /// <summary>
        /// Current screen height in pixels.
        /// </summary>
        public float ScreenHeight => Screen.height;

        /// <summary>
        /// Current safe area rect.
        /// </summary>
        public Rect SafeArea => Screen.safeArea;

        /// <summary>
        /// Fired when the screen category, orientation, or safe area changes.
        /// </summary>
        public event Action<ScreenCategory> OnLayoutChanged;

        /// <summary>
        /// Fired when screen orientation specifically changes.
        /// </summary>
        public event Action<ScreenOrientation> OnOrientationChanged;

        /// <summary>
        /// Fired when safe area changes.
        /// </summary>
        public event Action<Rect> OnSafeAreaChanged;

        private void Awake()
        {
            InitializeLayout();
        }

        private void Start()
        {
            // Re-check in case screen changed between Awake and Start
            CheckForLayoutChanges();
        }

        private void Update()
        {
            CheckForLayoutChanges();
        }

        /// <summary>
        /// Initialize layout state.
        /// </summary>
        private void InitializeLayout()
        {
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            _lastSafeArea = Screen.safeArea;
            _currentCategory = CalculateScreenCategory(Screen.width);
            _currentOrientation = Screen.orientation;

            if (_logLayoutChanges)
            {
                Debug.Log($"ResponsiveLayout initialized: {_currentCategory}, {Screen.width}x{Screen.height}");
            }
        }

        /// <summary>
        /// Check for layout changes and fire events if needed.
        /// </summary>
        private void CheckForLayoutChanges()
        {
            bool changed = false;
            Vector2 currentSize = new Vector2(Screen.width, Screen.height);

            // Check screen size change
            if (currentSize != _lastScreenSize)
            {
                _lastScreenSize = currentSize;
                var newCategory = CalculateScreenCategory(Screen.width);

                if (newCategory != _currentCategory)
                {
                    _currentCategory = newCategory;
                    changed = true;

                    if (_logLayoutChanges)
                    {
                        Debug.Log($"ResponsiveLayout: Category changed to {_currentCategory}");
                    }
                }
            }

            // Check orientation change
            if (Screen.orientation != _currentOrientation)
            {
                _currentOrientation = Screen.orientation;
                changed = true;

                if (_logLayoutChanges)
                {
                    Debug.Log($"ResponsiveLayout: Orientation changed to {_currentOrientation}");
                }

                OnOrientationChanged?.Invoke(_currentOrientation);
            }

            // Check safe area change
            Rect currentSafeArea = Screen.safeArea;
            if (currentSafeArea != _lastSafeArea)
            {
                _lastSafeArea = currentSafeArea;

                if (_logLayoutChanges)
                {
                    Debug.Log($"ResponsiveLayout: Safe area changed to {currentSafeArea}");
                }

                OnSafeAreaChanged?.Invoke(currentSafeArea);
            }

            // Fire layout changed event
            if (changed)
            {
                OnLayoutChanged?.Invoke(_currentCategory);
            }
        }

        /// <summary>
        /// Get the current screen category based on width.
        /// </summary>
        /// <returns>The screen category.</returns>
        public ScreenCategory GetScreenCategory()
        {
            return _currentCategory;
        }

        /// <summary>
        /// Get the safe area rect.
        /// </summary>
        /// <returns>The safe area in screen coordinates.</returns>
        public Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        /// <summary>
        /// Calculate the screen category from a width value.
        /// </summary>
        /// <param name="width">Screen width in pixels.</param>
        /// <returns>The screen category for that width.</returns>
        public static ScreenCategory CalculateScreenCategory(float width)
        {
            if (width < UITheme.Breakpoints.Compact)
            {
                return ScreenCategory.Compact;
            }
            else if (width < UITheme.Breakpoints.Regular)
            {
                return ScreenCategory.Regular;
            }
            else
            {
                return ScreenCategory.Large;
            }
        }

        /// <summary>
        /// Get the diagonal screen size in inches.
        /// </summary>
        /// <returns>Diagonal size in inches, or -1 if DPI is unavailable.</returns>
        public float GetDiagonalInches()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0)
            {
                // DPI not available, return -1 to indicate unknown
                return -1f;
            }

            float widthInches = Screen.width / dpi;
            float heightInches = Screen.height / dpi;

            return Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);
        }

        /// <summary>
        /// Check if the screen is in portrait orientation.
        /// </summary>
        /// <returns>True if portrait, false if landscape.</returns>
        public bool IsPortrait()
        {
            return Screen.height > Screen.width;
        }

        /// <summary>
        /// Check if the screen is in landscape orientation.
        /// </summary>
        /// <returns>True if landscape, false if portrait.</returns>
        public bool IsLandscape()
        {
            return Screen.width >= Screen.height;
        }

        /// <summary>
        /// Get the appropriate font size for the current screen category.
        /// </summary>
        /// <param name="fontCategory">The font category.</param>
        /// <returns>The font size in points.</returns>
        public int GetFontSize(FontCategory fontCategory)
        {
            return UITheme.GetFontSize(_currentCategory, fontCategory);
        }

        /// <summary>
        /// Force a layout update and fire events.
        /// </summary>
        public void ForceLayoutUpdate()
        {
            var oldCategory = _currentCategory;
            _currentCategory = CalculateScreenCategory(Screen.width);
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            _lastSafeArea = Screen.safeArea;
            _currentOrientation = Screen.orientation;

            OnLayoutChanged?.Invoke(_currentCategory);
            OnSafeAreaChanged?.Invoke(_lastSafeArea);

            if (_logLayoutChanges)
            {
                Debug.Log($"ResponsiveLayout: Forced update to {_currentCategory}");
            }
        }
    }
}
