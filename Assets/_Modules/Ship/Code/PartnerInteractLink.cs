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
        static public readonly TableKeyPair RequestCounter = TableKeyPair.Parse("guide:help.requests");

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
            Services.Data.AddVariable(RequestCounter, 1);
            Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, GameConsts.Target_V1ctor);
        }
    }
}