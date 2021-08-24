using BeauUtil;
using UnityEngine;
using System;
using UnityEngine.UI;
using Aqua;
using System.Collections;
using BeauRoutine;
using BeauPools;
using BeauRoutine.Splines;

namespace ProtoAqua.Modeling
{
    public class BattleAnimationPrototypeWaterProperty : MonoBehaviour
    {
        #region Inspector
        
        [SerializeField] private Image m_Background = null;
        [SerializeField] private Image m_Meter = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private VFX.Pool m_EffectPool = null;

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

            m_EffectPool.Reset();
        }

        public void AnimateValue(float inValue)
        {
            if (m_CurrentValue == inValue)
                return;

            m_CurrentValue = inValue;
            
            float valuePercent = m_WaterProp.RemapValue(inValue);
            m_Animation.Replace(this, AnimateMeter(m_Meter, valuePercent));
        }

        public void PlayIncoming(Transform inOriginator)
        {
            VFX effect = m_EffectPool.Alloc();
            effect.Graphic.color = m_WaterProp.Color();
            effect.Animation.Replace(effect, AnimateIncrease(effect, inOriginator.transform, transform)).TryManuallyUpdate(0);
        }

        public void PlayOutgoing(Transform inTarget)
        {
            VFX effect = m_EffectPool.Alloc();
            effect.Graphic.color = m_WaterProp.Color();
            effect.Animation.Replace(effect, AnimateIncrease(effect, transform, inTarget.transform)).TryManuallyUpdate(0);
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

        static private IEnumerator AnimateIncrease(VFX inEffect, Transform inA, Transform inB)
        {
            inEffect.Transform.SetPosition(inA.localPosition, Axis.XY, Space.Self);
            SimpleSpline simple = Spline.Simple(inA.localPosition, inB.localPosition, new Vector3(0, 100, 0));
            yield return inEffect.transform.MoveAlong(simple, 0.5f, Axis.XY, Space.Self).Ease(Curve.Smooth);
            inEffect.Free();
        }

        #endregion // Animations
    }
}