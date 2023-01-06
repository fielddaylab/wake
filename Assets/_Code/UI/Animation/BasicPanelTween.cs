using UnityEngine;
using BeauRoutine.Extensions;
using BeauRoutine;
using System.Collections;
using BeauUtil;

namespace Aqua
{
    public class BasicPanelTween : BasePanelAnimator
    {
        #region Inspector

        [SerializeField, Required] private RectTransform m_Transform = null;
        [SerializeField, Required] private CanvasGroup m_Group = null;

        [Header("Anim Settings")]

        [SerializeField] private Vector2 m_InitialOffset = new Vector2(0, -40);
        [SerializeField] private TweenSettings m_ShowAnimSettings = new TweenSettings(0.2f);
        [SerializeField] private Vector2 m_HideOffset = new Vector2(0, -40);
        [SerializeField] private TweenSettings m_HideAnimSettings = new TweenSettings(0.2f);

        #endregion // Inspector

        protected override void InstantShow()
        {
            m_Group.gameObject.SetActive(true);
            m_Group.alpha = 1;
            m_Transform.SetAnchorPos(0, Axis.Y);
        }

        protected override IEnumerator Show()
        {
            m_Group.alpha = 0;
            m_Group.gameObject.SetActive(true);
            m_Transform.SetAnchorPos(m_InitialOffset);
            yield return Routine.Combine(
                m_Transform.AnchorPosTo(0, m_ShowAnimSettings, Axis.XY),
                m_Group.FadeTo(1, m_ShowAnimSettings.Time)
            );
        }

        protected override void InstantHide()
        {
            m_Group.gameObject.SetActive(false);
            m_Group.alpha = 0;
            m_Transform.SetAnchorPos(0, Axis.Y);
        }

        protected override IEnumerator Hide()
        {
            yield return Routine.Combine(
                m_Transform.AnchorPosTo(m_HideOffset, m_HideAnimSettings, Axis.Y),
                m_Group.FadeTo(0, m_HideAnimSettings.Time)
            );

            m_Group.gameObject.SetActive(false);
        }
    }
}