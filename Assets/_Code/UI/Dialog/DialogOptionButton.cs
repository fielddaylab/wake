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

namespace Aqua
{
    public class DialogOptionButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_Transform = null;
        [SerializeField] private LayoutElement m_Layout = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private TMP_Text m_Text = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Option;

        public Button Button { get; }
        public StringHash32 Id { get; }

        public void Populate(StringHash32 inOption, string inText)
        {
            m_Option = inOption;
            m_Text.SetText(inText);
        }
    }
}