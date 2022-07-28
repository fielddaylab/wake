using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine.EventSystems;

namespace Aqua.Scripting
{
    [RequireComponent(typeof(ScriptObject))]
    public abstract class ScriptComponent : MonoBehaviour, IScriptComponent
    {
        [NonSerialized] protected ScriptObject m_Parent;

        public ScriptObject Parent { get { return m_Parent; } }

        public virtual void OnDeregister(ScriptObject inObject) { m_Parent = null; }
        public virtual void OnRegister(ScriptObject inObject) { m_Parent = inObject; }
        public virtual void PostRegister() { }
    }
}