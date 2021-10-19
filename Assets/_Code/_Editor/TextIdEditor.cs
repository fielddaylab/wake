using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    [CustomPropertyDrawer(typeof(TextId)), CanEditMultipleObjects]
    public class TextIdEditor : PropertyDrawer
    {
        private const float TextIconDisplayWidth = 64;
        private const int MaxSearchLines = 12;

        [SerializeField] private string m_CurrentText;

        [NonSerialized] private GUIStyle m_NullIconStyle;
        [NonSerialized] private GUIStyle m_FoundIconStyle;
        [NonSerialized] private GUIStyle m_OverwriteIconStyle;
        [NonSerialized] private GUIStyle m_MissingIconStyle;

        [NonSerialized] private GUIContent m_MissingContent;
        [NonSerialized] private GUIContent m_ValidContent;
        [NonSerialized] private GUIContent m_OverwriteContent;

        [NonSerialized] private GUIStyle m_SearchBoxStyle;

        [NonSerialized] private List<string> m_SearchResults = new List<string>();
        [NonSerialized] private int m_SearchScroll = 0;

        [NonSerialized] private string m_LastKnownKey;

        private bool m_Overwrite = true;

        private void InitializeStyles()
        {
            if (m_NullIconStyle == null)
            {
                m_NullIconStyle = new GUIStyle(EditorStyles.label);
                m_NullIconStyle.normal.textColor = Color.gray;
                m_NullIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_FoundIconStyle == null)
            {
                m_FoundIconStyle = new GUIStyle(EditorStyles.label);
                m_FoundIconStyle.normal.textColor = Color.green;
                m_FoundIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_OverwriteIconStyle == null)
            {
                m_OverwriteIconStyle = new GUIStyle(EditorStyles.miniButton);
                m_OverwriteIconStyle.normal.textColor = Color.green;
                m_OverwriteIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_MissingIconStyle == null)
            {
                m_MissingIconStyle = new GUIStyle(EditorStyles.miniButton);
                m_MissingIconStyle.normal.textColor = Color.yellow;
                m_MissingIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (m_SearchBoxStyle == null)
            {
                m_SearchBoxStyle = new GUIStyle(EditorStyles.helpBox);
                m_SearchBoxStyle.normal.background = Texture2D.whiteTexture;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeStyles();

            Rect firstLine = position;
            firstLine.height = EditorGUIUtility.singleLineHeight;

            label = EditorGUI.BeginProperty(firstLine, label, property);
            Rect propRect = firstLine;
            propRect.width -= TextIconDisplayWidth + 4;

            Event oldCurrent = new Event(Event.current);
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_Hash");

            if (!stringProp.hasMultipleDifferentValues && stringProp.stringValue != m_LastKnownKey) {
                m_Overwrite = true;
                m_LastKnownKey = stringProp.stringValue;
            }

            int nextControlId = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
            EditorGUI.PropertyField(propRect, stringProp, label);
            bool bSelected = GUIUtility.keyboardControl == nextControlId;

            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                m_Overwrite = true;
                m_LastKnownKey = stringProp.stringValue;
                hashProp.longValue = new StringHash32(stringProp.stringValue).HashValue;
            }

            Rect statusRect = new Rect(propRect.xMax + 4, propRect.y, TextIconDisplayWidth, propRect.height);

            Rect searchResultsBox = EditorGUI.IndentedRect(firstLine);
            searchResultsBox.height = 0;
            searchResultsBox.y -= 4;
            searchResultsBox.width -= TextIconDisplayWidth + 4;

            Rect textBox = EditorGUI.IndentedRect(position);
            textBox.height -= 20;
            textBox.y += 20;

            float textBoxWidthToReduce = 24;
            textBox.x += textBoxWidthToReduce;
            textBox.width -= textBoxWidthToReduce;

            string key = stringProp.stringValue;
            key = stringProp.stringValue;
            string foundContent = null;
            bool bFound = !string.IsNullOrEmpty(key) && LocEditor.TryLookup(key, out foundContent);
            if (m_Overwrite && bFound) {
                m_CurrentText = foundContent;
                m_Overwrite = false;
            }

            using(GUIScopes.IndentLevelScope.SetIndent(0)) {
                if (!string.IsNullOrEmpty(key)) {
                    m_CurrentText = EditorGUI.TextArea(textBox, m_CurrentText);
                }
                RenderSearchAndStatus(stringProp, hashProp, statusRect, searchResultsBox, key, foundContent, m_CurrentText, bFound, bSelected, oldCurrent);
            }

            UnityEditor.EditorGUI.EndProperty();
        }

        private void RenderSearchAndStatus(SerializedProperty stringProp, SerializedProperty hashProp, Rect statusPosition, Rect searchPosition, string key, string originalText, string newText, bool found, bool selected, Event evt) {
            if (stringProp.hasMultipleDifferentValues) {
                EditorGUI.LabelField(statusPosition, "---", m_NullIconStyle);
                return;
            }    
            if (string.IsNullOrEmpty(key)) {
                EditorGUI.LabelField(statusPosition, "Null", m_NullIconStyle);
                return;
            }


            if (found) {
                if (originalText == newText) {
                    var validContent = m_ValidContent ?? (m_ValidContent = new GUIContent("Good"));
                    EditorGUI.LabelField(statusPosition, validContent, m_FoundIconStyle);
                } else {
                    var updateContent = m_OverwriteContent ?? (m_OverwriteContent = new GUIContent("Update?"));
                    if (GUI.Button(statusPosition, updateContent, m_OverwriteIconStyle)) {
                        LocEditor.TrySet(key, newText);
                    }
                }

                return;
            }
            
            var guiContent = m_MissingContent ?? (m_MissingContent = new GUIContent("Create?"));
            if (GUI.Button(statusPosition, guiContent, m_MissingIconStyle)) {
                LocEditor.TrySet(key, newText);
                EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
            }

            if (selected && evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.PageUp)
                {
                    m_SearchScroll -= 1;
                    EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
                }
                else if (evt.keyCode == KeyCode.PageDown)
                {
                    m_SearchScroll += 1;
                    EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
                }
                else if (evt.keyCode == KeyCode.Space && m_SearchResults.Count == 1) {
                    string onlyResult = m_SearchResults[0];
                    m_LastKnownKey = onlyResult;
                    stringProp.stringValue = onlyResult;
                    hashProp.longValue = new StringHash32(onlyResult).HashValue;
                    m_Overwrite = true;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (selected && evt.type == EventType.Repaint) {
                PaintSearchBox(key, searchPosition);
            }
        }

        private void PaintSearchBox(string key, Rect searchPosition) {
            m_SearchResults.Clear();
            int resultCount = LocEditor.Search(key, m_SearchResults);
            int resultsToDisplay = Math.Min(MaxSearchLines, resultCount);

            m_SearchScroll = Mathf.Clamp(m_SearchScroll, 0, resultCount - resultsToDisplay);

            searchPosition.height = ((resultsToDisplay + 1) * EditorGUIUtility.singleLineHeight) + resultsToDisplay * 2;
            searchPosition.y -= searchPosition.height + 2;

            Rect searchBackgroundBox = searchPosition;
            searchBackgroundBox.x -= 2;
            searchBackgroundBox.y -= 2;
            searchBackgroundBox.width += 4;
            searchBackgroundBox.height += 4;

            using(new GUIScopes.ColorScope(Color.black.WithAlpha(0.8f)))
            {
                GUI.Box(searchBackgroundBox, " ", m_SearchBoxStyle);
            }

            float lineInterval = EditorGUIUtility.singleLineHeight + 2;
            
            Rect textRect = searchPosition;
            textRect.height = EditorGUIUtility.singleLineHeight;
            if (resultsToDisplay == 0)
            {
                GUI.Label(textRect, "No similar entries", m_NullIconStyle);
            }
            else
            {
                if (resultCount > MaxSearchLines)
                    GUI.Label(textRect, string.Format("{0} similar entries\t[PgUp/PgDn to scroll]", resultCount), m_NullIconStyle);
                else
                    GUI.Label(textRect, string.Format("{0} similar entries", resultCount), m_NullIconStyle);
                textRect.y += lineInterval;

                textRect.x += 4;
                textRect.width -= 4;

                for(int i = 0; i < resultsToDisplay; i++)
                {
                    GUI.Label(textRect, m_SearchResults[m_SearchScroll + i]);
                    textRect.y += lineInterval;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hashProp = property.FindPropertyRelative("m_Hash");
            if (hashProp.longValue == 0) {
                return EditorGUIUtility.singleLineHeight;
            }
            return EditorGUIUtility.singleLineHeight + 2 + 18f + 13 + 13;
        }
    }
}