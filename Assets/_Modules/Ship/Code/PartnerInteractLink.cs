using System;
using System.Collections;
using Aqua.Cameras;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using ScriptableBake;
using UnityEngine;
using static Aqua.Character.SceneInteractable;

namespace Aqua.Ship
{
    [RequireComponent(typeof(ScriptInspectable))]
    public sealed class PartnerInteractLink : ScriptComponent
    {
        public ScriptInspectable Inspectable {
            get { return this.CacheComponent(ref m_Inspectable); }
        }
        [NonSerialized] private ScriptInspectable m_Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable;
            inspect.Config.Action = ScriptInteractAction.Inspect;
            inspect.Config.OnPerform = (p, t) => {
                // trigger partner interaction
                Interact();
                return null;
            };
        }

        private void Interact() {
            Services.Data.AddVariable(PartnerButton.Var_RequestCounter, 1);
            Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, GameConsts.Target_V1ctor);
        }

        private void OnEnable() {
            Script.OnSceneLoad(() => Services.Events.Queue(PartnerButton.Event_WorldAvatarPresent));
        }

        private void OnDisable() {
            Services.Events?.Queue(PartnerButton.Event_WorldAvatarHidden);
        }
    }
}