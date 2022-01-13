using BeauUtil;
using BeauUtil.Variants;
using Aqua.Profile;
using Aqua.Scripting;
using Aqua.Debugging;
using BeauUtil.Debugger;
using Leaf.Runtime;
using System.Collections;

namespace Aqua
{
    public partial class DataService : ServiceBehaviour
    {
        public IVariantResolver VariableResolver { get { return m_VariableResolver; } }

        #region Variables

        /// <summary>
        /// Retrieves a variable with the given identifier and an optional context.
        /// </summary>
        public Variant GetVariable(StringSlice inId, object inContext = null)
        {
            TableKeyPair keyPair;
            Variant result = default(Variant);
            if (TableKeyPair.TryParse(inId, out keyPair))
            {
                VariableResolver.TryResolve(inContext, keyPair, out result);
            }
            return result;
        }

        /// <summary>
        /// Retrieves a variable with the given identifier and an optional context.
        /// </summary>
        public Variant GetVariable(TableKeyPair inId, object inContext = null)
        {
            Variant result = default(Variant);
            VariableResolver.TryResolve(inContext, inId, out result);
            return result;
        }

        /// <summary>
        /// Pops a variable with the given identifier.
        /// </summary>
        public Variant PopVariable(TableKeyPair inId, object inContext = null)
        {
            Variant result = GetVariable(inId, inContext);
            SetVariable(inId, null, inContext);
            return result;
        }

