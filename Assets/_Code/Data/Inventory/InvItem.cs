using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Inventory/Inventory Item", fileName = "NewInvItem")]
    public class InvItem : DBObject
    {
        #region Inspector

        [SerializeField] private InvItemFlags m_Flags = InvItemFlags.None;

        [Header("Text")]
        [SerializeField] private SerializedHash32 m_NameTextId = null;
        [SerializeField] private SerializedHash32 m_DescriptionTextId = null;

        [Header("Value")]
        [SerializeField, Range(0, 2000)] private int m_Default = 0;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;

        #endregion

        public InvItemFlags Flags() { return m_Flags; }

        public StringHash32 NameTextId() { return m_NameTextId; }
        public StringHash32 DescriptionTextId() { return m_DescriptionTextId; }
        
        public Sprite Icon() { return m_Icon; }

        public int DefaultValue() { return m_Default; }
    }
}