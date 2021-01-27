using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua.Profile
{
    public class InventoryData : ISerializedObject, ISerializedVersion
    {
        private List<PlayerInv> m_Items = new List<PlayerInv>();

        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();


        public IEnumerable<PlayerInv> Items()
        {
            foreach (var item in m_Items)
            {
                yield return item;
            }
        }
        public void InitializeDefault(bool defaultValue = true)
        {
            if (m_Items.Count == 0)
            {
                foreach (var item in Services.Assets.Inventory.Objects)
                {
                    var inItem = item;

                    if (defaultValue)
                    {
                        inItem.SetDefault();
                    }
                    else
                    {
                        inItem.Value = 0;
                    }
                    PlayerInv pItem = new PlayerInv(inItem);
                    m_Items.Add(pItem);
                }
            }
        }

        public bool ListIsEmpty()
        {
            if (m_Items.Count == 0)
            {
                return true;
            }
            return false;
        }


        public bool HasItem(StringHash32 Id)
        {
            foreach (var item in m_Items)
            {
                if (item.Item.Id() == Id)
                {
                    return true;
                }
            }
            return false;
        }

        public PlayerInv GetItemByName(string NameId)
        {
            foreach (var playerItem in m_Items)
            {
                var nameid = playerItem.Item.NameTextId();
                if (nameid == NameId)
                {
                    return playerItem;
                }
            }
            return null;
        }

        public PlayerInv GetItemById(StringHash32 Id)
        {
            foreach (var playerItem in m_Items)
            {
                if (playerItem.Item.Id() == Id)
                {
                    return playerItem;
                }
            }
            return null;
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