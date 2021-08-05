using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class WaterPropertyDial : MonoBehaviour
    {
        private const float MinAngle = 0;
        private const float MaxAngle = -180 - MinAngle;
        private const float AngleDelta = MaxAngle - MinAngle;

        public delegate void ValueChangedDelegate(WaterPropertyId inProperty, float inValue);
        public delegate void ReleasedDelegate(WaterPropertyId inProperty);

        public Slider Slider;
        public RectTransform Needle;
        public LocText Label;
        public CursorInteractionHint Tooltip;

        [Header("Stress Regions")]
        public Image MinStressed;
        public Image MinDeath;
        public Image MaxStressed;
        public Image MaxDeath;

        [Header("Indicators")]
        public RectGraphic HasMin;
        public RectGraphic HasMax;

        [NonSerialized] public WaterPropertyDesc Property;
        [NonSerialized] public ValueChangedDelegate OnChanged;
        [NonSerialized] public ReleasedDelegate OnReleased;

        private void Awake()
        {
            Slider.onValueChanged.AddListener(InvokeChanged);
            Slider.EnsureComponent<PointerListener>().onPointerUp.AddListener(InvokeReleased);
        }

        private void InvokeChanged(float inSliderValue)
        {
            float ratio = inSliderValue / Slider.maxValue;
            UpdateNeedle(ratio);
            float actualValue = Property.InverseRemap(ratio);
            OnChanged?.Invoke(Property.Index(), actualValue);
        }

        private void InvokeReleased(PointerEventData _)
        {
            OnReleased?.Invoke(Property.Index());
        }

        public void SetValue(float inValue)
        {
            float ratio = Property.RemapValue(inValue);
            float dialValue = ratio * Slider.maxValue;
            Slider.SetValueWithoutNotify(dialValue);
            UpdateNeedle(ratio);
        }

        private void UpdateNeedle(float inRatio)
        {
            Needle.SetRotation(MinAngle + AngleDelta * inRatio, Axis.Z, Space.Self);
        }

        static public void ConfigureStress(WaterPropertyDial inDial, ActorStateTransitionRange inRange)
        {
            ConfigureMinFill(inDial.MinStressed, inDial.Property, inRange.AliveMin);
            ConfigureMinFill(inDial.MinDeath, inDial.Property, inRange.StressedMin);

            ConfigureMaxFill(inDial.MaxStressed, inDial.Property, inRange.AliveMax);
            ConfigureMaxFill(inDial.MaxDeath, inDial.Property, inRange.StressedMax);
        }

        static public void DisplayRanges(WaterPropertyDial inDial, bool inMin, bool inMax)
        {
            inDial.MinStressed.gameObject.SetActive(inMin);
            inDial.MinDeath.gameObject.SetActive(inMin);
            inDial.HasMin.gameObject.SetActive(inMin);

            inDial.MaxStressed.gameObject.SetActive(inMax);
            inDial.MaxDeath.gameObject.SetActive(inMax);
            inDial.HasMax.gameObject.SetActive(inMax);
        }

        static private void ConfigureMinFill(Image inImage, WaterPropertyDesc inDesc, float inAmount)
        {
            if (float.IsInfinity(inAmount))
            {
                inImage.fillAmount = 0;
                return;
            }

            float minPercentage = inDesc.RemapValue(inAmount) / 2;
            inImage.fillAmount = minPercentage;
        }

        static private void ConfigureMaxFill(Image inImage, WaterPropertyDesc inDesc, float inAmount)
        {
            if (float.IsInfinity(inAmount))
            {
                inImage.fillAmount = 0;
                return;
            }

            float maxPercentage = (1 - inDesc.RemapValue(inAmount)) / 2;
            inImage.fillAmount = maxPercentage;
        }
    }
}