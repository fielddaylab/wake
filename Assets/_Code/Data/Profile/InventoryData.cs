using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Profile
{
    public class InventoryData : IProfileChunk, ISerializedVersion
    {
        private List<PlayerInv> m_Items = new List<PlayerInv>();
        private HashSet<StringHash32> m_ScannerIds = new HashSet<StringHash32>();
        private HashSet<StringHash32> m_UpgradeIds = new HashSet<StringHash32>();
        private uint m_WaterProperties = 0;

        [NonSerialized] private bool m_ItemListDirty = true;
        [NonSerialized] private bool m_HasChanges;

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
                        m_HasChanges = true;
                    }
                }
            }

            foreach(var property in Services.Assets.WaterProp.DefaultUnlocked())
            {
                m_WaterProperties |= (1U << (int) property);
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
            if (item != null && item.TryAdjust(inAmount))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool SetItem(StringHash32 inId, int inAmount)
        {
            var item = GetInv(inId, inAmount > 0);
            if (item != null && item.Set(inAmount))
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public PlayerInv GetItem(StringHash32 inId)
        {
            return GetInv(inId, true);
        }

        private PlayerInv GetInv(StringHash32 inId, bool inbCreate)
        {
            CleanItemList();

            Assert.True(Services.Assets.Inventory.HasId(inId), "Could not find ItemDesc with id '{0}'", inId);

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

        #region Water Properties

        public bool IsPropertyUnlocked(WaterPropertyId inId)
        {
            return Bits.Contains(m_WaterProperties, (int) inId);
        }

        public WaterPropertyMask GetPropertyUnlockedMask()
        {
            return new WaterPropertyMask((byte) m_WaterProperties);
        }

        public bool UnlockProperty(WaterPropertyId inId)
        {
            if (!Bits.Contains(m_WaterProperties, (int) inId))
            {
                Bits.Add(ref m_WaterProperties, (int) inId);
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.WaterPropertiesUpdated, inId);
                return true;
            }

            return false;
        }

        public bool LockProperty(WaterPropertyId inId)
        {
            if (Bits.Contains(m_WaterProperties, (int) inId))
            {
                Bits.Remove(ref m_WaterProperties, (int) inId);
                m_HasChanges = true;
                Services.Events.QueueForDispatch(GameEvents.WaterPropertiesUpdated, inId);
                return true;
            }

            return false;
        }

        #endregion // Water Properties

        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 2; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("items", ref m_Items);
            ioSerializer.UInt32ProxySet("scannerIds", ref m_ScannerIds);
            ioSerializer.UInt32ProxySet("upgradeIds", ref m_UpgradeIds);
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Serialize("waterProps", ref m_WaterProperties);
            }
            else
            {
                foreach(var property in Services.Assets.WaterProp.DefaultUnlocked())
                {
                    m_WaterProperties |= (1U << (int) property);
                }
            }
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