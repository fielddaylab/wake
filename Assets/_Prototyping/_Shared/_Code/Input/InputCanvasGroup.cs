using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InputCanvasGroup : BaseInputLayer
    {
        #region Inspector

        [SerializeField, HideInEditor] private CanvasGroup m_CanvasGroup = null;

        #endregion // Inspector

        #region Unity Events

        protected override void Awake()
        {
            base.Awake();
            this.CacheComponent(ref m_CanvasGroup);
        }

        #if UNITY_EDITOR

        protected override void Reset()
        {
            this.CacheComponent(ref m_CanvasGroup);
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null)
            {
                switch(c.renderMode)
                {
                    case RenderMode.ScreenSpaceCamera:
                        m_Priority = 1000 - (int) c.planeDistance;
                        break;

                    case RenderMode.ScreenSpaceOverlay:
                        m_Priority = 1000 + c.renderOrder;
                        break;
                }
            }
        }

        protected override void OnValidate()
        {
            this.CacheComponent(ref m_CanvasGroup);
            if (Application.isPlaying)
                UpdateEnabled(true);
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        protected override void SyncEnabled(bool inbEnabled)
        {
            m_CanvasGroup.blocksRaycasts = inbEnabled;
        }
    }
}