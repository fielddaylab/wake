using Aqua.Debugging;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

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
        public bool ThreeDMode = false;
        public float AudioListenerZOffset = 0;

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

        public void ReadData(ref CameraPose.Data data) {
            data.Position = RootTransform.position;
            data.Rotation = RootTransform.rotation;
            if (FOVPlane != null) {
                data.Mode = FOVPlane.enabled ? CameraFOVMode.Plane : CameraFOVMode.Direct;
                data.Target = FOVPlane.Target;
                data.Height = FOVPlane.Height;
                data.Zoom = FOVPlane.Zoom;
            }
            data.FOV = Camera.fieldOfView;
            data.Properties = CameraPoseProperties.All;
        }

        public void WriteData(in CameraPose.Data data) {
            #if UNITY_EDITOR
            Undo.RecordObject(this, "Writing camera pose data");
            Undo.RecordObject(RootTransform, "Writing camera pose data");
            if (FOVPlane) {
                Undo.RecordObject(FOVPlane, "Writing camera pose data");
                EditorUtility.SetDirty(FOVPlane);
            }
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(transform);
            #endif // UNITY_EDITOR

            RootTransform.SetPositionAndRotation(data.Position, data.Rotation);
            Camera.fieldOfView = data.FOV;
            if (FOVPlane) {
                FOVPlane.Target = data.Target;
                FOVPlane.Height = data.Height;
                FOVPlane.Zoom = data.Zoom;
                FOVPlane.enabled = data.Mode == CameraFOVMode.Plane;
            }
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

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            InitialTarget = null;

            if (transform != RootTransform) {
                transform.SetPosition(default(Vector3), Axis.XY, Space.Self);
            }
            
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

        [CustomEditor(typeof(CameraRig))]
        private class Inspector : UnityEditor.Editor
        {
            static private readonly string[] EditTabs = new string[] {
                "Pose" //, "Spline"
            };

            private Editor m_FOVPlaneEditor;
            private Editor m_CameraEditor;

            [SerializeField] private int m_SelectedTab = 0;
            [SerializeField] private CameraPose m_Pose;
            [SerializeField] private CameraSpline m_Spline;
            [SerializeField] private CameraPose.Data m_DefaultPose;
            [SerializeField] private bool m_EditMode;
            [SerializeField] private bool m_CameraExpanded = false;

            private void OnDisable()
            {
                if (m_EditMode) {
                    CameraRig rig = (CameraRig) target;
                    if (rig) {
                        rig.WriteData(m_DefaultPose);
                    }
                    m_EditMode = false;
                }

                if (m_FOVPlaneEditor) {
                    DestroyImmediate(m_FOVPlaneEditor);
                    m_FOVPlaneEditor = null;
                }

                if (m_CameraEditor) {
                    DestroyImmediate(m_CameraEditor);
                    m_CameraEditor = null;
                }
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                CameraRig rig = (CameraRig) target;
                if (rig.FOVPlane != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Plane", EditorStyles.boldLabel);

                    EditorGUI.BeginChangeCheck();
                    bool fovPlaneEnabled = EditorGUILayout.Toggle("Enabled", rig.FOVPlane.enabled);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(rig.FOVPlane, "Toggled fov plane enabled");
                        rig.FOVPlane.enabled = fovPlaneEnabled;
                        EditorUtility.SetDirty(rig.FOVPlane);
                    }

                    if (fovPlaneEnabled)
                    {
                        CreateCachedEditor(rig.FOVPlane, null, ref m_FOVPlaneEditor);
                        m_FOVPlaneEditor.OnInspectorGUI();
                    }
                }

                if (rig.Camera != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);

                    using(new EditorGUI.DisabledScope(rig.FOVPlane != null && rig.FOVPlane.enabled))
                    {
                        EditorGUI.BeginChangeCheck();
                        float newFOV = EditorGUILayout.Slider("Field Of View", rig.Camera.fieldOfView, 1E-05f, 179f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(rig.Camera, "Changing fov");
                            rig.Camera.fieldOfView = newFOV;
                            EditorUtility.SetDirty(rig.Camera);
                        }
                    }
                    
                    m_CameraExpanded = EditorGUILayout.Foldout(m_CameraExpanded, "Camera Details");
                    if (m_CameraExpanded)
                    {
                        CreateCachedEditor(rig.Camera, null, ref m_CameraEditor);
                        m_CameraEditor.OnInspectorGUI();
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Editing", EditorStyles.boldLabel);

                bool wasEditing = m_EditMode;
                m_EditMode = EditorGUILayout.Toggle("Begin Editing", m_EditMode);
                if (wasEditing != m_EditMode) {
                    if (wasEditing) {
                        rig.WriteData(m_DefaultPose);
                        m_DefaultPose = default;
                    } else {
                        rig.ReadData(ref m_DefaultPose);
                    }
                }

                using(new GUILayout.VerticalScope(EditorStyles.helpBox))
                using(new EditorGUI.DisabledScope(!m_EditMode)) {
                    m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, EditTabs, EditorStyles.toolbarButton);
                    EditorGUILayout.Separator();
                    switch(m_SelectedTab) {
                        case 0: {
                            m_Pose = (CameraPose)EditorGUILayout.ObjectField("Target Pose", m_Pose, typeof(CameraPose), true);
                            EditorGUILayout.BeginHorizontal();
                            using(new EditorGUI.DisabledScope(m_Pose == null)) {
                                if (GUILayout.Button("Snap To Pose")) {
                                    CameraPose.Data data = default;
                                    m_Pose.ReadData(ref data);
                                    rig.WriteData(data);
                                }
                                if (GUILayout.Button("Write Current to Pose")) {
                                    CameraPose.Data data = default;
                                    rig.ReadData(ref data);
                                    m_Pose.WriteData(data);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            break;
                        }

                        case 1: {
                            m_Spline = (CameraSpline) EditorGUILayout.ObjectField("Target Spline", m_Spline, typeof(CameraSpline), true);
                            EditorGUILayout.BeginHorizontal();
                            using(new EditorGUI.DisabledScope(m_Spline == null)) {
                                // TODO: Implement a better system
                                // if (GUILayout.Button("Snap To Pose")) {
                                //     CameraPose.Data data = default;
                                //     m_Pose.ReadData(ref data);
                                //     rig.WriteData(data);
                                // }
                                // if (GUILayout.Button("Write Current to Pose")) {
                                //     CameraPose.Data data = default;
                                //     rig.ReadData(ref data);
                                //     m_Pose.WriteData(data);
                                // }
                                // Undo.
                            }
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset")) {
                    rig.WriteData(m_DefaultPose);
                }
                if (GUILayout.Button("Write Current To Root")) {
                    rig.ReadData(ref m_DefaultPose);
                }
                EditorGUILayout.EndHorizontal();
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