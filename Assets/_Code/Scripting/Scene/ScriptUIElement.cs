using UnityEngine;
using System;
using Leaf.Runtime;
using UnityEngine.EventSystems;
using BeauUtil.Debugger;
using BeauUtil;

namespace Aqua.Scripting
{
    public class ScriptUIElement : ScriptComponent
    {
        #region Inspector

        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private Canvas m_Canvas = null;

        #endregion // Inspector

        [NonSerialized] private int m_OriginalCanvasLayer;
        [NonSerialized] private bool m_OnTop;
        [NonSerialized] private GameObject m_ClickHandler;

        [LeafMember]
        public void ForceOnTop()
        {
            if (m_OnTop)
                return;
            
            m_Group.ignoreParentGroups = true;
            m_OriginalCanvasLayer = m_Canvas.sortingLayerID;
            m_Canvas.sortingLayerID = GameSortingLayers.Cutscene;
            m_OnTop = true;
        }

        [LeafMember]
        public void ResetSorting()
        {
            if (!m_OnTop)
                return;
            
            m_Group.ignoreParentGroups = false;
            m_Canvas.sortingLayerID = m_OriginalCanvasLayer;
            m_OnTop = false;
        }

        [LeafMember]
        public void Click()
        {
            GameObject handler = GetClickHandler();
            if (handler)
                Services.Input.ExecuteClick(handler);
        }

        [LeafMember]
        public void ForceClick()
        {
            GameObject handler = GetClickHandler();
            if (handler)
                Services.Input.ForceClick(handler);
        }

        private GameObject GetClickHandler()
        {
            if (m_ClickHandler.IsReferenceNull())
            {
                IPointerClickHandler click = GetComponentInChildren<IPointerClickHandler>();
                if (click != null)
                {
                    m_ClickHandler = ((Component) click).gameObject;
                }
                else
                {
                    Log.Error("[ScriptUIElement] Unable to find clickable element on {0}", this.m_Parent.Id());
                }
            }
            
            return m_ClickHandler;
        }
    }
}