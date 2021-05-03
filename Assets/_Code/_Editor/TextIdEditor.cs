using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    [CustomPropertyDrawer(typeof(TextId)), CanEditMultipleObjects]
    public class TextIdEditor : PropertyDrawer
    {
        private const float TextIconDisplayWidth = 45;
        private const int MaxSearchLines = 12;

        [NonSerialized] private GUIStyle m_NullIconStyle;
        [NonSerialized] private GUIStyle m_FoundIconStyle;
        [NonSerialized] private GUIStyle m_MissingIconStyle;

        [NonSerialized] private GUIContent m_MissingContent;
        [NonSerialized] private GUIContent m_ValidContent;

        [NonSerialized] private GUIStyle m_SearchBoxStyle;

        [NonSerialized] private List<string> m_SearchResults = new List<string>();
        [NonSerialized] private int m_SearchScroll = 0;

        private void InitializeStyles()
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

            if (m_SearchBoxStyle == null)
            {
                m_SearchBoxStyle = new GUIStyle(EditorStyles.helpBox);
                m_SearchBoxStyle.normal.background = Texture2D.whiteTexture;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeStyles();

            label = EditorGUI.BeginProperty(position, label, property);
            Rect propRect = position;
            propRect.width -= TextIconDisplayWidth + 4;

            Rect statusRect = new Rect(propRect.xMax + 4, propRect.y, TextIconDisplayWidth, propRect.height);
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_Hash");

            int nextControlId = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
            EditorGUI.PropertyField(propRect, stringProp, label);
            bool bSelected = GUIUtility.keyboardControl == nextControlId;

            Rect searchResultsBox = EditorGUI.IndentedRect(position);
            searchResultsBox.height = 0;
            searchResultsBox.y -= 4;
            searchResultsBox.width -= TextIconDisplayWidth + 4;

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

                        if (bSelected && Event.current.type == EventType.KeyDown)
                        {
                            if (Event.current.keyCode == KeyCode.PageUp)
                            {
                                m_SearchScroll -= 1;
                                EditorUtility.SetDirty(property.serializedObject.targetObject);
                            }
                            else if (Event.current.keyCode == KeyCode.PageDown)
                            {
                                m_SearchScroll += 1;
                                EditorUtility.SetDirty(property.serializedObject.targetObject);
                            }
                        }

                        if (bSelected && Event.current.type == EventType.Repaint)
                        {
                            m_SearchResults.Clear();
                            int resultCount = LocEditor.Search(key, m_SearchResults);
                            int resultsToDisplay = Math.Min(MaxSearchLines, resultCount);

                            m_SearchScroll = Mathf.Clamp(m_SearchScroll, 0, resultCount - resultsToDisplay);

                            searchResultsBox.height = ((resultsToDisplay + 1) * EditorGUIUtility.singleLineHeight) + resultsToDisplay * 2;
                            searchResultsBox.y -= searchResultsBox.height + 2;

                            Rect searchBackgroundBox = searchResultsBox;
                            searchBackgroundBox.x -= 2;
                            searchBackgroundBox.y -= 2;
                            searchBackgroundBox.width += 4;
                            searchBackgroundBox.height += 4;

                            using(new GUIScopes.ColorScope(Color.black.WithAlpha(0.8f)))
                            {
                                GUI.Box(searchBackgroundBox, " ", m_SearchBoxStyle);
                            }

                            float lineInterval = EditorGUIUtility.singleLineHeight + 2;
                            
                            Rect textRect = searchResultsBox;
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