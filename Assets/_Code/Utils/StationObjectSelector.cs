using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Ship
{
    public class StationObjectSelector : MonoBehaviour, IScenePreloader, IBaked
    {
        public GameObject KelpStation;
        public GameObject CoralStation;
        public GameObject BayouStation;
        public GameObject ArcticStation;

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            StringHash32 currentStationId = Save.Map.CurrentStationId();
            if (currentStationId == MapIds.KelpStation)
                KelpStation.SetActive(true);
            else if (currentStationId == MapIds.CoralStation)
                CoralStation.SetActive(true);
            else if (currentStationId == MapIds.ArcticStation)
                BayouStation.SetActive(true);
            else if (currentStationId == MapIds.BayouStation)
                ArcticStation.SetActive(true);
            return null;
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            if (KelpStation)
                KelpStation.SetActive(false);
            if (CoralStation)
                CoralStation.SetActive(false);
            if (BayouStation)
                BayouStation.SetActive(false);
            if (ArcticStation)
                ArcticStation.SetActive(false);

            return true;
        }

        #endif // UNITY_EDITOR
    }
}