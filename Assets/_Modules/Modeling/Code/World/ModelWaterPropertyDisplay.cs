using System;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelWaterPropertyDisplay : MonoBehaviour {

        #region Inspector

        [SerializeField] private Graphic m_Background = null;
        [SerializeField] private Image m_Meter = null;
        [SerializeField] private Image m_Icon = null;

        #endregion // Inspector

        [NonSerialized] public WaterPropertyDesc Property;
        [NonSerialized] public float Value;

        public void Initialize(WaterPropertyDesc prop) {
            Property = prop;

            var colors = prop.Palette();
            m_Background.color = colors.Background;
            m_Meter.color = colors.Background;
            m_Icon.sprite = prop.Icon();

            SetValue(prop.DefaultValue());
        }

        public void SetValue(float value) {
            Value = value;
            m_Meter.fillAmount = Property.RemapValue(value);
        }
    }
}