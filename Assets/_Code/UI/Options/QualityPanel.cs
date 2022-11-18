using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class QualityPanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_QualityLevel = null;
        [SerializeField] private ToggleOptionBar m_ResolutionLevel = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_QualityLevel.Initialize<OptionsPerformance.FramerateMode>("options.quality.level.label",
                "options.quality.level.tooltip", OnQualityLevelChanged)
                .AddOption("options.quality.level.stable.label", "options.quality.level.stable.tooltip", OptionsPerformance.FramerateMode.Stable)
                .AddOption("options.quality.level.high.label", "options.quality.level.high.tooltip", OptionsPerformance.FramerateMode.High)
                .Build();

            m_ResolutionLevel.Initialize<OptionsPerformance.ResolutionMode>("options.quality.resolution.label",
                "options.quality.resolution.tooltip", OnResolutionLevelChanged)
                .AddOption("options.quality.resolution.min.label", "options.quality.resolution.min.tooltip", OptionsPerformance.ResolutionMode.Minimum)
                .AddOption("options.quality.resolution.moderate.label", "options.quality.resolution.moderate.tooltip", OptionsPerformance.ResolutionMode.Moderate)
                .AddOption("options.quality.resolution.high.label", "options.quality.resolution.high.tooltip", OptionsPerformance.ResolutionMode.High)
                .Build();
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_QualityLevel.Sync(inOptions.Performance.Framerate);
            m_ResolutionLevel.Sync(inOptions.Performance.Resolution);
        }

        private void OnQualityLevelChanged(OptionsPerformance.FramerateMode inFramerate)
        {
            Data.Performance.Framerate = inFramerate;
        }

        private void OnResolutionLevelChanged(OptionsPerformance.ResolutionMode inResolution)
        {
            Data.Performance.Resolution = inResolution;
        }
    }
}