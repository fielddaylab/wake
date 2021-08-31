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

            public bool UsePress;
            public bool UseHold;
        }

        #region Inspector

        [SerializeField] private MouseDistanceInputFilter m_MouseFilter = default;

        #endregion // Inspector

        #region Input Generation

        public void GenerateInput(Transform inPlayerTransform, Vector3? inLockOn, out InputData outInputData)
        {
            if (!IsInputEnabled)
            {
                outInputData = default(InputData);
                return;
            }

            m_MouseFilter.Process(Device, inPlayerTransform, inLockOn, out outInputData.Mouse);

            bool bAllowLeftClick = !Services.Input.IsPointerOverUI();

            outInputData.UseHold = bAllowLeftClick && Device.MouseDown(0);
            outInputData.UsePress = bAllowLeftClick && Device.MousePressed(0);
        }

        #endregion // Input Generation
    }
}