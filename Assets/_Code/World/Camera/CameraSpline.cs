using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;
using BeauRoutine.Splines;
using BeauRoutine;

namespace Aqua.Cameras
{
    public class CameraSpline : ScriptComponent
    {
        #region Inspector

        [Required] public CameraPose[] Nodes;
        [AutoEnum] public CameraPoseProperties Properties = CameraPoseProperties.All;

        #endregion // Inspector

        private CSpline m_Spline;
        private Vector3[] m_Positions;
        private Quaternion[] m_Directions;

        private void Awake() {
            m_Spline = new CSpline(Nodes.Length);
            m_Spline.SetAsCatmullRom();

            m_Positions = new Vector3[Nodes.Length];
            m_Directions = new Quaternion[Nodes.Length];

            for(int i = 0; i < Nodes.Length; i++) {
                m_Positions[i] = Nodes[i].transform.position;
                m_Directions[i] = Nodes[i].transform.rotation;
            }

            m_Spline.SetVertices(m_Positions);
            m_Spline.Process();

            Script.OnSceneLoad(() => {
                Services.Camera.SetFOVMode(CameraFOVMode.Direct);
                Services.Camera.MoveAlongSpline(this, 5, CameraPoseProperties.All);
            });
        }

        public void Interpolate(float percentage, ref CameraService.CameraState cameraState) {
            Vector3 pos = m_Spline.GetPoint(percentage);
            m_Spline.GetSegment(percentage, out SplineSegment seg);

            cameraState.Position = pos;
            cameraState.Rotation = Quaternion.SlerpUnclamped(m_Directions[seg.VertexA], m_Directions[seg.VertexB], seg.Interpolation);
            cameraState.FieldOfView = Mathf.LerpUnclamped(Nodes[seg.VertexA].FieldOfView, Nodes[seg.VertexB].FieldOfView, seg.Interpolation);
        }

        #if UNITY_EDITOR

        [ContextMenu("Retrieve Nodes from Children")]
        private void GetNodesFromChildren()
        {
            UnityEditor.Undo.RecordObject(this, "gathering poses");
            Nodes = GetComponentsInChildren<CameraPose>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR
    }
}