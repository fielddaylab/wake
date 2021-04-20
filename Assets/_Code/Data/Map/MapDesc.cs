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

        [Header("Assets")]
        [SerializeField] private string m_SceneName = null;
        [SerializeField] private SerializedHash32 m_LabelId = null;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private MapDesc m_Parent = null;

        [Header("Misc")]
        [SerializeField] private PropertyBlock m_AdditionalProperties = null;

        #endregion // Inspector

        public string SceneName() { return m_SceneName; }
        public StringHash32 LabelId() { return m_LabelId; }
        public Sprite Icon() { return m_Icon; }
        public MapDesc Parent() { return m_Parent; }

        public PropertyBlock AdditionalProperties() { return m_AdditionalProperties; }
        public T GetProperty<T>(string inName) { return m_AdditionalProperties.Get<T>(inName); }
        public T GetProperty<T>(string inName, T inDefault) { return m_AdditionalProperties.Get<T>(inName, inDefault); }
    }
}