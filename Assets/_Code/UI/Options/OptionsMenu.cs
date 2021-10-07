using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using AquaAudio;
using Aqua.Scripting;
using BeauUtil;
using TMPro;

namespace Aqua.Option 
{
    public class OptionsMenu : SharedPanel 
    {
        public class Panel : MonoBehaviour
        {
            public OptionsData Data { get; private set; }

            public virtual void Load(OptionsData inOptions) { Data = inOptions; }
        }

        #region Inspector

        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private TMP_Text m_UserNameLabel = null;

        [Header("Pages")]
        [SerializeField] private AudioPanel m_AudioPanel = null;
        [SerializeField] private GamePanel m_GamePanel = null;
        [SerializeField] private QualityPanel m_QualityPanel = null;
        [SerializeField] private AccessibilityPanel m_AccessibilityPanel = null;

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

            if (m_UserNameLabel)
            {
                m_UserNameLabel.SetText(Services.Data.Profile.Id);
            }

            if (m_InputLayer.PushPriority())
            {
                Services.Pause.Pause();
            }
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);

            LoadOptions(Services.Data.Options);
        }

        protected override void OnHide(bool inbInstant)
        {
            if (Services.Valid)
            {
                Services.Data?.SaveOptionsSettings();
            }

            if (m_InputLayer.PopPriority())
            {
                Services.Pause?.Resume();
            }
            
            base.OnHide(inbInstant);
        }

        private void LoadOptions(OptionsData inData)
        {
            m_AudioPanel.Load(inData);
            m_GamePanel.Load(inData);
            m_QualityPanel.Load(inData);
            m_AccessibilityPanel.Load(inData);
        }
    }
}