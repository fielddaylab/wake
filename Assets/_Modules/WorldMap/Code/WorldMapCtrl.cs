using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class WorldMapCtrl : MonoBehaviour, IScenePreloader, IBaked
    {
        static public readonly StringHash32 Event_RequestChangeStation = "worldmap:request-change-station"; // StringHash32 stationId
        static public readonly StringHash32 Event_StationChanging = "worldmap:station-changing"; // StringHash32 stationId

        [SerializeField] private CanvasGroup m_SceneExitButtonGroup = null;
        [SerializeField] private ShipOutPopup m_ShipOutPopup = null;
        [SerializeField, HideInInspector] private StationButton[] m_AllStations;

        [NonSerialized] private StringHash32 m_CurrentStation;
        [NonSerialized] private StringHash32 m_TargetStation;
        [NonSerialized] private StationButton m_TargetButton;

        private Routine m_ExitButtonRoutine;
        private Routine m_ShipOutRoutine;

        private void Awake()
        {
            Services.Events.Register<StationButton>(Event_RequestChangeStation, OnRequestChangeStation, this);

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

        private void OnShipOutClicked()
        {
            m_ShipOutRoutine.Replace(ShipoutSequence(m_TargetStation)).Tick();
        }

        static private IEnumerator ShipoutSequence(StringHash32 stationId)
        {
            Services.UI.ShowLetterbox();

            // TODO: SOmething fancy??

            yield return 0.2f;

            StateUtil.LoadPreviousSceneWithWipe(null, null, SceneLoadFlags.Default | SceneLoadFlags.SuppressTriggers);
            yield return 0.3;

            while(StateUtil.IsLoading) {
                yield return null;
            }

            yield return 1;

            Services.Camera.AddShake(new Vector2(0, 0.2f), new Vector2(0.1f, 0.1f), 0.8f);
            yield return 1;

            Save.Map.SetCurrentStationId(stationId);
            StateUtil.LoadSceneWithWipe("Helm");
            yield return 0.3f;

            Services.UI.HideLetterbox();
        }

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

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            List<StationButton> stations = new List<StationButton>();
            SceneHelper.ActiveScene().Scene.GetAllComponents<StationButton>(stations);
            m_AllStations = stations.ToArray();
            return true;
        }

        #endif // UNITY_EDITOR
    
        #endregion // IScene
    }
}