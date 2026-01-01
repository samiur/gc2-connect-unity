// ABOUTME: Editor tool that generates the CameraRig prefab with all camera components.
// ABOUTME: Creates a properly configured camera with CameraController, FollowCamera, and OrbitCamera.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using OpenRange.Visualization;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate the CameraRig prefab.
    /// </summary>
    public static class CameraRigGenerator
    {
        private const string PrefabsPath = "Assets/Prefabs/Camera";

        [MenuItem("OpenRange/Create Camera Rig Prefab", priority = 160)]
        public static void CreateCameraRigPrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Camera Rig Prefab",
                "This will create the CameraRig prefab with all camera components. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();
            CreateCameraRigPrefabAsset();
            AssetDatabase.Refresh();
            Debug.Log("CameraRigGenerator: Camera rig prefab created successfully!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Camera");
            }
        }

        /// <summary>
        /// Creates the CameraRig prefab with camera and components.
        /// </summary>
        private static void CreateCameraRigPrefabAsset()
        {
            // Create root object
            var rigGo = new GameObject("CameraRig");

            // Create camera object as child
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(rigGo.transform);
            cameraGo.transform.localPosition = new Vector3(0f, 3f, -8f);
            cameraGo.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            cameraGo.tag = "MainCamera";

            // Add Camera component
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.depth = 0;

            // Add AudioListener
            cameraGo.AddComponent<AudioListener>();

            // Add CameraController to root
            var controller = rigGo.AddComponent<CameraController>();

            // Add FollowCamera
            var followCamera = rigGo.AddComponent<FollowCamera>();

            // Add OrbitCamera
            var orbitCamera = rigGo.AddComponent<OrbitCamera>();

            // Configure CameraController via SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_cameraTransform").objectReferenceValue = cameraGo.transform;
            so.FindProperty("_defaultMode").enumValueIndex = 0; // Static
            so.FindProperty("_autoSwitchModes").boolValue = true;
            so.FindProperty("_transitionDuration").floatValue = 0.5f;
            so.FindProperty("_staticPosition").vector3Value = new Vector3(0f, 3f, -8f);
            so.FindProperty("_staticLookAt").vector3Value = new Vector3(0f, 0f, 50f);
            so.FindProperty("_topDownHeight").floatValue = 100f;
            so.FindProperty("_topDownCenter").vector3Value = new Vector3(0f, 0f, 125f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Configure FollowCamera via SerializedObject
            var followSo = new SerializedObject(followCamera);
            followSo.FindProperty("_offset").vector3Value = new Vector3(0f, 5f, -15f);
            followSo.FindProperty("_followDamping").floatValue = 5f;
            followSo.FindProperty("_lookDamping").floatValue = 8f;
            followSo.FindProperty("_minHeight").floatValue = 2f;
            followSo.FindProperty("_heightMultiplier").floatValue = 0.5f;
            followSo.FindProperty("_maxHeight").floatValue = 50f;
            followSo.FindProperty("_lookAheadDistance").floatValue = 10f;
            followSo.FindProperty("_groundClearance").floatValue = 1f;
            followSo.ApplyModifiedPropertiesWithoutUndo();

            // Configure OrbitCamera via SerializedObject
            var orbitSo = new SerializedObject(orbitCamera);
            orbitSo.FindProperty("_orbitCenter").vector3Value = new Vector3(0f, 0f, 125f);
            orbitSo.FindProperty("_defaultDistance").floatValue = 50f;
            orbitSo.FindProperty("_minDistance").floatValue = 10f;
            orbitSo.FindProperty("_maxDistance").floatValue = 200f;
            orbitSo.FindProperty("_minPitch").floatValue = 5f;
            orbitSo.FindProperty("_maxPitch").floatValue = 85f;
            orbitSo.FindProperty("_mouseSensitivity").floatValue = 3f;
            orbitSo.FindProperty("_scrollSensitivity").floatValue = 10f;
            orbitSo.FindProperty("_groundClearance").floatValue = 1f;
            orbitSo.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/CameraRig.prefab";
            PrefabUtility.SaveAsPrefabAsset(rigGo, prefabPath);
            Object.DestroyImmediate(rigGo);

            Debug.Log($"CameraRigGenerator: Created camera rig prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Add Camera Rig To Scene", priority = 161)]
        public static void AddCameraRigToScene()
        {
            // Check if prefab exists
            string prefabPath = $"{PrefabsPath}/CameraRig.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                if (EditorUtility.DisplayDialog(
                    "Camera Rig Not Found",
                    "CameraRig prefab not found. Create it first?",
                    "Create",
                    "Cancel"))
                {
                    CreateCameraRigPrefab();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                else
                {
                    return;
                }
            }

            // Instantiate in scene
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position = Vector3.zero;
                Selection.activeGameObject = instance;
                Undo.RegisterCreatedObjectUndo(instance, "Add Camera Rig");
                Debug.Log("CameraRigGenerator: Added Camera Rig to scene");
            }
        }
    }
}
