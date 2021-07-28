using UnityEngine;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVInput : WorldInput
    {
        #region Inspector

        [SerializeField] private float m_TapThreshold = 0.5f;

        #endregion // Inspector

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnDestroy() 
        {
            Services.Events?.DeregisterAll(this);
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
                outInputData.Target = Services.Camera.GameplayPlanePosition(inLockOn.Value);
            }
            else
            {
                outInputData.Target = GetMousePositionInWorld(inPlayerTransform);
            }
            
            outInputData.Offset = outInputData.Target.Value - (Vector2) inPlayerTransform.position;

            outInputData.UseHold = bAllowLeftClick && Input.GetMouseButton(0);
            outInputData.UsePress = bAllowLeftClick && Input.GetMouseButtonDown(0);
        }

        private Vector2 GetMousePositionInWorld(Transform inTransform)
        {
            return Services.Camera.ScreenToWorldOnPlane(Input.mousePosition, inTransform);
        }

        #endregion // Input Generation
    }
}