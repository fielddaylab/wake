using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Map/Map Description", fileName = "NewMapDesc")]
    public class MapDesc : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private MapCategory m_Category = MapCategory.Station;
        [SerializeField, AutoEnum] private MapFlags m_Flags = 0;

        [Header("Assets")]
        [SerializeField] private string m_SceneName = null;
        [SerializeField] private TextId m_LabelId = null;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private MapDesc m_Parent = null;

        [Header("Environment")]
        [SerializeField, FilterBestiary(BestiaryDescCategory.Environment)] private BestiaryDesc m_EnvironmentEntry = null;

        [Header("Misc")]
        [SerializeField] private PropertyBlock m_AdditionalProperties = null;

        #endregion // Inspector

        public MapCategory Category() { return m_Category; }
        public MapFlags Flags() { return m_Flags; }

        public bool HasFlags(MapFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(MapFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public string SceneName() { return m_SceneName; }
        public TextId LabelId() { return m_LabelId; }
        public Sprite Icon() { return m_Icon; }
        public MapDesc Parent() { return m_Parent; }

        public BestiaryDesc Environment() { return m_EnvironmentEntry; }

        public PropertyBlock AdditionalProperties() { return m_AdditionalProperties; }
        public T GetProperty<T>(string inName) { return m_AdditionalProperties.Get<T>(inName); }
        public T GetProperty<T>(string inName, T inDefault) { return m_AdditionalProperties.Get<T>(inName, inDefault); }
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
}