using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class PropertySlider : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Slider m_Slider = null;
        [SerializeField] private Image m_Icon = null;

        #endregion // Inspector
        [NonSerialized] private WaterPropertyId m_Id;

        [NonSerialized] private StringHash32 m_LabelId;

        public Slider Slider { get { return m_Slider; } }
        public WaterPropertyId Id { get { return m_Id; } }
        public StringHash32 LabelId { get { return m_LabelId; } }

        public void Load(WaterPropertyDesc PropertyDesc, Sprite inIcon, bool inbInteractable)
        {
            m_Id = PropertyDesc?.Index() ?? WaterPropertyId.MAX;
            m_Icon.sprite = inIcon;
            m_LabelId = PropertyDesc?.LabelId() ?? StringHash32.Null;

        }
    }
}