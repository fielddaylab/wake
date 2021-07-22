using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Character
{
    public class SpawnLocationMap : MonoBehaviour, ISceneOptimizable
    {
        [SerializeField, HideInInspector] private SpawnLocation[] m_Locations;

        public SpawnLocation FindLocation(StringHash32 inId)
        {
            for(int i = 0, count = m_Locations.Length; i < count; i++)
            {
                if (m_Locations[i].Id == inId)
                    return m_Locations[i];
            }

            Log.Error("[SpawnLocationMap] No location found for '{0}'", inId);
            return null;
        }

        public SpawnLocation FindLocationForLastEntrance()
        {
            return FindLocation(Services.State.LastEntranceId);
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            List<SpawnLocation> locations = new List<SpawnLocation>(8);
            SceneHelper.ActiveScene().Scene.GetAllComponents<SpawnLocation>(true, locations);
            m_Locations = locations.ToArray();
        }

        #endif // UNITY_EDITOR
    }
}