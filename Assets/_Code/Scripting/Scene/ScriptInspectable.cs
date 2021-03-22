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
    public class ScriptInspectable : ScriptComponent, IPointerClickHandler
    {
        [LeafMember]
        public void Inspect()
        {
            ScriptObject.Inspect(m_Parent);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            ScriptObject.Inspect(m_Parent);
        }

        #region IScriptComponent

        public override void OnRegister(ScriptObject inObject)
        {
            base.OnRegister(inObject);
            this.EnsureComponent<CursorInteractionHint>();
        }

        #endregion // IScriptComponent
    }
}