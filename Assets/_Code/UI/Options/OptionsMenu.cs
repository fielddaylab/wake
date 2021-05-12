using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using AquaAudio;
namespace Aqua.Option 
{
    public class OptionsMenu : SharedPanel 
    {

        #region Inspector

        [SerializeField] public CanvasGroup m_Canvas;
        [SerializeField] public RectTransform m_Group;
        [SerializeField] public Button m_CloseButton;
        [SerializeField] public Transform m_SoundGroup;

        #endregion //Inspector

        protected override void Awake() {
            base.Awake();

            if(!Services.Data.IsOptionsLoaded())
            {
                Services.Data.LoadOptionsSettings();
            }

            m_CloseButton.onClick.AddListener(Hide);
        }

        public void Show() 
        {
            base.Show();

            SetInputState(true);

            var sources = AudioSettings.Sources();
            int i = 0;
            foreach(var sound in m_SoundGroup.GetComponentsInChildren<SoundOptions>()) 
            {
                var source = sources[i++];
                sound.Initialize(source, Services.Data.Settings.GetBusMix(source));
            }
        }

        public void Hide()
        {
            base.Hide();
            SetInputState(false);

            Services.Events.Dispatch(GameEvents.OptionsClosed);
        }
    }
}