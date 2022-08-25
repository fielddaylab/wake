using System;
using Aqua.Character;
using Aqua.Debugging;
using BeauUtil;
using UnityEngine;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Trigger Region")]
    public class ScriptTrigger : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private int m_HasPlayer;

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D inCollider)
        {
            if (++m_HasPlayer == 1)
            {
                PlayerBody body = inCollider.GetComponentInParent<PlayerBody>();
                if (body != null) {
                    body.AddRegion(Parent.Id());
                }

                using(var table = TempVarTable.Alloc())
                {
                    table.Set("regionId", Parent.Id());
                    Trigger(GameTriggers.PlayerEnterRegion, table);
                }
            }
        }

        private void OnPlayerExit(Collider2D inCollider)
        {
            if (!Services.Script)
                return;

            if (--m_HasPlayer == 0)
            {
                PlayerBody body = inCollider.GetComponentInParent<PlayerBody>();
                if (body != null) {
                    body.RemoveRegion(Parent.Id());
                }

                using(var table = TempVarTable.Alloc())
                {
                    table.Set("regionId", Parent.Id());
                    Trigger(GameTriggers.PlayerExitRegion, table);
                }
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