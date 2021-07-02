using Aqua;
using BeauUtil;
using BeauUtil.Debugger;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Detailed simulator stats after a simulator tick.
    /// </summary>
    public struct SimulationResultDetails
    {
        public WaterPropertyBlockF32 StartingEnvironment;
        public TempList8<ActorStateId> StartingStates;
        public TempList8<ActorStateId> AfterLightStates;
        public TempList8<WaterPropertyBlockF32> Produced;
        public TempList8<WaterPropertyBlockF32> Consumed;
        public TempList16<CritterEatDetails> Eaten;
        public TempList8<uint> Deaths;
        public TempList8<uint> Growth;
    }
}