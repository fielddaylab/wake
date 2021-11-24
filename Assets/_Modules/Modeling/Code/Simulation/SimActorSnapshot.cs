using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Modeling {
    public unsafe struct SimActorSnapshot {
        public StringHash32 Id;
        public uint Population;
        public ActorStateId State;
    }
}