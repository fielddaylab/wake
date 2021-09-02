using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Ship
{
    public class StationSceneLoader : MonoBehaviour, ISceneSubsceneSelector
    {
        public string KelpStationPath;
        public string CoralStationPath;
        public string ArcticStationPath;
        public string BayouStationPath;
        public string FinalStationPath;

        IEnumerable<string> ISceneSubsceneSelector.GetAdditionalScenesNames(SceneBinding inNew, object inContext)
        {
            StringHash32 currentStationId = Services.Data.Profile.Map.CurrentStationId();
            if (currentStationId == MapIds.KelpStation)
                yield return KelpStationPath;
            else if (currentStationId == MapIds.CoralStation)
                yield return CoralStationPath;
            else if (currentStationId == MapIds.ArcticStation)
                yield return ArcticStationPath;
            else if (currentStationId == MapIds.BayouStation)
                yield return BayouStationPath;
            else if (currentStationId == MapIds.FinalStation)
                yield return FinalStationPath;
        }
    }
}