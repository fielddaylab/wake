using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    static public class Simulator
    {
        public const int MaxTrackedCritters = 8;

        /// <summary>
        /// Generates a single result from the given profile and initial data.
        /// </summary>
        static public unsafe SimulationResult Simulate(SimulationProfile inProfile, in SimulationResult inInitial, SimulatorFlags inFlags)
        {
            bool bLogging = (inFlags & SimulatorFlags.Debug) != 0;

            WaterPropertyBlockF32 environment = inInitial.Environment;
            IReadOnlyList<CritterProfile> profiles = inProfile.Critters();
            int critterCount = profiles.Count;

            // no static cached arrays here - since CritterData is just a data struct we can allocate it all on the stack!
            CritterData* dataBlock = stackalloc CritterData[critterCount];

            // setup
            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].CopyFrom(ref dataBlock[i], inInitial);
                profiles[i].SetupTick(ref dataBlock[i], environment, inFlags);
            }

            // produce stuff
            for(int i = 0; i < critterCount; ++i)
            {
                if (dataBlock[i].Population > 0)
                {
                    WaterPropertyBlockF32 produce = profiles[i].CalculateProduction(dataBlock[i]);

                    if (bLogging)
                    {
                        Debug.LogFormat("[Simulator] Critter '{0}' produced {1}", profiles[i].Id().ToDebugString(), produce);
                    }

                    environment += produce;
                }
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
                        
                        if (bLogging && canConsume > 0)
                        {
                            Debug.LogFormat("[Simulator] Critter '{0}' consumed {1} of {2}", profiles[i].Id().ToDebugString(), canConsume, j);
                        }
                    }
                }
            }

            // eat stuff

            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                uint remainingFood = (uint) data.Hunger;
                if (remainingFood > 0)
                {
                    ListSlice<int> foodTypes = profiles[i].EatTargetIndices();
                    for(int j = 0; j < foodTypes.Length && remainingFood > 0; j++)
                    {
                        int targetIdx = foodTypes[j];
                        if (dataBlock[targetIdx].Population > 0)
                        {
                            uint actualEat = profiles[targetIdx].TryEat(ref dataBlock[targetIdx], remainingFood);
                            remainingFood -= actualEat;

                            if (bLogging)
                            {
                                Debug.LogFormat("[Simulator] Critter '{0}' consumed {1} mass of '{2}'", profiles[i].Id().ToDebugString(), actualEat, profiles[targetIdx].Id().ToDebugString());
                            }
                        }
                    }

                    data.Hunger = remainingFood;
                }
            }

            SimulationResult result = default(SimulationResult);
            result.Timestamp = (ushort) (inInitial.Timestamp + 1);
            result.Environment = environment;

            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].EndTick(ref dataBlock[i], inFlags);
                profiles[i].CopyTo(dataBlock[i], ref result);
            }

            if (bLogging)
            {
                Debug.LogFormat(Dump(result));
            }

            return result;
        }
    
        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, SimulationResult[] ioResults, int inTickScale = 1, SimulatorFlags inFlags = 0)
        {
            SimulationResult initial = inProfile.InitialState;
            ioResults[0] = initial;
            GenerateToBuffer(inProfile, initial, ioResults, 1, ioResults.Length - 1, inTickScale, inFlags);
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, int inStartIdx, int inLength, int inTickScale = 1, SimulatorFlags inFlags = 0)
        {
            SimulationResult current = inInitial;
            for(int i = 0; i < inLength; ++i)
            {
                int counter = inTickScale;
                while(counter-- > 0)
                {
                    current = Simulator.Simulate(inProfile, current, inFlags);
                }
                ioResults[inStartIdx + i] = current;
            }
        }

        /// <summary>
        /// Dumps the given simulation result to string.
        /// </summary>
        static public string Dump(in SimulationResult inResult)
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append("[Simulator] Values at Tick ").Append(inResult.Timestamp)
                    .Append("\n ").Append(inResult.Environment);
                for(int i = 0; i < inResult.Actors.Count; ++i)
                {
                    var actorStats = inResult.Actors[i];
                    psb.Builder.Append("\n ").Append(actorStats.Id.ToDebugString())
                        .Append(" Population = ").Append(actorStats.Population);
                }
                return psb.ToString();
            }
        }
    }

    [Flags]
    public enum SimulatorFlags : byte
    {
        Debug = 0x01
    }
}