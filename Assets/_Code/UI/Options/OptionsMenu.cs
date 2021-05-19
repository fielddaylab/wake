using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using AquaAudio;
using Aqua.Scripting;

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

        [NonSerialized] private BaseInputLayer m_InputLayer = null;

        protected override void Awake() 
        {
            base.Awake();

            m_CloseButton.onClick.AddListener(() => Hide());

            m_InputLayer = BaseInputLayer.Find(this);
            Services.Events.Register(GameEvents.SceneWillUnload, InstantHide);
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
            base.OnDestroy();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            if (m_InputLayer.PushPriority())
            {
                Services.Pause.Pause();
            }

            var sources = OptionAudio.Sources();
            int i = 0;
            foreach(var sound in m_SoundGroup.GetComponentsInChildren<SoundOptions>()) 
            {
                AudioBusId busId = sources[i++];
                sound.Initialize(busId, Services.Data.Options.Audio[busId]);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            if (Services.Valid)
            {
                if (Services.Data.Options.HasChanges())
                {
                    AutoSave.Force();
                }
            }

            if (m_InputLayer.PopPriority())
            {
                Services.Pause?.Resume();
            }
            
            base.OnHide(inbInstant);
        }
    }
}