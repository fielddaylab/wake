using Aqua;
using BeauUtil;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Critter stats after a simulator tick.
    /// </summary>
    public struct CritterResult
    {
        public StringHash32 Id;
        public uint Population;

        public override string ToString()
        {
            return string.Format("{0}: {1}", Id.ToDebugString(), Population);
        }
    }
}