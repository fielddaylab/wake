using System;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct ActorState
    {
        public ushort Id;
        public FourCC Type;

        public ActorStateFlags Flags;
        public ushort Age;
        public ushort Mass;

        public ushort MetPropertyRequirements; // property requirements
        public VarState<ushort> DesiredResources; // resource deficit
        public VarState<ushort> ProducingResources; // resource production

        // starvation counters
        public VarState<byte> ResourceStarvation; // starvation counter (resources)
        public VarState<byte> PropertyStarvation; // starvation counter (properties)

        public byte OffsetA;
        public byte OffsetB;
    }

    [Flags, LabeledEnum(false)]
    public enum ActorStateFlags : ushort
    {
        // stateful flags
        Alive = 0x01,

        // temp flags
        QueuedToReproduce = 0x100,
        DoneForTick = 0x200,
        FailedToEat = 0x400,

        // groups
        [Hidden] TempMask = QueuedToReproduce | DoneForTick | FailedToEat
    }
}