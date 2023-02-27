using UnityEngine;
using System.Runtime.InteropServices;

namespace Aqua.Option
{
    public class QualityPanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_QualityLevel = null;
        [SerializeField] private ToggleOptionBar m_ResolutionLevel = null;
        [SerializeField] private CheckboxOption m_FullscreenToggle = null;

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

            m_FullscreenToggle.Initialize("options.quality.fullscreen.label",
                "options.quality.fullscreen.tooltip", OnFullscreenChanged);
        }

        private void OnEnable() {
            Services.Camera.OnFullscreenChanged.Register(OnFullscreenUpdated);
        }

        private void OnDisable() {
            Services.Camera.OnFullscreenChanged.Deregister(OnFullscreenUpdated);
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_QualityLevel.Sync(inOptions.Performance.Framerate);
            m_ResolutionLevel.Sync(inOptions.Performance.Resolution);

            m_FullscreenToggle.Sync(Screen.fullScreen);
        }

        private void OnQualityLevelChanged(OptionsPerformance.FramerateMode inFramerate)
        {
            Data.Performance.Framerate = inFramerate;
        }

        private void OnResolutionLevelChanged(OptionsPerformance.ResolutionMode inResolution)
        {
            Data.Performance.Resolution = inResolution;
        }

        private void OnFullscreenChanged(bool fullscreen) {
            Screen.fullScreen = fullscreen;
            #if UNITY_WEBGL && !UNITY_EDITOR
            NativeFullscreen_SetFullscreen(fullscreen);
            #endif // UNITY_WEBGL && !UNITY_EDITOR
        }

        private void OnFullscreenUpdated(bool fullscreen) {
            m_FullscreenToggle.Sync(fullscreen);
        }
        
        #if UNITY_WEBGL

        [DllImport("__Internal")]
        static private extern void NativeFullscreen_SetFullscreen(bool fullscreen);

        #endif // UNITY_WEBGL
    }
}