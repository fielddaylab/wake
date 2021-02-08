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
        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(MapDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR

        static public string LookupScene(StringHash32 inMapId)
        {
            return Services.Assets.Map.Get(inMapId)?.SceneName();
        }
    }
}