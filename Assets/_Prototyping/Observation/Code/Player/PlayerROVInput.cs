using UnityEngine;
using Aqua;
using Aqua.StationMap;
using Aqua.Character;

namespace ProtoAqua.Observation
{
    public class PlayerROVInput : WorldInput
    {
        public struct InputData
        {
            public MouseDistanceInputFilter.Output Mouse;
            public DirectionKeysInputFilter.Output Keyboard;

            public bool Move;
            public Vector2 MoveVector;

            public bool UsePress;
            public bool UseHold;

            public bool UseAltPress;
            public bool UseAltHold;
        }

        #region Inspector

        [SerializeField] private MouseDistanceInputFilter m_MouseFilter = default;
        [SerializeField] private DirectionKeysInputFilter m_KeyboardFilter = default;

        #endregion // Inspector

        #region Input Generation

        public void GenerateInput(Transform inPlayerTransform, Vector3? inLockOn, PlayerBodyStatus inStatus, out InputData outInputData)
        {
            if (!IsInputEnabled)
            {
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
            } else {
                outInputData.Move = false;
                outInputData.MoveVector = default(Vector2);
            }
        }

        #endregion // Input Generation
    }
}