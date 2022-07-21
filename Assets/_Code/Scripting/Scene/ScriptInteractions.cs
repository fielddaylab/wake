using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

namespace Aqua.Scripting {
    public delegate IEnumerator ScriptInteractCallback(ScriptInteractParams parameters, ScriptThreadHandle thread);
    public delegate void ScriptInteractSetupCallback(ref ScriptInteractParams parameters);

    public struct ScriptInteractParams {
        public ScriptInteractConfig Config;
        public RuntimeObjectHandle<ScriptComponent> Invoker;
        public RuntimeObjectHandle<ScriptComponent> Source;
        public bool Available;
    }

    [Serializable]
    public struct ScriptInteractConfig {
        public ScriptInteractSetupCallback PreTrigger;
        public ScriptInteractCallback OnLocked;
        public ScriptInteractCallback OnPerform;

        [AutoEnum] public ScriptInteractAction Action;
        public SerializedHash32 TargetId;
        public SerializedHash32 TargetEntranceId;
        [AutoEnum] public SceneLoadFlags LoadFlags;
    }

    public enum ScriptInteractAction {
        Inspect,
        GoToMap,
        GoToPreviousScene,
        GoToView,
        Talk
    }

    [LabeledEnum(false)]
    public enum ScriptInteractLockMode {
        [Order(0)]
        DisableObject,

        [Order(1)]
        DisableInteract,
        
        [Order(3)]
        AllowInteract,
        
        [Order(2)]
        DisableContextPopup
    }

}