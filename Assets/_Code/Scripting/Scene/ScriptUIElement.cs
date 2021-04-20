using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine.Extensions;

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
    }
}