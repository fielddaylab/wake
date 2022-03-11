using System;
using System.Collections;
using Aqua.Animation;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class SaveIcon : MonoBehaviour
    {
        static private readonly TextId Label_Saving = "ui.save.saving";
        static private readonly TextId Label_SaveComplete = "ui.save.complete";
        
        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private SpriteAnimator m_Icon = null;
        [SerializeField] private LocText m_Label = null;

        [Header("Animations")]
        [SerializeField] private SpriteAnimation m_SavingAnimation = null;
        [SerializeField] private SpriteAnimation m_SaveCompleteAnimation = null;

        #endregion // Inspector

        private float m_DisplayTime;
        private Routine m_DisplayRoutine;

        private void Awake()
        {
            m_Canvas.enabled = false;
            m_Group.alpha = 0;
            m_Icon.Pause();

            Services.Events.Register(GameEvents.ProfileSaveBegin, OnSaveBegin, this)
                .Register(GameEvents.ProfileSaveCompleted, OnSaveComplete, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnSaveBegin()
        {
            m_DisplayTime = Time.realtimeSinceStartup;
            m_DisplayRoutine.Replace(this, Show()).Tick();
        }

        private void OnSaveComplete()
        {
            m_DisplayRoutine.Replace(this, Complete()).Tick();
        }

        private IEnumerator Show()
        {
            m_DisplayTime = Time.realtimeSinceStartup;

            m_Canvas.enabled = true;
            m_Icon.Play(m_SavingAnimation, true);
            m_Label.SetText(Label_Saving);
            yield return m_Group.FadeTo(1, 0.1f);
        }

        private IEnumerator Complete()
        {
            m_Label.SetText(Label_SaveComplete);
            float tsThreshold = m_DisplayTime + m_SavingAnimation.TotalDuration();

            if (m_Group.alpha < 1)
                yield return m_Group.FadeTo(1, (1 - m_Group.alpha) * 0.1f);
            
            while(Time.realtimeSinceStartup < tsThreshold || m_Icon.FrameIndex != 0)
                yield return null;

            m_Icon.Play(m_SaveCompleteAnimation);
            while(m_Icon.IsPlaying())
                yield return null;

            yield return m_Group.FadeTo(0, 0.1f).DelayBy(0.1f);
            m_Canvas.enabled = false;
        }
    }
}