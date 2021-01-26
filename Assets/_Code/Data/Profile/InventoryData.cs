using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua.Profile
{
    public class InventoryData : ISerializedObject, ISerializedVersion
    {
        private List<InvItem> m_Items = new List<InvItem>();

        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();

        public bool CheckUpdateResource(StringHash32 itemId, int value)
        {
            foreach (var item in m_Items)
            {
                if (item.ItemId().Equals(itemId))
                {
                    if ((item.Value() + value) < 0)
                    {
                        return false;
                    }
                    else
                    {
                        item.UpdateValue(value);
                        return true;
                    }

                }
            }
            throw new KeyNotFoundException("item " + itemId + "doesn't exist.");
        }

        public bool HasItem(StringHash32 ItemId)
        {
            foreach (var item in m_Items)
            {
                if (item.ItemId().Equals(ItemId))
                {
                    return true;
                }
            }
            return false;
        }

        public InvItem GetItem(StringHash32 ItemId)
        {
            if (m_Items.Count == 0)
            {
                foreach (var item in Services.Assets.Inventory.Objects)
                {
                    m_Items.Add(item);
                }
            }
            foreach (var item in m_Items)
            {
                if (item.ItemId().Equals(m_Items))
                {
                    return item;
                }
            }
            throw new KeyNotFoundException("Item " + ItemId + " couldn't be found");
        }


        #region Scanner

        public bool WasScanned(StringHash32 inId)
        {
            return m_ScannerIds.Contains(inId);
        }

        public bool RegisterScanned(StringHash32 inId)
        {
            return m_ScannerIds.Add(inId);
        }

        #endregion // Scanner

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            // TODO: Implement
        }

        #endregion // ISerializedData
    }
}