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
    [AddComponentMenu("Aqualab/Scripting/Auto Save")]
    [RequireComponent(typeof(ScriptObject))]
    public class AutoSave : ScriptComponent
    {
        private enum Mode
        {
            Delayed,
            Now
        }

        private const float AutosaveDelay = 15f;

        private void Awake()
        {
            Services.Events.Register(GameEvents.SceneLoaded, OnSceneLoaded, this)
                .Register<Mode>(GameEvents.ProfileAutosaveHint, OnHint, this)
                .Register(GameEvents.ProfileAutosaveSuppress, OnSuppress, this)
                .Register<StringHash32>(GameEvents.ProfileSpawnLocationUpdate, OnSpawnLocationUpdate, this)
                .Register(GameEvents.BestiaryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.InventoryUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.SiteDataUpdated, OnAutosaveDelayedEvent, this)
                .Register(GameEvents.ArgueDataUpdated, OnAutosaveDelayedEvent, this)
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
        [NonSerialized] private bool m_SceneStarted;
        [NonSerialized] private float m_DelayTimestamp;
        [NonSerialized] private bool m_Suppressed;
        [NonSerialized] private StringHash32 m_SaveLocation;

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
            
            m_SceneStarted = true;
            if (m_SaveLocation.IsEmpty) {
                m_SaveLocation = Services.State.LastEntranceId;
            }

            Save.Map.FullSync();
            m_InstantAutosave.Stop();
            m_DelayedAutosave.Stop();
            if (CanSave())
            {
                Services.Data.SaveProfile(m_SaveLocation, true);
            }
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

        private void OnSpawnLocationUpdate(StringHash32 locationId)
        {
            if (m_SaveLocation != locationId) {
                m_SaveLocation = locationId;
                if (m_SceneStarted) {
                    OnAutosaveEvent();
                }
            }
        }

        private void OnAutosaveEvent()
        {
            if (!CanSave())
                return;
            
            m_DelayedAutosave.Stop();
            if (!m_InstantAutosave)
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Queueing instant save");
                m_InstantAutosave = Routine.Start(this, NearInstantSave(m_SaveLocation));
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
                m_InstantAutosave = Routine.Start(this, NearInstantSave(m_SaveLocation));
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
                m_DelayedAutosave = Routine.Start(this, DelayedSave(m_SaveLocation));
            }
            else
            {
                DebugService.Log(LogMask.DataService, "[AutoSave] Delayed save re-delayed due to more changes");
            }
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
            while(Script.ShouldBlock() || Time.unscaledTime < m_DelayTimestamp + AutosaveDelay)
            {
                if (Services.Data.IsSaving())
                    yield break;
                
                yield return null;
            }

            Services.Data.SaveProfile(inLocation);
        }

        #endregion // Saving

        [LeafMember("AutoSaveNow"), Preserve]
        static public void Force()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveHint, Mode.Now);
        }

        [LeafMember("AutoSaveHint"), Preserve]
        static public void Hint()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveHint, Mode.Delayed);
        }

        [LeafMember("AutoSaveSuppress"), Preserve]
        static public void Suppress()
        {
            Services.Events.Dispatch(GameEvents.ProfileAutosaveSuppress);
        }

        [LeafMember("AutoSaveSetSpawn"), Preserve]
        static public void SetSpawnLocation(StringHash32 spawnLocation)
        {
            Services.Events.Dispatch(GameEvents.ProfileSpawnLocationUpdate, spawnLocation);
        }
    }
}