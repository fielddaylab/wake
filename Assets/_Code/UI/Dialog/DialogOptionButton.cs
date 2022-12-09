using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using Aqua.Scripting;
using BeauUtil;
using Leaf;
using Leaf.Runtime;
using BeauUtil.Variants;
using BeauUtil.Debugger;
using Aqua.Portable;

namespace Aqua
{
    public class DialogOptionButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private LayoutOffset m_Offset = null;
        [SerializeField, Required] private CanvasGroup m_Group = null;
        [SerializeField, Required] private TMP_Text m_Text = null;
        [SerializeField, Required] private Button m_Button = null;

        [Header("Animation")]

        [SerializeField] private Vector2 m_ToOnOffset = Vector2.zero;
        [SerializeField] private TweenSettings m_ToOnTween = default(TweenSettings);
        [SerializeField] private Vector2 m_ToOffOffset = Vector2.zero;
        [SerializeField] private TweenSettings m_ToOffTween = default(TweenSettings);

        #endregion // Inspector

        [NonSerialized] private Variant m_Option;
        [NonSerialized] private Routine m_Anim;

        [NonSerialized] private LeafChoice m_Choice;
        [NonSerialized] private bool m_SelectorMode;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            m_Choice = null;
            m_Anim.Stop();
            m_Option = Variant.Null;
        }

        public void Populate(Variant inOption, string inText, LeafChoice inChoice, bool inbSelectorMode)
        {
            m_Option = inOption;
            m_Text.SetText(inText);
            m_Choice = inChoice;
            m_Group.alpha = 0;
            m_SelectorMode = inbSelectorMode;
        }

        public void Prep()
        {
            m_Group.alpha = 0;
            m_Group.blocksRaycasts = false;
            m_Offset.Offset0 = default(Vector2);
        }

        public IEnumerator AnimateOn(float inDelay)
        {
            yield return inDelay;
            yield return Routine.Combine(
                m_Group.FadeTo(1, m_ToOnTween.Time),
                Tween.Vector(m_Offset.Offset0, m_ToOnOffset, (v) => m_Offset.Offset0 = v, m_ToOnTween).From()
            );

            m_Group.blocksRaycasts = true;
        }

        public IEnumerator AnimateOff(float inDelay)
        {
            m_Group.blocksRaycasts = false;
            yield return inDelay;
            yield return Routine.Combine(
                m_Group.FadeTo(0, m_ToOffTween.Time),
                Tween.Vector(m_Offset.Offset0, m_ToOffOffset, (v) => m_Offset.Offset0 = v, m_ToOffTween)
            );
        }

        private void OnClick() {
            Assert.NotNull(m_Choice);
            Future<StringHash32> selectorFuture = null;
            if (m_SelectorMode && Services.Script.TryHandleChoiceSelector(m_Option.AsStringHash(), out selectorFuture)) {
                selectorFuture.OnComplete(OnSelectorSelected);
            } else {
                m_Choice.Choose(m_Option);
                m_Group.blocksRaycasts = false;
            }
        }

        private void OnSelectorSelected(StringHash32 inSelected) {
            Assert.NotNull(m_Choice);
            m_Choice.Choose(m_Option, inSelected);
            m_Group.blocksRaycasts = false;
        }
    }
}