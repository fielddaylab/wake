using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Aqua;
using System;
using BeauPools;

namespace ProtoAqua.Argumentation
{
    public class ArgueChatLine : MonoBehaviour, IPoolConstructHandler
    {
        #region Inspector

        [SerializeField] private HorizontalLayoutGroup m_Layout = null;
        [SerializeField] private Graphic m_Background = null;
        [SerializeField] private LocText m_Text = null;

        [Header("Tail")]
        [SerializeField] private RectTransform m_TailTransform = null;
        [SerializeField] private Graphic m_TailGraphic = null;

        #endregion // Inspector

        [NonSerialized] private Color m_DefaultBackground;
        [NonSerialized] private Color m_DefaultText;
        [NonSerialized] private float m_TailOffset;

        #region Commands

        public void SetColors(ColorPalette4 inColors)
        {
            m_Background.SetColor(inColors.Background);
            m_TailGraphic.SetColor(inColors.Background);
            m_Text.Graphic.SetColor(inColors.Content);
        }

        public void SetColors(Color inText, Color inBackground)
        {
            m_Background.SetColor(inBackground);
            m_TailGraphic.SetColor(inBackground);
            m_Text.Graphic.SetColor(inText);
        }

        public void OverrideColors(Color? inText, Color? inBackground)
        {
            if (inBackground.HasValue)
            {
                m_Background.SetColor(inBackground.Value);
                m_TailGraphic.SetColor(inBackground.Value);
            }
            if (inText.HasValue)
            {
                m_Text.Graphic.SetColor(inText.Value);
            }
        }

        public void Populate(string inText, ScriptCharacterDef inActor)
        {
            m_Text.SetText(inText);

            ColorPalette4? palette = inActor.TextPaletteOverride();
            if (palette != null)
            {
                SetColors(palette.Value);
            }
            else
            {
                SetColors(m_DefaultText, m_DefaultBackground);
            }

            if (inActor.HasFlags(ScriptActorTypeFlags.IsPlayer))
            {
                m_Layout.childAlignment = TextAnchor.MiddleRight;
                Vector2 anchor = new Vector2(1, 0);
                m_TailTransform.anchorMin = m_TailTransform.anchorMax = anchor;
                m_TailTransform.SetAnchorPos(-m_TailOffset, Axis.X);
            }
            else
            {
                m_Layout.childAlignment = TextAnchor.MiddleLeft;
                Vector2 anchor = new Vector2(0, 0);
                m_TailTransform.anchorMin = m_TailTransform.anchorMax = anchor;
                m_TailTransform.SetAnchorPos(m_TailOffset, Axis.X);
            }

            m_Layout.ForceRebuild();
        }

        public IEnumerator Shake()
        {
            RectTransform r = m_Layout.GetComponent<RectTransform>();
            yield return r.AnchorPosTo(r.anchoredPosition.x + 16, 0.3f, Axis.X).Wave(Wave.Function.CosFade, 3);
        }

        #endregion // Commands

        #region IPoolConstructHandler

        void IPoolConstructHandler.OnConstruct()
        {
            m_DefaultBackground = m_Background.color;
            m_DefaultText = m_Text.Graphic.color;
            m_TailOffset = Math.Abs(m_TailTransform.anchoredPosition.x);
        }

        void IPoolConstructHandler.OnDestruct()
        {
        }

        #endregion // IPoolConstructHandler
    }
}
