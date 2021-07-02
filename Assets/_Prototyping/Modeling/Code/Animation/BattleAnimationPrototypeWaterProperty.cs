using BeauUtil;
using UnityEngine;
using System;
using UnityEngine.UI;
using Aqua;
using System.Collections;
using BeauRoutine;
using BeauPools;

namespace ProtoAqua.Modeling
{
    public class BattleAnimationPrototypeWaterProperty : MonoBehaviour
    {
        #region Inspector
        
        [SerializeField] private Image m_Background = null;
        [SerializeField] private Image m_Meter = null;
        [SerializeField] private Image m_Icon = null;

        #endregion // Inspector

        [NonSerialized] private WaterPropertyDesc m_WaterProp = null;
        [NonSerialized] private float m_CurrentValue;
        private Routine m_Animation;

        public void Initialize(WaterPropertyDesc inWaterProp)
        {
            m_WaterProp = inWaterProp;

            var colors = inWaterProp.Palette();
            m_Background.color = colors.Background;
            m_Meter.color = colors.Content;
            m_Icon.sprite = inWaterProp.Icon();

            SetValue(inWaterProp.DefaultValue());
        }

        private void OnDisable()
        {
            m_Animation.Stop();
        }

        public void SetValue(float inValue)
        {
            m_Animation.Stop();

            m_CurrentValue = inValue;

            float valuePercent = m_WaterProp.RemapValue(inValue);
            InstantMeter(m_Meter, valuePercent);
        }

        public void AnimateValue(float inValue)
        {
            if (m_CurrentValue == inValue)
                return;

            m_CurrentValue = inValue;
            
            float valuePercent = m_WaterProp.RemapValue(inValue);
            m_Animation.Replace(this, AnimateMeter(m_Meter, valuePercent));
        }

        #region Animations

        static private void InstantMeter(Image inImage, float inPercent)
        {
            inImage.fillAmount = inPercent;
        }

        static private IEnumerator AnimateMeter(Image inImage, float inPercent)
        {
            yield return inImage.FillTo(inPercent, 0.2f).Ease(Curve.CubeOut).ForceOnCancel();
        }

        #endregion // Animations
    }
}