using System;
using UnityEngine;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Observation
{
    [Serializable]
    public class ScanData
    {
        static public readonly string NullId = "[null]";

        #region Inspector

        [SerializeField] private string m_BaseId = NullId;
        [SerializeField, AutoEnum] private ScanDataFlags m_Flags = 0;

        [Header("Text")]
        [SerializeField] private string m_HeaderText = null;
        [SerializeField] private string m_DescText = null;
        [SerializeField] private string m_DescTextShort = null;

        [Header("Interaction")]
        [SerializeField, Range(1, 3)] private int m_ScanSpeed = 1;

        #endregion // Inspector

        public string BaseId() { return m_BaseId; }
        public ScanDataFlags Flags() { return m_Flags; }
    }

    [Flags]
    public enum ScanDataFlags : byte
    {
        Actor       = 0x01,
        Environment = 0x02,
        Character   = 0x04,

        Important   = 0x10,
    }
}