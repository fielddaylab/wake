using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    public class SpawnLocation : ScriptComponent
    {
        [SerializeField] private SerializedHash32 m_EntranceIdOverride = null;
        [SerializeField] private Transform m_LocationOverride = null;
        [SerializeField] private FacingId m_Facing = FacingId.Invalid;

        public StringHash32 Id { get { return !m_EntranceIdOverride.IsEmpty ? m_EntranceIdOverride.Hash() : Parent.Id(); } }
        public Transform Location { get { return m_LocationOverride ? m_LocationOverride : transform; } }
        public FacingId Facing { get { return m_Facing; } }
    }
}