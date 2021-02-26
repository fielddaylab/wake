using Aqua;
using BeauUtil;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Result of a simulator step.
    /// </summary>
    public struct SimulationResult
    {
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
    }
}