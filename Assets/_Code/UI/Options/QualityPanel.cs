using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class QualityPanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_QualityLevel = null;
        [SerializeField] private ButtonOption m_AutoDetectButton = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_QualityLevel.Initialize<OptionsPerformance.QualityMode>("options.quality.res.label",
                "options.quality.res.tooltip", OnResolutionLevelChanged)
                .AddOption("options.quality.res.low.label", "options.quality.res.low.tooltip", OptionsPerformance.QualityMode.Low)
                .AddOption("options.quality.res.medium.label", "options.quality.res.medium.tooltip", OptionsPerformance.QualityMode.Medium)
                .AddOption("options.quality.res.high.label", "options.quality.res.high.tooltip", OptionsPerformance.QualityMode.High)
                .Build();

            m_AutoDetectButton.Initialize("options.quality.autodetect.label", "options.quality.autodetect.tooltip", OnAutoDetectClick);
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_QualityLevel.Sync(inOptions.Performance.Resolution);
        }

        private void OnResolutionLevelChanged(OptionsPerformance.QualityMode inResolution)
        {
            Data.Performance.Resolution = inResolution;
        }

        private void OnAutoDetectClick()
        {
            Data.Performance = Perf.GenerateDefaultPerformanceSettings();

            OptionsData options = Save.Options;
            options.SetDirty();
            
            Services.Events.QueueForDispatch(GameEvents.OptionsUpdated, options);

            Load(Data);
        }
    }
}