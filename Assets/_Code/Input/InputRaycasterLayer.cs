using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BeauUtil.Debugger;
using ScriptableBake;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [RequireComponent(typeof(BaseRaycaster))]
    public class InputRaycasterLayer : BaseInputLayer, IBaked
    {
        #region Inspector

        [SerializeField] private BaseRaycaster[] m_Raycasters = null;

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

            BaseRaycaster[] raycasters = GetComponentsInChildren<BaseRaycaster>(true);
            if (!ArrayUtils.ContentEquals(raycasters, m_Raycasters)) {
                m_Raycasters = raycasters;
                Log.Warn("InputRaycasterLayer '{0}' updated its list of BaseRaycasters!", name);
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }
        }

        int IBaked.Order => 100000;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            m_Raycasters = GetComponentsInChildren<BaseRaycaster>(true);
            return true;
        }

#endif // UNITY_EDITOR

        private void CacheRaycasters()
        {
            if (m_Raycasters == null || m_Raycasters.Length == 0)
                m_Raycasters = GetComponentsInChildren<BaseRaycaster>(true);
        }

        #endregion // Unity Events

        protected override void SyncEnabled(bool inbEnabled)
        {
            for(int i = m_Raycasters.Length - 1; i >= 0; --i)
                m_Raycasters[i].enabled = inbEnabled;
        }
    }
}