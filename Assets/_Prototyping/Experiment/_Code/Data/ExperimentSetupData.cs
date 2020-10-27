using System.Collections.Generic;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupData
    {
        public TankType Tank;
        public StringHash32 EcosystemId;
        public readonly HashSet<StringHash32> ActorIds = new HashSet<StringHash32>();

        public ExperimentSetupData Clone()
        {
            ExperimentSetupData clone = new ExperimentSetupData();
            clone.Tank = Tank;
            clone.EcosystemId = EcosystemId;
            foreach(var id in ActorIds)
                clone.ActorIds.Add(id);
            return clone;
        }

        public void Reset()
        {
            Tank = TankType.None;
            EcosystemId = StringHash32.Null;
            ActorIds.Clear();
        }
    }
}