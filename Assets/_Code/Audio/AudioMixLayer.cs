using System;
using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace AquaAudio
{
    public class AudioMixLayer : ScriptComponent
    {
        [SerializeField] private AudioMixLayerData m_Data = new AudioMixLayerData();
        [SerializeField, Range(0, 1)] private float m_InspectorFactor = 0;

        private void OnEnable() {
            Services.Audio.AddMixLayer(m_Data);
        }

        private void OnDisable() {
            Services.Audio?.RemoveMixLayer(m_Data);
        }

        [Preserve]
        private void OnDidApplyAnimationProperties() {
            m_Data.Factor = m_InspectorFactor;
        }
    }
}