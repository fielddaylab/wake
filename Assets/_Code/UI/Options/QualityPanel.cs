using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class QualityPanel : OptionsMenu.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_QualityLevel = null;

        #endregion // Inspector

        private void Awake()
        {
            m_QualityLevel.Initialize<OptionsPerformance.FramerateMode>("ui.options.quality.level.label",
                "ui.options.quality.level.tooltip", OnQualityLevelChanged)
                .AddOption("ui.options.quality.level.stable.label", "ui.options.quality.level.stable.tooltip", OptionsPerformance.FramerateMode.Stable)
                .AddOption("ui.options.quality.level.high.label", "ui.options.quality.level.high.tooltip", OptionsPerformance.FramerateMode.High)
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