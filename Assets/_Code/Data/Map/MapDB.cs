using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Map/Map Database", fileName = "MapDB")]
    public class MapDB : DBObjectCollection<MapDesc>
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_DefaultStationId = "Station1";

        #endregion // Inspector

        private Dictionary<string, StringHash32> m_SceneMapping = new Dictionary<string, StringHash32>();
        private HashSet<MapDesc> m_Stations = new HashSet<MapDesc>();

        public StringHash32 DefaultStationId() { return m_DefaultStationId; }
        public ICollection<MapDesc> Stations() { return m_Stations; }

        protected override void ConstructLookupForItem(MapDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);
            
            m_SceneMapping.Add(inItem.SceneName(), inItem.Id());
            if (inItem.HasFlags(MapFlags.IsStation))
                m_Stations.Add(inItem);
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(MapDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR

        static public string LookupScene(StringHash32 inMapId)
        {
            return Services.Assets.Map.Get(inMapId)?.SceneName();
        }

        static public StringHash32 LookupMap(SceneBinding inScene)
        {
            StringHash32 mapId;
            Services.Assets.Map.m_SceneMapping.TryGetValue(inScene.Name, out mapId);
            return mapId;
        }
    }
}