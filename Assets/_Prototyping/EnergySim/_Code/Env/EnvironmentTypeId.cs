using BeauData;
using BeauPools;

namespace ProtoAqua.Energy
{
    static public class EnvironmentTypeId
    {
        static private FourCC Register(string inCode, string inName)
        {
            return FourCC.Register(typeof(EnvironmentType), inCode, inName);
        }

        static public readonly FourCC KelpForest = Register("KLPF", "Kelp Forest");
    }

    public sealed class EnvironmentTypeIdAttribute : FourCCSelectorAttribute
    {
        public EnvironmentTypeIdAttribute()
            : base(typeof(EnvironmentType))
            { }
    }
}