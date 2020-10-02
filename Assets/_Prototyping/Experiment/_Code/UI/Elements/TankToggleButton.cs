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
    public class TankToggleButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Toggle m_Toggle = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector

        [NonSerialized] private TankType m_TankType;

        public Toggle Toggle { get { return m_Toggle; } }
        public TankType Type { get { return m_TankType; } }

        public void Load(ExperimentSettings.TankDefinition inDef, bool inbInteractible)
        {
            m_TankType = inDef.Tank;
            m_Icon.sprite = inDef.Icon;
            m_ColorGroup.BlocksRaycasts = inbInteractible;
            m_Toggle.interactable = inbInteractible;
            m_ColorGroup.Color = Services.Tweaks.Get<ExperimentSettings>().SetupButtonColor(inbInteractible);
        }
    }
}