using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Character
{
    public class SpawnLocationMap : MonoBehaviour, IBaked
    {
        [SerializeField, HideInInspector] private SpawnLocation[] m_Locations;

        public SpawnLocation FindLocation(StringHash32 inId)
        {
            SpawnLocation location;
            for(int i = 0, count = m_Locations.Length; i < count; i++)
            {
                location = m_Locations[i];
                if (location.isActiveAndEnabled && location.Id == inId)
                    return location;
            }

            Log.Warn("[SpawnLocationMap] No location found for '{0}'", inId);
            return null;
        }

        public SpawnLocation FindLocationForLastEntrance(StringHash32 inDefault = default)
        {
            StringHash32 entrance = Services.State.LastEntranceId;
            if (entrance.IsEmpty)
                entrance = inDefault;

            return FindLocation(entrance);
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            List<SpawnLocation> locations = new List<SpawnLocation>(8);
            SceneHelper.ActiveScene().Scene.GetAllComponents<SpawnLocation>(true, locations);
            m_Locations = locations.ToArray();

            return true;
        }

        #endif // UNITY_EDITOR
    }
}