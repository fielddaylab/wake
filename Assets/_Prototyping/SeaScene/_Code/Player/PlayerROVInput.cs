using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVInput : WorldInput
    {
        #region Inspector

        [SerializeField] private float m_TapThreshold = 0.5f;

        #endregion // Inspector

        [NonSerialized] private bool m_ToolModeToggle = false;
        [NonSerialized] private float m_LastToolModeDown = -1;

        protected override void Awake()
        {
            base.Awake();
            OnInputDisabled.AddListener(HandleInputDisabled);
        }

        private void HandleInputDisabled()
        {
            m_ToolModeToggle = false;
            m_LastToolModeDown = -1;
        }

        #region Input Generation

        public void GenerateInput(Transform inPlayerTransform, Vector3? inLockOn, out PlayerROV.InputData outInputData)
        {
            if (!IsInputEnabled)
            {
                outInputData = default(PlayerROV.InputData);
                return;
            }

            bool bAllowLeftClick = !Services.Input.IsPointerOverUI();

            if (inLockOn.HasValue)
            {
                outInputData.Target = ObservationServices.Camera.GameplayPlanePosition(inLockOn.Value);
            }
            else
            {
                outInputData.Target = GetMousePositionInWorld(inPlayerTransform);
            }
            
            outInputData.Offset = outInputData.Target.Value - (Vector2) inPlayerTransform.position;

            outInputData.UseHold = bAllowLeftClick && Input.GetMouseButton(0);
            outInputData.UsePress = bAllowLeftClick && Input.GetMouseButtonDown(0);

            bool bRightMouseDown = Input.GetMouseButton(1);

            if (bRightMouseDown)
            {
                if (m_LastToolModeDown == -1)
                {
                    m_LastToolModeDown = Time.timeSinceLevelLoad;
                }
            }
            else
            {
                if (m_LastToolModeDown >= 0)
                {
                    float deltaTime = Time.timeSinceLevelLoad - m_LastToolModeDown;
                    m_LastToolModeDown = -1;

                    if (deltaTime <= m_TapThreshold)
                    {
                        m_ToolModeToggle = !m_ToolModeToggle;
                    }
                }
            }

            outInputData.ToolMode = m_ToolModeToggle || bRightMouseDown;
        }

        private Vector2 GetMousePositionInWorld(Transform inTransform)
        {
            return ObservationServices.Camera.ScreenToWorldOnPlane(Input.mousePosition, inTransform);
        }

        #endregion // Input Generation
    }
}