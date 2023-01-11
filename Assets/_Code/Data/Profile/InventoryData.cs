using System;
using System.Collections.Generic;
using Aqua.Journal;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using EasyBugReporter;

namespace Aqua.Profile
{
    public class InventoryData : IProfileChunk, ISerializedVersion, ISerializedCallbacks
    {
        private RingBuffer<PlayerInv> m_Items = new RingBuffer<PlayerInv>();
        private HashSet<StringHash32> m_ScannerIds = Collections.NewSet<StringHash32>(64);
        private HashSet<StringHash32> m_UpgradeIds = Collections.NewSet<StringHash32>(20);
        private List<StringHash32> m_JournalIds = new List<StringHash32>();

        private uint m_Cash;
        private uint m_Exp;

        [NonSerialized] private bool m_ItemListDirty = true;
        [NonSerialized] private bool m_HasChanges;

        #region Items

        public uint Cash()
        {
            return m_Cash;
        }

        public uint Exp()
        {
            return m_Exp;
        }

        public bool HasItem(StringHash32 inId)
        {
            if (inId == ItemIds.Cash)
            {
                return m_Cash > 0;
            }
            else if (inId == ItemIds.Exp)
            {
                return m_Exp > 0;
            }
            else
            {
                PlayerInv item;
                return TryFindInv(inId, out item) && item.Count > 0;
            }
        }

        public uint ItemCount(StringHash32 inId)
        {
            if (inId == ItemIds.Cash)
            {
                return m_Cash;
            }
            else if (inId == ItemIds.Exp)
            {
                return m_Exp;
            }
            else
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
        }
        
