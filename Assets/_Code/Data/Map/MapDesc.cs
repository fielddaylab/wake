using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;
using BeauUtil.Debugger;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Map Description", fileName = "NewMapDesc")]
    public class MapDesc : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private MapCategory m_Category = MapCategory.Station;
        [SerializeField, AutoEnum] private MapFlags m_Flags = 0;

        [Header("Assets")]
        [SerializeField] private string m_SceneName = null;
        [SerializeField] private TextId m_LabelId = default;
        [SerializeField] private TextId m_ShortLabelId = default;
        [SerializeField] private TextId m_ProperNameId = default;
        [SerializeField] private TextId m_MarkerLabelId = default;
        [SerializeField] private TextId m_StationHeaderId = default;
        [SerializeField] private TextId m_StationDescId = default;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, StreamingPath("png,jpg,jpeg,webm,mp4")] private string m_HeaderImagePath = null;
        [SerializeField] private MapDesc m_Parent = null;
        [SerializeField] private MapDesc m_QuickTravel = null;
        [SerializeField, FilterBestiaryId(BestiaryDescCategory.Environment)] private StringHash32 m_EnvironmentId = null;
        [SerializeField] private int m_StationSortingOrder = 0;

        [Header("Misc")]
        [SerializeField] private PropertyBlock m_AdditionalProperties = null;

        #endregion // Inspector

        public MapCategory Category() { return m_Category; }
        public MapFlags Flags() { return m_Flags; }

        public bool HasFlags(MapFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(MapFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public string SceneName() { return m_SceneName; }
        [LeafLookup("Name")] public TextId LabelId() { return m_LabelId; }
        [LeafLookup("ShortName")] public TextId ShortLabelId() { return m_ShortLabelId.IsEmpty ? m_LabelId : m_ShortLabelId; }
        [LeafLookup("ProperName")] public TextId ProperNameId() { return m_ProperNameId.IsEmpty ? m_LabelId : m_ProperNameId; }
        public TextId MarkerLabelId() { Assert.True(m_Category == MapCategory.DiveSite, "MapDesc {0} is not a dive site", Id()); return m_MarkerLabelId; }
        public TextId StationHeaderId() { Assert.True(m_Category == MapCategory.Station, "MapDesc {0} is not a station", Id()); return m_StationHeaderId ;}
        public TextId StationDescId() { Assert.True(m_Category == MapCategory.Station, "MapDesc {0} is not a station", Id()); return m_StationDescId ;}
        public Sprite Icon() { return m_Icon; }
        public string HeaderImagePath() { return m_HeaderImagePath; }
        public MapDesc Parent() { return m_Parent; }
        public MapDesc QuickTravel() { return m_QuickTravel; }

        [LeafLookup("Environment")] public StringHash32 EnvironmentId() { return m_EnvironmentId; }
        public int SortingOrder() { Assert.True(m_Category == MapCategory.Station, "MapDesc {0} is not a station", Id()); return m_StationSortingOrder; }

        public IReadOnlyPropertyBlock<PropertyName> AdditionalProperties() { return m_AdditionalProperties; }
        public T GetProperty<T>(string inName) { return m_AdditionalProperties.Get<T>(inName); }
        public T GetProperty<T>(string inName, T inDefault) { return m_AdditionalProperties.Get<T>(inName, inDefault); }

        #if UNITY_EDITOR

        [CustomEditor(typeof(MapDesc)), CanEditMultipleObjects]
        private class Inspector : Editor {
            private SerializedProperty m_CategoryProperty;
            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_SceneNameProperty;
            private SerializedProperty m_LabelIdProperty;
            private SerializedProperty m_ShortLabelIdProperty;
            private SerializedProperty m_ProperNameIdProperty;
            private SerializedProperty m_MarkerLabelIdProperty;
            private SerializedProperty m_StationHeaderIdProperty;
            private SerializedProperty m_StationDescIdProperty;
            private SerializedProperty m_IconProperty;
            private SerializedProperty m_HeaderImagePathProperty;
            private SerializedProperty m_ParentProperty;
            private SerializedProperty m_QuickTravelProperty;
            private SerializedProperty m_EnvironmentIdProperty;
            private SerializedProperty m_StationSortingOrderProperty;
            private SerializedProperty m_AdditionalPropertiesProperty;

            private MapDB m_MapDB;

            private void OnEnable() {
                m_CategoryProperty = serializedObject.FindProperty("m_Category");
                m_FlagsProperty = serializedObject.FindProperty("m_Flags");
                m_SceneNameProperty = serializedObject.FindProperty("m_SceneName");
                m_LabelIdProperty = serializedObject.FindProperty("m_LabelId");
                m_ShortLabelIdProperty = serializedObject.FindProperty("m_ShortLabelId");
                m_MarkerLabelIdProperty = serializedObject.FindProperty("m_MarkerLabelId");
                m_ProperNameIdProperty = serializedObject.FindProperty("m_ProperNameId");
                m_StationHeaderIdProperty = serializedObject.FindProperty("m_StationHeaderId");
                m_StationDescIdProperty = serializedObject.FindProperty("m_StationDescId");
                m_IconProperty = serializedObject.FindProperty("m_Icon");
                m_HeaderImagePathProperty = serializedObject.FindProperty("m_HeaderImagePath");
                m_ParentProperty = serializedObject.FindProperty("m_Parent");
                m_QuickTravelProperty = serializedObject.FindProperty("m_QuickTravel");
                m_EnvironmentIdProperty = serializedObject.FindProperty("m_EnvironmentId");
                m_StationSortingOrderProperty = serializedObject.FindProperty("m_StationSortingOrder");
                m_AdditionalPropertiesProperty = serializedObject.FindProperty("m_AdditionalProperties");

                m_MapDB = ValidationUtils.FindAsset<MapDB>();

                foreach(MapDesc desc in targets) {
                    m_MapDB.TryAdd(desc);
                }
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUILayout.PropertyField(m_CategoryProperty);
                EditorGUILayout.PropertyField(m_FlagsProperty);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_SceneNameProperty);
                EditorGUILayout.PropertyField(m_LabelIdProperty);
                EditorGUILayout.PropertyField(m_ShortLabelIdProperty);
                EditorGUILayout.PropertyField(m_ProperNameIdProperty);
                EditorGUILayout.PropertyField(m_IconProperty);

                if (!m_CategoryProperty.hasMultipleDifferentValues) {
                    switch((MapCategory) m_CategoryProperty.intValue) {
                        case MapCategory.DiveSite: {
                            EditorGUILayout.Space();
                            EditorGUILayout.PropertyField(m_EnvironmentIdProperty);
                            EditorGUILayout.PropertyField(m_ParentProperty);
                            EditorGUILayout.PropertyField(m_HeaderImagePathProperty);
                            EditorGUILayout.Space();
                            EditorGUILayout.PropertyField(m_MarkerLabelIdProperty);
                            break;
                        }

                        case MapCategory.Station: {
                            EditorGUILayout.Space();
                            EditorGUILayout.PropertyField(m_StationHeaderIdProperty);
                            EditorGUILayout.PropertyField(m_StationDescIdProperty);
                            EditorGUILayout.PropertyField(m_StationSortingOrderProperty);
                            EditorGUILayout.PropertyField(m_QuickTravelProperty);
                            EditorGUILayout.PropertyField(m_HeaderImagePathProperty);
                            break;
                        }

                        default: {
                            EditorGUILayout.Space();
                            EditorGUILayout.PropertyField(m_ParentProperty);
                            break;
                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_AdditionalPropertiesProperty);

                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }

    public enum MapCategory
    {
        ShipRoom,
        Land,
        Station,
        DiveSite
    }

    [Flags]
    public enum MapFlags
    {
        UnlockedByDefault = 0x01,
        HasRooms = 0x02
    }

    public class MapIdAttribute : DBObjectIdAttribute {
        public MapCategory? Category;

        public MapIdAttribute() : base(typeof(MapDesc)) {
            Category = null;
        }

        public MapIdAttribute(MapCategory inCategory) : base(typeof(MapDesc)) {
            Category = inCategory;
        }

        public override bool Filter(DBObject inObject) {
            if (Category.HasValue) {
                MapDesc map = (MapDesc) inObject;
                return map.Category() == Category.Value;
            } else {
                return true;
            }
        }

        public override string Name(DBObject inObject) {
            if (Category.HasValue) {
                return inObject.name;
            }

            MapDesc desc = (MapDesc) inObject;
            return string.Format("{0}/{1}", desc.Category().ToString(), desc.name);
        }
    }
}