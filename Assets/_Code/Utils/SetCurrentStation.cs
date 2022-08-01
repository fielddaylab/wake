using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class SetCurrentStation : MonoBehaviour
    {
        [MapId(MapCategory.Station)] public StringHash32 StationId;

        private void Awake() {
            if (!StationId.IsEmpty) {
                Script.OnSceneLoad(() => {
                    Save.Map.SetCurrentStationId(StationId);
                });
            }
        }
    }
}