        public bool AdjustItem(StringHash32 inId, int inAmount)
        {
            if (inAmount == 0)
                return true;

            if (inId == ItemIds.Cash)
            {
                if (TryAdjust(inId, ref m_Cash, inAmount, false))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

            if (inId == ItemIds.Exp)
            {
                if (TryAdjust(inId, ref m_Exp, inAmount, false))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

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

            if (inId == ItemIds.Cash)
            {
                if (TryAdjust(inId, ref m_Cash, inAmount, true))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

            if (inId == ItemIds.Exp)
            {
                if (TryAdjust(inId, ref m_Exp, inAmount, true))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

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
            if (inId == ItemIds.Cash)
            {
                if (TrySet(inId, ref m_Cash, inAmount, false))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

            if (inId == ItemIds.Exp)
            {
                if (TrySet(inId, ref m_Exp, inAmount, false))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

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
            if (inId == ItemIds.Cash)
            {
                if (TrySet(inId, ref m_Cash, inAmount, true))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

            if (inId == ItemIds.Exp)
            {
                if (TrySet(inId, ref m_Exp, inAmount, true))
                {
                    m_HasChanges = true;
                    return true;
                }

                return false;
            }

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
            if (inId == ItemIds.Cash)
            {
                return new PlayerInv(inId, m_Cash, Assets.Item(inId));
            }
            else if (inId == ItemIds.Exp)
            {
                return new PlayerInv(inId, m_Exp, Assets.Item(inId));
            }

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
                Services.Events.Queue(GameEvents.InventoryUpdated, ioItem.ItemId);
            }
            return true;
        }

        private bool TryAdjust(StringHash32 inItemId, ref uint ioItem, int inValue, bool inbSuppressEvent)
        {
            if (inValue == 0 || (ioItem + inValue) < 0)
                return false;

            ioItem = (uint) (ioItem + inValue);
            if (!inbSuppressEvent)
            {
                Services.Events.Queue(GameEvents.InventoryUpdated, inItemId);
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
                    Services.Events.Queue(GameEvents.InventoryUpdated, ioItem.ItemId);
                }
                return true;
            }

            return false;
        }

        private bool TrySet(StringHash32 inItemId, ref uint ioItem, uint inValue, bool inbSuppressEvent)
        {
            if (ioItem != inValue)
            {
                ioItem = (uint) inValue;
                if (!inbSuppressEvent)
                {
                    Services.Events.Queue(GameEvents.InventoryUpdated, inItemId);
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
                Services.Events.Queue(GameEvents.ScanLogUpdated, inId);
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
                Services.Events.Queue(GameEvents.InventoryUpdated, inUpgradeId);
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
                Services.Events.Queue(GameEvents.InventoryUpdated, inUpgradeId);
                return true;
            }

            return false;
        }

        public int UpgradeCount() {
            return m_UpgradeIds.Count;
        }

        #endregion // Upgrades

        #region Journals

        public bool HasJournalEntry(StringHash32 inJournalId)
        {
            Assert.True(Services.Assets.Journal.HasId(inJournalId), "Could not find JournalDesc with id '{0}'", inJournalId);
            return m_JournalIds.Contains(inJournalId);
        }

        public bool AddJournalEntry(StringHash32 inJournalId)
        {
            Assert.True(Services.Assets.Journal.HasId(inJournalId), "Could not find JournalDesc with id '{0}'", inJournalId);
            if (m_JournalIds.Contains(inJournalId))
                return false;

            m_JournalIds.Add(inJournalId);
            return true;
        }

        public bool RemoveJournalEntry(StringHash32 inJournalId)
        {
            Assert.True(Services.Assets.Journal.HasId(inJournalId), "Could not find JournalDesc with id '{0}'", inJournalId);
            return m_JournalIds.Remove(inJournalId);
        }

        public ListSlice<StringHash32> AllJournalEntryIds()
        {
            return m_JournalIds;
        }

        #endregion // Journals

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
                        if (item.Id() == ItemIds.Cash)
                        {
                            m_Cash = item.DefaultAmount();
                        }
                        else if (item.Id() == ItemIds.Exp)
                        {
                            m_Exp = item.DefaultAmount();
                        }
                        else
                        {
                            ref PlayerInv playerInv = ref RequireInv(item.Id());
                            playerInv.Count = item.DefaultAmount();
                        }
                    }
                }
            }

            foreach(var journal in Services.Assets.Journal.Objects) {
                if (journal.IsDefault()) {
                    AddJournalEntry(journal.Id());
                }
            }
        }

        // v3: removed water property
        // v5: fast path for cash/exp
        ushort ISerializedVersion.Version { get { return 5; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("items", ref m_Items);
            ioSerializer.UInt32ProxySet("scannerIds", ref m_ScannerIds);
            ioSerializer.UInt32ProxySet("upgradeIds", ref m_UpgradeIds);
            if (ioSerializer.ObjectVersion >= 2 && ioSerializer.ObjectVersion < 3)
            {
                uint waterChem = 0;
                ioSerializer.Serialize("waterProps", ref waterChem);
                if (waterChem != 0) {
                    m_UpgradeIds.Add(ItemIds.WaterModeling);
                }
            }

            if (ioSerializer.ObjectVersion >= 4) {
                ioSerializer.UInt32ProxyArray("journalIds", ref m_JournalIds);
            }

            if (ioSerializer.ObjectVersion >= 5) {
                ioSerializer.Serialize("cash", ref m_Cash);
                ioSerializer.Serialize("exp", ref m_Exp);
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

            SavePatcher.PatchIds(m_UpgradeIds);

            var invDB = Services.Assets.Inventory;

            for(int i = m_Items.Count - 1; i >= 0; i--)
            {
                ref PlayerInv inv = ref m_Items[i];
                SavePatcher.PatchId(ref inv.ItemId);

                if (!invDB.HasId(inv.ItemId)) {
                    Log.Warn("[InventoryData] Unknown item id '{0}'", inv.ItemId);
                    m_Items.FastRemoveAt(i);
                } else {
                    inv.Item = Assets.Item(inv.ItemId);
                }

                if (inv.ItemId == ItemIds.Cash) {
                    m_Cash = inv.Count;
                    m_Items.FastRemoveAt(i);
                } else if (inv.ItemId == ItemIds.Exp) {
                    m_Exp = inv.Count;
                    m_Items.FastRemoveAt(i);
                }
            }

            for(int i = m_JournalIds.Count - 1; i >= 0; i--)
            {
                SavePatcher.PatchIds(m_JournalIds);
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

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            writer.Header("Inventory");
            foreach(var item in m_Items) {
                writer.KeyValue(Assets.NameOf(item.ItemId), item.Count);
            }
            writer.KeyValue("Cash", m_Cash);
            writer.KeyValue("Exp", m_Exp);
            writer.Header("Upgrade Ids");
            foreach(var itemId in m_UpgradeIds) {
                writer.Text(Assets.NameOf(itemId));
            }
            writer.Header("Scanner Ids");
            foreach(var scannerId in m_ScannerIds) {
                writer.Text(scannerId.ToDebugString());
            }
            writer.Header("Journal Ids");
            foreach(var journalId in m_JournalIds) {
                writer.Text(Assets.NameOf(journalId));
            }
        }

        #endregion // IProfileChunk
    }
}