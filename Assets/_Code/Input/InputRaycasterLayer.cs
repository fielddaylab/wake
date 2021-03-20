using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(BaseRaycaster))]
    public class InputRaycasterLayer : BaseInputLayer
    {
        #region Inspector

        [SerializeField, HideInEditor] private BaseRaycaster[] m_Raycasters = null;

        #endregion // Inspector

        #region Unity Events

        protected override void Awake()
        {
            base.Awake();
            CacheRaycasters();
        }

        #if UNITY_EDITOR

        protected override void Reset()
        {
            CacheRaycasters();
            Canvas c = GetComponent<Canvas>();
            if (c != null)
            {
                switch(c.renderMode)
                {
                    case RenderMode.ScreenSpaceCamera:
                        m_Priority = 1000 - (int) c.planeDistance;
                        break;

                    case RenderMode.ScreenSpaceOverlay:
                        m_Priority = 1000 + c.sortingOrder;
                        break;
                }
            }
        }

        protected override void OnValidate()
        {
            CacheRaycasters();
            if (Application.isPlaying && Time.frameCount > 2)
                UpdateEnabled(true);
        }

        #endif // UNITY_EDITOR

        private void CacheRaycasters()
        {
            if (m_Raycasters == null || m_Raycasters.Length == 0)
                m_Raycasters = GetComponents<BaseRaycaster>();
        }

        #endregion // Unity Events

        protected override void SyncEnabled(bool inbEnabled)
        {
            for(int i = m_Raycasters.Length - 1; i >= 0; --i)
                m_Raycasters[i].enabled = inbEnabled;
        }
    }
}