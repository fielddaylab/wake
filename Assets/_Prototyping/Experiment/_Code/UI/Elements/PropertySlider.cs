using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class PropertySlider : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private Graphic m_Background = null;
        [SerializeField] private Graphic m_Fill = null;

        #endregion // Inspector
        [NonSerialized] private WaterPropertyId m_Id;

        [NonSerialized] private StringHash32 m_LabelId;

        public Slider Slider { get { return m_Slider; } }
        public WaterPropertyId Id { get { return m_Id; } }

        public void Load(WaterPropertyDesc inProperty, Sprite inIcon, bool inbInteractable)
        {
            m_Id = inProperty.Index();
            m_Icon.sprite = inIcon;
            m_LabelId = inProperty.LabelId();
            m_Slider.minValue = inProperty.MinValue();
            m_Slider.maxValue = inProperty.MaxValue();
            m_Slider.SetValueWithoutNotify(inProperty.DefaultValue());

            ColorPalette4 palette = inProperty.Palette();
            m_Background.color = palette.Shadow;
            m_Fill.color = palette.Background;
        }
    }
}