using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    [CustomPropertyDrawer(typeof(TextId))]
    public class TextIdEditor : PropertyDrawer
    {
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

            Rect statusRect = new Rect(propRect.xMax + 4, propRect.y, TextIconDisplayWidth, propRect.height);
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_Hash");
            EditorGUI.PropertyField(propRect, stringProp, label);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                hashProp.longValue = new StringHash32(stringProp.stringValue).HashValue;
            }

            using(GUIScopes.IndentLevelScope.SetIndent(0))
            {
                EditorGUIUtility.AddCursorRect(statusRect, MouseCursor.Link);

                string key = null;
                bool bFound = false;

                if (stringProp.hasMultipleDifferentValues)
                {
                    EditorGUI.LabelField(statusRect, "---", m_NullIconStyle);
                }
                else
                {
                    key = stringProp.stringValue;
                    string content = null;
                    bFound = !string.IsNullOrEmpty(key) && LocEditor.TryLookup(key, out content);

                    if (string.IsNullOrEmpty(key))
                    {
                        EditorGUI.LabelField(statusRect, "Null", m_NullIconStyle);
                    }
                    else if (bFound)
                    {
                        var guiContent = m_ValidContent ?? (m_ValidContent = new GUIContent("Good"));
                        guiContent.tooltip = content;
                        EditorGUI.LabelField(statusRect, guiContent, m_FoundIconStyle);
                    }
                    else
                    {
                        var guiContent = m_MissingContent ?? (m_MissingContent = new GUIContent("???"));
                        EditorGUI.LabelField(statusRect, guiContent, m_MissingIconStyle);
                    }
                }

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && statusRect.Contains(Event.current.mousePosition))
                {
                    if (bFound)
                    {
                        LocEditor.OpenFile(key);
                    }
                    else
                    {
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