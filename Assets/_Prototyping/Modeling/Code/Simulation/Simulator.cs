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
        public const int MaxTrackedCritters = 16;
        public const float MaxEatProportion = 0.75f;
        public const float MaxReproduceProportion = 0.75f;
        public const float MaxDeathProportion = 0.5f;
        public const uint HungerPerCritter = 32;

        /// <summary>
        /// Generates a single result from the given profile and initial data.
        /// </summary>
        static public unsafe SimulationResult Simulate(SimulationProfile inProfile, in SimulationResult inInitial, SimulatorFlags inFlags)
        {
            bool bLogging = (inFlags & SimulatorFlags.Debug) != 0;

            if (bLogging && inInitial.Timestamp == 0)
            {
                Debug.LogFormat(Dump(inInitial));
            }

            WaterPropertyBlockF32 environment = inInitial.Environment;
            SimulationRandom random = inInitial.Random;
            IReadOnlyList<CritterProfile> profiles = inProfile.Critters();
            int critterCount = profiles.Count;

            // no static cached arrays here - since CritterData is just a data struct we can allocate it all on the stack!
            CritterData* dataBlock = stackalloc CritterData[critterCount];
            double* massToConsume = stackalloc double[MaxTrackedCritters];

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

            // initial food pass
            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                double remainingFood = data.Hunger;
                if (remainingFood <= 0)
                    continue;

                ListSlice<CritterProfile.EatConfig> eatConfigs = profiles[i].EatTargets(data.State);
                int eatCount = eatConfigs.Length;
                CritterProfile.EatConfig eatTarget;

                double foodToAllocate = remainingFood;

                // initial pass, proportional allocation
                for(int j = 0; j < eatCount; j++)
                {
                    eatTarget = eatConfigs[j];
                    short targetIndex = eatTarget.Index;
                    if (targetIndex >= 0 && dataBlock[targetIndex].Population > 0)
                    {
                        uint targetTotalMass = profiles[targetIndex].MassPerPopulation() * dataBlock[targetIndex].Population;
                        double targetMass = remainingFood * eatTarget.MassScale * eatTarget.Proportion; // convert to mass units
                        if (targetMass > targetTotalMass)
                            targetMass = targetTotalMass;

                        massToConsume[j] = targetMass;
                        foodToAllocate -= Math.Min(foodToAllocate, targetMass * eatTarget.MassScaleInv); // back to hunger units
                    }
                    else
                    {
                        massToConsume[j] = 0;
                    }
                }

                // if remainder, allocate as much as possible
                if (foodToAllocate > 0)
                {
                    for(int j = 0; j < eatCount && foodToAllocate > 0; j++)
                    {
                        eatTarget = eatConfigs[j];
                        short targetIndex = eatTarget.Index;
                        if (targetIndex >= 0 && dataBlock[targetIndex].Population > 0)
                        {
                            double remainingTotalMass = Math.Max(0, profiles[targetIndex].MassPerPopulation() * dataBlock[targetIndex].Population - massToConsume[j]);
                            double targetMass = foodToAllocate * eatTarget.MassScale; // to mass units
                            if (targetMass > remainingTotalMass)
                                targetMass = remainingTotalMass;

                            massToConsume[j] += targetMass;
                            foodToAllocate -= Math.Min(foodToAllocate, targetMass * eatTarget.MassScaleInv); // to hunger units
                        }
                    }
                }

                for(int j = 0; j < eatConfigs.Length && remainingFood > 0; j++)
                {
                    eatTarget = eatConfigs[j];
                    short targetIndex = eatTarget.Index;
                    if (eatTarget.Index >= 0 && massToConsume[j] > 0)
                    {
                        uint massToEat = (uint) Math.Round(massToConsume[j]); // this accounts for any tiny errors between full model and player model
                        uint actualEat = profiles[targetIndex].TryBeEaten(ref dataBlock[targetIndex], massToEat);
                        remainingFood -= actualEat * eatTarget.MassScaleInv; // convert back from mass to hunger
                        if (bLogging)
                        {
                            Debug.LogFormat("[Simulator] Critter '{0}' consumed {1} mass of '{2}'", profiles[i].Id().ToDebugString(), actualEat, profiles[targetIndex].Id().ToDebugString());
                        }
                    }
                }

                data.Hunger = (uint) remainingFood;
            }

            SimulationResult result = default(SimulationResult);
            result.Timestamp = (ushort) (inInitial.Timestamp + 1);
            result.Random = random;
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

        // Shuffles a set of indices
        static private unsafe void Shuffle(short* ioIndices, int inLength, ref SimulationRandom ioRandom)
        {
            uint i = (uint) inLength;
            uint j;
            while (--i > 0)
            {
                short old = ioIndices[i];
                ioIndices[i] = ioIndices[j = ioRandom.Next(i + 1)];
                ioIndices[j] = old;
            }
        }

        // Shuffles a set of indices
        static private unsafe void Shuffle(int* ioIndices, int inLength, ref SimulationRandom ioRandom)
        {
            uint i = (uint) inLength;
            uint j;
            while (--i > 0)
            {
                int old = ioIndices[i];
                ioIndices[i] = ioIndices[j = ioRandom.Next(i + 1)];
                ioIndices[j] = old;
            }
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
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, int inTickScale = 1, SimulatorFlags inFlags = 0)
        {
            ioResults[0] = inInitial;
            GenerateToBuffer(inProfile, inInitial, ioResults, 1, ioResults.Length - 1, inTickScale, inFlags);
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