using System;
using System.Collections.Generic;
using System.IO;
using BeauPools;
using BeauRoutine.Extensions;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAudio
{
    [CreateAssetMenu(menuName = "Prototype/Audio/Audio Package")]
    public class AudioPackage : ScriptableObject
    {
        #region Inspector

        [SerializeField, EditModeOnly] private AudioEvent[] m_Events = null;

        #endregion // Inspector

        [NonSerialized] private int m_RefCount;

        public IReadOnlyList<AudioEvent> Events() { return m_Events; }

        internal void IncrementRefCount()
        {
            ++m_RefCount;
        }

        internal bool DecrementRefCount()
        {
            return --m_RefCount <= 0;
        }

        internal bool ShouldUnload()
        {
            return m_RefCount <= 0;
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            ValidationUtils.EnsureUnique(ref m_Events);
        }

        [ContextMenu("Load All In Directory")]
        private void FindAllTweaks()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            m_Events = ValidationUtils.FindAllAssets<AudioEvent>(myDirectory);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.CustomEditor(typeof(AudioPackage))]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All In Directory"))
                {
                    foreach(AudioPackage audioPackage in targets)
                    {
                        audioPackage.FindAllTweaks();
                    }
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}