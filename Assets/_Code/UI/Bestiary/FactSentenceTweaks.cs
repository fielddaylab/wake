using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauUtil.Variants;
using BeauPools;
using BeauUtil.Debugger;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Fact Sentence Tweaks")]
    public class FactSentenceTweaks : TweakAsset
    {
        #region Types

        [Serializable] private class FactFragmentPool : SerializablePool<FactSentenceFragment> { }
        
        [Serializable]
        private struct TextBackgroundColorPair
        {
            public Color Text;
            public Color Background;

            static public readonly TextBackgroundColorPair Default = new TextBackgroundColorPair()
            {
                Text = Color.white,
                Background = ColorBank.DarkGray,
            };
        }

        #endregion // Types

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private Transform m_PoolRoot = null;

        #region Inspector

        [Header("Pools")]
        [SerializeField] private FactFragmentPool m_Default = null;

        [Header("Colors")]
        [SerializeField] private TextBackgroundColorPair m_NounColor = TextBackgroundColorPair.Default;
        [SerializeField] private TextBackgroundColorPair m_VerbColor = TextBackgroundColorPair.Default;
        [SerializeField] private TextBackgroundColorPair m_AdjectiveColor = TextBackgroundColorPair.Default;
        [SerializeField] private TextBackgroundColorPair m_AmountColor = TextBackgroundColorPair.Default;
        [SerializeField] private TextBackgroundColorPair m_ConjunctionColor = TextBackgroundColorPair.Default;
        [SerializeField] private TextBackgroundColorPair m_ConditionColor = TextBackgroundColorPair.Default;

        #endregion // Inspector

        #region TweakAsset

        protected override void Apply()
        {
            if (m_Initialized)
                return;
            
            m_Initialized = true;
            GameObject poolRootGO = new GameObject("FactSentencePool");
            poolRootGO.hideFlags = HideFlags.DontSave;
            poolRootGO.SetActive(false);

            DontDestroyOnLoad(poolRootGO);
            m_PoolRoot = poolRootGO.transform;

            SetupPool(m_Default);

            SceneHelper.OnSceneUnload += UnloadFromScene;
            Application.quitting += OnQuitting;
        }

        protected override void Remove()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
            #endif // UNITY_EDITOR

            if (!m_Initialized)
                return;

            m_Default.Dispose();
            UnityHelper.SafeDestroyGO(ref m_PoolRoot);
            SceneHelper.OnSceneUnload -= UnloadFromScene;
            Application.quitting -= OnQuitting;
            
            m_Initialized = false;
        }

        #endregion // TweakAsset

        #region Setup/Cleanup

        private void SetupPool(FactFragmentPool inPool)
        {
            inPool.ConfigureTransforms(m_PoolRoot, null, true);
        }

        private void UnloadFromScene(SceneBinding inBinding, object inContext)
        {
            int recycleCount = m_Default.FreeAllInScene(inBinding);
            if (recycleCount > 0)
            {
                Log.Warn("[FactSentenceTweaks] Cleaned up {0} fragments still present in unloading scene", recycleCount);
            }
        }

        private void OnQuitting()
        {
            Remove();
        }

        #endregion // Setup/Cleanup

        #region Alloc

        public FactSentenceFragment Alloc(in BFFragment inFragment, Transform inTarget)
        {
            // TODO: Differentiate between different fragment types?

            FactSentenceFragment fragment = m_Default.Alloc(inTarget);
            TextBackgroundColorPair colors = FragmentColors(inFragment);
            fragment.PreConfigure(colors.Background, colors.Text);

            return fragment;
        }

        #endregion // Alloc

        private TextBackgroundColorPair FragmentColors(in BFFragment inFragment)
        {
            switch(inFragment.Type)
            {
                case BestiaryFactFragmentType.Noun:
                    return m_NounColor;

                case BestiaryFactFragmentType.Verb:
                    return m_VerbColor;

                case BestiaryFactFragmentType.Adjective:
                    return m_AdjectiveColor;

                case BestiaryFactFragmentType.Amount:
                    return m_AmountColor;
                
                case BestiaryFactFragmentType.Conjunction:
                    return m_ConjunctionColor;

                case BestiaryFactFragmentType.Condition:
                    return m_ConditionColor;

                default:
                    throw new ArgumentOutOfRangeException("inFragment.Type");
            }
        }
    }
}