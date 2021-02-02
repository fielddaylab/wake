using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Aqua.Profile;
using UnityEngine;
using BeauUtil.Services;
using Leaf;

namespace Aqua
{
    public partial class ProgressionWatcher : MonoBehaviour
    {
        [NonSerialized] private LeafAsset m_JobScript;
        [NonSerialized] private LeafAsset m_ActScript;

        private void Awake()
        {
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, OnJobStarted, this)
                .Register<StringHash32>(GameEvents.JobSwitched, OnJobSwitched, this)
                .Register<StringHash32>(GameEvents.JobCompleted, OnJobCompleted, this)
                .Register<uint>(GameEvents.ActChanged, OnActChanged, this)
                .Register(GameEvents.ProfileLoaded, InitScripts, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void InitScripts()
        {
            SetActScript(Services.Data.CurrentAct());
            SetJobScript(Services.Data.CurrentJobId());
        }

        private void OnActChanged(uint inAct)
        {
            SetActScript(inAct);
        }

        private void OnJobStarted(StringHash32 inJobId)
        {
            SetJobScript(inJobId);
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobStarted, null, null, table);
            }
        }

        private void OnJobSwitched(StringHash32 inJobId)
        {
            SetJobScript(inJobId);
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobSwitched, null, null, table);
            }
        }

        private void OnJobCompleted(StringHash32 inJobId)
        {
            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", inJobId);
                Services.Script.TriggerResponse(GameTriggers.JobCompleted, null, null, table);
            }
        }

        private void SetActScript(uint inAct)
        {
            LeafAsset script = Services.Assets.Acts.Act(inAct)?.Scripting();
            if (m_ActScript == script)
                return;

            if (m_ActScript)
                Services.Script.UnloadScript(m_ActScript);
            
            m_ActScript = script;
            
            if (m_ActScript)
                Services.Script.LoadScript(m_ActScript);
        }

        private void SetJobScript(StringHash32 inJob)
        {
            LeafAsset script = Services.Assets.Jobs.Get(inJob)?.Scripting();
            if (m_JobScript == script)
                return;

            if (m_JobScript)
                Services.Script.UnloadScript(m_JobScript);

            m_JobScript = script;
            
            if (m_JobScript)
                Services.Script.LoadScript(m_JobScript);
        }
    }
}