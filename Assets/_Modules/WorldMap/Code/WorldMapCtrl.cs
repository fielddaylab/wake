using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Debugging;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class WorldMapCtrl : MonoBehaviour, IScenePreloader, IBaked
    {
        static public readonly StringHash32 Event_RequestChangeStation = "worldmap:request-change-station"; // StringHash32 stationId
        static public readonly StringHash32 Event_StationChanging = "worldmap:station-changing"; // StringHash32 stationId

        static public readonly TableKeyPair Var_BetweenDreamTravelCount = TableKeyPair.Parse("world:betweenDreamTravelCount");
        static public readonly TableKeyPair Var_SamConvoActivated = TableKeyPair.Parse("world:final.samConvo.activated");

        static public readonly StringHash32 Trigger_WorldMapLeaving = "WorldMapLeave";

        [SerializeField] private CanvasGroup m_SceneExitButtonGroup = null;
        [SerializeField] private ShipOutPopup m_ShipOutPopup = null;
        [SerializeField, HideInInspector] private StationButton[] m_AllStations;

        [NonSerialized] private StringHash32 m_CurrentStation;
        [NonSerialized] private StringHash32 m_TargetStation;
        [NonSerialized] private StationButton m_TargetButton;

        private Routine m_ExitButtonRoutine;
        private Routine m_ShipOutRoutine;

        static private bool s_DreamLoadFlag;

        private void Awake()
        {
            Services.Events.Register<StationButton>(Event_RequestChangeStation, OnRequestChangeStation, this)
                .Register(GameEvents.MapsUpdated, RefreshMapData, this)
                .Register(GameEvents.StationChanged, RefreshMapData, this);

            m_ShipOutPopup.OnShowEvent.AddListener((_) => {
                m_ExitButtonRoutine.Replace(this, m_SceneExitButtonGroup.Hide(0.2f));
            });
            m_ShipOutPopup.OnHideCompleteEvent.AddListener((_) => {
                if (m_ShipOutRoutine) {
                    return;
                }

                m_SceneExitButtonGroup.alpha = 0;
                m_ExitButtonRoutine.Replace(this, m_SceneExitButtonGroup.Show(0.2f));
                if (m_TargetButton != null) {
                    m_TargetButton.CancelSelected();
                    m_TargetButton = null;
                }
            });

            m_ShipOutPopup.OnShipOutClicked += OnShipOutClicked;
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnRequestChangeStation(StationButton inButton)
        {
            m_TargetButton = inButton;
            m_TargetStation = inButton.StationId();

            m_ShipOutPopup.Populate(Assets.Map(m_TargetStation), JobUtils.SummarizeJobProgress(m_TargetStation, Save.Current));
            m_ShipOutPopup.Show();

            // m_ShipOutButton.gameObject.SetActive(m_TargetStation != m_CurrentStation);
        }

        private void RefreshMapData() {
            if (m_ShipOutRoutine) {
                return;
            }

            MapDB mapDB = Services.Assets.Map;
            MapData profileData = Save.Map;

            m_CurrentStation = profileData.CurrentStationId();
            m_TargetStation = m_CurrentStation;
            StringHash32 jobStation = Save.CurrentJob.Job?.StationId() ?? StringHash32.Null;

            foreach(var station in m_AllStations)
            {
                StringHash32 id = station.StationId();
                if (!profileData.IsStationUnlocked(id))
                {
                    station.Hide();
                }
                else
                {
                    MapDesc desc = mapDB.Get(id);

                    bool seen = profileData.HasVisitedLocation(id);
                    
                    if (m_CurrentStation == id)
                    {
                        station.Show(desc, true, true, jobStation == id);
                    }
                    else
                    {
                        station.Show(desc, false, seen, jobStation == id);
                    }
                }
            }
        }

        private void OnShipOutClicked()
        {
            m_ShipOutRoutine.Replace(ShipoutSequence(m_TargetStation)).Tick();
            m_ShipOutPopup.Hide();
        }

        static private IEnumerator ShipoutSequence(StringHash32 stationId)
        {
            Services.UI.ShowLetterbox();
            Script.WriteVariable("session:fromDream", null);

            yield return 0.2f;

            // todo: play sound effects

            StringHash32 oldStationId = Save.Map.CurrentStationId();
            Save.Map.SetCurrentStationId(stationId);

            using(var table = TempVarTable.Alloc()) {
                table.Set("previousStation", oldStationId);
                table.Set("nextStation", stationId);

                var response = Services.Script.TriggerResponse(Trigger_WorldMapLeaving, table);
                if (response.IsRunning()) {
                    while(Script.ShouldBlockIgnoreLetterbox()) {
                        yield return null;
                    }

                    yield return 1;
                }
            }

            StateUtil.LoadSceneWithFader("StationTransition", null, null, SceneLoadFlags.StopMusic | SceneLoadFlags.SuppressTriggers | SceneLoadFlags.SuppressAutoSave, 0.5f);
            yield return 0.6f;

            Services.UI.HideLetterbox();

            while(StateUtil.IsLoading) {
                yield return null;
            }

            yield return 5f;

            using(var table = TempVarTable.Alloc()) {
                table.Set("previousStation", oldStationId);
                table.Set("nextStation", stationId);

                Services.Script.TriggerResponse(GameTriggers.TravelingToStation, table);
            }

            while(Script.ShouldBlock() || !Services.Assets.PreloadGroupIsPrimaryLoaded(stationId)) {
                yield return 0.5f;
            }

            s_DreamLoadFlag = false;
            int betweenDreamsTravelCount = Script.ReadVariable(Var_BetweenDreamTravelCount, 0).AsInt();
            Script.WriteVariable("world:betweenDreamTravelCount", betweenDreamsTravelCount + 1);

            // TODO: dream implementation
            using (var table = TempVarTable.Alloc()) {
                table.Set("previousStation", oldStationId);
                table.Set("nextStation", stationId);

                var dreamTrigger = Services.Script.TriggerResponse(GameTriggers.PlayerDream, table);
                if (s_DreamLoadFlag || dreamTrigger.IsRunning()) {
                    yield break;
                }
            }

            using(var fader = Services.UI.WorldFaders.AllocFader()) {
                Services.Audio.FadeOut(1);
                yield return fader.Object.Show(Color.black, 1);
                yield return Services.State.LoadScene("Cabin");
                Services.Audio.FadeIn(1);
                yield return fader.Object.Hide(1, false);
            }
        }

        #region Dream

        [LeafMember("PrepareDream"), Preserve]
        static private IEnumerator LeafPrepareDream(string mapName) {
            s_DreamLoadFlag = true;
            Script.WriteVariable("world:betweenDreamTravelCount", 0); // player has seen dream, so reset tally

            StringHash32 preloadGroup = "Scene/" + mapName;
            bool preloaded = false;
            if (Services.Assets.PreloadGroup(preloadGroup)) {
                preloaded = true;
                while(!Services.Assets.PreloadGroupIsPrimaryLoaded(preloadGroup)) {
                    yield return 1f;
                }
            }
            Routine.Start(LeafLoadDream(mapName, preloaded ? preloadGroup : null));
        }

        static private IEnumerator LeafLoadDream(string mapName, StringHash32 preloadName) {
            using(var fader = Services.UI.WorldFaders.AllocFader()) {
                Services.Events.Dispatch(GameEvents.HotbarHide);
                Services.Audio.FadeOut(1);
                yield return fader.Object.Show(Color.black, 1);
                yield return 1f;
                yield return Services.State.LoadScene(mapName);
                if (!preloadName.IsEmpty) {
                    Script.OnSceneLoad(() => {
                        Services.Assets.CancelPreload(preloadName);
                        Services.Events.Dispatch(GameEvents.HotbarShow);
                    });
                }
                Services.Audio.FadeIn(1);
                yield return fader.Object.Hide(1, false);
            }
        }

        [LeafMember("CurrBetweenDreamTravels"), Preserve]
        static private int LeafCurrBetweenDreamTravels() {
            return Script.ReadVariable(Var_BetweenDreamTravelCount, 0).AsInt();
        }


        #endregion // Dream

        #region Sam

        [LeafMember("ActivateSamConvo"), Preserve]
        static private void LeafActivateSamConvo() {
            Script.WriteVariable("world:final.samConvo.activated", true);
        }

        [LeafMember("IsSamConvoActivated"), Preserve]
        static private bool LeafIsSamConvoActivated() {
            return Script.ReadVariable(Var_SamConvoActivated, false).AsBool();
        }

        #endregion // Sam

        #region IScene

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext)
        {
            MapDB mapDB = Services.Assets.Map;
            MapData profileData = Save.Map;

            m_CurrentStation = profileData.CurrentStationId();
            m_TargetStation = m_CurrentStation;
            StringHash32 jobStation = Save.CurrentJob.Job?.StationId() ?? StringHash32.Null;

            foreach(var station in m_AllStations)
            {
                StringHash32 id = station.StationId();
                if (!profileData.IsStationUnlocked(id))
                {
                    station.Hide();
                }
                else
                {
                    MapDesc desc = mapDB.Get(id);

                    bool seen = profileData.HasVisitedLocation(id);
                    
                    if (m_CurrentStation == id)
                    {
                        station.Show(desc, true, true, jobStation == id);
                    }
                    else
                    {
                        station.Show(desc, false, seen, jobStation == id);
                    }

                    yield return null;
                }
            }

            // m_ShipOutButton.gameObject.SetActive(false);
        }
    
        #endregion // IScene
    
        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            List<StationButton> stations = new List<StationButton>();
            SceneHelper.ActiveScene().Scene.GetAllComponents<StationButton>(stations);
            m_AllStations = stations.ToArray();
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}