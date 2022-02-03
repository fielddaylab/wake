using System.Collections.Generic;
using UnityEngine;
using BeauUtil;
using Aqua.Character;

namespace Aqua.Character
{
    [RequireComponent(typeof(SpawnLocationMap))]
    public class SpawnCtrl : MonoBehaviour, ISceneLoadHandler, ISceneOptimizable
    {
        [SerializeField, HideInInspector] private PlayerBody m_Player = null;
        [SerializeField, HideInInspector] private SpawnLocationMap m_Spawns;

        [SerializeField] private SerializedHash32 m_DefaultEntrance = null;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            SpawnLocation location = m_Spawns.FindLocationForLastEntrance(m_DefaultEntrance);
            if (location != null)
                m_Player.TeleportTo(location);
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Player = FindObjectOfType<PlayerBody>();
            m_Spawns = GetComponent<SpawnLocationMap>();
        }

        #endif // UNITY_EDITOR
    }
}