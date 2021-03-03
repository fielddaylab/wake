using Aqua;
using BeauUtil;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Result of a simulator step.
    /// </summary>
    public struct SimulationResult
    {
        public uint Timestamp;
        public WaterPropertyBlockF32 Environment;
        public TempList8<CritterResult> Actors;

        public void ClearCritters()
        {
            Actors.Clear();
        }

        public void SetCritters(StringHash32 inActorId, uint inPopulation, ActorStateId inStateId = ActorStateId.Alive)
        {
            CritterResult crit;
            for(int i = Actors.Count - 1; i >= 0; i--)
            {
                crit = Actors[i];
                if (crit.Id == inActorId)
                {
                    crit.Population = inPopulation;
                    crit.State = inStateId;
                    Actors[i] = crit;
                    return;
                }
            }
            Actors.Add(new CritterResult()
            {
                Id = inActorId,
                Population = inPopulation,
                State = inStateId
            });
        }

        public CritterResult GetCritters(StringHash32 inActorId)
        {
            CritterResult crit;
            for(int i = Actors.Count - 1; i >= 0; i--)
            {
                crit = Actors[i];
                if (crit.Id == inActorId)
                    return crit;
            }
            return default(CritterResult);
        }

        public int IndexOf(StringHash32 inActorId)
        {
            CritterResult crit;
            for(int i = Actors.Count - 1; i >= 0; i--)
            {
                crit = Actors[i];
                if (crit.Id == inActorId)
                    return i;
            }
            return -1;
        }
    
        /// <summary>
        /// Calculates the error between two simulations.
        /// </summary>
        static public float CalculateError(in SimulationResult inA, in SimulationResult inB)
        {
            float errorAccum = 0;
            int errorCount = 0;

            errorAccum += GraphingUtils.RPD(inA.Environment.Oxygen, inB.Environment.Oxygen);
            errorAccum += GraphingUtils.RPD(inA.Environment.Temperature, inB.Environment.Temperature);
            errorAccum += GraphingUtils.RPD(inA.Environment.Light, inB.Environment.Light);
            errorAccum += GraphingUtils.RPD(inA.Environment.PH, inB.Environment.PH);
            errorAccum += GraphingUtils.RPD(inA.Environment.CarbonDioxide, inB.Environment.CarbonDioxide);
            errorAccum += GraphingUtils.RPD(inA.Environment.Salinity, inB.Environment.Salinity);
            errorAccum += GraphingUtils.RPD(inA.Environment.Food, inB.Environment.Food);
            errorCount += 7;

            int critterCount = inA.Actors.Count;
            CritterResult critA, critB;
            for(int i = 0; i < critterCount; ++i)
            {
                critA = inA.Actors[i];
                critB = inB.GetCritters(critA.Id);
                errorAccum += GraphingUtils.RPD(critA.Population, critB.Population);
                errorAccum += GraphingUtils.RPD((float) critA.State, (float) critB.State);
            }
            errorCount += critterCount * 2;

            return errorAccum / errorCount;
        }
    }
}