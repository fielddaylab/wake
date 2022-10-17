using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua.Scripting {
    public sealed class ScriptLight : ScriptComponent, IScenePreloader {
        public enum Mode {
            GlobalAmbient,
            SingleLight
        }

        #region Inspector

        [SerializeField] private Mode m_Mode = Mode.GlobalAmbient;
        [SerializeField, ShowIfField("ShowLightRef")] private Light m_Light = null;

        #endregion // Inspector

        [NonSerialized] private float m_OriginalIntensity;
        [NonSerialized] private Color m_OriginalColor;
        [NonSerialized] private float m_OriginalRange;

        [NonSerialized] private float m_IntensityMultiplier = 1;
        [NonSerialized] private Color? m_ColorOverride;
        [NonSerialized] private float m_RangeMultiplier = 1;

        private Routine m_IntensityTransition;
        private Routine m_ColorTransition;
        private Routine m_RangeTransition;

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            ReadInitialState();
            return null;
        }

        private void ReadInitialState() {
            switch(m_Mode) {
                case Mode.GlobalAmbient: {
                    m_OriginalIntensity = RenderSettings.ambientIntensity;
                    m_OriginalColor = RenderSettings.ambientLight;
                    m_OriginalRange = float.MaxValue;
                    break;
                }

                case Mode.SingleLight: {
                    m_OriginalColor = m_Light.color;
                    m_OriginalIntensity = m_Light.intensity;
                    m_OriginalRange = m_Light.range;
                    break;
                }
            }
        }

        private void WriteSettings() {
            Color col = m_ColorOverride.GetValueOrDefault(m_OriginalColor);
            float intensity = m_OriginalIntensity * m_IntensityMultiplier;
            float range = m_OriginalRange * m_RangeMultiplier;

            switch(m_Mode) {
                case Mode.GlobalAmbient: {
                    RenderSettings.ambientIntensity = intensity;
                    RenderSettings.ambientLight = m_OriginalColor;
                    break;
                }

                case Mode.SingleLight: {
                    m_Light.color = col;
                    m_Light.intensity = intensity;
                    m_Light.range = range;
                    break;
                }
            }
        }

        [LeafMember("SetLightIntensity"), Preserve]
        public void SetIntensity(float newIntensityMultiplier, float transitionTime = 0) {
            if (transitionTime <= 0) {
                m_IntensityMultiplier = newIntensityMultiplier;
                m_IntensityTransition.Stop();
                WriteSettings();
                return;
            }

            m_IntensityTransition.Replace(this, Tween.Float(m_IntensityMultiplier, newIntensityMultiplier, (f) => {
                m_IntensityMultiplier = f;
                WriteSettings();
            }, transitionTime));
        }

        [LeafMember("SetLightRange"), Preserve]
        public void SetRange(float newRangeMultiplier, float transitionTime = 0) {
            if (transitionTime <= 0) {
                m_RangeMultiplier = newRangeMultiplier;
                m_RangeTransition.Stop();
                WriteSettings();
                return;
            }

            m_IntensityTransition.Replace(this, Tween.Float(m_IntensityMultiplier, newRangeMultiplier, (f) => {
                m_IntensityMultiplier = f;
                WriteSettings();
            }, transitionTime));
        }

        [LeafMember("SetLightColor"), Preserve]
        public void SetColor(StringSlice colorString, float transitionTime = 0) {
            Color c = Parsing.ParseColor(colorString);
            SetColor(c, transitionTime);
        }

        public void SetColor(Color color, float transitionTime = 0) {
            if (transitionTime <= 0) {
                m_ColorOverride = color;
                m_ColorTransition.Stop();
                WriteSettings();
                return;
            }

            if (!m_ColorOverride.HasValue) {
                m_ColorOverride = m_OriginalColor;
            }

            m_ColorTransition.Replace(this, Tween.Color(m_ColorOverride.Value, color, (c) => {
                m_ColorOverride = c;
                WriteSettings();
            }, transitionTime));
        }

        [LeafMember("ResetLightColor"), Preserve]
        public void ResetColor(float transitionTime = 0) {
            if (!m_ColorOverride.HasValue) {
                return;
            }

            if (transitionTime <= 0) {
                m_ColorOverride = null;
                m_ColorTransition.Stop();
                WriteSettings();
                return;
            }

            m_ColorTransition.Replace(this, Tween.Color(m_ColorOverride.Value, m_OriginalColor, (c) => {
                m_ColorOverride = c;
                WriteSettings();
            }, transitionTime).OnComplete(() => {
                m_ColorOverride = null;
                WriteSettings();
            }));
        }

        #if UNITY_EDITOR

        private bool ShowLightRef() {
            return m_Mode == Mode.SingleLight;
        }

        #endif // UNITY_EDITOR
    }
}