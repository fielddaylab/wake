using System;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Configuration")]
    public class EnergyConfig : TweakAsset
    {
        #region Types

        [Serializable]
        public struct ColorSpritePair
        {
            public Color Color;
            public Sprite Icon;
        }

        #endregion // Types

        #region Inspector

        [Header("Calculations")]

        [SerializeField] private float m_ErrorScale = 1;

        [Header("Display")]

        [Header("Sync Gradients")]

        [SerializeField] private Gradient m_SyncGradientBold = null;
        [SerializeField] private Gradient m_SyncGradientBoldText = null;
        [SerializeField] private Gradient m_SyncGradientSubdued = null;
        [SerializeField] private Gradient m_SyncGradientSubduedText = null;
        [SerializeField] private Gradient m_SyncGradientProgress = null;

        [Header("Sync Colors")]

        [SerializeField] private ColorSpritePair m_SyncedLabel = default(ColorSpritePair);
        [SerializeField] private ColorSpritePair m_TooHighLabel = default(ColorSpritePair);
        [SerializeField] private ColorSpritePair m_TooLowLabel = default(ColorSpritePair);

        #endregion // Inspector

        public float ErrorScale() { return m_ErrorScale; }

        public float CalculateSync(in EnergySimContext inContextA, in EnergySimContext inContextB)
        {
            float error = 100 * EnergySim.CalculateError(inContextA.CachedCurrent, inContextB.CachedCurrent, inContextA.Database);
            float sync = 100 - (float) Math.Min(Math.Ceiling(error * m_ErrorScale), 100);
            return sync;
        }

        public Color EvaluateSyncGradientBold(float inSync, bool inbText)
        {
            if (inbText)
                return m_SyncGradientBoldText.Evaluate(inSync / 100);
            return m_SyncGradientBold.Evaluate(inSync / 100);
        }

        public Color EvaluateSyncGradientSubdued(float inSync, bool inbText)
        {
            if (inbText)
                return m_SyncGradientSubduedText.Evaluate(inSync / 100);
            return m_SyncGradientSubdued.Evaluate(inSync / 100);
        }

        public Color EvaluateSyncGradientProgress(float inSync)
        {
            return m_SyncGradientProgress.Evaluate(inSync / 100);
        }

        public ColorSpritePair GetLabelSettings(float inDifference)
        {
            if (inDifference > 0)
                return m_TooHighLabel;
            if (inDifference < 0)
                return m_TooLowLabel;
            return m_SyncedLabel;
        }
    }
}