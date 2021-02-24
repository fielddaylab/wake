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
    public class ReturnButtonWrapper : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Button = null;

        private void Awake()
        {
            Services.Events.Register(ExperimentEvents.ExperimentBegin, OnBegin, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnEnd, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnBegin()
        {
            m_Button.gameObject.SetActive(false);
        }

        private void OnEnd()
        {
            m_Button.gameObject.SetActive(true);
        }
    }
}