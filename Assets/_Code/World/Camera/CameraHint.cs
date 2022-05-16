using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraHint : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Children)] private Collider2D m_Region = null;

        [Header("Camera Parameters")]
        [SerializeField, Range(0.1f, 25)] private float m_Zoom = 1;
        [SerializeField] private float m_Lerp = 1;
        [SerializeField] private Vector3 m_Offset = default;
        [SerializeField] private Vector3 m_Look = default;

        [Header("Weight")]
        [SerializeField] private float m_Weight = 0;

        #endregion // Inspector
        
        [NonSerialized] internal uint m_HintHandle;

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Region, OnTargetEnter, OnTargetExit);
        }

        private void OnTargetEnter(Collider2D inCollider)
        {
            if (m_HintHandle != 0)
                return;

            ref var hint = ref Services.Camera.AddHint(m_Region.transform, m_Lerp, m_Weight, m_Zoom);
            hint.Offset = m_Offset;
            hint.Look = m_Look;
            m_HintHandle = hint.Id;
            Services.Data.SetVariable(GameVars.CameraRegion, Parent.Id());
        }

        private void OnTargetExit(Collider2D inCollider)
        {
            if (m_HintHandle == 0 || !Services.Camera)
                return;

            Services.Camera.RemoveHint(m_HintHandle);
            m_HintHandle = 0;

            Services.Data?.CompareExchange(GameVars.CameraRegion, Parent.Id(), StringHash32.Null);
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.IsPlaying(this) && m_HintHandle != 0 && UnityEditor.Selection.Contains(gameObject))
            {
                ref var hint = ref Services.Camera.FindHint(m_HintHandle);
                hint.Lerp = m_Lerp;
                hint.WeightOffset = m_Weight;
                hint.Zoom = m_Zoom;
                hint.Offset = m_Offset;
                hint.Look = m_Look;
            }
        }

        #endif // UNITY_EDITOR
    }
}