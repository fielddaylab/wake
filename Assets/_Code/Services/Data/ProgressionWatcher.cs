using System;
using System.Collections;
using Aqua.Scripting;
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
                .Register(GameEvents.ProfileLoaded, InitScripts, this);
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

            LoadAct(Services.Data.CurrentAct());
            LoadJob(Services.Data.CurrentJobId());
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
            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobStarted, table);
            }
        }

        private void OnJobSwitched(StringHash32 inJobId)
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobSwitched, table);
            }
        }

        private void OnJobCompleted(StringHash32 inJobId)
        {
            var job = Assets.Job(inJobId);

            // inventory adjustment
            Save.Inventory.AdjustItem(ItemIds.Cash, job.CashReward());
            Save.Inventory.AdjustItem(ItemIds.Gear, job.GearReward());

            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobCompleted, table);
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
            using(var table = TempVarTable.Alloc())
            {
                switch(inUpdateParams.Type)
                {
                    case BestiaryUpdateParams.UpdateType.Entity:
                        {
                            table.Set("entryId", inUpdateParams.Id);
                            Services.Script.TriggerResponse(GameTriggers.BestiaryEntryAdded, table);
                            break;
                        }

                    case BestiaryUpdateParams.UpdateType.Fact:
                        {
                            table.Set("factId", inUpdateParams.Id);
                            Services.Script.TriggerResponse(GameTriggers.BestiaryFactAdded, table);
                            break;
                        }
                }
            }
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
