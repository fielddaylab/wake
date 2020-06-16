using BeauData;
using BeauPools;

namespace ProtoAqua.Energy
{
    public struct EnvironmentState
    {
        public FourCC Type;

        public VarState<ushort> OwnedResources;
        public VarState<float> Properties;
    }
}