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
    public sealed class StationMapLink : ScriptComponent {
        public ScriptInspectable Inspectable {
            get { return this.CacheComponent(ref m_Inspectable); }
        }
        [NonSerialized] private ScriptInspectable m_Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable;
            inspect.Config.Action = ScriptInteractAction.GoToMap;
            inspect.Config.PreTrigger = (ref ScriptInteractParams p) => {
                p.Config.TargetId = Save.Map.CurrentStationId();
            };
        }
    }
}