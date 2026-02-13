/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    25 Oct 2020
 * 
 * File:    LeafAssetEditor.cs
 * Purpose: Leaf asset editor.
 */

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Leaf.Editor
{
    [CustomEditor(typeof(LeafAsset)), CanEditMultipleObjects]
    public class LeafAssetEditor : UnityEditor.Editor
    {
        private const int MaxChars = 7000;

        [NonSerialized] private GUIStyle m_Style;

        protected void OnEnable()
        {
            GetType().GetProperty("alwaysAllowExpansion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, true);
        }

        public override void OnInspectorGUI()
        {
            if (m_Style == null)
            {
                m_Style = new GUIStyle("ScriptText");
            }

            long size = 0;
            foreach(LeafAsset t in targets)
            {
                if (t != null)
                {
                    size += (long) t.Bytes().Length;
                }
            }

            EditorGUILayout.LabelField("Size", EditorUtility.FormatBytes(size));

            bool bEnabled = GUI.enabled;
            GUI.enabled = true;

            LeafAsset asset = target as LeafAsset;
            if (asset != null)
            {
                
            }

            GUI.enabled = bEnabled;
        }
    }
}