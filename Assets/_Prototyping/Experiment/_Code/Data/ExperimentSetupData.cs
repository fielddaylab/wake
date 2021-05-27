using System.Collections.Generic;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupData
    {
        public TankType Tank;
        public StringHash32 EnvironmentId;
        public WaterPropertyBlockF32 EnvironmentProperties;

        public StringHash32 CritterId;
        public readonly HashSet<StringHash32> ActorIds = new HashSet<StringHash32>();
        public WaterPropertyId PropertyId = WaterPropertyId.MAX;

        public ExperimentSetupData Clone()
        {
            ExperimentSetupData clone = new ExperimentSetupData();
            clone.Tank = Tank;
            clone.EnvironmentId = EnvironmentId;
            clone.CritterId = CritterId;
            clone.PropertyId = PropertyId;
            clone.EnvironmentProperties = EnvironmentProperties;
            foreach(var id in ActorIds)
                clone.ActorIds.Add(id);
            return clone;
        }

        public void Reset()
        {
            Tank = TankType.None;
            EnvironmentId = StringHash32.Null;
            PropertyId = WaterPropertyId.MAX;
            CritterId = StringHash32.Null;
            EnvironmentProperties = default(WaterPropertyBlockF32);
            ActorIds.Clear();
        }
    }
}