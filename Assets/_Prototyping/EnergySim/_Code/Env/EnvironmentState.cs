using System.Runtime.InteropServices;
using BeauData;
using BeauPools;

namespace ProtoAqua.Energy
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EnvironmentState
    {
        public FourCC Type;

        public VarState<ushort> OwnedResources;
        public VarState<float> Properties;
    }
}