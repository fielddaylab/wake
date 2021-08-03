using System;
using Aqua;
using Aqua.Cameras;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProtoAqua.ExperimentV2
{
    public class WaterPropertyDial : MonoBehaviour
    {
        public delegate void DialDelegate(WaterPropertyId inProperty, float inValue);

        public Slider Slider;
        public Image Icon;
        public LocText Label;
        public CursorInteractionHint Tooltip;

        [NonSerialized] public WaterPropertyDesc Property;
        [NonSerialized] public DialDelegate OnChanged;

        private void Awake()
        {
            Slider.onValueChanged.AddListener(InvokeChanged);
        }

        private void InvokeChanged(float inSliderValue)
        {
            float actualValue = Property.InverseRemap(inSliderValue / Slider.maxValue);
            OnChanged?.Invoke(Property.Index(), actualValue);
        }

        static public void SetValue(WaterPropertyDial inDial, float inValue)
        {
            float dialValue = inDial.Property.RemapValue(inValue) * inDial.Slider.maxValue;
            inDial.Slider.SetValueWithoutNotify(dialValue);
        }
    }
}