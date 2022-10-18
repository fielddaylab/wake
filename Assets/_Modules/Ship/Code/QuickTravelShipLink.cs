using System;
using System.Collections;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Ship
{
    [RequireComponent(typeof(ScriptInspectable))]
    public sealed class QuickTravelShipLink : ScriptComponent
    {
        public ScriptInspectable Inspectable {
            get { return this.CacheComponent(ref m_Inspectable); }
        }
        [NonSerialized] private ScriptInspectable m_Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable;
            inspect.Config.Action = ScriptInteractAction.GoToMap;
            inspect.Config.PreTrigger = (ref ScriptInteractParams p) => {
                MapDesc travelDest = Assets.Map(Save.Map.CurrentStationId()).QuickTravel();
                p.Config.TargetId = travelDest.Id();
                p.Config.TargetEntranceId = null;
            };
        }

        private void OnEnable() {
            if (Script.IsLoading) {
                return;
            }

            MapDesc quickTravel = Assets.Map(Save.Map.CurrentStationId()).QuickTravel();
            if (!quickTravel || !Save.Map.HasVisitedLocation(quickTravel.Id())) {
                this.gameObject.SetActive(false);
            }
        }
    }
}