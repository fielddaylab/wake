using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class QualityPanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_QualityLevel = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_QualityLevel.Initialize<OptionsPerformance.FramerateMode>("options.quality.level.label",
                "options.quality.level.tooltip", OnQualityLevelChanged)
                .AddOption("options.quality.level.stable.label", "options.quality.level.stable.tooltip", OptionsPerformance.FramerateMode.Stable)
                .AddOption("options.quality.level.high.label", "options.quality.level.high.tooltip", OptionsPerformance.FramerateMode.High)
                .Build();
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_QualityLevel.Sync(inOptions.Performance.Framerate);
        }

        private void OnQualityLevelChanged(OptionsPerformance.FramerateMode inFramerate)
        {
            Data.Performance.Framerate = inFramerate;
        }
    }
}