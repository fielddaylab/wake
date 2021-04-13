using Aqua;
using BeauUtil;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Result of a simulator step.
    /// </summary>
    public struct SimulationResult
    {
        public ushort Timestamp;
        public SimulationRandom Random;
        public WaterPropertyBlockF32 Environment;
        public TempList16<CritterResult> Actors;

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
                    Actors[i] = crit;
                    return;
                }
            }
            Actors.Add(new CritterResult()
            {
                Id = inActorId,
                Population = inPopulation,
            });
        }

        public void AdjustCritters(StringHash32 inActorId, int inAdjust)
        {
            CritterResult crit;
            for(int i = Actors.Count - 1; i >= 0; i--)
            {
                crit = Actors[i];
                if (crit.Id == inActorId)
                {
                    long newPop = crit.Population + inAdjust;
                    if (newPop < 0)
                        newPop = 0;
                    crit.Population = (uint) newPop;
                    Actors[i] = crit;
                    return;
                }
            }
            
            if (inAdjust < 0)
                inAdjust = 0;
            
            Actors.Add(new CritterResult()
            {
                Id = inActorId,
                Population = (uint) inAdjust,
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
            errorCount += 6;

            int critterCount = inA.Actors.Count;
            CritterResult critA, critB;
            for(int i = 0; i < critterCount; ++i)
            {
                critA = inA.Actors[i];
                critB = inB.GetCritters(critA.Id);
                errorAccum += GraphingUtils.RPD(critA.Population, critB.Population);
            }
            errorCount += critterCount;

            return errorAccum / errorCount;
        }
    }
}