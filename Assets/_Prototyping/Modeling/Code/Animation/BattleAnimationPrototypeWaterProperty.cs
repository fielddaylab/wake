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

        public void Initialize(WaterPropertyDesc inWaterProp)
        {
            m_WaterProp = inWaterProp;

            var colors = inWaterProp.Palette();
            m_Background.color = colors.Background;
            m_Meter.color = colors.Content;
            m_Icon.sprite = inWaterProp.Icon();

            SetValue(inWaterProp.DefaultValue());
        }

        public void SetValue(float inValue)
        {
            m_CurrentValue = inValue;

            float valuePercent = m_WaterProp.RemapValue(inValue);
            InstantMeter(m_Meter, valuePercent);

            m_EffectPool.Reset();
        }

        public IEnumerator AnimateValue(float inValue)
        {
            if (m_CurrentValue == inValue)
                return null;

            m_CurrentValue = inValue;
            
            float valuePercent = m_WaterProp.RemapValue(inValue);
            return AnimateMeter(m_Meter, valuePercent);
        }

        public IEnumerator PlayIncoming(Transform inOriginator)
        {
            VFX effect = m_EffectPool.Alloc();
            effect.Graphic.color = m_WaterProp.Color();
            effect.Transform.SetPosition(inOriginator.localPosition, Axis.XY, Space.Self);
            return AnimateIncrease(effect, inOriginator, transform);
        }

        public IEnumerator PlayOutgoing(Transform inTarget)
        {
            VFX effect = m_EffectPool.Alloc();
            effect.Graphic.color = m_WaterProp.Color();
            effect.Transform.SetPosition(transform.localPosition, Axis.XY, Space.Self);
            return AnimateIncrease(effect, transform, inTarget);
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
            SimpleSpline simple = Spline.Simple(inA.localPosition, inB.localPosition, new Vector3(0, 100, 0));
            yield return inEffect.transform.MoveAlong(simple, 0.5f, Axis.XY, Space.Self).Ease(Curve.Smooth);
            inEffect.Free();
        }

        #endregion // Animations
    }
}