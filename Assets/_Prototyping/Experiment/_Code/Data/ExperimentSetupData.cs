using System.Collections.Generic;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupData
    {
        public TankType Tank;
        public StringHash32 EcosystemId;
        public readonly HashSet<StringHash32> ActorIds = new HashSet<StringHash32>();

        public StringHash32 CritterX;

        public List<PlayerFactParams> FactParams = new List<PlayerFactParams>();

        public StringHash32 CritterY;

        public WaterPropertyId PropertyId;

        public ExperimentSetupData Clone()
        {
            ExperimentSetupData clone = new ExperimentSetupData();
            clone.Tank = Tank;
            clone.EcosystemId = EcosystemId;
            clone.CritterX = CritterX;
            clone.CritterY = CritterY;
            clone.PropertyId = PropertyId;
            foreach(var id in ActorIds)
                clone.ActorIds.Add(id);
            foreach(var fact in FactParams) {
                clone.FactParams.Add(fact);
            }
            return clone;
        }

        public void Process(StringHash32 Critter)
        {
            CritterX = Critter;
            foreach (var fact in Services.Data.Profile.Bestiary.GetFactsForEntity(Critter)){
                FactParams.Add(fact);
            }
        }

        public IEnumerable<PlayerFactParams> GetEat() {
            foreach(var fact in FactParams) {
                if(fact.GetType().Equals(typeof(BFEat))) {
                    yield return fact;
                }
            }
        }
        public IEnumerable<PlayerFactParams> GetProp() {
            foreach(var fact in FactParams) {
                if(fact.GetType().Equals(typeof(BFWaterProperty))) {
                    yield return fact;
                }
            }
        }

        public void Reset()
        {
            Tank = TankType.None;
            EcosystemId = StringHash32.Null;
            PropertyId = WaterPropertyId.None;
            CritterX = StringHash32.Null;
            CritterY = StringHash32.Null;
            ActorIds.Clear();
        }
    }
}