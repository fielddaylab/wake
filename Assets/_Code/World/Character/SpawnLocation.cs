using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character {
    public class SpawnLocation : ScriptComponent {
        
        [Header("Overrides")]
        [SerializeField] private SerializedHash32 m_EntranceIdOverride = null;
        [SerializeField] private Transform m_LocationOverride = null;
        [SerializeField] private FacingId m_Facing = FacingId.Invalid;
        
        [Header("Trigger")]
        [SerializeField] private Collider2D m_TriggerRegion = null;

        public bool HasEntranceOverride() {
            return !m_EntranceIdOverride.IsEmpty;
        }

        public void OverrideEntrance(StringHash32 inEntranceId) {
            m_EntranceIdOverride = inEntranceId;
        }

        public StringHash32 Id { get { return !m_EntranceIdOverride.IsEmpty ? m_EntranceIdOverride.Hash() : Parent.Id(); } }
        public Transform Location { get { return m_LocationOverride ? m_LocationOverride : transform; } }
        public FacingId Facing { get { return m_Facing; } }

        private void OnEnable() {
            if (m_TriggerRegion != null) {
                WorldUtils.ListenForPlayer(m_TriggerRegion, (c) => {
                    AutoSave.SetSpawnLocation(Id);
                }, null);
            }
        }
    }
}