using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using BeauRoutine;
using BeauUtil;
using Aqua;
using System;

namespace ProtoAqua.Map {

    public class StationManager : SharedManager, ISceneLoadHandler
    {
        [SerializeField] private Transform m_ShipTransform = null;
        
        [NonSerialized] private NavigationStation[] m_Stations;

        public void SetStation(string inStationId, Transform inTarget)
        {
            if (Services.Data.Profile.Map.SetCurrentStationId(inStationId))
            {
                Routine.Start(this, MoveToStation(inTarget));
            }
        }

        private IEnumerator MoveToStation(Transform inTarget)
        {
            Services.UI.ShowLetterbox();
            Vector3 currentPos = m_ShipTransform.position;
            Vector3 targetPos = inTarget.position;
            float scaleX = Mathf.Abs(m_ShipTransform.localScale.x) * (currentPos.x < targetPos.x ? 1 : -1);
            m_ShipTransform.SetScale(scaleX, Axis.X);

            yield return m_ShipTransform.MoveToWithSpeed(targetPos, 6, Axis.XY).Ease(Curve.Smooth);
            yield return 0.2f;
            StateUtil.LoadPreviousSceneWithWipe();
            yield return 0.3;
            Services.UI.HideLetterbox();
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            List<NavigationStation> allStations = new List<NavigationStation>(4);
            inScene.Scene.GetAllComponents(true, allStations);
            m_Stations = allStations.ToArray();

            StringHash32 currentStation = Services.Data.Profile.Map.CurrentStationId();
            foreach(var station in m_Stations)
            {
                if (station.Id() == currentStation)
                {
                    m_ShipTransform.SetPosition(station.Mount().position, Axis.XY);
                    break;
                }
            }
        }
    }
}