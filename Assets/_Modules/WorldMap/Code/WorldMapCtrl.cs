using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class WorldMapCtrl : MonoBehaviour, ISceneLoadHandler, ISceneOptimizable
    {
        static public readonly StringHash32 Event_RequestChangeStation = "worldmap:request-change-station"; // StringHash32 stationId

        [SerializeField] private Transform m_PlayerTransform = null;
        [SerializeField] private Button m_ShipOutButton = null;
        [SerializeField, HideInInspector] private StationButton[] m_AllStations;

        [NonSerialized] private StringHash32 m_CurrentStation;
        [NonSerialized] private StringHash32 m_TargetStation;

        private void Awake()
        {
            Services.Events.Register<StringHash32>(Event_RequestChangeStation, OnRequestChangeStation, this);

            m_ShipOutButton.onClick.AddListener(OnShipOutClicked);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnRequestChangeStation(StringHash32 inRequestChangeStation)
        {
            if (m_TargetStation == inRequestChangeStation)
                return;

            m_TargetStation = inRequestChangeStation;

            StationButton button;
            m_AllStations.TryGetValue(inRequestChangeStation, out button);
            m_PlayerTransform.SetPosition(button.transform.position, Axis.XY, Space.Self);

            m_ShipOutButton.gameObject.SetActive(m_TargetStation != m_CurrentStation);
        }

        private void OnShipOutClicked()
        {
            Routine.Start(this, ShipoutSequence()).TryManuallyUpdate(0);
        }

        private IEnumerator ShipoutSequence()
        {
            Services.UI.ShowLetterbox();

            // TODO: SOmething fancy??

            Services.Data.Profile.Map.SetCurrentStationId(m_TargetStation);

            yield return 0.2f;

            StateUtil.LoadPreviousSceneWithWipe();
            yield return 0.3;

            Services.UI.HideLetterbox();
        }

        #region IScene

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            MapDB mapDB = Services.Assets.Map;
            MapData profileData = Services.Data.Profile.Map;

            m_CurrentStation = profileData.CurrentStationId();
            m_TargetStation = m_CurrentStation;

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

                    if (m_CurrentStation == id)
                    {
                        m_PlayerTransform.SetPosition(station.transform.position, Axis.XY, Space.Self);
                        station.Show(desc, true);
                    }
                    else
                    {
                        station.Show(desc, false);
                    }
                }
            }

            m_ShipOutButton.gameObject.SetActive(false);
        }

        void ISceneOptimizable.Optimize()
        {
            List<StationButton> stations = new List<StationButton>();
            SceneHelper.ActiveScene().Scene.GetAllComponents<StationButton>(stations);
            m_AllStations = stations.ToArray();
        }
    
        #endregion // IScene
    }
}