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
    }

    public sealed class ActorTypeIdAttribute : FourCCSelectorAttribute
    {
        public ActorTypeIdAttribute()
            : base(typeof(ActorType))
            { }

        protected override bool ShouldCacheList() { return true; }
    }
}