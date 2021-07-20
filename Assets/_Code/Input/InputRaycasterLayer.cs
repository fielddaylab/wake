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
            m_Raycasters = null;
            CacheRaycasters();
            ResetPriority();
        }

        protected override void OnValidate()
        {
            if (Application.IsPlaying(this))
                return;
            
            m_Raycasters = null;
            CacheRaycasters();
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