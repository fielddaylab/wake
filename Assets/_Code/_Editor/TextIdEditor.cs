using BeauUtil;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    [CustomPropertyDrawer(typeof(TextId))]
    public class TextIdEditor : PropertyDrawer
    {
        private const float HashDisplayWidth = 90;

        private GUIStyle m_HashStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_HashStyle == null)
            {
                m_HashStyle = new GUIStyle(EditorStyles.label);
                m_HashStyle.normal.textColor = Color.gray;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            Rect propRect = position;
            propRect.width -= HashDisplayWidth - 4;
            Rect labelRect = new Rect(propRect.xMax + 4, propRect.y, HashDisplayWidth, propRect.height);
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_Hash");
            EditorGUI.PropertyField(propRect, stringProp, label);
            if (UnityEditor.EditorGUI.EndChangeCheck()
                || (!stringProp.hasMultipleDifferentValues && !string.IsNullOrEmpty(stringProp.stringValue) && hashProp.longValue == 0))
            {
                hashProp.longValue = new StringHash32(stringProp.stringValue).HashValue;
            }
            
            int lastIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.LabelField(labelRect, "0x" + hashProp.longValue.ToString("X8"), m_HashStyle);

            EditorGUI.indentLevel = lastIndent;

            UnityEditor.EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}