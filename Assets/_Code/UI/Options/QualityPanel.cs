using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace Aqua.Option
{
    public class QualityPanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private ToggleOptionBar m_ResolutionLevel = null;
        [SerializeField] private CheckboxOption m_FullscreenToggle = null;
        [SerializeField] private ToggleOptionBar m_AnimationQuality = null;
        [SerializeField] private ToggleOptionBar m_ParticleQuality = null;
        [SerializeField] private ButtonOption m_AutoDetectButton = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_ResolutionLevel.Initialize<OptionsPerformance.ResolutionMode>("options.quality.resolution.label",
                "options.quality.resolution.tooltip", OnResolutionLevelChanged)
                .AddOption("options.quality.resolution.min.label", "options.quality.resolution.min.tooltip", OptionsPerformance.ResolutionMode.Minimum)
                .AddOption("options.quality.resolution.moderate.label", "options.quality.resolution.moderate.tooltip", OptionsPerformance.ResolutionMode.Moderate)
                .AddOption("options.quality.resolution.high.label", "options.quality.resolution.high.tooltip", OptionsPerformance.ResolutionMode.High)
                .Build();

            m_AnimationQuality.Initialize<OptionsPerformance.FeatureMode>("options.quality.animation.label",
                "options.quality.animation.tooltip", OnAnimationQualityChanged)
                .AddOption("options.quality.animation.low.label", "options.quality.animation.low.tooltip", OptionsPerformance.FeatureMode.Low)
                .AddOption("options.quality.animation.medium.label", "options.quality.animation.medium.tooltip", OptionsPerformance.FeatureMode.Medium)
                .AddOption("options.quality.animation.high.label", "options.quality.animation.high.tooltip", OptionsPerformance.FeatureMode.High)
                .Build();

            m_ParticleQuality.Initialize<OptionsPerformance.FeatureMode>("options.quality.effects.label",
                "options.quality.effects.tooltip", OnParticleQualityChanged)
                .AddOption("options.quality.effects.low.label", "options.quality.effects.low.tooltip", OptionsPerformance.FeatureMode.Low)
                .AddOption("options.quality.effects.medium.label", "options.quality.effects.medium.tooltip", OptionsPerformance.FeatureMode.Medium)
                .AddOption("options.quality.effects.high.label", "options.quality.effects.high.tooltip", OptionsPerformance.FeatureMode.High)
                .Build();

            m_FullscreenToggle.Initialize("options.quality.fullscreen.label",
                "options.quality.fullscreen.tooltip", OnFullscreenChanged);

            if (m_AutoDetectButton) {
                m_AutoDetectButton.Initialize("options.quality.autoDetect.label", "options.quality.autoDetect.tooltip", OnAutoDetectClicked);
            }
        }

        private void OnEnable() {
            Services.Camera.OnFullscreenChanged.Register(OnFullscreenUpdated);
        }

        private void OnDisable() {
            if (Services.Valid) {
                Services.Camera?.OnFullscreenChanged.Deregister(OnFullscreenUpdated);
            }
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);
            
            m_ResolutionLevel.Sync(inOptions.Performance.Resolution);
            m_ParticleQuality.Sync(inOptions.Performance.EffectsQuality);
            m_AnimationQuality.Sync(inOptions.Performance.AnimationQuality);

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

        private void OnAnimationQualityChanged(OptionsPerformance.FeatureMode inQuality)
        {
            Data.Performance.AnimationQuality = inQuality;

            Services.Events.Queue(GameEvents.OptionsUpdated, Data);
        }

        private void OnParticleQualityChanged(OptionsPerformance.FeatureMode inQuality)
        {
            Data.Performance.EffectsQuality = inQuality;

            Services.Events.Queue(GameEvents.OptionsUpdated, Data);
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
        
        private void OnAutoDetectClicked() {
            if (SystemInfo.graphicsMemorySize < 64 || SystemInfo.graphicsShaderLevel < 25 || SystemInfo.maxTextureSize < 9000) {
                Data.Performance.Resolution = OptionsPerformance.ResolutionMode.Minimum;
                Data.Performance.AnimationQuality = OptionsPerformance.FeatureMode.Low;
            } else if (SystemInfo.systemMemorySize >= 128 && SystemInfo.graphicsMemorySize >= 256 && SystemInfo.maxTextureSize > 9000) {
                Data.Performance.Resolution = OptionsPerformance.ResolutionMode.High;
                Data.Performance.AnimationQuality = OptionsPerformance.FeatureMode.High;
            } else {
                Data.Performance.Resolution = OptionsPerformance.ResolutionMode.Moderate;
                Data.Performance.AnimationQuality = OptionsPerformance.FeatureMode.Medium;
            }
            
            Load(Data);
        }

        #if UNITY_WEBGL

        [DllImport("__Internal")]
        static private extern void NativeFullscreen_SetFullscreen(bool fullscreen);

        #endif // UNITY_WEBGL
    }
}