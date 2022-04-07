using Aqua.Debugging;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Cameras
{
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour, IBaked
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

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
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

            return true;
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
            
            Vector2 size;
            size.y = plane.Height / plane.Zoom;
            size.x = plane.Height * Camera.aspect / plane.Zoom;

            GizmoViz.Box(center, size, plane.transform.rotation, ColorBank.Green, ColorBank.White, RectEdges.All, 1);
        }

        #endif // UNITY_EDITOR
    }
}