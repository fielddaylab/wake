using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSummary : ExperimentSetupSubscreen
    {
        static private readonly StringHash32 DurationLabel = "experiment.summary.duration";

        #region Inspector

        [Header("Headers")]
        [SerializeField] private LocText m_DurationText = null;
        [SerializeField] private LayoutGroup m_PropertiesLayout = null;
        [SerializeField] private SetupElementDisplay m_TankTypeDisplay = null;
        [SerializeField] private SetupElementDisplay m_EnvironmentDisplay = null;
        [SerializeField] private SetupElementDisplay m_CritterDisplay = null;
        [SerializeField] private SetupElementDisplay m_PropertyDisplay = null;

        [Header("Discoveries")]
        [SerializeField] private RectTransform m_NewDiscoveriesGroup = null;
        [SerializeField] private RectTransform m_NoDiscoveriesGroup = null;

        [Header("Facts")]
        [SerializeField] private VerticalLayoutGroup m_BehaviorLayout = null;
        [SerializeField] private FactPools m_FactPools = null;

        #endregion // Inspector

        private ExperimentResultData m_Result;

        protected override void OnDisable()
        {
            m_FactPools.FreeAll();
            m_Result = null;

            base.OnDisable();
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            m_FactPools.FreeAll();
            m_DurationText.SetText(DurationLabel);

            var tankType = Config.GetTank(m_Result.Setup.Tank);
            m_TankTypeDisplay.gameObject.SetActive(true);
            m_TankTypeDisplay.Load(tankType.Icon, tankType.ShortLabelId);

            switch(tankType.Tank)
            {
                case TankType.Measurement:
                case TankType.Stressor:
                    {
                        m_EnvironmentDisplay.gameObject.SetActive(false);
                        PopulateCritter(m_Result.Setup.CritterId);
                        PopulateProperty(m_Result.Setup.PropertyId);
                        break;
                    }

                default:
                    {
                        PopulateEnvironment(m_Result.Setup.EnvironmentId);
                        m_CritterDisplay.gameObject.SetActive(false);
                        m_PropertyDisplay.gameObject.SetActive(false);
                        break;
                    }
            }

            m_PropertiesLayout.ForceRebuild();

            if (m_Result.NewFactIds.Count > 0)
            {
                m_NewDiscoveriesGroup.gameObject.SetActive(true);
                m_NoDiscoveriesGroup.gameObject.SetActive(false);

                foreach(var factId in m_Result.NewFactIds)
                {
                    m_FactPools.Alloc(Services.Assets.Bestiary.Fact(factId), (BestiaryDesc) null);
                }
                m_BehaviorLayout.ForceRebuild();
            }
            else
            {
                m_NewDiscoveriesGroup.gameObject.SetActive(false);
                m_NoDiscoveriesGroup.gameObject.SetActive(true);
            }
        }

        public void SetResult(ExperimentResultData inResult)
        {
            m_Result = inResult;
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