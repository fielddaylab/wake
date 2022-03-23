using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Map Database", fileName = "MapDB")]
    public class MapDB : DBObjectCollection<MapDesc>
    {
        #region Inspector

        [Header("Rooms")]
        [SerializeField] private SerializedHash32[] m_RoomIds = Array.Empty<SerializedHash32>();

        [Header("Defaults")]
        [SerializeField, MapId(MapCategory.Station)] private SerializedHash32 m_DefaultStationId = "Station1";
        [SerializeField] private SerializedHash32[] m_DefaultUnlockedRooms = Array.Empty<SerializedHash32>();

        #endregion // Inspector

        private readonly Dictionary<string, StringHash32> m_SceneMapping = new Dictionary<string, StringHash32>();
        private readonly HashSet<MapDesc> m_Stations = new HashSet<MapDesc>();
        private readonly HashSet<MapDesc> m_DiveSites = new HashSet<MapDesc>();

        public StringHash32 DefaultStationId() { return m_DefaultStationId; }
        public ListSlice<SerializedHash32> DefaultUnlockedRooms() { return m_DefaultUnlockedRooms; }

        public ListSlice<SerializedHash32> Rooms() { return m_RoomIds; }
        public IReadOnlyCollection<MapDesc> Stations() { return m_Stations; }
        public IReadOnlyCollection<MapDesc> DiveSites() { return m_DiveSites; }

        protected override void ConstructLookupForItem(MapDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);
            
            m_SceneMapping.Add(inItem.SceneName(), inItem.Id());

            switch(inItem.Category())
            {
                case MapCategory.DiveSite:
                    {
                        m_DiveSites.Add(inItem);
                        break;
                    }

                case MapCategory.Station:
                    {
                        m_Stations.Add(inItem);
                        break;
                    }
            }
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(MapDB))]
        private class Inspector : BaseInspector
        {}

        static private Dictionary<string, StringHash32> s_EditorSceneMap;

        #endif // UNITY_EDITOR

        static public string LookupScene(StringHash32 inMapId)
        {
            return Assets.Map(inMapId)?.SceneName();
        }

        static public StringHash32 LookupMap(SceneBinding inScene)
        {
            StringHash32 mapId;
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (s_EditorSceneMap == null) {
                    s_EditorSceneMap = new Dictionary<string, StringHash32>();
                    foreach(var map in ValidationUtils.FindAllAssets<MapDesc>()) {
                        s_EditorSceneMap.Add(map.SceneName(), map.Id());
                    }
                }
                s_EditorSceneMap.TryGetValue(inScene.Name, out mapId);
                return mapId;
            }
            #endif // UNITY_EDITOR
            Services.Assets.Map.m_SceneMapping.TryGetValue(inScene.Name, out mapId);
            return mapId;
        }

        static public StringHash32 LookupCurrentMap()
        {
            return LookupMap(SceneHelper.ActiveScene());
        }
    }
}