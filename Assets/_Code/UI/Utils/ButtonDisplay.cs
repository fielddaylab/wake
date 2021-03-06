using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class ButtonDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Image m_Icon = null;
        [SerializeField] private TMP_Text m_Text = null;

        #endregion // Inspector

        [NonSerialized] private RectTransform m_IconRect;
        [NonSerialized] private Routine m_Anim;

        private void Awake()
        {
            m_Icon.CacheComponent(ref m_IconRect);
        }

        private void OnDisable()
        {
            m_Anim.Stop();
        }

        public void Assign(KeyCode inKeyCode)
        {
            m_Icon.CacheComponent(ref m_IconRect);

            KeycodeDisplayMap.Mapping mapping = Services.UI.KeycodeMap.ForKey(inKeyCode);
            Vector2 sizeDelta = m_IconRect.sizeDelta;
            Rect spriteSize = mapping.Image.rect;
            sizeDelta.x = sizeDelta.y * spriteSize.width / spriteSize.height;

            m_Icon.sprite = mapping.Image;
            if (string.IsNullOrEmpty(mapping.Text))
            {
                m_Text.gameObject.SetActive(false);
            }
            else
            {
                m_Text.SetText(mapping.Text);
                m_Text.gameObject.SetActive(true);
            }
        }

        public void PlayAnimation()
        {
            m_Anim.Replace(this, PressAnimation());
        }

        private IEnumerator PressAnimation()
        {
            return m_IconRect.ScaleTo(0.92f, 0.08f, Axis.XY).Yoyo().Ease(Curve.CubeInOut).RevertOnCancel();
        }
    }
}