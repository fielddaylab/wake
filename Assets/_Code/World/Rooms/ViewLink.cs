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
    public sealed class ViewLink : ScriptComponent {
        public ViewNode Node;
        public TweenSettings Transition = new TweenSettings(0.3f, Curve.Smooth);
        public Transform TransitionControlPoint;
        public ActiveGroup Group;
        public bool AlwaysAvailable = false;

        [NonSerialized] public ScriptInspectable Inspectable;

        public override void OnRegister(ScriptObject parent) {
            base.OnRegister(parent);

            var inspect = Inspectable = GetComponent<ScriptInspectable>();
            inspect.Config.Action = ScriptInteractAction.GoToView;
        }
    }
}