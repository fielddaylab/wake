using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    [CustomPropertyDrawer(typeof(TextId))]
    public class TextIdEditor : PropertyDrawer
    {
        [SerializeField] private string m_TempText;

        private const float TextIconDisplayWidth = 45;

        private GUIStyle m_NullIconStyle;
        private GUIStyle m_FoundIconStyle;
        private GUIStyle m_MissingIconStyle;

        private GUIContent m_MissingContent;
        private GUIContent m_ValidContent;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_NullIconStyle == null)
            {
                m_NullIconStyle = new GUIStyle(EditorStyles.label);
                m_NullIconStyle.normal.textColor = Color.gray;
            }

            if (m_FoundIconStyle == null)
            {
                m_FoundIconStyle = new GUIStyle(EditorStyles.label);
                m_FoundIconStyle.normal.textColor = Color.green;
            }

            if (m_MissingIconStyle == null)
            {
                m_MissingIconStyle = new GUIStyle(EditorStyles.label);
                m_MissingIconStyle.normal.textColor = Color.yellow;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            Rect propRect = position;
            propRect.width -= TextIconDisplayWidth - 4;
            Rect iconRect = new Rect(propRect.xMax + 4, propRect.y, TextIconDisplayWidth, propRect.height);
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_Hash");
            EditorGUI.PropertyField(propRect, stringProp, label);
            if (UnityEditor.EditorGUI.EndChangeCheck()
                || (!stringProp.hasMultipleDifferentValues && !string.IsNullOrEmpty(stringProp.stringValue) && hashProp.longValue == 0))
            {
                hashProp.longValue = new StringHash32(stringProp.stringValue).HashValue;
            }
            
            string key = stringProp.stringValue;
            if (string.IsNullOrEmpty(key))
            {
                using(GUIScopes.IndentLevelScope.SetIndent(0))
                {
                    EditorGUI.LabelField(iconRect, "Null", m_NullIconStyle);
                }
            }
            else
            {
                string content;
                if (LocEditor.TryLookup(key, out content))
                {
                    using(GUIScopes.IndentLevelScope.SetIndent(0))
                    {
                        var guiContent = m_ValidContent ?? (m_ValidContent = new GUIContent("Valid"));
                        guiContent.tooltip = content;
                        EditorGUI.LabelField(iconRect, guiContent, m_FoundIconStyle);
                        m_TempText = content;
                    }
                }
                else
                {
                    using(GUIScopes.IndentLevelScope.SetIndent(0))
                    {
                        var guiContent = m_MissingContent ?? (m_MissingContent = new GUIContent("Unkn", "Right-click to open file for editing"));
                        EditorGUI.LabelField(iconRect, guiContent, m_MissingIconStyle);
                        
                        if (Event.current.type == EventType.ContextClick && iconRect.Contains(Event.current.mousePosition))
                            LocEditor.AttemptOpenFile(key);
                    }
                }
            }

            UnityEditor.EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}