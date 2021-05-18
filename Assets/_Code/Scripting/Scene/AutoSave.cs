using BeauUtil;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    [RequireComponent(typeof(ScriptObject))]
    public class AutoSave : ScriptComponent
    {
        private const float AutosaveDelay = 10f;

        private void Awake()
        {
            Services.Events.Register(GameEvents.SceneLoaded, OnAutosaveEvent, this)
                .Register(GameEvents.SceneWillUnload, OnAutosaveEvent, this)
                .Register(GameEvents.ProfileAutosaveHint, OnAutosaveEvent, this)
                .Register(GameEvents.BestiaryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.InventoryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.ModelUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.JobSwitched, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.JobTaskCompleted, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.CutsceneEnd, OnAutosaveDelayedEvent, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
            m_DelayedAutosave.Stop();
            m_InstantAutosave.Stop();
        }

        private Routine m_InstantAutosave;
        private Routine m_DelayedAutosave;
        private float m_DelayTimestamp;

        #region Handlers

        [LeafMember("Hint")]
        private void LeafHint()
        {
            OnAutosaveEvent();
        }

        private void OnAutosaveEvent()
        {
            if (!CanSave())
                return;
            
            m_DelayedAutosave.Stop();
            if (!m_InstantAutosave)
                m_InstantAutosave = Routine.Start(this, NearInstantSave());
        }

        private void OnAutosaveDelayedEvent()
        {
            if (!CanSave())
                return;
            
            m_InstantAutosave.Stop();
            m_DelayTimestamp = Time.unscaledTime;
            if (!m_DelayedAutosave)
                m_DelayedAutosave = Routine.Start(this, DelayedSave());
        }

        private bool CanSave()
        {
            return Services.Valid && Services.Data.AutosaveEnabled() && Services.Data.Profile.HasChanges();
        }

        #endregion // Handlers

        #region Saving

        private IEnumerator NearInstantSave()
        {
            yield return null;
            Services.Data.SaveProfile(true);
        }

        private IEnumerator DelayedSave()
        {
            while(Services.Script.IsCutscene() || Time.unscaledTime < m_DelayTimestamp + AutosaveDelay)
            {
                if (Services.Data.IsSaving())
                    yield break;
                
                yield return null;
            }

            Services.Data.SaveProfile();
        }

        #endregion // Saving

        static public void Hint()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveHint);
        }
    }
}