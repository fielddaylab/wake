using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System;
using BeauUtil;
using UnityEngine.Events;
using BeauPools;
using BeauUtil.Debugger;

namespace ProtoAqua.Modeling
{
    public class CritterPopulationSlider : MonoBehaviour, IPoolAllocHandler
    {
        [Serializable] public class Pool : SerializablePool<CritterPopulationSlider> { }
        public class ActorCountEvent : UnityEvent<ActorCountI32> { }

        #region Inspector

        [SerializeField] private Image m_Icon = null;
        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private Graphic m_Color = null;
        [SerializeField] private TMP_InputField m_Text = null;
        [SerializeField] private RectTransform m_DesiredValue = null;

        #endregion // Inspector

        [NonSerialized] private ActorCountI32 m_Data;
        [NonSerialized] private float m_MinValue;
        [NonSerialized] private float m_MaxValue;
        [NonSerialized] private float m_DisplayScale;
        [NonSerialized] private float m_Increment;
        
        public readonly ActorCountEvent OnPopulationChanged = new ActorCountEvent();

        #region Unity Events

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(OnSliderValueChanged);
            m_Text.onEndEdit.AddListener(OnTextValueChanged);
        }

        #endregion // Unity Events

        public void Load(StringHash32 inActorId, int inPopulation, int inMin = 0, int inMax = -1, int inDesired = -1)
        {
            Load(Services.Assets.Bestiary[inActorId], inPopulation, inMin, inMax, inDesired);
        }

        public void Load(BestiaryDesc inDesc, int inPopulation, int inMin = 0, int inMax = -1, int inDesired = -1)
        {
            var body = inDesc.FactOfType<BFBody>();

            m_Icon.sprite = inDesc.Icon();
            m_Color.color = inDesc.Color();

            m_Data.Id = inDesc.Id();
            m_Data.Population = inPopulation;

            if (inMax < 0)
                inMax = (int) body.PopulationSoftCap;

            m_MinValue = inMin;
            m_MaxValue = inMax;

            m_DisplayScale = body.MassDisplayScale;
            m_Increment = body.PopulationSoftIncrement;

            m_Slider.wholeNumbers = true;
            m_Slider.minValue = (float) Math.Floor(m_MinValue / m_Increment);
            m_Slider.maxValue = (float) Math.Ceiling(m_MaxValue / m_Increment);
            m_Slider.SetValueWithoutNotify(inPopulation / m_Increment);

            if (inDesired > 0)
            {
                float fraction = Mathf.InverseLerp(m_MinValue, m_MaxValue, inDesired);
                m_DesiredValue.anchorMin = m_DesiredValue.anchorMax = new Vector2(fraction, 0.5f);
                m_DesiredValue.gameObject.SetActive(true);
            }
            else
            {
                m_DesiredValue.gameObject.SetActive(false);
            }

            int displayPop = ToDisplayPopulation(inPopulation);
            m_Text.SetTextWithoutNotify(displayPop.ToString());
        }

        private int ToDisplayPopulation(int inPopulation)
        {
            return (int) Math.Round(inPopulation * m_DisplayScale); 
        }

        private int ToRealPopulation(int inPopulation)
        {
            return (int) Math.Round(inPopulation / m_DisplayScale);
        }

        #region Handlers

        private void OnSliderValueChanged(float inValue)
        {
            int actualValue = (int) Mathf.Clamp(inValue * m_Increment, m_MinValue, m_MaxValue);
            TryUpdateValue(actualValue, true, false);
        }

        private void OnTextValueChanged(string inValue)
        {
            int val = StringParser.ParseInt(inValue);
            val = (int) Mathf.Clamp(val, m_MinValue * m_DisplayScale, m_MaxValue * m_DisplayScale);
            val = ToRealPopulation(val);
            TryUpdateValue(val, false, true);
        }

        private void TryUpdateValue(int inPopulation, bool inbUpdateText, bool inbUpdateSlider)
        {
            if (m_Data.Population == inPopulation)
                return;

            m_Data.Population = inPopulation;

            int displayPop = ToDisplayPopulation(inPopulation);
            if (inbUpdateText)
            {
                m_Text.SetTextWithoutNotify(displayPop.ToString());
            }
            if (inbUpdateSlider)
            {
                m_Slider.SetValueWithoutNotify(inPopulation / m_Increment);
            }

            OnPopulationChanged.Invoke(m_Data);
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            OnPopulationChanged.RemoveAllListeners();
        }

        #endregion // Handlers
    }
}