using System;
using Aqua;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenBegin : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private LayoutGroup m_PropertiesLayout = null;
        [SerializeField] private SetupElementDisplay m_TankTypeDisplay = null;
        [SerializeField] private SetupElementDisplay m_EnvironmentDisplay = null;
        [SerializeField] private SetupElementDisplay m_CritterDisplay = null;
        [SerializeField] private SetupElementDisplay m_PropertyDisplay = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        public Action OnSelectStart;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_StartButton.onClick.AddListener(() => OnSelectStart?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            var tankType = Config.GetTank(Setup.Tank);
            m_TankTypeDisplay.gameObject.SetActive(true);
            m_TankTypeDisplay.Load(tankType.Icon, tankType.ShortLabelId);

            switch(tankType.Tank)
            {
                case TankType.Measurement:
                case TankType.Stressor:
                    {
                        m_EnvironmentDisplay.gameObject.SetActive(false);
                        PopulateCritter(Setup.CritterId);
                        PopulateProperty(Setup.PropertyId);
                        break;
                    }

                default:
                    {
                        PopulateEnvironment(Setup.EnvironmentId);
                        m_CritterDisplay.gameObject.SetActive(false);
                        m_PropertyDisplay.gameObject.SetActive(false);
                        break;
                    }
            }

            m_PropertiesLayout.ForceRebuild();
        }

        private void PopulateCritter(StringHash32 inCritterId)
        {
            m_CritterDisplay.gameObject.SetActive(true);
            var critter = Services.Assets.Bestiary.Get(inCritterId);
            m_CritterDisplay.Load(critter.Icon(), critter.CommonName());
        }

        private void PopulateProperty(WaterPropertyId inProperty)
        {
            m_PropertyDisplay.gameObject.SetActive(true);
            var prop = Services.Assets.WaterProp.Property(inProperty);
            m_PropertyDisplay.Load(prop.Icon(), prop.LabelId());
        }

        private void PopulateEnvironment(StringHash32 inEnvironmentId)
        {
            m_EnvironmentDisplay.gameObject.SetActive(true);
            var env = Services.Assets.Bestiary.Get(inEnvironmentId);
            m_EnvironmentDisplay.Load(env.Icon(), env.CommonName());
        }
    }
}