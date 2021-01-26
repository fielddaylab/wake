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
        [SerializeField] private SerializedHash32 m_ItemId = null;

        [Header("Value")]

        [SerializeField, Range(0, 2000)] private int m_Default = 0;

        [Header("Assets")]

        [SerializeField] private Sprite m_Icon = null;

        #endregion

        [NonSerialized] private int m_CurrentValue = 0;

        public StringHash32 ItemId() { return m_ItemId; }

        public Sprite Icon() { return m_Icon; }

        public int Value()
        {
            if (m_CurrentValue == 0)
            {
                m_CurrentValue = m_Default;
            }

            return m_CurrentValue;
        }

        public void UpdateValue(int value)
        {
            m_CurrentValue += value;

            return;
        }
    }
}