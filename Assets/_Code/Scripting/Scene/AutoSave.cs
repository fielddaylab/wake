using BeauUtil;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using Leaf.Runtime;
using Aqua.Debugging;
using System;
using UnityEngine.Scripting;

namespace Aqua.Scripting
{
    [RequireComponent(typeof(ScriptObject))]
    public class AutoSave : ScriptComponent
    {
        private enum Mode
        {
            Delayed,
            Now
        }

        private const float AutosaveDelay = 5f;

        private void Awake()
        {
            Services.Events.Register(GameEvents.SceneLoaded, OnSceneLoaded, this)
                .Register(GameEvents.SceneWillUnload, OnAutosaveEvent, this)
                .Register<Mode>(GameEvents.ProfileAutosaveHint, OnHint, this)
                .Register(GameEvents.ProfileAutosaveSuppress, OnSuppress, this)
                .Register(GameEvents.BestiaryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.InventoryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.ModelUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.JobSwitched, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.JobTaskCompleted, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.CutsceneEnd, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.TimeDayChanged, OnAutoSaveTimeEvent, this)
                .Register(GameEvents.TimeDayNightChanged, OnAutoSaveTimeEvent, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
            m_DelayedAutosave.Stop();
            m_InstantAutosave.Stop();
        }

        private Routine m_InstantAutosave;
        private Routine m_DelayedAutosave;
        [NonSerialized] private float m_DelayTimestamp;
        [NonSerialized] private bool m_Suppressed;

        #region Handlers

        [LeafMember("Hint"), Preserve]
        private void LeafHint()
        {
            OnAutosaveEvent();
        }

        private void OnSceneLoaded()
        {
            if (!Services.Valid || !Services.Data.IsProfileLoaded())
                return;

            Services.Data.Profile.Map.FullSync();
            OnAutosaveEventPreserveEntrance();
        }

        private void OnHint(Mode inMode)
        {
            if (inMode == Mode.Now)
                OnAutosaveEvent();
            else
                OnAutosaveDelayedEvent();
        }

        private void OnSuppress()
        {
            if (!m_Suppressed)
            {
                m_Suppressed = true;
                DebugService.Log(LogMask.DataService, "[AutoSave] Suppressing autosave");
            }

            m_InstantAutosave.Stop();
            m_DelayedAutosave.Stop();
        }

        private void OnAutosaveEvent()
        {
            if (!CanSave())
                return;
            
            m_DelayedAutosave.Stop();
            if (!m_InstantAutosave)
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Queueing instant save");
                m_InstantAutosave = Routine.Start(this, NearInstantSave(null));
            }
        }

        private void OnAutosaveEventPreserveEntrance()
        {
            if (!CanSave())
                return;
            
            m_DelayedAutosave.Stop();
            if (!m_InstantAutosave)
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Queueing instant save");
                m_InstantAutosave = Routine.Start(this, NearInstantSave(Services.State.LastEntranceId));
            }
        }

        private void OnAutosaveDelayedEvent()
        {
            if (!CanSave())
                return;
            
            m_InstantAutosave.Stop();
            m_DelayTimestamp = Time.unscaledTime;
            if (!m_DelayedAutosave)
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Queueing delayed save");
                m_DelayedAutosave = Routine.Start(this, DelayedSave(null));
            }
            else
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Delayed save re-delayed due to more changes");
            }
        }

        private void OnAutoSaveTimeEvent()
        {
            Services.Data.Profile.Map.SyncTime();
            OnAutosaveEvent();
        }

        private bool CanSave()
        {
            return !m_Suppressed && Services.Valid && Services.Data.AutosaveEnabled() && Services.Data.NeedsSave();
        }

        #endregion // Handlers

        #region Saving

        private IEnumerator NearInstantSave(StringHash32 inLocation)
        {
            yield return null;
            Services.Data.SaveProfile(inLocation, true);
        }

        private IEnumerator DelayedSave(StringHash32 inLocation)
        {
            while(Services.Script.IsCutscene() || Time.unscaledTime < m_DelayTimestamp + AutosaveDelay)
            {
                if (Services.Data.IsSaving())
                    yield break;
                
                yield return null;
            }

            Services.Data.SaveProfile(inLocation);
        }

        #endregion // Saving

        static public void Force()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveHint, Mode.Now);
        }

        static public void Hint()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveHint, Mode.Delayed);
        }

        static public void Suppress()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveSuppress);
        }
    }
}