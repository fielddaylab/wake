using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Observation
{
    public class PlayerROVWorldUI : MonoBehaviour
    {
        #region Inspector

        [Header("Cursor")]

        [SerializeField] private ColorGroup m_CursorGroup = null;
        [SerializeField] private SpriteAnimator m_CursorSprite = null;
        [SerializeField] private TweenSettings m_CursorFadeTween = default(TweenSettings);
        [SerializeField] private TweenSettings m_CursorBounceTween = default(TweenSettings);
        [SerializeField] private float m_CursorBounceTweenScale = 0.8f;

        [Header("Animations")]
        [SerializeField] private SpriteAnimation m_MoveAnimation = null;
        [SerializeField] private SpriteAnimation m_ScanAnimation = null;

        [Header("Cursor Types")]

        #endregion // Inspector

        [NonSerialized] private Routine m_CursorEnableAnim;
        [NonSerialized] private Routine m_CursorSwitchAnim;
        [NonSerialized] private bool m_CursorOn;
        [NonSerialized] private PlayerROVCursor m_CursorMode;

        public void ShowMoveArrow(Vector2 inOffset, float inPower)
        {
            SetCursorMode(PlayerROVCursor.Move);
            SetCursorOn(true);

            m_CursorGroup.transform.SetPosition(inOffset, Axis.XY, Space.Self);
            m_CursorGroup.transform.SetRotation(Mathf.Atan2(inOffset.y, inOffset.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);

            m_CursorSprite.Animation = m_MoveAnimation;
            m_CursorSprite.FrameIndex = Mathf.RoundToInt(Mathf.Clamp01(inPower) * (m_CursorSprite.Animation.FrameCount() - 1));
        }

        public void ShowScan(Vector2 inOffset, bool inbScanning)
        {
            SetCursorMode(PlayerROVCursor.Scan);
            SetCursorOn(true);

            m_CursorGroup.transform.SetPosition(inOffset, Axis.XY, Space.Self);
            m_CursorGroup.transform.SetRotation(0, Axis.Z, Space.Self);

            m_CursorSprite.Animation = m_ScanAnimation;
            m_CursorSprite.FrameIndex = inbScanning ? 1 : 0;
        }

        public void HideCursor()
        {
            SetCursorMode(PlayerROVCursor.None);
            SetCursorOn(false);
        }

        private void SetCursorOn(bool inbOn)
        {
            if (m_CursorOn == inbOn)
                return;

            m_CursorOn = inbOn;
            if (m_CursorOn)
            {
                m_CursorEnableAnim.Replace(this, ShowCursorAnim());
            }
            else
            {
                m_CursorEnableAnim.Replace(this, HideCursorAnim());
                m_CursorSwitchAnim.Stop();
            }

            Cursor.visible = !inbOn;
        }

        private void SetCursorMode(PlayerROVCursor inMode)
        {
            if (m_CursorMode != inMode)
            {
                m_CursorMode = inMode;
                if (m_CursorOn && inMode != PlayerROVCursor.None)
                    m_CursorSwitchAnim.Replace(this, CursorBounceAnim());
            }
        }

        private IEnumerator ShowCursorAnim()
        {
            if (!m_CursorGroup.gameObject.activeSelf)
            {
                m_CursorGroup.SetAlpha(0);
                m_CursorGroup.gameObject.SetActive(true);
            }

            if (m_CursorGroup.GetAlpha() < 1)
            {
                yield return Tween.Float(m_CursorGroup.GetAlpha(), 1, m_CursorGroup.SetAlpha, m_CursorFadeTween);
            }
        }

        private IEnumerator CursorBounceAnim()
        {
            yield return m_CursorGroup.transform.ScaleTo(m_CursorBounceTweenScale, m_CursorBounceTween, Axis.XY).From().ForceOnCancel();
        }

        private IEnumerator HideCursorAnim()
        {
            if (m_CursorGroup.gameObject.activeSelf)
            {
                yield return Tween.Float(m_CursorGroup.GetAlpha(), 0, m_CursorGroup.SetAlpha, m_CursorFadeTween);
                m_CursorGroup.gameObject.SetActive(false);
            }
        }
    }

    public enum PlayerROVCursor
    {
        None,
        Move,
        Scan
    }
}