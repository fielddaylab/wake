using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class InventoryData : ISerializedObject, ISerializedVersion
    {
        private List<PlayerInv> m_Items = new List<PlayerInv>();
        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();

        [NonSerialized] private bool m_ItemListDirty = true;
        [NonSerialized] public bool HasFlags = false;

        #region Items

        public IEnumerable<PlayerInv> Items()
        {
            CleanItemList();
            return m_Items;
        }

        public void SetDefaults()
        {
            foreach(var item in Services.Assets.Inventory.Objects)
            {
                if (item.DefaultValue() > 0)
                {
                    var playerInv = GetInv(item.Id(), false);
                    if (playerInv == null)
                    {
                        m_Items.Add(new PlayerInv(item));
                        m_ItemListDirty = true;
                    }
                }
            }
        }

        public bool HasItem(StringHash32 inId)
        {
            return GetInv(inId, false)?.Value() > 0;
        }

        public int ItemCount(StringHash32 inId)
        {
            return GetInv(inId, false)?.Value() ?? 0;
        }
        
        public bool AdjustItem(StringHash32 inId, int inAmount)
        {
            if (inAmount == 0)
                return true;

            var item = GetInv(inId, inAmount > 0);
            return item != null && item.TryAdjust(inAmount);
        }

        public bool GiveItem(StringHash32 inId, bool hasArtifact=false)
        {
            HasFlags = hasArtifact;
            return AdjustItem(inId, 1);
        }

        public void SetItem(StringHash32 inId, int inAmount)
        {
            var item = GetInv(inId, inAmount > 0);
            item?.Set(inAmount);
        }

        private PlayerInv GetInv(StringHash32 inId, bool inbCreate)
        {
            CleanItemList();

            Assert.True(Services.Assets.Inventory.HasId(inId), "Could not find ItemDesc with id '{0}'", inId.ToDebugString());

            PlayerInv item;
            if (!m_Items.TryBinarySearch(inId, out item) && inbCreate)
            {
                item = new PlayerInv(inId);
                m_Items.Add(item);
                m_ItemListDirty = true;
            }

            return item;
        }

        private void CleanItemList()
        {
            if (!m_ItemListDirty)
                return;

            m_Items.SortByKey<StringHash32, PlayerInv, PlayerInv>();
            m_ItemListDirty = false;
        }

        #endregion // Inventory

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