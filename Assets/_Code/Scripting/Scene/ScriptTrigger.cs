using BeauUtil;
using UnityEngine;

namespace Aqua.Scripting
{
    public class SceneTrigger : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D inCollider)
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("regionId", Parent.Id());
                Services.Script.TriggerResponse(GameTriggers.PlayerEnterRegion, null, null, table);
            }
        }

        private void OnPlayerExit(Collider2D inCollider)
        {
            if (!Services.Script)
                return;
            
            using(var table = TempVarTable.Alloc())
            {
                table.Set("regionId", Parent.Id());
                Services.Script.TriggerResponse(GameTriggers.PlayerExitRegion, null, null, table);
            }
        }
    }
}