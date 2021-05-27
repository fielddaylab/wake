using System;
using UnityEngine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class SetupToggleButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private ColorGroup m_ColorGroup = null;
        [SerializeField, Required] private CursorInteractionHint m_CursorHint = null;
        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector
        [NonSerialized] private Variant m_Id;

        public Toggle Toggle { get { return m_Toggle; } }
        public Variant Id { get { return m_Id; } }

        public void Load(Variant inId, Sprite inIcon, StringHash32 inLabel, bool inbInteractable)
        {
            m_Id = inId;
            m_Icon.sprite = inIcon;
            m_ColorGroup.BlocksRaycasts = inbInteractable;
            m_Toggle.interactable = inbInteractable;
            m_CursorHint.TooltipId = inLabel;
            if (m_Label)
                m_Label.SetText(inLabel);
            m_ColorGroup.Color = Services.Tweaks.Get<ExperimentSettings>().SetupButtonColor(inbInteractable);
            m_Toggle.SetIsOnWithoutNotify(false);
        }
    }
}