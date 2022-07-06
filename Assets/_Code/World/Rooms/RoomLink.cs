using System;
using System.Collections;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.View
{
    [RequireComponent(typeof(ScriptInspectable))]
    public sealed class RoomLink : ScriptComponent {
        [MapId(MapCategory.ShipRoom)] public StringHash32 MapId;
        public ActiveGroup Group;
        public ActiveGroup Highlight;
        public bool AlwaysAvailable = false;

        public ScriptInspectable Inspectable {
            get { return this.CacheComponent(ref m_Inspectable); }
        }
        [NonSerialized] private ScriptInspectable m_Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable;
            inspect.Config.Action = ScriptInteractAction.GoToMap;
            inspect.Config.TargetId = MapId;
        }

        static public void UpdateStatus(RoomLink link, StringHash32 currentMapId, bool force) {
            bool isCurrent, isActive;
            isCurrent = link.MapId == currentMapId;
            isActive = link.AlwaysAvailable || Save.Map.IsRoomUnlocked(link.MapId);
            link.Group.SetActive(isActive, force);
            link.Highlight.SetActive(isCurrent, force);
            link.Inspectable.Locked = isCurrent || !isActive;
            if (link.Inspectable.Hint) {
                link.Inspectable.Hint.enabled = !isCurrent;
            }
        }
    }
}