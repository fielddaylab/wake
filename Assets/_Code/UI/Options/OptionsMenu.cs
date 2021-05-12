using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using AquaAudio;
namespace Aqua.Option 
{
    public class OptionsMenu : BasePanel 
    {

        #region Inspector

        [SerializeField] public CanvasGroup m_Canvas;
        [SerializeField] public RectTransform m_Group;
        [SerializeField] public Button m_CloseButton;
        [SerializeField] public Transform m_SoundGroup;

        #endregion //Inspector

        [NonSerialized]
        private AudioBusId[] m_Ids = new AudioBusId[3]{
            AudioBusId.Master,
            AudioBusId.SFX,
            AudioBusId.Music
        };

        protected override void Awake() {
            base.Awake();

            m_CloseButton.onClick.AddListener(Hide);
        }

        public void Show() 
        {
            base.Show();

            SetInputState(true);
            int i = 0;
            foreach(var sound in m_SoundGroup.GetComponentsInChildren<SoundOptions>()) 
            {
                sound.Initialize(m_Ids[i++]);
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