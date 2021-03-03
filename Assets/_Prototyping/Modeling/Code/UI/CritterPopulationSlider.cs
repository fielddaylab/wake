using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System;
using BeauUtil;
using UnityEngine.Events;
using BeauPools;

namespace ProtoAqua.Modeling
{
    public class CritterPopulationSlider : MonoBehaviour, IPoolAllocHandler
    {
        public class ActorCountEvent : UnityEvent<ActorCount> { }

        #region Inspector

        [SerializeField] private Image m_Icon = null;
        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private TMP_InputField m_Text = null;

        #endregion // Inspector

        [NonSerialized] private ActorCount m_Data;
        [NonSerialized] private float m_SliderScale;
        
        public readonly ActorCountEvent OnPopulationChanged = new ActorCountEvent();

        #region Unity Events

        private void Awake()
        {
            m_Slider.onValueChanged.AddListener(OnSliderValueChanged);
            m_Text.onEndEdit.AddListener(OnTextValueChanged);
        }

        #endregion // Unity Events

        public void Load(StringHash32 inActorId, uint inPopulation)
        {
            Load(Services.Assets.Bestiary[inActorId], inPopulation);
        }

        public void Load(BestiaryDesc inDesc, uint inPopulation)
        {
            var body = inDesc.FactOfType<BFBody>();

            m_Icon.sprite = inDesc.Icon();

            m_Data.Id = inDesc.Id();
            m_Data.Population = inPopulation;

            m_SliderScale = body.PopulationSoftIncrement();

            m_Slider.SetValueWithoutNotify(0);
            m_Slider.minValue = 0;
            m_Slider.maxValue = body.PopulationSoftCap() / m_SliderScale;
            m_Slider.wholeNumbers = true;

            m_Slider.SetValueWithoutNotify(inPopulation / m_SliderScale);
            m_Text.SetTextWithoutNotify(inPopulation.ToString());
        }

        #region Handlers

        private void OnSliderValueChanged(float inValue)
        {
            uint val = (uint) (inValue * m_SliderScale);
            TryUpdateValue(val, true, false);
        }

        private void OnTextValueChanged(string inValue)
        {
            uint val = StringParser.ParseUInt(inValue);
            val = Math.Min(val, (uint) m_Slider.maxValue);
            TryUpdateValue(val, false, true);
        }

        private void TryUpdateValue(uint inPopulation, bool inbUpdateText, bool inbUpdateSlider)
        {
            if (m_Data.Population == inPopulation)
                return;

            m_Data.Population = inPopulation;
            if (inbUpdateText)
                m_Text.SetTextWithoutNotify(inPopulation.ToString());
            if (inbUpdateSlider)
                m_Slider.SetValueWithoutNotify(inPopulation / m_SliderScale);

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