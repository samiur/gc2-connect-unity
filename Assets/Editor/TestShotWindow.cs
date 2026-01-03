// ABOUTME: Editor window for firing test shots without GC2 hardware.
// ABOUTME: Allows testing ball flight, camera follow, and trajectory rendering during development.

using UnityEditor;
using UnityEngine;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;
using OpenRange.UI;
using OpenRange.Visualization;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor window for firing test shots without GC2 hardware.
    /// Useful for development and testing ball flight visualization.
    /// </summary>
    public class TestShotWindow : EditorWindow
    {
        // Shot parameters
        private float _ballSpeed = 150f;
        private float _launchAngle = 12f;
        private float _azimuth = 0f;
        private float _backSpin = 3000f;
        private float _sideSpin = 0f;

        // Presets
        private int _selectedPreset = 0;
        private readonly string[] _presetNames = { "Custom", "Driver", "7-Iron", "Wedge", "Hook", "Slice" };

        // Environmental conditions
        private float _temperature = 70f;
        private float _elevation = 0f;
        private float _humidity = 50f;
        private float _windSpeed = 0f;
        private float _windDirection = 0f;

        // Foldout states
        private bool _showShotParams = true;
        private bool _showEnvironment = false;
        private bool _showQuickActions = true;

        [MenuItem("OpenRange/Test Shot Window", priority = 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<TestShotWindow>("Test Shot");
            window.minSize = new Vector2(300, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Shot Window", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Fire test shots to see ball flight and trajectory without GC2 hardware.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Preset selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(50));
            int newPreset = EditorGUILayout.Popup(_selectedPreset, _presetNames);
            if (newPreset != _selectedPreset)
            {
                _selectedPreset = newPreset;
                ApplyPreset(newPreset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Shot parameters
            _showShotParams = EditorGUILayout.Foldout(_showShotParams, "Shot Parameters", true);
            if (_showShotParams)
            {
                EditorGUI.indentLevel++;
                _ballSpeed = EditorGUILayout.Slider("Ball Speed (mph)", _ballSpeed, 50f, 200f);
                _launchAngle = EditorGUILayout.Slider("Launch Angle (°)", _launchAngle, 0f, 45f);
                _azimuth = EditorGUILayout.Slider("Azimuth (°)", _azimuth, -20f, 20f);
                _backSpin = EditorGUILayout.Slider("Back Spin (rpm)", _backSpin, 0f, 12000f);
                _sideSpin = EditorGUILayout.Slider("Side Spin (rpm)", _sideSpin, -3000f, 3000f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Environmental conditions
            _showEnvironment = EditorGUILayout.Foldout(_showEnvironment, "Environment", true);
            if (_showEnvironment)
            {
                EditorGUI.indentLevel++;
                _temperature = EditorGUILayout.Slider("Temperature (°F)", _temperature, 40f, 100f);
                _elevation = EditorGUILayout.Slider("Elevation (ft)", _elevation, 0f, 8000f);
                _humidity = EditorGUILayout.Slider("Humidity (%)", _humidity, 0f, 100f);
                _windSpeed = EditorGUILayout.Slider("Wind Speed (mph)", _windSpeed, 0f, 30f);
                _windDirection = EditorGUILayout.Slider("Wind Direction (°)", _windDirection, 0f, 360f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Quick actions
            _showQuickActions = EditorGUILayout.Foldout(_showQuickActions, "Quick Actions", true);
            if (_showQuickActions)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Driver", GUILayout.Height(30)))
                {
                    ApplyPreset(1);
                    FireTestShot();
                }
                if (GUILayout.Button("7-Iron", GUILayout.Height(30)))
                {
                    ApplyPreset(2);
                    FireTestShot();
                }
                if (GUILayout.Button("Wedge", GUILayout.Height(30)))
                {
                    ApplyPreset(3);
                    FireTestShot();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // Fire button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Fire Test Shot", GUILayout.Height(40)))
            {
                FireTestShot();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // Reset button
            if (GUILayout.Button("Reset Ball"))
            {
                ResetBall();
            }

            // Status
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to fire test shots.", MessageType.Warning);
            }
            else
            {
                var shotProcessor = Object.FindAnyObjectByType<ShotProcessor>();
                var ballController = Object.FindAnyObjectByType<BallController>();

                EditorGUILayout.LabelField("ShotProcessor: " + (shotProcessor != null ? "Found" : "Not Found"));
                EditorGUILayout.LabelField("BallController: " + (ballController != null ? "Found" : "Not Found"));

                if (ballController != null)
                {
                    EditorGUILayout.LabelField("Ball Phase: " + ballController.CurrentPhase.ToString());
                    EditorGUILayout.LabelField("Animating: " + ballController.IsAnimating);
                }
            }
        }

        private void ApplyPreset(int preset)
        {
            switch (preset)
            {
                case 1: // Driver
                    _ballSpeed = 167f;
                    _launchAngle = 10.9f;
                    _azimuth = 0f;
                    _backSpin = 2686f;
                    _sideSpin = 0f;
                    break;

                case 2: // 7-Iron
                    _ballSpeed = 120f;
                    _launchAngle = 16.3f;
                    _azimuth = 0f;
                    _backSpin = 7097f;
                    _sideSpin = 0f;
                    break;

                case 3: // Wedge
                    _ballSpeed = 102f;
                    _launchAngle = 24.2f;
                    _azimuth = 0f;
                    _backSpin = 9304f;
                    _sideSpin = 0f;
                    break;

                case 4: // Hook
                    _ballSpeed = 150f;
                    _launchAngle = 12f;
                    _azimuth = 0f;
                    _backSpin = 3000f;
                    _sideSpin = -1500f;
                    break;

                case 5: // Slice
                    _ballSpeed = 150f;
                    _launchAngle = 12f;
                    _azimuth = 0f;
                    _backSpin = 3000f;
                    _sideSpin = 1500f;
                    break;
            }

            Repaint();
        }

        private void FireTestShot()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("TestShotWindow: Enter Play Mode to fire test shots.");
                return;
            }

            // Create shot data with correct property names from GC2ShotData
            var shotData = new GC2ShotData
            {
                BallSpeed = _ballSpeed,
                LaunchAngle = _launchAngle,
                Direction = _azimuth,
                TotalSpin = Mathf.Sqrt(_backSpin * _backSpin + _sideSpin * _sideSpin),
                BackSpin = _backSpin,
                SideSpin = _sideSpin,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HasClubData = false
            };

            // Find ShotProcessor
            var shotProcessor = Object.FindAnyObjectByType<ShotProcessor>();
            if (shotProcessor != null)
            {
                // Set environmental conditions (includes wind)
                shotProcessor.SetEnvironmentalConditions(_temperature, _elevation, _humidity, _windSpeed, _windDirection);

                // Process the shot
                shotProcessor.ProcessShot(shotData);
                Debug.Log($"TestShotWindow: Fired test shot - {_ballSpeed:F1} mph, {_launchAngle:F1}° launch, {_backSpin:F0} rpm backspin");
            }
            else
            {
                // Fallback: Direct simulation if no ShotProcessor
                Debug.Log("TestShotWindow: No ShotProcessor found, running direct simulation...");
                RunDirectSimulation(shotData);
            }
        }

        private void RunDirectSimulation(GC2ShotData shotData)
        {
            // Create simulator with constructor parameters
            var simulator = new TrajectorySimulator(
                tempF: _temperature,
                elevationFt: _elevation,
                humidityPct: _humidity,
                windSpeedMph: _windSpeed,
                windDirDeg: _windDirection
            );

            // Run simulation
            var result = simulator.Simulate(
                shotData.BallSpeed,
                shotData.LaunchAngle,
                shotData.Direction,
                shotData.BackSpin,
                shotData.SideSpin
            );

            Debug.Log($"TestShotWindow: Simulated - Carry: {result.CarryDistance:F1} yds, Total: {result.TotalDistance:F1} yds, Apex: {result.MaxHeight:F1} ft, " +
                      $"Landing: {result.LandingAngle:F1}° @ {result.LandingSpeed:F1} mph, Roll: {result.RollDistance:F1} yds");

            // Try to play the result
            var ballController = Object.FindAnyObjectByType<BallController>();
            if (ballController != null)
            {
                ballController.PlayShot(result);
            }

            // Try to show trajectory
            var trajectoryRenderer = Object.FindAnyObjectByType<TrajectoryRenderer>();
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.ShowTrajectory(result);
            }

            // Update ShotDataBar
            var shotDataBar = Object.FindAnyObjectByType<ShotDataBar>();
            if (shotDataBar != null)
            {
                shotDataBar.UpdateDisplay(shotData, result);
            }
        }

        private void ResetBall()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var ballController = Object.FindAnyObjectByType<BallController>();
            if (ballController != null)
            {
                ballController.Reset();
                Debug.Log("TestShotWindow: Ball reset to tee position");
            }

            var trajectoryRenderer = Object.FindAnyObjectByType<TrajectoryRenderer>();
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.Hide();
            }
        }
    }
}
