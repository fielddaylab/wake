using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        [Header("Objects")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera = null;
        [Required(ComponentLookupDirection.Children)] public CameraFOVPlane FOVPlane = null;
        [Required] public Transform RootTransform = null;
        [Required] public Transform EffectsTransform = null;

        [Header("Settings")]
        public CameraMode DefaultMode = CameraMode.Scripted;

        [SerializeField, HideInInspector] public CameraTarget InitialTarget;

        #endregion // Inspector

        public void SetPlane(Transform inPlane)
        {
            FOVPlane.Target = inPlane;
        }

        public void SetPlane(Transform inPlane, float inHeight)
        {
            FOVPlane.Target = inPlane;
            FOVPlane.Height = inHeight;
        }

        public void SetHeight(float inHeight)
        {
            FOVPlane.Height = inHeight;
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            Camera = GetComponentInChildren<Camera>(true);
            FOVPlane = GetComponentInChildren<CameraFOVPlane>(true);
            RootTransform = transform;
            if (Camera.transform != transform)
                EffectsTransform = Camera.transform;
        }

        void ISceneOptimizable.Optimize()
        {
            InitialTarget = null;

            EffectsTransform.SetPosition(default(Vector3), Axis.XY, Space.Self);
            
            var potentialTargets = FindObjectsOfType<CameraTarget>();
            foreach(var target in potentialTargets)
            {
                if (target.isActiveAndEnabled)
                {
                    InitialTarget = target;
                    break;
                }
            }
        }

        [UnityEditor.CustomEditor(typeof(CameraRig))]
        private class Inspector : UnityEditor.Editor
        {
            private UnityEditor.Editor m_FOVPlaneEditor;

            private void OnDisable()
            {
                if (m_FOVPlaneEditor) {
                    DestroyImmediate(m_FOVPlaneEditor);
                    m_FOVPlaneEditor = null;
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                CameraRig rig = (CameraRig) target;
                if (rig.FOVPlane != null)
                {
                    UnityEditor.EditorGUILayout.Space();
                    UnityEditor.EditorGUILayout.LabelField("Plane", UnityEditor.EditorStyles.boldLabel);
                    CreateCachedEditor(rig.FOVPlane, null, ref m_FOVPlaneEditor);
                    m_FOVPlaneEditor.OnInspectorGUI();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            CameraFOVPlane plane = FOVPlane;
            
            Vector3 center = plane.transform.position;
            if (plane.Target != null)
                center.z = plane.Target.position.z;
            
            Vector3 size;
            size.y = plane.Height / plane.Zoom;
            size.x = plane.Height * Camera.aspect / plane.Zoom;
            size.z = 0.01f;
            Gizmos.color = ColorBank.Green.WithAlpha(0.25f);
            Gizmos.DrawCube(center, size);

            Gizmos.color = ColorBank.White.WithAlpha(0.8f);

            Vector3 topRight = center + size / 2;
            Vector3 bottomLeft = center - size / 2;
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y);
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y);

            topRight.z = topLeft.z = bottomLeft.z = bottomRight.z = center.z - 0.0001f;

            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }

        #endif // UNITY_EDITOR
    }
}