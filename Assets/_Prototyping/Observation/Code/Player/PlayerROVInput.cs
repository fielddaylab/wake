using UnityEngine;
using Aqua;
using Aqua.StationMap;
using Aqua.Character;
using System;

namespace ProtoAqua.Observation
{
    public class PlayerROVInput : WorldInput
    {
        public enum DashType
        {
            None,
            Primary,
            Secondary
        }

        public struct InputData
        {
            public MouseDistanceInputFilter.Output Mouse;
            public DirectionKeysInputFilter.Output Keyboard;

            public bool Move;
            public DashType Dash;
            public Vector2 MoveVector;

            public bool UsePress;
            public bool UseHold;

            public bool UseAltPress;
            public bool UseAltHold;
        }

        #region Inspector

        [SerializeField] private MouseDistanceInputFilter m_MouseFilter = default;
        [SerializeField] private DirectionKeysInputFilter m_KeyboardFilter = default;
        
        [Header("Dash")]
        [SerializeField] private float m_DashTapRequiredWindow = 0.6f;
        [SerializeField] private int m_DashTapRequiredCount = 3;
        [SerializeField] private float m_DashTapRequiredAccuracy = 0.8f;
        [SerializeField] private bool m_DashAllowChain = true;

        #endregion // Inspector

        [NonSerialized] private float m_DashTapWindow = 0;
        [NonSerialized] private Vector2 m_MoveTapLastVector = default(Vector2);
        [NonSerialized] private int m_DashTapCounter = 0;

        #region Input Generation

        public void GenerateInput(Transform inPlayerTransform, Vector3? inLockOn, PlayerBodyStatus inStatus, float inDeltaTime, InputData inLastInputData, out InputData outInputData)
        {
            if (!IsInputEnabled)
            {
                ClearDash();
                outInputData = default(InputData);
                return;
            }

            m_MouseFilter.Process(Device, inPlayerTransform, inLockOn, null, out outInputData.Mouse);
            m_KeyboardFilter.Process(Device, out outInputData.Keyboard);

            bool bAllowMove = (inStatus & PlayerBodyStatus.DisableMovement) == 0;
            bool bAllowTools = (inStatus & PlayerBodyStatus.DisableTools) == 0;
            
            bool bAllowInput = (inStatus & PlayerBodyStatus.Stunned) == 0;
            bool bAllowMouse = bAllowInput && !Services.Input.IsPointerOverUI();

            outInputData.UseHold = bAllowTools && bAllowMouse && Device.MouseDown(0);
            outInputData.UsePress = bAllowTools && bAllowMouse && Device.MousePressed(0);

            outInputData.UseAltPress = bAllowTools && Device.KeyPressed(KeyCode.Space);
            outInputData.UseAltHold = bAllowTools && Device.KeyDown(KeyCode.Space);

            if (bAllowMove) {
                outInputData.Move = outInputData.UseHold || outInputData.Keyboard.KeyDown;
                outInputData.MoveVector = outInputData.Keyboard.KeyDown ? outInputData.Keyboard.NormalizedOffset : outInputData.Mouse.NormalizedOffset;

                if (!inLastInputData.Move && outInputData.Move) {
                    outInputData.Dash = TryDash(outInputData.MoveVector);
                } else {
                    outInputData.Dash = DashType.None;
                }
            } else {
                outInputData.Move = false;
                outInputData.Dash = DashType.None;
                outInputData.MoveVector = default(Vector2);
                ClearDash();
            }

            // decrement dash window
            if (m_DashTapWindow > 0) {
                m_DashTapWindow -= inDeltaTime;
                if (m_DashTapWindow <= 0) {
                    m_DashTapCounter = 0;
                }
            }
        }

        #endregion // Input Generation

        #region Dash

        public void ClearDash() {
            m_DashTapCounter = 0;
            m_DashTapWindow = 0;
            m_MoveTapLastVector = default(Vector2);
        }

        private DashType TryDash(Vector2 moveVec) {
            m_DashTapWindow = m_DashTapRequiredWindow;

            Vector2 lastMoveVec = m_MoveTapLastVector;
            m_MoveTapLastVector = moveVec;

            if (m_DashTapWindow > 0) {
                // must tap/move in around the same direction as before
                float accuracy = Vector2.Dot(lastMoveVec, moveVec);
                if (accuracy >= m_DashTapRequiredAccuracy) {
                    m_DashTapCounter++;

                    // can repeatedly dash but window is smaller
                    if (m_DashTapCounter >= m_DashTapRequiredCount) {
                        if (!m_DashAllowChain) {
                            m_DashTapCounter = 0;
                            m_DashTapWindow = 0;
                        } else {
                            m_DashTapWindow *= 0.7f;
                        }
                        return m_DashTapCounter > m_DashTapRequiredCount ? DashType.Secondary : DashType.Primary;
                    }

                    return DashType.None;
                }
            }

            m_DashTapCounter = 1;
            return DashType.None;
        }

        #endregion // Dash
    }
}