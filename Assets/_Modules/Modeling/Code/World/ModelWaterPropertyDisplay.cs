using System;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelWaterPropertyDisplay : MonoBehaviour {

        #region Inspector

        public CanvasGroup CanvasGroup;

        [SerializeField] private Graphic m_Background = null;
        [SerializeField] private Image m_Meter = null;
        [SerializeField] private Image m_Icon = null;

        #endregion // Inspector

        [NonSerialized] public WaterPropertyDesc Property;
        [NonSerialized] public float Value;

        [NonSerialized] public int Index;
        [NonSerialized] public WorldFilterMask Mask;

        public void Initialize(WaterPropertyDesc prop) {
            Property = prop;

            var colors = prop.Palette();
            m_Background.color = colors.Background;
            m_Meter.color = colors.Background;
            m_Icon.sprite = prop.Icon();

            SetValue(prop.DefaultValue());

            #if UNITY_EDITOR
            gameObject.name = prop.name;
            #endif // UNITY_EDITOR
        }

        public void SetValue(float value) {
            Value = value;
            m_Meter.fillAmount = Property.RemapValue(value);
        }
    }
}