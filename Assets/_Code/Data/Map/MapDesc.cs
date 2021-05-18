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

        [SerializeField, AutoEnum] private MapFlags m_Flags = 0;

        [Header("Assets")]
        [SerializeField] private string m_SceneName = null;
        [SerializeField] private TextId m_LabelId = null;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private MapDesc m_Parent = null;

        [Header("Misc")]
        [SerializeField] private PropertyBlock m_AdditionalProperties = null;

        #endregion // Inspector

        public MapFlags Flags() { return m_Flags; }

        public bool HasFlags(MapFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(MapFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public string SceneName() { return m_SceneName; }
        public TextId LabelId() { return m_LabelId; }
        public Sprite Icon() { return m_Icon; }
        public MapDesc Parent() { return m_Parent; }

        public PropertyBlock AdditionalProperties() { return m_AdditionalProperties; }
        public T GetProperty<T>(string inName) { return m_AdditionalProperties.Get<T>(inName); }
        public T GetProperty<T>(string inName, T inDefault) { return m_AdditionalProperties.Get<T>(inName, inDefault); }
    }

    [Flags]
    public enum MapFlags
    {
        IsStation = 0x01
    }
}