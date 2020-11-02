using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    static public class ActorTypeId
    {
        static private FourCC Register(string inCode, string inName)
        {
            return FourCC.Register(typeof(ActorType), inCode, inName);
        }

        static public readonly FourCC Kelp = Register("KELP", "Kelp");
        static public readonly FourCC Urchin = Register("URCH", "Urchin");
        static public readonly FourCC SeaOtter = Register("SOTT", "Sea Otter");
        static public readonly FourCC SeaStar = Register("STAR", "Sea Star");

        static public readonly FourCC Coral = Register("CORL", "Coral");
        static public readonly FourCC Algae = Register("ALGE", "Algae");
        static public readonly FourCC Anemone = Register("ANEM", "Anemone");
        static public readonly FourCC Clownfish = Register("CLWN", "Clownfish");
        static public readonly FourCC Mangrove = Register("MANG", "Mangrove");
    }

    public sealed class ActorTypeIdAttribute : FourCCSelectorAttribute
    {
        public ActorTypeIdAttribute()
            : base(typeof(ActorType))
            { }

        protected override bool ShouldCacheList() { return true; }
    }
}