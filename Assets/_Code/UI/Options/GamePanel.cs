using UnityEngine;
using AquaAudio;

namespace Aqua.Option
{
    public class GamePanel : OptionsDisplay.Panel 
    {
        #region Inspector

        [SerializeField] private CheckboxOption m_KeyboardShortcutsOption = null;

        #endregion // Inspector

        protected override void Init()
        {
            m_KeyboardShortcutsOption.Initialize("options.game.showKeyboardShortcuts.label",
                "options.game.showKeyboardShortcuts.tooltip", OnKeyboardShortcutsUpdated);
        }

        public override void Load(OptionsData inOptions)
        {
            base.Load(inOptions);

            m_KeyboardShortcutsOption.Sync(Accessibility.DisplayShortcuts);
        }

        private void OnKeyboardShortcutsUpdated(bool active) {
            Save.Options.Accessibility.SetFlag(OptionAccessibilityFlags.DisplayKeyboardShortcuts, active);
        }
    }
}