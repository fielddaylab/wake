using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace ProtoAqua.Observation
{
    public class PlayerROVInput : WorldInput
    {
        #region Inspector

        [SerializeField] private float m_TapThreshold = 0.5f;

        #endregion // Inspector

        [NonSerialized] private bool m_ToolModeToggle;
        [NonSerialized] private float m_LastToolModeDown;
        
        #region WorldInput

        protected override void OnInputDisabled()
        {
            m_ToolModeToggle = false;
            m_LastToolModeDown = -1;
        }

        #endregion // World Input

        #region Input Generation

        public void GenerateInput(Transform inPlayerTransform, Transform inLockOn, out PlayerROV.InputData outInputData)
        {
            if (!m_InputEnabled)
            {
                outInputData = default(PlayerROV.InputData);
                return;
            }

            if (inLockOn)
            {
                outInputData.Target = ObservationServices.Camera.GameplayPlanePosition(inLockOn);
            }
            else
            {
                outInputData.Target = GetMousePositionInWorld(inPlayerTransform);
            }
            
            outInputData.Offset = outInputData.Target.Value - (Vector2) inPlayerTransform.position;

            outInputData.UseHold = Input.GetMouseButton(0);
            outInputData.UsePress = Input.GetMouseButtonDown(0);
            outInputData.UseRelease = Input.GetMouseButtonUp(0);

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