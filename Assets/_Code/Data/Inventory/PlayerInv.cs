using UnityEngine;
using BeauUtil;
using System.Collections.Generic;
using System;
using BeauData;

namespace Aqua
{
    public class PlayerInv : IKeyValuePair<StringHash32, PlayerInv>, ISerializedObject
    {
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

        public InvItem Item
        {
            get { return m_CachedItem ?? (m_CachedItem = Services.Assets.Inventory.Get(m_ItemId)); }
        }

        public int Value() { return m_CurrentValue; }

        internal bool TryAdjust(int inValue)
        {
            if (inValue == 0 || (m_CurrentValue + inValue) < 0)
                return false;

            m_CurrentValue += inValue;
            Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, m_ItemId);
            return true;
        }

        internal bool Set(int inValue)
        {
            if (inValue < 0)
            {
                inValue = 0;
            }

            if (m_CurrentValue != inValue)
            {
                m_CurrentValue = inValue;
                Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, m_ItemId);
                return true;
            }

            return false;
        }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("id", ref m_ItemId);
            ioSerializer.Serialize("value", ref m_CurrentValue, 1);
        }
    }
    
}