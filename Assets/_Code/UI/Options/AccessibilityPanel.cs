using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class AccessibilityPanel : OptionsDisplay.Panel
    {
        #region Inspector

        [SerializeField] private CheckboxOption m_PhotosensitiveOption = null;
        [SerializeField] private CheckboxOption m_ReduceCameraOption = null;
        
        [Header("Text-to-Speech")]
        [SerializeField] private LocText m_TTSLabel = null;
        [SerializeField] private CanvasGroup m_TTSGroup = null;
        [SerializeField] private ToggleOptionBar m_TTSToggle = null;
        [SerializeField] private CanvasGroup m_TTSEnabledGroup = null;
        [SerializeField] private SliderOption m_TTSRateSlider = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_PhotosensitiveOption.Initialize("options.accessibility.photosensitive.label",
                "options.accessibility.photosensitive.tooltip",
                (b) => { Data.Accessibility.SetFlag(OptionAccessibilityFlags.ReduceFlashing, b); });

            m_ReduceCameraOption.Initialize("options.accessibility.reduceCamera.label",
                "options.accessibility.reduceCamera.tooltip",
                (b) => { Data.Accessibility.SetFlag(OptionAccessibilityFlags.ReduceCameraMovement, b); });

            m_TTSToggle.Initialize<OptionsAccessibility.TTSMode>("options.accessibility.tts.label", "options.accessibility.tts.tooltip", OnTTSChanged)
                .AddOption("options.accessibility.tts.off.label", "options.accessibility.tts.off.tooltip", OptionsAccessibility.TTSMode.Off)
                .AddOption("options.accessibility.tts.tooltips.label", "options.accessibility.tts.tooltips.tooltip", OptionsAccessibility.TTSMode.Tooltips)
                .AddOption("options.accessibility.tts.full.label", "options.accessibility.tts.full.tooltip", OptionsAccessibility.TTSMode.Full)
                .Build();

            m_TTSRateSlider.Initialize("options.accessibility.ttsRate.label",
                "options.accessibility.ttsRate.tooltip",
                (f) => { Data.Accessibility.TTSRate = f; },
                0.25f, 2f, 1, 0.25f);
            m_TTSRateSlider.GenerateString = (f) => string.Format("{0}x", f);
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);

            m_PhotosensitiveOption.Sync(inOptions.Accessibility.HasFlag(OptionAccessibilityFlags.ReduceFlashing));
            m_ReduceCameraOption.Sync(inOptions.Accessibility.HasFlag(OptionAccessibilityFlags.ReduceCameraMovement));

            m_TTSToggle.Sync(inOptions.Accessibility.TTS);
            m_TTSRateSlider.Sync(inOptions.Accessibility.TTSRate);

            UpdateTTSGroup();
        }

        private void OnTTSChanged(OptionsAccessibility.TTSMode inTTS)
        {
            Data.Accessibility.TTS = inTTS;
            UpdateTTSGroup();
        }

        private void UpdateTTSGroup()
        {
            bool bHasTTS = TTS.IsAvailable();

            if (bHasTTS)
            {
                m_TTSLabel.SetText("options.accessibility.tts.available");

                m_TTSGroup.interactable = true;
                m_TTSGroup.alpha = 1;

                if (Data.Accessibility.TTS > 0)
                {
                    m_TTSEnabledGroup.interactable = true;
                    m_TTSEnabledGroup.alpha = 1;
                }
                else
                {
                    m_TTSEnabledGroup.interactable = false;
                    m_TTSEnabledGroup.alpha = 0.5f;
                }
            }
            else
            {
                m_TTSLabel.SetText("options.accessibility.notAvailable");

                m_TTSGroup.interactable = false;
                m_TTSGroup.alpha = 0.5f;

                m_TTSEnabledGroup.interactable = false;
                m_TTSEnabledGroup.alpha = 1;
            }
        }
    }
}