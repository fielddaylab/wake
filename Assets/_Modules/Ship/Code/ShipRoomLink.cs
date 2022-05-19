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
    public sealed class ShipRoomLink : ScriptComponent {
        [MapId(MapCategory.ShipRoom)] public StringHash32 MapId;
        public ActiveGroup Group;
        public ActiveGroup Highlight;
        public bool AlwaysAvailable = false;

        [NonSerialized] public ScriptInspectable Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable = GetComponent<ScriptInspectable>();
            inspect.Config.Action = ScriptInteractAction.GoToMap;
            inspect.Config.TargetId = MapId;
        }
    }
}