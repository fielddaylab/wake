using Aqua.Debugging;
using BeauUtil;
using UnityEngine;

namespace Aqua.Scripting
{
    public class ScriptTrigger : ScriptComponent
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
                Services.Script.TriggerResponse(GameTriggers.PlayerEnterRegion, table);
            }
        }

        private void OnPlayerExit(Collider2D inCollider)
        {
            if (!Services.Script)
                return;
            
            using(var table = TempVarTable.Alloc())
            {
                table.Set("regionId", Parent.Id());
                Services.Script.TriggerResponse(GameTriggers.PlayerExitRegion, table);
            }
        }

        #if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (m_Collider == null)
                return;

            if (UnityEditor.Selection.Contains(this))
                return;
            
            RenderBox(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Collider == null)
                return;
            
            RenderBox(1);
        }

        private void RenderBox(float inAlpha)
        {
            Vector3 center = m_Collider.transform.position;
            Vector2 size = m_Collider.bounds.size;
            Vector2 offset = m_Collider.offset;
            center.x += offset.x;
            center.y += offset.y;

            GizmoViz.Box(center, size, m_Collider.transform.rotation, ColorBank.DarkGoldenrod, ColorBank.White, RectEdges.All, inAlpha);
        }

        #endif // UNITY_EDITOR
    }
}