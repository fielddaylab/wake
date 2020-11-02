using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class SetupToggleButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Toggle m_Toggle = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector

        [NonSerialized] private Variant m_Id;

        public Toggle Toggle { get { return m_Toggle; } }
        public Variant Id { get { return m_Id; } }

        public void Load(Variant inId, Sprite inIcon, bool inbInteractable)
        {
            m_Id = inId;
            m_Icon.sprite = inIcon;
            m_ColorGroup.BlocksRaycasts = inbInteractable;
            m_Toggle.interactable = inbInteractable;
            m_ColorGroup.Color = Services.Tweaks.Get<ExperimentSettings>().SetupButtonColor(inbInteractable);
            m_Toggle.SetIsOnWithoutNotify(false);
        }
    }
}