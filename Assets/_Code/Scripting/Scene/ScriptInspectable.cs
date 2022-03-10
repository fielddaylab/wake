using BeauUtil;
using UnityEngine;
using Leaf.Runtime;
using UnityEngine.EventSystems;
using BeauUtil.UI;
using UnityEngine.Scripting;
using System;

namespace Aqua.Scripting
{
    public class ScriptInspectable : ScriptComponent, IPointerClickHandler
    {
        [SerializeField] private PointerListener m_Proxy = null;

        public Action<ScriptInspectable> OnInspect;

        [LeafMember, Preserve]
        public void Inspect()
        {
            ScriptObject.Inspect(m_Parent);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            ScriptObject.Inspect(m_Parent);
            OnInspect?.Invoke(this);
        }

        #region IScriptComponent

        public override void OnRegister(ScriptObject inObject)
        {
            base.OnRegister(inObject);
            if (m_Proxy) {
                m_Proxy.EnsureComponent<CursorInteractionHint>();
                m_Proxy.onClick.AddListener(((IPointerClickHandler)this).OnPointerClick);
            } else {
                this.EnsureComponent<CursorInteractionHint>();
            }
        }

        #endregion // IScriptComponent
    }
}