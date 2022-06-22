using System.Collections.Generic;
using UnityEngine;
using BeauUtil;
using Aqua.Character;
using ScriptableBake;

namespace Aqua.Character
{
    [RequireComponent(typeof(SpawnLocationMap))]
    public class SpawnCtrl : MonoBehaviour, ISceneLoadHandler, IBaked
    {
        public delegate bool CustomSpawnHandler(PlayerBody player, StringHash32 lastEntrance);

        [SerializeField, HideInInspector] private PlayerBody m_Player = null;
        [SerializeField, HideInInspector] private SpawnLocationMap m_Spawns;

        [SerializeField] private SerializedHash32 m_DefaultEntrance = null;

        public CustomSpawnHandler CustomSpawn;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_Player.PrepareSpawn();

            if (CustomSpawn != null && CustomSpawn(m_Player, Services.State.LastEntranceId)) {
                return;
            }
            
            SpawnLocation location = m_Spawns.FindLocationForLastEntrance(m_DefaultEntrance);
            if (location != null) {
                m_Player.TeleportTo(location);
            }
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            m_Player = FindObjectOfType<PlayerBody>();
            m_Spawns = GetComponent<SpawnLocationMap>();

            return true;
        }

        #endif // UNITY_EDITOR
    }
}