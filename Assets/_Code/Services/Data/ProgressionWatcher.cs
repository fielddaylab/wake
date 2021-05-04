using System;
using BeauUtil;
using BeauUtil.Services;
using Leaf;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(AssetsService), typeof(EventService))]
    public partial class ProgressionWatcher : ServiceBehaviour
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
                .Register<StringHash32>(GameEvents.ModelUpdated, OnModelUpdated, this)
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
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobStarted, null, null, table);
            }
        }

        private void OnJobSwitched(StringHash32 inJobId)
        {
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobSwitched, null, null, table);
            }
        }

        private void OnJobCompleted(StringHash32 inJobId)
        {
            var job = Services.Assets.Jobs.Get(inJobId);

            // inventory adjustment
            Services.Data.Profile.Inventory.AdjustItem(GameConsts.CashId, job.CashReward());
            Services.Data.Profile.Inventory.AdjustItem(GameConsts.GearsId, job.GearReward());

            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobCompleted, null, null, table);
            }
        }

        private void OnJobTaskCompleted(StringHash32 inTaskId)
        {
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", m_LoadedJobId);
                table.Set("taskId", inTaskId);
                Services.Script.TriggerResponse(GameTriggers.JobTaskCompleted, null, null, table);
            }
        }

        private void OnJobTasksUpdated()
        {
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", m_LoadedJobId);
                Services.Script.TriggerResponse(GameTriggers.JobTasksUpdated, null, null, table);
            }
        }

        private void OnBestiaryUpdated(BestiaryUpdateParams inUpdateParams)
        {
            using(var table = Services.Script.GetTempTable())
            {
                switch(inUpdateParams.Type)
                {
                    case BestiaryUpdateParams.UpdateType.Entity:
                        {
                            table.Set("entryId", inUpdateParams.Id);
                            Services.Script.TriggerResponse(GameTriggers.BestiaryEntryAdded, null, null, table);
                            break;
                        }

                    case BestiaryUpdateParams.UpdateType.Fact:
                        {
                            table.Set("factId", inUpdateParams.Id);
                            Services.Script.TriggerResponse(GameTriggers.BestiaryFactAdded, null, null, table);
                            break;
                        }
                }
            }
        }

        private void OnModelUpdated(StringHash32 inFactAdded)
        {
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("factId", inFactAdded);
                Services.Script.TriggerResponse(GameTriggers.BestiaryFactAddedToModel, null, null, table);
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
            
            m_ActScript = Services.Assets.Acts.Act(inAct)?.Scripting();
            
            if (m_ActScript)
                Services.Script.LoadScript(m_ActScript);
        }

        private void LoadJob(StringHash32 inJob)
        {
            if (m_LoadedJobId == inJob)
                return;
            
            m_LoadedJobId = inJob;

            if (m_JobScript)
                Services.Script.UnloadScript(m_JobScript);

            JobDesc job = Services.Assets.Jobs.Get(inJob);

            m_JobScript = job?.Scripting();
            
            if (m_JobScript)
                Services.Script.LoadScript(m_JobScript);
        }

        #endregion // Loading
    }
}