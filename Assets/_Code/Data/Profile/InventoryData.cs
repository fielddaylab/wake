using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class InventoryData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private RingBuffer<PlayerInv> m_Items = new RingBuffer<PlayerInv>();
        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UpgradeIds = new HashSet<StringHash32>();

        [NonSerialized] private bool m_ItemListDirty = true;
        [NonSerialized] private bool m_HasChanges;

        #region Items

        public ListSlice<PlayerInv> Items()
        {
            CleanItemList();
            return m_Items;
        }

        public IEnumerable<PlayerInv> GetItems(InvItemCategory inCategory)
        {
            if (inCategory == InvItemCategory.Upgrade)
            {
                foreach(var upgrade in m_UpgradeIds)
                {
                    yield return new PlayerInv(upgrade, 1, Assets.Item(upgrade));
                }
            }
            else
            {
                var db = Services.Assets.Inventory;
                CleanItemList();
                foreach(var item in m_Items)
                {
                    InvItem desc = Assets.Item(item.ItemId);
                    if ((item.Count > 0 || db.IsAlwaysVisible(item.ItemId)) && desc.Category() == inCategory)
                        yield return item;
                }
            }
        }

        public int GetItems(InvItemCategory inCategory, ICollection<PlayerInv> outItems)
        {
            if (inCategory == InvItemCategory.Upgrade)
            {
                foreach(var upgrade in m_UpgradeIds)
                {
                    outItems.Add(new PlayerInv(upgrade, 1, Assets.Item(upgrade)));
                }
                return m_UpgradeIds.Count;
            }
            else
            {
                var db = Services.Assets.Inventory;
                int count = 0;
                foreach(var item in m_Items)
                {
                    InvItem desc = db.Get(item.ItemId);
                    if ((item.Count > 0 || db.IsAlwaysVisible(item.ItemId)) && desc.Category() == inCategory)
                    {
                        outItems.Add(item);
                        count++;
                    }
                }
                return count;
            }
        }

        public bool HasItem(StringHash32 inId)
        {
            PlayerInv item;
            return TryFindInv(inId, out item) && item.Count > 0;
        }

        public uint ItemCount(StringHash32 inId)
        {
            InvItem itemDesc = Assets.Item(inId);
            if (itemDesc.Category() == InvItemCategory.Upgrade)
            {
                return m_UpgradeIds.Contains(inId) ? 1u : 0u;
            }

            PlayerInv item;
            TryFindInv(inId, out item);
            return item.Count;
        }
        
        public bool AdjustItem(StringHash32 inId, int inAmount)
        {
            if (inAmount == 0)
                return true;

            ref PlayerInv item = ref RequireInv(inId);
            if (TryAdjust(ref item, inAmount, false))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool AdjustItemWithoutNotify(StringHash32 inId, int inAmount)
        {
            if (inAmount == 0)
                return true;

            ref PlayerInv item = ref RequireInv(inId);
            if (TryAdjust(ref item, inAmount, true))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool SetItem(StringHash32 inId, uint inAmount)
        {
            ref PlayerInv item = ref RequireInv(inId);
            if (TrySet(ref item, inAmount, false))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool SetItemWithoutNotify(StringHash32 inId, uint inAmount)
        {
            ref PlayerInv item = ref RequireInv(inId);
            if (TrySet(ref item, inAmount, true))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public PlayerInv GetItem(StringHash32 inId)
        {
            PlayerInv inv;
            TryFindInv(inId, out inv);
            return inv;
        }

        private bool TryFindInv(StringHash32 inId, out PlayerInv outItem)
        {
            CleanItemList();

            Assert.True(Services.Assets.Inventory.HasId(inId), "Could not find ItemDesc with id '{0}'", inId);
            Assert.True(Services.Assets.Inventory.IsCountable(inId), "Item '{0}' is not countable", inId);

            if (!m_Items.TryBinarySearch(inId, out outItem))
            {
                outItem = new PlayerInv(inId, 0, Assets.Item(inId));
                return false;
            }

            return true;
        }

        private ref PlayerInv RequireInv(StringHash32 inId)
        {
            CleanItemList();

            Assert.True(Services.Assets.Inventory.HasId(inId), "Could not find ItemDesc with id '{0}'", inId);
            Assert.True(Services.Assets.Inventory.IsCountable(inId), "Item '{0}' is not countable", inId);

            int index = m_Items.BinarySearch(inId);
            if (index < 0)
            {
                index = m_Items.Count;
                m_Items.PushBack(new PlayerInv(inId, 0, Assets.Item(inId)));
                m_ItemListDirty = true;
                m_HasChanges = true;
            }
            
            return ref m_Items[index];
        }

        private void CleanItemList()
        {
            if (!m_ItemListDirty)
                return;

            m_Items.SortByKey<StringHash32, PlayerInv, PlayerInv>();
            m_ItemListDirty = false;
        }

        private bool TryAdjust(ref PlayerInv ioItem, int inValue, bool inbSuppressEvent)
        {
            if (inValue == 0 || (ioItem.Count + inValue) < 0)
                return false;

            ioItem.Count = (uint) (ioItem.Count + inValue);
            if (!inbSuppressEvent)
            {
                Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, ioItem.ItemId);
            }
            return true;
        }

        private bool TrySet(ref PlayerInv ioItem, uint inValue, bool inbSuppressEvent)
        {
            if (ioItem.Count != inValue)
            {
                ioItem.Count = (uint) inValue;
                if (!inbSuppressEvent)
                {
                    Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, ioItem.ItemId);
                }
                return true;
            }

            return false;
        }

        #endregion // Inventory

        #region Scanner

        public bool WasScanned(StringHash32 inId)
        {
            return m_ScannerIds.Contains(inId);
        }

        public bool RegisterScanned(StringHash32 inId)
        {
            if (m_ScannerIds.Add(inId))
            {
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.ScanLogUpdated, inId);
                return true;
            }

            return false;
        }

        #endregion // Scanner

        #region Upgrades

        public bool HasUpgrade(StringHash32 inUpgradeId)
        {
            Assert.True(Services.Assets.Inventory.HasId(inUpgradeId), "Could not find ItemDesc with id '{0}'", inUpgradeId);
            return m_UpgradeIds.Contains(inUpgradeId);
        }

        public bool AddUpgrade(StringHash32 inUpgradeId)
        {
            Assert.True(Services.Assets.Inventory.HasId(inUpgradeId), "Could not find ItemDesc with id '{0}'", inUpgradeId);
            if (m_UpgradeIds.Add(inUpgradeId))
            {
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, inUpgradeId);
                return true;
            }

            return false;
        }

        public bool RemoveUpgrade(StringHash32 inUpgradeId)
        {
            Assert.True(Services.Assets.Inventory.HasId(inUpgradeId), "Could not find ItemDesc with id '{0}'", inUpgradeId);
            if (m_UpgradeIds.Remove(inUpgradeId))
            {
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.InventoryUpdated, inUpgradeId);
                return true;
            }

            return false;
        }

        #endregion // Upgrades

        #region IProfileChunk

        public void SetDefaults()
        {
            foreach(var item in Services.Assets.Inventory.Objects)
            {
                if (item.DefaultAmount() > 0)
                {
                    if (item.Category() == InvItemCategory.Upgrade)
                    {
                        AddUpgrade(item.Id());
                    }
                    else
                    {
                        ref PlayerInv playerInv = ref RequireInv(item.Id());
                        playerInv.Count = item.DefaultAmount();
                    }
                }
            }
        }

        // v3: removed water property
        ushort ISerializedVersion.Version { get { return 3; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("items", ref m_Items);
            ioSerializer.UInt32ProxySet("scannerIds", ref m_ScannerIds);
            ioSerializer.UInt32ProxySet("upgradeIds", ref m_UpgradeIds);
            if (ioSerializer.ObjectVersion >= 2 && ioSerializer.ObjectVersion < 3)
            {
                uint ignored = 0;
                ioSerializer.Serialize("waterProps", ref ignored);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            var invDB = Services.Assets.Inventory;

            for(int i = m_Items.Count - 1; i >= 0; i--)
            {
                ref PlayerInv inv = ref m_Items[i];
                if (!invDB.HasId(inv.ItemId)) {
                    Log.Warn("[InventoryData] Unknown item id '{0}'", inv.ItemId);
                    m_Items.FastRemoveAt(i);
                } else {
                    inv.Item = Assets.Item(inv.ItemId);
                }
            }

            m_UpgradeIds.RemoveWhere((itemId) => {
                if (!invDB.HasId(itemId))
                {
                    Log.Warn("[InventoryData] Unknown upgrade id '{0}'", itemId);
                    return true;
                }

                return false;
            });
        }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        #endregion // IProfileChunk
    }
}