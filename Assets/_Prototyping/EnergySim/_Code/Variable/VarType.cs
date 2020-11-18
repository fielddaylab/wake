using System;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Aqualab/Energy/Variable Type")]
    public class VarType : ScriptableObject, ISimType<VarType>, IKeyValuePair<FourCC, VarType>
    {
        #region Types

        [Serializable]
        public struct ConfigRange
        {
            public float Min;
            public float Max;
            public float Increment;
        }

        #endregion // Types

        #region Inspector

        [SerializeField, VarTypeId] private FourCC m_Id = FourCC.Zero;
        [SerializeField] private string m_ScriptName = null;
        [SerializeField, AutoEnum] private VarTypeFlags m_Flags = default(VarTypeFlags);
        [SerializeField, AutoEnum] private ContentArea m_ContentAreas = default(ContentArea);

        [Header("Settings")]
        [SerializeField] private VarCalculationType m_Calculation = VarCalculationType.Resource;
        [SerializeField] private sbyte m_Priority = 0;

        [Header("Other")]
        [SerializeField] private ConfigRange m_ConfigSettings = default(ConfigRange);
        [SerializeField] private PropertyBlock m_ExtraData = default(PropertyBlock);

        #endregion // Inspector

        [NonSerialized] private DerivedVarCalculationDelegate m_DerivedDelegate;

        [NonSerialized] private VarTypeDatabase m_Database;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, VarType>.Key { get { return m_Id; } }

        VarType IKeyValuePair<FourCC, VarType>.Value { get { return this; } }

        #endregion // KeyValuePair

        #region ISimType

        void ISimType<VarType>.Hook(SimTypeDatabase<VarType> inDatabase)
        {
            if (inDatabase is VarTypeDatabase)
            {
                m_Database = (VarTypeDatabase) inDatabase;
            }
        }

        void ISimType<VarType>.Unhook(SimTypeDatabase<VarType> inDatabase)
        {
            if (m_Database == inDatabase)
            {
                m_Database = null;
            }
        }

        #endregion // ISimType

        #region Accessors

        public FourCC Id() { return m_Id; }
        public string ScriptName() { return m_ScriptName; }
        public VarTypeFlags Flags() { return m_Flags; }
        public ContentArea ContentAreas() { return m_ContentAreas; }

        public VarCalculationType CalcType() { return m_Calculation; }
        public sbyte Priority() { return m_Priority; }

        public ConfigRange ConfigSettings() { return m_ConfigSettings; }
        public PropertyBlock ExtraData() { return m_ExtraData; }

        #endregion // Accessors

        public bool HasFlags(VarTypeFlags inFlags) { return (m_Flags & inFlags) == inFlags; }
        public bool HasAnyFlags(VarTypeFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasContentArea(ContentArea inContentArea) { return (m_ContentAreas & inContentArea) == inContentArea; }
        public bool HasAnyContentArea(ContentArea inContentArea) { return (m_ContentAreas & inContentArea) != 0; }

        /// <summary>
        /// Sets this VarType configuration as dirty.
        /// </summary>
        public void Dirty()
        {
            m_Database?.Dirty();
        }

        #region Unity Events

        #if UNITY_EDITOR

        private void OnValidate()
        {
            Dirty();
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }

    /// <summary>
    /// How a variable is calculated.
    /// </summary>
    public enum VarCalculationType : byte
    {
        // No extra calculations
        Resource,

        // derived exclusively from environment
        Derived,

        // driven by external factors
        Extern
    }

    /// <summary>
    /// Additional variable behavior flags.
    /// </summary>
    [Flags, LabeledEnum]
    public enum VarTypeFlags : ushort
    {
        [Hidden]
        None = 0,

        [Label("Food/Convert From Mass")]
        ConvertFromMass = 0x001,

        [Label("Visibility/Always Hide")]
        HideAlways = 0x002,

        [Label("Visibility/Hide If Zero")]
        HideIfZero = 0x004,
    }

    /// <summary>
    /// Delegate for deriving a variable value.
    /// </summary>
    public delegate float DerivedVarCalculationDelegate(IEnergySimStateReader inReader);
}