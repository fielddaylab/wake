using System.Collections.Generic;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupData
    {
        public TankType Tank;
        public StringHash32 EcosystemId;
        public readonly HashSet<StringHash32> ActorIds = new HashSet<StringHash32>();

        public void Reset()
        {
            Tank = TankType.None;
            EcosystemId = StringHash32.Null;
            ActorIds.Clear();
        }
    }
}