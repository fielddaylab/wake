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

        [Header("Text")]
        [SerializeField] private string m_NameTextId = null;

        [SerializeField] private InvItemFlags m_ItemType = InvItemFlags.None;

        [Header("Value")]
        [SerializeField, Range(0, 2000)] private int m_Default = 0;

        [Header("Assets")]

        [SerializeField] private Sprite m_Icon = null;

        #endregion

        public InvItemFlags ItemType() { return m_ItemType; }

        public string NameTextId() { return m_NameTextId; }
        public Sprite Icon() { return m_Icon; }

        public int DefaultValue() { return m_Default; }
    }
}