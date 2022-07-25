using System;
using System.Collections;
using Aqua.Scripting;
using BeauPools;
using BeauUtil;
using BeauUtil.Services;
using Leaf;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(AssetsService), typeof(EventService))]
    internal partial class ProgressionWatcher : ServiceBehaviour
    {
        [NonSerialized] private LeafAsset m_JobScript;
        [NonSerialized] private LeafAsset m_ActScript;

        [NonSerialized] private int m_LoadedActId = -1;
        [NonSerialized] private StringHash32 m_LoadedJobId;
        [NonSerialized] private bool m_JobLoading;

        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register<StringHash32>(GameEvents.JobStarted, OnJobStarted, this)
                .Register<StringHash32>(GameEvents.JobPreload, OnJobPreload, this)
                .Register<StringHash32>(GameEvents.JobSwitched, OnJobSwitched, this)
                .Register<StringHash32>(GameEvents.JobCompleted, OnJobCompleted, this)
                .Register<StringHash32>(GameEvents.JobTaskCompleted, OnJobTaskCompleted, this)
                .Register(GameEvents.JobTasksUpdated, OnJobTasksUpdated, this)
                .Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdated, this)
                .Register<uint>(GameEvents.ActChanged, OnActChanged, this)
                .Register(GameEvents.ProfileLoaded, InitScripts, this)
                .Register<StringHash32>(GameEvents.InventoryUpdated, OnItemUpdated, this)
                .Register<ScienceLevelUp>(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated, this);
        }

        protected override void Shutdown()
        {
            ClearState();
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }

        private void InitScripts()
        {
            ClearState();

            LoadAct(Save.ActIndex);
            LoadJob(Save.CurrentJobId);
        }

        private void ClearState()
        {
            if (m_JobScript)
                Services.Script?.UnloadScript(m_JobScript);
            m_JobScript = null;

            if (m_ActScript)
                Services.Script?.UnloadScript(m_ActScript);
            m_ActScript = null;

            m_LoadedJobId = StringHash32.Null;
            m_LoadedActId = -1;
        }

        #region Handlers

        private void OnActChanged(uint inAct)
        {
            LoadAct(inAct);
        }

        private void OnJobPreload(StringHash32 inJobId)
        {
            LoadJob(inJobId);
        }

        private void OnJobStarted(StringHash32 inJobId)
        {
            var table = TempVarTable.Alloc();
            table.Set("jobId", inJobId);
            Services.Script.QueueTriggerResponse(GameTriggers.JobStarted, 600, table);
        }

        private void OnJobSwitched(StringHash32 inJobId)
        {
            var table = TempVarTable.Alloc();
            table.Set("jobId", inJobId);
            Services.Script.QueueTriggerResponse(GameTriggers.JobSwitched, 500, table);
        }

        private void OnJobCompleted(StringHash32 inJobId)
        {
            var job = Assets.Job(inJobId);

            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", inJobId);
                var response = Services.Script.TriggerResponse(GameTriggers.JobCompleted, null, null, table, () => JobCompletedPopup(job));
                if (!response.IsRunning())
                {
                    JobCompletedPopup(job);
                }
            }
        }

        private void JobCompletedPopup(JobDesc inJob)
        {
            var character = Assets.Character(inJob.PosterId());

            // inventory adjustment
            Save.Inventory.AdjustItem(ItemIds.Cash, inJob.CashReward());
            Save.Inventory.AdjustItem(ItemIds.Exp, inJob.ExpReward());

            using(var psb = PooledStringBuilder.Create(Loc.Format("ui.popup.jobComplete.description")))
            {
                psb.Builder.Append('\n', 2);

                if (inJob.ExpReward() > 0)
                {
                    psb.Builder.Append(Loc.Format("ui.popup.jobComplete.expReward", inJob.ExpReward())).Append('\n');
                }

                if (inJob.CashReward() > 0)
                {
                    psb.Builder.Append(Loc.Format("ui.popup.jobComplete.cashReward", inJob.CashReward())).Append('\n');
                }

                psb.Builder.TrimEnd(StringUtils.DefaultNewLineChars);

                Services.Audio.PostEvent("job.completed");

                Services.UI.Popup.Display(
                    Loc.Format("ui.popup.jobComplete.header", inJob.NameId()),
                    psb.Builder.Flush(), character?.DefaultPortrait()
                );
            }
        }

        private void OnJobTaskCompleted(StringHash32 inTaskId)
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", m_LoadedJobId);
                table.Set("taskId", inTaskId);
                Services.Script.TriggerResponse(GameTriggers.JobTaskCompleted, table);
            }
        }

        private void OnJobTasksUpdated()
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", m_LoadedJobId);
                Services.Script.TriggerResponse(GameTriggers.JobTasksUpdated, table);
            }
        }

        private void OnBestiaryUpdated(BestiaryUpdateParams inUpdateParams)
        {
            switch(inUpdateParams.Type)
            {
                case BestiaryUpdateParams.UpdateType.Entity:
                    {
                        var table = TempVarTable.Alloc();
                        table.Set("entryId", inUpdateParams.Id);
                        Services.Script.QueueTriggerResponse(GameTriggers.BestiaryEntryAdded, 200, table);
                        break;
                    }

                case BestiaryUpdateParams.UpdateType.Fact:
                    {
                        var table = TempVarTable.Alloc();
                        table.Set("factId", inUpdateParams.Id);
                        Services.Script.QueueTriggerResponse(GameTriggers.BestiaryFactAdded, 200, table);
                        break;
                    }
            }
        }

        private void OnItemUpdated(StringHash32 inItemId)
        {
            var item = Assets.Item(inItemId);
            if (item.Category() == InvItemCategory.Upgrade)
            {
                using(var table = TempVarTable.Alloc())
                {
                    table.Set("upgradeId", inItemId);
                    Services.Script.TriggerResponse(GameTriggers.UpgradeAdded, table);
                }
            }

            if (inItemId == ItemIds.Exp)
            {
                // ScienceUtils.AttemptLevelUp(Save.Current, out var _);
                Services.Script.QueueTriggerResponse(GameTriggers.PlayerExpUp, -5);
            }

        }

        private void OnScienceLevelUpdated(ScienceLevelUp inLevelUp)
        {
            if (inLevelUp.LevelAdjustment <= 0)
                return;

            var scienceTweaks = Services.Tweaks.Get<ScienceTweaks>();
            int cashAdjust = scienceTweaks.CashPerLevel() * inLevelUp.LevelAdjustment;
            uint newLevel = inLevelUp.OriginalLevel + (uint) inLevelUp.LevelAdjustment;

            Services.Script.QueueInvoke(() => {
                Save.Inventory.AdjustItem(ItemIds.Cash, cashAdjust);
                Services.Audio.PostEvent("ShopPurchase");
                Services.UI.Popup.DisplayWithClose(
                    Loc.Format("ui.popup.levelUp.header", newLevel),
                    Loc.Format("ui.popup.levelUp.description", inLevelUp.OriginalLevel + 1, newLevel + 1, cashAdjust),
                null, PopupFlags.ShowCloseButton
                ).OnComplete((_) => {
                    Services.Script.TriggerResponse(GameTriggers.PlayerLevelUp);
                });
            });
        }

        #endregion // Handlers

        #region Loading

        private void LoadAct(uint inAct)
        {
            if (m_LoadedActId == inAct)
                return;
            
            m_LoadedActId = (int) inAct;
            
            if (m_ActScript)
                Services.Script.UnloadScript(m_ActScript);
            
            m_ActScript = Assets.Act(inAct)?.Scripting();
            
            if (m_ActScript)
                Services.Script.LoadScriptNow(m_ActScript);
        }

        private void LoadJob(StringHash32 inJob)
        {
            if (m_LoadedJobId == inJob)
                return;
            
            m_LoadedJobId = inJob;

            if (m_JobScript)
                Services.Script.UnloadScript(m_JobScript);

            JobDesc job = Assets.Job(inJob);

            m_JobScript = job?.Scripting();
            
            if (m_JobScript)
                Services.Script.LoadScriptNow(m_JobScript);
        }

        #endregion // Loading
    }
}
