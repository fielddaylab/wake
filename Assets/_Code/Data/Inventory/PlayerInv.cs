using UnityEngine;
using BeauUtil;
using System.Collections.Generic;
using System;

namespace Aqua
{
    public class PlayerInv : IKeyValuePair<StringHash32, PlayerInv>
    {

        public InvItem Item
        {
            get { return m_CachedItem ?? (m_CachedItem = Services.Assets.Inventory.Get(m_ItemId)); }
        }

        [NonSerialized] private InvItem m_CachedItem;
        private StringHash32 m_ItemId;
        private int m_CurrentValue = 0;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, PlayerInv>.Key { get { return m_ItemId; } }
        PlayerInv IKeyValuePair<StringHash32, PlayerInv>.Value { get { return this; } }

        #endregion // KeyValue

        public PlayerInv()
        {
            m_CurrentValue = 0;
        }

        public PlayerInv(InvItem invItem)
        {
            m_CachedItem = invItem;
            m_ItemId = Item.Id();
            m_CurrentValue = invItem.DefaultValue();
        }

        public PlayerInv(StringHash32 inId)
        {
            m_ItemId = inId;
            m_CurrentValue = 0;
        }

        public int Value() { return m_CurrentValue; }

        public bool TryAdjust(int value)
        {
            if ((m_CurrentValue + value) < 0)
                return false;

            m_CurrentValue += value;
            return true;
        }

        public void Set(int inValue)
        {
            if (inValue < 0)
                inValue = 0;
            m_CurrentValue = inValue;
        }
    }
    
}