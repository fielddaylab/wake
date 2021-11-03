using UnityEngine;
using BeauRoutine.Extensions;
using BeauUtil;
using System;
using System.Collections;
using BeauRoutine;
using Aqua.Scripting;

namespace Aqua
{
    public class SharedPanel : BasePanel, IScriptComponent
    {
        [NonSerialized] private Canvas m_CachedCanvas;
        [NonSerialized] private int m_OriginalSortingLayer;
        [NonSerialized] private int m_BroughtToFront = 0;

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

        #region IScriptComponent

        public ScriptObject Parent { get; private set; }

        public virtual void OnRegister(ScriptObject inObject)
        {
            Parent = inObject;
        }

        public virtual void OnDeregister(ScriptObject inObject)
        {
        }

        #endregion // IScriptComponent

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

        #region Default Animations

        static protected IEnumerator DefaultFadeOn(RectTransform inTransform, CanvasGroup inGroup, float inDuration, Curve inCurve)
        {
            if (!inTransform.gameObject.activeSelf)
            {
                inTransform.gameObject.SetActive(true);
                inGroup.alpha = 0;
            }

            yield return Routine.Inline(inGroup.FadeTo(1, inDuration).Ease(inCurve));
        }

        static protected IEnumerator DefaultFadeOff(RectTransform inTransform, CanvasGroup inGroup, float inDuration, Curve inCurve)
        {
            yield return Routine.Inline(inGroup.FadeTo(0, inDuration).Ease(inCurve));
            inTransform.gameObject.SetActive(false);
        }

        #endregion // Default Animations

        #region Ordering

        /// <summary>
        /// Brings this panel in front of cutscene.
        /// </summary>
        public void BringToFront(int inLayerId = GameSortingLayers.Cutscene)
        {
            if (m_BroughtToFront != inLayerId)
                return;
            
            Canvas.sortingLayerID = inLayerId;
            m_BroughtToFront = inLayerId;
        }

        /// <summary>
        /// Resets the sorting order of this panel.
        /// </summary>
        public void ResetOrder()
        {
            if (m_BroughtToFront != 0)
                return;
            
            Canvas.sortingLayerID = m_OriginalSortingLayer; 
            m_BroughtToFront = 0;
        }

        #endregion // Ordering
    }
}