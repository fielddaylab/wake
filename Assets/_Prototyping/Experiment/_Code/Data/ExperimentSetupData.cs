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

        public List<StringHash32> FactIds = new List<StringHash32>();

        public List<float> SliderValues = new List<float>();

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
            foreach(var id in FactIds) {
                clone.FactIds.Add(id);
            }
            foreach(var value in SliderValues) {
                clone.SliderValues.Add(value);
            }
            return clone;
        }

        public void Reset()
        {
            Tank = TankType.None;
            EcosystemId = StringHash32.Null;
            PropertyId = WaterPropertyId.MAX;
            CritterX = StringHash32.Null;
            CritterY = StringHash32.Null;
            FactIds.Clear();
            ActorIds.Clear();
            SliderValues.Clear();
        }

        #region Helpers

        public void Process(StringHash32 Critter)
        {
            CritterX = Critter;
            foreach (var fact in Services.Data.Profile.Bestiary.GetFactsForEntity(Critter)){
                FactIds.Add(fact.FactId);
            }
        }

        public List<StringHash32> GetTargets() {
            List<StringHash32> result = new List<StringHash32>();
            foreach(var id in FactIds) {
                var fact = Services.Data.Profile.Bestiary.GetFact(id);
                if(IsBFEat(fact.Fact)) {
                    var res = (BFEat)fact.Fact;
                    result.Add(res.Target().Id());
                }
            }
            return result;
        }

        public PlayerFactParams GetResult() {
            foreach(var id in FactIds) {
                var fact = Services.Data.Profile.Bestiary.GetFact(id);
                if(IsBFEat(fact.Fact)) {
                    var res = (BFEat)fact.Fact;
                    if(res.Target().Id().Equals(CritterY)) return fact;
                }
            }
            return null;
        }

        public void SetTargets(string cType) {
            var filteredIds = FactIds;
            foreach(var id in FactIds) {
                var fact = Services.Data.Profile.Bestiary.GetFact(id);
                if(cType == "critter") {
                    if(!IsBFEat(fact.Fact)) {
                        filteredIds.Remove(id);
                    }
                }
            }
            FactIds = filteredIds;
        }

        public bool IsBFEat(DBObject x) {
            return x.GetType().Equals(typeof(BFEat));
        }

        #endregion //Helpers
    }
}