        /// <summary>
        /// Sets a variable with the given identifier and an optional context.
        /// </summary>
        public void SetVariable(StringSlice inId, Variant inValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inId, out keyPair)
                || !VariableResolver.TryModify(inContext, keyPair, VariantModifyOperator.Set, inValue))
            {
                Log.Error("[DataService] Unable to set variable '{0}' to {1}", inId, inValue.ToDebugString());
            }
            else
            {
                Services.Events.QueueForDispatch(GameEvents.VariableSet, keyPair);
            }
        }

        /// <summary>
        /// Sets a variable with the given identifier and an optional context.
        /// </summary>
        public void SetVariable(TableKeyPair inId, Variant inValue, object inContext = null)
        {
            if (!VariableResolver.TryModify(inContext, inId, VariantModifyOperator.Set, inValue))
            {
                Log.Error("[DataService] Unable to set variable '{0}' to {1}", inId, inValue);
            }
            else
            {
                Services.Events.QueueForDispatch(GameEvents.VariableSet, inId);
            }
        }

        /// <summary>
        /// Adds to a variable with the given identifier and an optional context.
        /// </summary>
        public void AddVariable(StringSlice inId, Variant inValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inId, out keyPair)
                || !VariableResolver.TryModify(inContext, keyPair, VariantModifyOperator.Add, inValue))
            {
                Log.Error("[DataService] Unable to add variable '{0}' to {1}", inId, inValue);
            }
            else
            {
                Services.Events.QueueForDispatch(GameEvents.VariableSet, keyPair);
            }
        }

        /// <summary>
        /// Adds to a variable with the given identifier and an optional context.
        /// </summary>
        public void AddVariable(TableKeyPair inId, Variant inValue, object inContext = null)
        {
            if (!VariableResolver.TryModify(inContext, inId, VariantModifyOperator.Add, inValue))
            {
                Log.Error("[DataService] Unable to add variable '{0}' to {1}", inId, inValue);
            }
            else
            {
                Services.Events.QueueForDispatch(GameEvents.VariableSet, inId);
            }
        }

        /// <summary>
        /// Retrieves a variable with the given id.
        /// If the value equals the old value, the variable is set to the new value.
        /// </summary>
        public bool CompareExchange(StringSlice inId, Variant inOldValue, Variant inNewValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inId, out keyPair))
            {
                return false;
            }

            Variant result = default(Variant);
            VariableResolver.TryResolve(inContext, keyPair, out result);
            if (result == inOldValue)
            {
                SetVariable(keyPair, inNewValue, inContext);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Retrieves a variable with the given id.
        /// If the value equals the old value, the variable is set to the new value.
        /// </summary>
        public bool CompareExchange(TableKeyPair inId, Variant inOldValue, Variant inNewValue, object inContext = null)
        {
            Variant result = default(Variant);
            VariableResolver.TryResolve(inContext, inId, out result);
            if (result == inOldValue)
            {
                SetVariable(inId, inNewValue, inContext);
                return true;
            }
            
            return false;
        }

        #endregion // Variables

        #region Tables

        /// <summary>
        /// Binds a table.
        /// </summary>
        public void BindTable(StringHash32 inId, VariantTable inTable)
        {
            m_VariableResolver.SetTable(inId, inTable);
            DebugService.Log(LogMask.Loading | LogMask.DataService, "[DataService] Bound table '{0}'", inId);
        }

        /// <summary>
        /// Unbinds a table.
        /// </summary>
        public void UnbindTable(StringHash32 inId)
        {
            m_VariableResolver.ClearTable(inId);
            DebugService.Log(LogMask.Loading | LogMask.DataService, "[DataService] Unbound table '{0}'", inId);
        }

        #endregion // Tables

        #region Conditions

        /// <summary>
        /// Checks if the given conditions are true.
        /// If empty, will also return true.
        /// </summary>
        public bool CheckConditions(StringSlice inConditions, object inContext = null)
        {
            return VariableResolver.TryEvaluate(inContext, inConditions, Services.Script.LeafInvoker);
        }

        #endregion // Conditions

        private void InitVariableResolver()
        {
            m_VariableResolver = new CustomVariantResolver();

            m_VariableResolver.SetVar(GameVars.SceneName, GetSceneName);
            m_VariableResolver.SetVar(GameVars.MapId, () => MapDB.LookupCurrentMap());
            m_VariableResolver.SetVar(GameVars.LastEntrance, () => Services.State.LastEntranceId);

            m_VariableResolver.SetVar(GameVars.PlayerGender, GetPlayerPronouns);
            m_VariableResolver.SetVar(GameVars.CurrentJob, GetJobId);
            m_VariableResolver.SetVar(GameVars.CurrentStation, GetStationId);
            m_VariableResolver.SetVar(GameVars.ActNumber, GetActNumber);
        }

        private void HookSaveDataToVariableResolver(SaveData inData)
        {
            m_VariableResolver.SetTable("global", inData.Script.GlobalTable);
            m_VariableResolver.SetTable("jobs", inData.Script.JobsTable);
            m_VariableResolver.SetTable("world", inData.Script.PartnerTable);
            m_VariableResolver.SetTable("player", inData.Script.PlayerTable);
            m_VariableResolver.SetTable("kevin", inData.Script.PartnerTable);
        }

        #region Callbacks

        private Variant GetPlayerPronouns()
        {
            switch(CurrentCharacterPronouns())
            {
                case Pronouns.Masculine:
                    return "m";
                case Pronouns.Feminine:
                    return "f";
                default:
                    return "x";
            }
        }

        static private Variant GetSceneName()
        {
            return SceneHelper.ActiveScene().Name;
        }

        private Variant GetJobId()
        {
            return m_CurrentSaveData.Jobs.CurrentJobId;
        }

        private Variant GetStationId()
        {
            return m_CurrentSaveData.Map.CurrentStationId();
        }

        private Variant GetActNumber()
        {
            return m_CurrentSaveData.Script.ActIndex;
        }

        #endregion // Callbacks

        #region Leaf

        static private class LeafIntegration
        {
            static private readonly RingBuffer<BFBase> s_BatchedFacts = new RingBuffer<BFBase>(8, RingBufferMode.Expand);
            static private readonly RingBuffer<BFDiscoveredFlags> s_BatchedFactFlags = new RingBuffer<BFDiscoveredFlags>(8, RingBufferMode.Expand);

            static public void ClearBatches()
            {
                s_BatchedFacts.Clear();
                s_BatchedFacts.Clear();
            }

            private enum PopupMode
            {
                Silent,
                Popup,
                Batch
            }

            #region Bestiary/Inventory

            [LeafMember("HasEntity"), UnityEngine.Scripting.Preserve]
            static private bool HasEntity(StringHash32 inEntityId)
            {
                return Save.Bestiary.HasEntity(inEntityId);
            }

            [LeafMember("GiveEntity"), UnityEngine.Scripting.Preserve]
            static private IEnumerator GiveEntity([BindContext] ScriptThread inThread, StringHash32 inEntityId, PopupMode inMode = PopupMode.Popup)
            {
                if (Save.Bestiary.RegisterEntity(inEntityId) && inMode == PopupMode.Popup)
                {
                    inThread.Dialog = null;

                    if (Services.UI.IsSkippingCutscene())
                        return null;

                    Services.Audio.PostEvent("item.popup.new");
                    return Script.PopupNewEntity(Assets.Bestiary(inEntityId)).Wait();
                }

                return null;
            }

            [LeafMember("HasFact"), UnityEngine.Scripting.Preserve]
            static private bool HasFact(StringHash32 inFactId)
            {
                return Save.Bestiary.HasFact(inFactId);
            }

            [LeafMember("IsFactGraphed"), UnityEngine.Scripting.Preserve]
            static private bool IsFactGraphed(StringHash32 inMapId, StringHash32 inFactId)
            {
                var data = Save.Science.TryGetSiteData(inMapId);
                return data != null && data.GraphedFacts.Contains(inFactId);
            }

            [LeafMember("IsEntityGraphed"), UnityEngine.Scripting.Preserve]
            static private bool IsEntityGraphed(StringHash32 inMapId, StringHash32 inEntityId)
            {
                var data = Save.Science.TryGetSiteData(inMapId);
                return data != null && data.GraphedCritters.Contains(inEntityId);
            }

            [LeafMember("GiveFact"), UnityEngine.Scripting.Preserve]
            static private IEnumerator GiveFact([BindContext] ScriptThread inThread, StringHash32 inFactId, PopupMode inMode = PopupMode.Popup)
            {
                BFBase fact = Assets.Fact(inFactId);
                if (Save.Bestiary.RegisterFact(inFactId, fact.Type == BFTypeId.Model) && inMode != PopupMode.Silent)
                {
                    BFDiscoveredFlags flags = Save.Bestiary.GetDiscoveredFlags(inFactId);

                    if (inMode == PopupMode.Batch)
                    {
                        if (Services.UI.IsSkippingCutscene())
                            return null;

                        s_BatchedFacts.PushBack(fact);
                        s_BatchedFactFlags.PushBack(flags);
                    }
                    else
                    {
                        inThread.Dialog = null;

                        if (Services.UI.IsSkippingCutscene())
                        {
                            s_BatchedFactFlags.Clear();
                            s_BatchedFacts.Clear();
                            return null;
                        }

                        Services.Audio.PostEvent("item.popup.new");

                        IEnumerator popup;

                        if (s_BatchedFacts.Count > 0)
                        {
                            s_BatchedFacts.PushBack(fact);
                            s_BatchedFactFlags.PushBack(flags);

                            popup = Script.PopupNewFacts(s_BatchedFacts, s_BatchedFactFlags).Wait();

                            s_BatchedFactFlags.Clear();
                            s_BatchedFacts.Clear();
                        }
                        else
                        {
                            popup = Script.PopupNewFact(fact).Wait();
                        }

                        return popup;
                    }
                }
                return null;
            }

            [LeafMember("UpgradeFact"), UnityEngine.Scripting.Preserve]
            static private IEnumerator UpgradeFact([BindContext] ScriptThread inThread, StringHash32 inFactId, BFDiscoveredFlags inFlags = BFDiscoveredFlags.Rate, PopupMode inMode = PopupMode.Popup)
            {
                BFBase fact = Assets.Fact(inFactId);
                if (Save.Bestiary.AddDiscoveredFlags(inFactId, inFlags) && inMode != PopupMode.Silent)
                {
                    BFDiscoveredFlags flags = Save.Bestiary.GetDiscoveredFlags(inFactId);

                    if (inMode == PopupMode.Batch)
                    {
                        if (Services.UI.IsSkippingCutscene())
                            return null;

                        s_BatchedFacts.PushBack(fact);
                        s_BatchedFactFlags.PushBack(flags);
                    }
                    else
                    {
                        inThread.Dialog = null;

                        if (Services.UI.IsSkippingCutscene())
                        {
                            s_BatchedFactFlags.Clear();
                            s_BatchedFacts.Clear();
                            return null;
                        }

                        Services.Audio.PostEvent("item.popup.new");

                        IEnumerator popup;

                        if (s_BatchedFacts.Count > 0)
                        {
                            s_BatchedFacts.PushBack(fact);
                            s_BatchedFactFlags.PushBack(flags);

                            popup = Script.PopupUpgradedFacts(s_BatchedFacts, s_BatchedFactFlags).Wait();
                            s_BatchedFactFlags.Clear();
                            s_BatchedFacts.Clear();
                        }
                        else
                        {
                            popup = Script.PopupUpgradedFact(fact).Wait();
                        }

                        return popup;
                    }
                }
                return null;
            }

            [LeafMember("FinishFactBatch"), UnityEngine.Scripting.Preserve]
            static private IEnumerator CompleteFactBatch([BindContext] ScriptThread inThread)
            {
                if (s_BatchedFacts.Count <= 0)
                    return null;
                
                if (Services.UI.IsSkippingCutscene())
                {
                    s_BatchedFactFlags.Clear();
                    s_BatchedFacts.Clear();
                    return null;
                }

                inThread.Dialog = null;
                Services.Audio.PostEvent("item.popup.new");

                IEnumerator popup;

                popup = Script.PopupUpgradedFacts(s_BatchedFacts, s_BatchedFactFlags).Wait();
                s_BatchedFactFlags.Clear();
                s_BatchedFacts.Clear();

                return popup;
            }

            [LeafMember("HasItem"), UnityEngine.Scripting.Preserve]
            static private bool HasItem(StringHash32 inItemId)
            {
                return Save.Inventory.HasItem(inItemId);
            }

            [LeafMember("ItemCount"), UnityEngine.Scripting.Preserve]
            static private int ItemCount(StringHash32 inItemId)
            {
                return Save.Inventory.ItemCount(inItemId);
            }

            [LeafMember("HasItemCount"), UnityEngine.Scripting.Preserve]
            static private bool HasItemCount(StringHash32 inItemId, int inCount)
            {
                return Save.Inventory.ItemCount(inItemId) >= inCount;
            }

            [LeafMember("CanAfford"), UnityEngine.Scripting.Preserve]
            static private bool CanAfford(StringHash32 inItemId)
            {
                var itemDesc = Assets.Item(inItemId);
                var invData = Save.Inventory;
                return invData.ItemCount(ItemIds.Cash) >= itemDesc.BuyCoinsValue() && invData.ItemCount(ItemIds.Gear) >= itemDesc.BuyGearsValue();
            }

            [LeafMember("PurchaseItem"), UnityEngine.Scripting.Preserve]
            static private IEnumerator PurchaseItem([BindContext] ScriptThread inThread, StringHash32 inItemId)
            {
                var itemDesc = Assets.Item(inItemId);
                var invData = Save.Inventory;
                invData.AdjustItem(ItemIds.Cash, -itemDesc.BuyCoinsValue());
                invData.AdjustItem(ItemIds.Gear, -itemDesc.BuyGearsValue());

                if (itemDesc.Category() == InvItemCategory.Upgrade) {
                    invData.AddUpgrade(inItemId);
                } else {
                    invData.AdjustItem(inItemId, 1);
                }
                
                inThread.Dialog = null;

                if (Services.UI.IsSkippingCutscene())
                    return null;
                
                Services.Audio.PostEvent("item.popup.new");
                return Services.UI.Popup.Display(
                    Loc.Format("ui.popup.newItem.header", itemDesc.NameTextId()),
                    Loc.Find(itemDesc.DescriptionTextId()),
                    new StreamedImageSet(itemDesc.SketchPath(), itemDesc.Icon())
                ).Wait();
            }

            [LeafMember("GiveItem"), UnityEngine.Scripting.Preserve]
            static private void GiveItem(StringHash32 inItemId, int inCount = 1)
            {
                Assert.True(inCount >= 0, "GiveItem must be passed a non-negative number");
                Save.Inventory.AdjustItem(inItemId, inCount);
            }

            [LeafMember("TakeItem"), UnityEngine.Scripting.Preserve]
            static private bool TakeItem(StringHash32 inItemId, int inCount = 1)
            {
                Assert.True(inCount >= 0, "TakeItem must be passed a non-negative number");
                return Save.Inventory.AdjustItem(inItemId, -inCount);
            }

            [LeafMember("SetItem"), UnityEngine.Scripting.Preserve]
            static private void SetItem(StringHash32 inItemId, int inCount)
            {
                Assert.True(inCount >= 0, "SetItem must be passed a non-negative number");
                Save.Inventory.SetItem(inItemId, inCount);
            }

            [LeafMember("HasUpgrade"), UnityEngine.Scripting.Preserve]
            static private bool HasUpgrade(StringHash32 inUpgradeId)
            {
                return Save.Inventory.HasUpgrade(inUpgradeId);
            }

            [LeafMember("GiveUpgrade"), UnityEngine.Scripting.Preserve]
            static private IEnumerator GiveUpgrade([BindContext] ScriptThread inThread, StringHash32 inUpgradeId, PopupMode inMode = PopupMode.Popup)
            {
                if (Save.Inventory.AddUpgrade(inUpgradeId) && inMode != PopupMode.Silent)
                {
                    inThread.Dialog = null;

                    if (Services.UI.IsSkippingCutscene())
                        return null;
                    
                    InvItem item = Assets.Item(inUpgradeId);
                    Services.Audio.PostEvent("item.popup.new");
                    return Services.UI.Popup.Display(
                        Loc.Format("ui.popup.newUpgrade.header", item.NameTextId()),
                        Loc.Find(item.DescriptionTextId()),
                        new StreamedImageSet(item.SketchPath(), item.Icon())
                    ).Wait();
                }

                return null;
            }

            [LeafMember("HasScanned"), UnityEngine.Scripting.Preserve]
            static private bool HasScanned(StringHash32 inNodeId)
            {
                return Save.Inventory.WasScanned(inNodeId);
            }

            [LeafMember("HasWaterProperty"), UnityEngine.Scripting.Preserve]
            static private bool HasProperty(WaterPropertyId inProperty)
            {
                return Save.Inventory.IsPropertyUnlocked(inProperty);
            }

            [LeafMember("GiveWaterProperty"), UnityEngine.Scripting.Preserve]
            static private bool GiveWaterProperty(WaterPropertyId inProperty)
            {
                return Save.Inventory.UnlockProperty(inProperty);
            }

            #endregion // Bestiary/Inventory

            #region Shop

            #endregion // Shop

            #region Jobs

            [LeafMember("JobStartedOrComplete"), UnityEngine.Scripting.Preserve]
            static private bool JobStartedOrComplete(StringHash32 inId)
            {
                return Save.Jobs.IsStartedOrComplete(inId);
            }

            [LeafMember("JobInProgress"), UnityEngine.Scripting.Preserve]
            static private bool JobInProgress(StringHash32 inId)
            {
                return Save.Jobs.IsInProgress(inId);
            }

            [LeafMember("JobCompleted"), UnityEngine.Scripting.Preserve]
            static private bool JobCompleted(StringHash32 inId)
            {
                return Save.Jobs.IsComplete(inId);
            }

            [LeafMember("JobAvailable"), UnityEngine.Scripting.Preserve]
            static private bool JobAvailable(StringHash32 inId)
            {
                return Services.Assets.Jobs.IsAvailableAndUnstarted(inId);
            }

            [LeafMember("JobTaskActive"), UnityEngine.Scripting.Preserve]
            static private bool JobTaskActive(StringHash32 inId)
            {
                return Save.Jobs.IsTaskActive(inId);
            }

            [LeafMember("JobTaskCompleted"), UnityEngine.Scripting.Preserve]
            static private bool JobTaskCompleted(StringHash32 inId)
            {
                return Save.Jobs.IsTaskComplete(inId);
            }

            [LeafMember("JobTaskTop"), UnityEngine.Scripting.Preserve]
            static public bool JobTaskTop(StringHash32 inId)
            {
                return Save.Jobs.IsTaskTop(inId);
            }

            [LeafMember("AnyJobsAvailable"), UnityEngine.Scripting.Preserve]
            static private int AnyJobsAvailable()
            {
                var unstarted = Services.Assets.Jobs.UnstartedJobs().GetEnumerator();
                int count = 0;
                
                while(unstarted.MoveNext())
                    ++count;

                return count;
            }

            [LeafMember("AnyJobsInProgress"), UnityEngine.Scripting.Preserve]
            static private int AnyJobsInProgress()
            {
                return Save.Jobs.InProgressJobs().Length;
            }

            [LeafMember("AnyJobsCompleted"), UnityEngine.Scripting.Preserve]
            static private int AnyJobsCompleted()
            {
                return Save.Jobs.CompletedJobIds().Count;
            }

            [LeafMember("UnlockJob"), UnityEngine.Scripting.Preserve]
            static private bool UnlockJob(StringHash32 inJobId)
            {
                return Save.Jobs.UnlockHiddenJob(inJobId);
            }

            [LeafMember("SetJob"), UnityEngine.Scripting.Preserve]
            static private bool SetJob(StringHash32 inJobId)
            {
                return Save.Jobs.SetCurrentJob(inJobId);
            }

            [LeafMember("CompleteJob"), UnityEngine.Scripting.Preserve]
            static private bool CompleteJob(StringHash32 inJobId = default(StringHash32))
            {
                if (inJobId.IsEmpty)
                {
                    inJobId = Save.Jobs.CurrentJobId;
                    if (inJobId.IsEmpty)
                    {
                        Log.Error("[ScriptingService] Attempting to complete job, but no job specified and no job active");
                        return false;
                    }
                }
                
                return Save.Jobs.MarkComplete(Save.Jobs.GetProgress(inJobId));
            }

            #endregion // Jobs

            #region World

            [LeafMember("StationUnlocked"), UnityEngine.Scripting.Preserve]
            static private bool StationUnlocked(StringHash32 inStationId)
            {
                return Save.Map.IsStationUnlocked(inStationId);
            }

            [LeafMember("UnlockStation"), UnityEngine.Scripting.Preserve]
            static private bool UnlockStation(StringHash32 inStationId)
            {
                return Save.Map.UnlockStation(inStationId);
            }

            [LeafMember("LockStation"), UnityEngine.Scripting.Preserve]
            static private bool LockStation(StringHash32 inStationId)
            {
                return Save.Map.LockStation(inStationId);
            }

            [LeafMember("SiteUnlocked"), UnityEngine.Scripting.Preserve]
            static private bool SiteUnlocked(StringHash32 inSiteId)
            {
                return Save.Map.IsSiteUnlocked(inSiteId);
            }

            [LeafMember("UnlockSite"), UnityEngine.Scripting.Preserve]
            static private bool UnlockSite(StringHash32 inSiteId)
            {
                return Save.Map.UnlockSite(inSiteId);
            }

            [LeafMember("LockSite"), UnityEngine.Scripting.Preserve]
            static private bool LockSite(StringHash32 inSiteId)
            {
                return Save.Map.LockSite(inSiteId);
            }

            [LeafMember("RoomUnlocked"), UnityEngine.Scripting.Preserve]
            static private bool RoomUnlocked(StringHash32 inRoomId)
            {
                return Save.Map.IsRoomUnlocked(inRoomId);
            }

            [LeafMember("UnlockRoom"), UnityEngine.Scripting.Preserve]
            static private bool UnlockRoom(StringHash32 inRoomId)
            {
                return Save.Map.UnlockRoom(inRoomId);
            }

            [LeafMember("LockRoom"), UnityEngine.Scripting.Preserve]
            static private bool LockRoom(StringHash32 inRoomId)
            {
                return Save.Map.LockRoom(inRoomId);
            }

            #endregion // World

            [LeafMember("Seen"), UnityEngine.Scripting.Preserve]
            static private bool Seen(StringHash32 inNodeId)
            {
                return Save.Script.HasSeen(inNodeId, PersistenceLevel.Profile);
            }

            [LeafMember("Random"), UnityEngine.Scripting.Preserve]
            static private bool GetRandomByRarity(StringSlice inData)
            {
                float floatVal;
                if (StringParser.TryParseFloat(inData, out floatVal))
                {
                    return RNG.Instance.Chance(floatVal);
                }

                StringHash32 id = inData.Hash32();
                if (inData == RandomRare)
                    return RNG.Instance.Chance(Services.Data.m_RareChance);
                if (inData == RandomUncommon)
                    return RNG.Instance.Chance(Services.Data.m_UncommonChance);
                if (inData == RandomCommon)
                    return RNG.Instance.Chance(Services.Data.m_CommonChance);
                
                Log.Error("[DataService] Unknown rarity '{0}'", inData);
                return false;
            }

            static private readonly StringHash32 RandomRare = "rare";
            static private readonly StringHash32 RandomUncommon = "uncommon";
            static private readonly StringHash32 RandomCommon = "common";
        }

        #endregion // Leaf
    }
}