using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
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
        public Image Icon;
        public LocText Label;
        public LocText Value;
        public CursorInteractionHint Tooltip;

        [Header("Stress Regions")]
        public Graphic AliveRegion;
        public Graphic MinStressed;
        public Graphic MinDeath;
        public Graphic MaxStressed;
        public Graphic MaxDeath;

        [Header("Indicators")]
        public GameObject HasMin;
        public GameObject HasMax;

        [NonSerialized] public WaterPropertyDesc Property;
        [NonSerialized] public ValueChangedDelegate OnChanged;
        [NonSerialized] public ReleasedDelegate OnReleased;

        private void Awake()
        {
            if (Slider)
            {
                Slider.onValueChanged.AddListener(InvokeChanged);
                Slider.EnsureComponent<PointerListener>().onPointerUp.AddListener(InvokeReleased);
            }
        }

        private void InvokeChanged(float inSliderValue)
        {
            float ratio = inSliderValue / Slider.maxValue;
            float actualValue = Property.InverseRemap(ratio);
            AdjustValueLabel(actualValue);
            OnChanged?.Invoke(Property.Index(), actualValue);
        }

        private void InvokeReleased(PointerEventData _)
        {
            OnReleased?.Invoke(Property.Index());
        }

        public void SetValue(float inValue)
        {
            float ratio = Property.RemapValue(inValue);
            
            if (Slider != null)
            {
                float dialValue = ratio * Slider.maxValue;
                Slider.SetValueWithoutNotify(dialValue);
                AdjustValueLabel(inValue);
            }
        }

        private void AdjustValueLabel(float inActualValue)
        {
            Value.SetTextFromString(Property.FormatValue(inActualValue));

            RectTransform valueTransform = Value.Graphic.rectTransform;
            TMP_Text valueText = Value.Graphic;
            if (Slider.normalizedValue < 0.5f) {
                valueTransform.pivot = new Vector2(0, 0.5f);
                valueTransform.SetAnchorPos(Math.Abs(valueTransform.anchoredPosition.x), Axis.X);
                valueText.alignment = TextAlignmentOptions.Left;
            } else {
                valueTransform.pivot = new Vector2(1, 0.5f);
                valueTransform.SetAnchorPos(-Math.Abs(valueTransform.anchoredPosition.x), Axis.X);
                valueText.alignment = TextAlignmentOptions.Right;
            }
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
            inDial.HasMin.SetActive(inMin);

            inDial.MaxStressed.gameObject.SetActive(inMax);
            inDial.MaxDeath.gameObject.SetActive(inMax);
            inDial.HasMax.SetActive(inMax);
        }

        static private void ConfigureMinFill(Graphic inSection, WaterPropertyDesc inDesc, float inAmount)
        {
            if (float.IsInfinity(inAmount))
            {
                inSection.rectTransform.anchorMax = new Vector2(0, 1);
                return;
            }

            float minPercentage = RangeDisplay.AdjustInputIgnoreEdges(inDesc.RemapValue(inAmount), 0.9f);
            inSection.rectTransform.anchorMax = new Vector2(minPercentage, 1);
        }

        static private void ConfigureMaxFill(Graphic inSection, WaterPropertyDesc inDesc, float inAmount)
        {
            if (float.IsInfinity(inAmount))
            {
                inSection.rectTransform.anchorMin = new Vector2(1, 0);
                return;
            }

            float maxPercentage = RangeDisplay.AdjustInputIgnoreEdges(inDesc.RemapValue(inAmount), 0.9f);
            inSection.rectTransform.anchorMin = new Vector2(maxPercentage, 0);
        }
    }
}