using UnityEngine;
using BeauRoutine.Extensions;
using BeauUtil;
using System;

namespace Aqua
{
    public class SharedPanel : BasePanel
    {
        [NonSerialized] private Canvas m_CachedCanvas;
        [NonSerialized] private int m_OriginalSortingLayer;
        [NonSerialized] private bool m_BroughtToFront = false;

        public Canvas Canvas
        {
            get
            {
                if (m_CachedCanvas.IsReferenceNull())
                {
                    m_CachedCanvas = GetComponentInParent<Canvas>();
                    m_OriginalSortingLayer = m_CachedCanvas.sortingLayerID;
                }
                return m_CachedCanvas;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            Services.UI.RegisterPanel(this);
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            ResetOrder();

            base.OnHideComplete(inbInstant);
        }

        protected virtual void OnDestroy()
        {
            Services.UI?.DeregisterPanel(this);
        }

        #region Ordering

        /// <summary>
        /// Brings this panel in front of cutscene.
        /// </summary>
        public void BringToFront()
        {
            if (m_BroughtToFront)
                return;
            
            Canvas.sortingLayerID = GameSortingLayers.Cutscene;
            m_BroughtToFront = true;
        }

        /// <summary>
        /// Resets the sorting order of this panel.
        /// </summary>
        public void ResetOrder()
        {
            if (!m_BroughtToFront)
                return;
            
            Canvas.sortingLayerID = m_OriginalSortingLayer; 
            m_BroughtToFront = false;
        }

        #endregion // Ordering
    }
}