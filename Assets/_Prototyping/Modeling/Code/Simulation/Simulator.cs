using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    static public class Simulator
    {
        public const int MaxTrackedCritters = 8;

        static public unsafe SimulationResult Simulate(SimulationProfile inProfile, in SimulationResult inInitial)
        {
            WaterPropertyBlockF32 environment = inInitial.Environment;
            IReadOnlyList<CritterProfile> profiles = inProfile.Critters();
            int critterCount = profiles.Count;

            CritterData* dataBlock = stackalloc CritterData[critterCount];

            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].CopyFrom(ref dataBlock[i], inInitial);
                profiles[i].SetupTick(ref dataBlock[i], environment);
            }

            // produce stuff

            for(int i = 0; i < critterCount; ++i)
            {
                environment += dataBlock[i].ToProduce;
                dataBlock[i].ToProduce = default(WaterPropertyBlockF32);
            }

            // consume stuff

            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                for(WaterPropertyId j = 0; j <= WaterPropertyId.TRACKED_MAX; ++j)
                {
                    float desired = data.ToConsume[j];
                    if (desired > 0)
                    {
                        float canConsume = Math.Min(desired, environment[j]);
                        environment[j] -= canConsume;
                        data.ToConsume[j] -= canConsume;
                    }
                }
            }

            // eat stuff

            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                uint remainingFood = (uint) data.ToConsume[WaterPropertyId.Food];
                if (remainingFood > 0)
                {
                    ListSlice<int> foodTypes = profiles[i].EatTargetIndices();
                    for(int j = 0; j < foodTypes.Length && remainingFood > 0; j++)
                    {
                        int targetIdx = foodTypes[j];
                        uint actualEat = profiles[targetIdx].TryEat(ref dataBlock[targetIdx], remainingFood);
                        remainingFood -= actualEat;
                    }

                    data.ToConsume[WaterPropertyId.Food] = remainingFood;
                }
                data.AttemptedEat = true;
            }

            SimulationResult result = default(SimulationResult);
            result.Environment = environment;

            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].EndTick(ref dataBlock[i]);
                profiles[i].CopyTo(dataBlock[i], ref result);
            }

            return result;
        }
    }
}