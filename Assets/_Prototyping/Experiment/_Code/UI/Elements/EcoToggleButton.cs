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
    public class EcoToggleButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Toggle m_Toggle = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_EcoId;

        public Toggle Toggle { get { return m_Toggle; } }
        public StringHash32 EcoId { get { return m_EcoId; } }

        public void Load(ExperimentSettings.EcoDefinition inDef, bool inbInteractible)
        {
            m_EcoId = inDef.Id;
            m_Icon.sprite = inDef.Icon;
            m_ColorGroup.BlocksRaycasts = inbInteractible;
            m_Toggle.interactable = inbInteractible;
            m_ColorGroup.Color = Services.Tweaks.Get<ExperimentSettings>().SetupButtonColor(inbInteractible);
        }
    }
}