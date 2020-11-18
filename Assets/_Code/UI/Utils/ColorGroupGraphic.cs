using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using BeauRoutine.Extensions;
using BeauRoutine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [RequireComponent(typeof(ColorGroup))]
    public class ColorGroupGraphic : Graphic
    {
        [SerializeField, Required(ComponentLookupDirection.Self)] private ColorGroup m_ColorGroup = null;

        [NonSerialized] private Routine m_CrossFade;
        private Action<Color> m_ColorSetter;
        private Action<float> m_AlphaSetter;

        public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
        {
            if (!useAlpha && !useRGB)
                return;

            Color currentColor = m_ColorGroup.Color;
            if (currentColor == targetColor)
                return;
            
            if (!useRGB)
            {
                targetColor.r = currentColor.r;
                targetColor.g = currentColor.g;
                targetColor.b = currentColor.b;
            }
            if (!useAlpha)
            {
                targetColor.a = currentColor.a;
            }

            if (duration <= 0)
            {
                m_CrossFade.Stop();
                m_ColorGroup.Color = targetColor;
                return;
            }

            m_AlphaSetter = m_AlphaSetter ?? UpdateAlpha;
            m_ColorSetter = m_ColorSetter ?? UpdateColor;

            if (useRGB)
                m_CrossFade.Replace(this, Tween.Color(currentColor, targetColor, m_ColorSetter, duration, useAlpha ? ColorUpdate.FullColor : ColorUpdate.PreserveAlpha));
            else if (useAlpha)
                m_CrossFade.Replace(this, Tween.Float(currentColor.a, targetColor.a, m_AlphaSetter, duration));

            if (ignoreTimeScale)
                m_CrossFade.SetPhase(RoutinePhase.RealtimeUpdate);
        }

        private void UpdateAlpha(float inAlpha)
        {
            m_ColorGroup.SetAlpha(inAlpha);
        }

        private void UpdateColor(Color inColor)
        {
            m_ColorGroup.SetColor(inColor);
        }

        public override void SetMaterialDirty() { return; }
        public override void SetVerticesDirty() { return; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        protected override void UpdateGeometry()
        {
        }

        #if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.Reset();
            m_ColorGroup = this.CacheComponent(ref m_ColorGroup);
        }

        [CustomEditor(typeof(ColorGroupGraphic)), CanEditMultipleObjects]
        private class Inspector : Editor
        {
            public override void OnInspectorGUI()
            {
                UnityEditor.SerializedObject obj = new UnityEditor.SerializedObject(targets);
                EditorGUILayout.PropertyField(obj.FindProperty("m_RaycastTarget"));
                obj.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR
    }
}