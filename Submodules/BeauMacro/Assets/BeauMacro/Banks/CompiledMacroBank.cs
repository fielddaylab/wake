/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    CompiledMacroBank.cs
 * Purpose: Compiled macros bank. Read-only.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace BeauMacro
{
    /// <summary>
    /// Compiled and serializable macro bank.
    /// </summary>
    [Serializable]
    public sealed class CompiledMacroBank : MacroBank
    {
        #region Types

        [Serializable]
        private struct MacroEntry
        {
            public int Key;
            public Macro Value;

            public MacroEntry(int inKey, Macro inValue)
            {
                Key = inKey;
                Value = inValue;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField]
        private MacroEntry[] m_MacroEntries = new MacroEntry[0];

        [SerializeField]
        private string[] m_StringEntries = new string[0];

        #endregion // Inspector

        private Dictionary<int, Macro> m_MacroMap;

        internal CompiledMacroBank() { }

        internal CompiledMacroBank(Dictionary<int, Macro> inMap, string[] inStrings)
        {
            m_MacroMap = new Dictionary<int, Macro>(inMap);
            m_StringEntries = (string[]) inStrings.Clone();

            m_MacroEntries = new MacroEntry[m_MacroMap.Count];

            int idx = 0;
            foreach (var kv in m_MacroMap)
            {
                m_MacroEntries[idx++] = new MacroEntry(kv.Key, kv.Value);
            }
        }

        #region MacroBank

        internal override Dictionary<int, Macro> GetMacros()
        {
            if (m_MacroMap == null)
            {
                m_MacroMap = new Dictionary<int, Macro>(m_MacroEntries.Length);
                foreach (var entry in m_MacroEntries)
                {
                    m_MacroMap.Add(entry.Key, entry.Value);
                }
            }
            return m_MacroMap;
        }

        internal override string[] GetStrings()
        {
            return m_StringEntries;
        }

        #endregion // MacroBank

        #if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(CompiledMacroBank))]
        private class Drawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty macroArray = property.FindPropertyRelative("m_MacroEntries");
                SerializedProperty stringArray = property.FindPropertyRelative("m_StringEntries");

                label = EditorGUI.BeginProperty(position, label, property);

                Rect labelRect = position;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);

                Rect helpRect = position;
                helpRect.x += EditorGUIUtility.labelWidth;
                helpRect.width -= EditorGUIUtility.labelWidth;
                EditorGUI.HelpBox(helpRect, string.Format("{0} macro(s), {1} string(s)", macroArray.arraySize, stringArray.arraySize), MessageType.Info);

                EditorGUI.EndProperty();
            }
        }

        #endif // UNITY_EDITOR
    }
}