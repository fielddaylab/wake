using System;
using BeauUtil;
using Aqua;
using UnityEngine;

namespace ProtoAqua.Observation
{
    [CreateAssetMenu(menuName = "Aqualab Content/Morse Code Patterns")]
    public class PatternData : TweakAsset, IEditorOnlyData
    {
        [Serializable]
        private struct Entry {
            public SerializedHash32 Id;
            public string Pattern;
            public float Pitch;

            [Multiline] public string Notes;
        }

        [SerializeField] private Entry[] m_Entries = null;

        public bool TryGetEntry(StringHash32 entryId, out string pattern, out float pitch) {
            foreach(var entry in m_Entries) {
                if (entry.Id == entryId) {
                    pattern = entry.Pattern;
                    pitch = entry.Pitch;
                    return true;
                }
            }

            pattern = null;
            pitch = 1;
            return false;
        }

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            for(int i = 0; i < m_Entries.Length; i++) {
                ValidationUtils.StripDebugInfo(ref m_Entries[i].Id);
                m_Entries[i].Notes = null;
            }
        }

        #endif // UNITY_EDITOR
    }
}