using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;

namespace Aqua.Scripting
{
    public interface IScriptComponent
    {
        void OnRegister(ScriptObject inObject);
        void OnDeregister(ScriptObject inObject);
    }
}