using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    static public class VarTypeId
    {
        static private FourCC Register(string inCode, string inName)
        {
            return FourCC.Register(typeof(VarType), inCode, inName);
        }

        // resources

        static public readonly FourCC Food = Register("FOOD", "Resource/Local/Food");

        static public readonly FourCC Oxygen = Register("OXYG", "Resource/Chem/Oxygen");
        static public readonly FourCC CarbonDioxide = Register("CO2", "Resource/Chem/Carbon Dioxide");
        static public readonly FourCC CarbonicAcid = Register("HCO3", "Resource/Chem/Carbonic Acid");

        // derived properties

        static public readonly FourCC Density = Register("DENS", "Derived/Organism Density");
        static public readonly FourCC PH = Register("PH", "Derived/PH Level");

        // extern properties

        static public readonly FourCC Light = Register("LGHT", "Extern/Light");
        static public readonly FourCC Temp = Register("TEMP", "Extern/Temperature");
        static public readonly FourCC Turbidity = Register("TURB", "Extern/Turbidity");
    }

    public sealed class VarTypeIdAttribute : FourCCSelectorAttribute
    {
        public VarTypeIdAttribute()
            : base(typeof(VarType))
            { }

        protected override bool ShouldCacheList() { return true; }
    }
}