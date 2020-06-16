using System;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Prototype/Energy/Variable Type")]
    public class VarType : ScriptableObject, IKeyValuePair<FourCC, VarType>
    {
        #region Types

        #endregion // Types

        #region Inspector

        [SerializeField, VarTypeId] private FourCC m_Id = FourCC.Zero;
        [SerializeField, AutoEnum] private VarTypeFlags m_Flags = default(VarTypeFlags);

        [Header("Settings")]
        [SerializeField] private VarCalculationType m_Calculation = VarCalculationType.Resource;
        [SerializeField] private sbyte m_Priority = 0;

        [Header("Other")]
        [SerializeField] private PropertyBlock m_ExtraData = default(PropertyBlock);

        #endregion // Inspector

        [NonSerialized] private DerivedVarCalculationDelegate m_DerivedDelegate;
        [NonSerialized] private ExternVarCalculationDelegate m_ExternDelegate;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, VarType>.Key { get { return m_Id; } }

        VarType IKeyValuePair<FourCC, VarType>.Value { get { return this; } }

        #endregion // KeyValuePair

        #region Accessors

        public FourCC Id() { return m_Id; }
        public VarTypeFlags Flags() { return m_Flags; }

        public VarCalculationType CalcType() { return m_Calculation; }
        public sbyte Priority() { return m_Priority; }

        public PropertyBlock ExtraData() { return m_ExtraData; }

        #endregion // Accessors

        public bool HasFlags(VarTypeFlags inFlags) { return (m_Flags & inFlags) == inFlags; }
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
    }

    /// <summary>
    /// Delegate for deriving a variable value.
    /// </summary>
    public delegate float DerivedVarCalculationDelegate(IEnergySimStateReader inReader);
    
    /// <summary>
    /// Delegate for driving a variable's value from an external source.
    /// </summary>
    public delegate float ExternVarCalculationDelegate(IEnergySimStateReader inReader, IEnergySimScenario inContext);
}