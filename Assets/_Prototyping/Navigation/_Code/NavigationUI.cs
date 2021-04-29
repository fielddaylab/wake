using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace ProtoAqua.Navigation
{
    public class NavigationUI : SharedPanel
    {
        static public readonly StringHash32 Event_Dive = "nav:dive";

        static private readonly StringHash32 DiveTooltipKey = "ui.nav.dive.tooltip";
        static private readonly StringHash32 DockTooltipKey = "ui.nav.dock.tooltip";
        static private readonly StringHash32 DockLabelKey = "ui.nav.dock.label";
        
        private enum Mode
        {
            Dive,
            Dock
        }

        #region Inspector

        [SerializeField] private Button m_InteractButton = null;
        [SerializeField] private LocText m_InteractLabel = null;
        [SerializeField] private Image m_InteractButtonIcon = null;
        [SerializeField] private CursorInteractionHint m_HoverHint = null; 

        [Header("Assets")]
        [SerializeField] private Sprite m_DiveIcon = null;
        [SerializeField] private Sprite m_DockIcon = null;

        #endregion // Inspector

        [NonSerialized] private string m_TargetScene;
        [NonSerialized] private Mode m_Mode;

        protected override void Start()
        {
            base.Start();

            m_InteractButton.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            switch(m_Mode)
            {
                case Mode.Dive:
                    {
                        Hide();
                        Services.Events.Dispatch(GameEvents.BeginDive);
                        Routine.Start(FadeRoutine());
                        break;
                    }

                case Mode.Dock:
                    {
                        Hide();
                        StateUtil.LoadSceneWithWipe("Ship", null);
                        break;
                    }
            }
        }

        public void DisplayDive(string inLabel, string inScene)
        {
            m_Mode = Mode.Dive;
            m_TargetScene = inScene;
            m_InteractButtonIcon.sprite = m_DiveIcon;
            m_HoverHint.TooltipId = DiveTooltipKey;
            m_InteractLabel.SetText(inLabel);

            Show();
        }

        public void DisplayDock()
        {
            m_Mode = Mode.Dock;
            m_TargetScene = null;
            m_InteractButtonIcon.sprite = m_DockIcon;
            m_HoverHint.TooltipId = DockTooltipKey;
            m_InteractLabel.SetText(DockLabelKey);
            
            Show();
        }

        private IEnumerator FadeRoutine()
        {
            Services.UI.ShowLetterbox();
            Services.Data.SetVariable(GameVars.DiveSite, m_TargetScene);
            Services.Events.Dispatch(Event_Dive, m_TargetScene);
            yield return 2;
            StateUtil.LoadSceneWithWipe(m_TargetScene);
            yield return 0.3f;
            Services.UI.HideLetterbox();
        }
    }
}
