using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    static public class Simulator
    {
        public const int MaxTrackedCritters = 8;
        public const float MaxEatProportion = 0.75f;
        public const float MaxReproduceProportion = 0.75f;
        public const float MaxDeathProportion = 0.5f;
        public const uint HungerPerCritter = 32;

        static public readonly WaterPropertyId[] PreemptiveProperties = new WaterPropertyId[] { WaterPropertyId.Light };
        static public readonly WaterPropertyId[] SecondaryProperties = new WaterPropertyId[] { WaterPropertyId.Oxygen, WaterPropertyId.CarbonDioxide, WaterPropertyId.PH, WaterPropertyId.Temperature };
        static private readonly WaterPropertyMask ConsumeMask = new WaterPropertyMask(SecondaryProperties);

        private const int FixedShift = 12;
        private const ulong FixedOne = 1 << FixedShift;

        /// <summary>
        /// Generates a single result from the given profile and initial data.
        /// </summary>
        static public unsafe SimulationResult Simulate(SimulationProfile inProfile, in SimulationResult inInitial, SimulatorFlags inFlags)
        {
            SimulationResultDetails _;
            return Simulate(inProfile, inInitial, inFlags, out _);
        }

        /// <summary>
        /// Generates a single result from the given profile and initial data.
        /// </summary>
        static public unsafe SimulationResult Simulate(SimulationProfile inProfile, in SimulationResult inInitial, SimulatorFlags inFlags, out SimulationResultDetails outDetails)
        {
            bool bLogging = (inFlags & SimulatorFlags.Debug) != 0;
            bool bDetails = (inFlags & SimulatorFlags.OutputDetails) != 0;

            if (bLogging && inInitial.Timestamp == 0)
            {
                Log.Msg(Dump(inInitial));
            }

            outDetails = default(SimulationResultDetails);

            WaterPropertyBlockF32 environment = inInitial.Environment;
            SimulationRandom random = inInitial.Random;
            IReadOnlyList<CritterProfile> profiles = inProfile.Critters();
            int critterCount = profiles.Count;

            // no static cached arrays here - since CritterData is just a data struct we can allocate it all on the stack!
            CritterData* dataBlock = stackalloc CritterData[critterCount];
            uint* massToConsume = stackalloc uint[MaxTrackedCritters];

            // ensure light levels are correct
            WaterPropertyBlockF32 initialEnv = inProfile.InitialState.Environment;
            environment.Light = initialEnv.Light;

            if (bLogging)
            {
                Log.Format("[Simulator] Beginning tick {0}", inInitial.Timestamp);
            }
            if (bDetails)
            {
                outDetails.StartingEnvironment = environment;
            }

            // setup
            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].CopyFrom(ref dataBlock[i], inInitial);
                profiles[i].SetupTick(ref dataBlock[i], environment, inFlags);

                if (bDetails)
                {
                    outDetails.StartingStates.Add(dataBlock[i].State);
                }
            }

            if (bLogging)
            {
                Log.Format("[Simulator] Performing preemptive consumption");
            }

            // preemptive consume
            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                WaterPropertyBlockF32 toConsume = default;
                for(int j = 0; j < PreemptiveProperties.Length; ++j)
                {
                    WaterPropertyId prop = PreemptiveProperties[j];
                    float desired = data.ToConsume[prop];
                    if (desired > 0)
                    {
                        float canConsume = Math.Min(desired, environment[prop]);
                        environment[prop] -= canConsume;
                        data.ToConsume[prop] -= canConsume;
                        
                        if (bLogging && canConsume > 0)
                        {
                            Log.Msg("[Simulator] Critter '{0}' consumed {1} of {2}", profiles[i].Id(), canConsume, prop);
                        }

                        toConsume[prop] = canConsume;
                    }
                }

                if (bDetails)
                {
                    outDetails.Consumed.Add(toConsume);
                }
            }

            // reevaluate state
            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].ReevaluateState(ref dataBlock[i], environment, inFlags, ConsumeMask);
                if (bDetails)
                {
                    outDetails.AfterLightStates.Add(dataBlock[i].State);
                }
            }

            if (bLogging)
            {
                Log.Format("[Simulator] Performing production and consumption");
            }

            // produce stuff
            for(int i = 0; i < critterCount; ++i)
            {
                if (dataBlock[i].Population > 0)
                {
                    WaterPropertyBlockF32 produce = profiles[i].CalculateProduction(dataBlock[i]);

                    if (bLogging)
                    {
                        Log.Msg("[Simulator] Critter '{0}' produced {1}", profiles[i].Id(), produce);
                    }

                    environment += produce;
                    if (bDetails)
                    {
                        outDetails.Produced.Add(produce);
                    }
                }
            }

            // consume stuff
            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                WaterPropertyBlockF32 consumed = default;
                if (bDetails)
                {
                    consumed = outDetails.Consumed[i];
                }

                for(WaterPropertyId prop = 0; prop <= WaterPropertyId.TRACKED_MAX; ++prop)
                {
                    float desired = data.ToConsume[prop];
                    if (desired > 0)
                    {
                        float canConsume = Math.Min(desired, environment[prop]);
                        environment[prop] -= canConsume;
                        data.ToConsume[prop] -= canConsume;
                        
                        if (bLogging && canConsume > 0)
                        {
                            Log.Msg("[Simulator] Critter '{0}' consumed {1} of {2}", profiles[i].Id(), canConsume, prop);
                        }

                        consumed[prop] += canConsume;
                    }
                }

                if (bDetails)
                {
                    outDetails.Consumed[i] = consumed;
                }
            }

            // food pass
            for(int i = 0; i < critterCount; ++i)
            {
                ref CritterData data = ref dataBlock[i];
                uint remainingFood = data.Hunger;
                if (remainingFood <= 0)
                    continue;

                ListSlice<CritterProfile.EatConfig> eatConfigs = profiles[i].EatTargets(data.State);
                int eatCount = eatConfigs.Length;
                CritterProfile.EatConfig eatTarget;

                uint foodToAllocate = remainingFood;

                // initial pass, proportional allocation
                for(int j = 0; j < eatCount; j++)
                {
                    eatTarget = eatConfigs[j];
                    short targetIndex = eatTarget.Index;
                    if (targetIndex >= 0 && dataBlock[targetIndex].Population > 0)
                    {
                        uint targetTotalMass = profiles[targetIndex].MassPerPopulation() * dataBlock[targetIndex].Population;
                        uint targetMass = FixedMultiply(FixedMultiply(remainingFood, eatTarget.MassScale), eatTarget.Proportion); // convert to mass units
                        if (targetMass > targetTotalMass)
                            targetMass = targetTotalMass;

                        massToConsume[j] = targetMass;
                        foodToAllocate -= Math.Min(foodToAllocate, FixedMultiply(targetMass, eatTarget.MassScaleInv)); // back to hunger units
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
                            uint remainingTotalMass = Math.Max(0, profiles[targetIndex].MassPerPopulation() * dataBlock[targetIndex].Population - massToConsume[j]);
                            uint targetMass = FixedMultiply(foodToAllocate, eatTarget.MassScale); // to mass units
                            if (targetMass > remainingTotalMass)
                                targetMass = remainingTotalMass;

                            massToConsume[j] += targetMass;
                            foodToAllocate -= Math.Min(foodToAllocate, FixedMultiply(targetMass, eatTarget.MassScaleInv)); // to hunger units
                        }
                    }
                }

                for(int j = 0; j < eatConfigs.Length && remainingFood > 0; j++)
                {
                    eatTarget = eatConfigs[j];
                    short targetIndex = eatTarget.Index;
                    if (eatTarget.Index >= 0 && massToConsume[j] > 0)
                    {
                        uint massToEat = massToConsume[j];
                        uint actualEat = profiles[targetIndex].TryBeEaten(ref dataBlock[targetIndex], massToEat);
                        if (actualEat > 0)
                        {
                            remainingFood -= FixedMultiply(actualEat, eatTarget.MassScaleInv); // convert back from mass to hunger
                            if (bLogging)
                            {
                                Log.Msg("[Simulator] Critter '{0}' consumed {1} mass of '{2}'", profiles[i].Id(), actualEat, profiles[targetIndex].Id());
                            }

                            if (bDetails)
                            {
                                uint popEaten = actualEat / profiles[targetIndex].MassPerPopulation();
                                outDetails.Eaten.Add(new CritterEatDetails((ushort) i, (ushort) targetIndex, popEaten));
                            }
                        }
                    }
                }

                data.Hunger = remainingFood;
            }

            SimulationResult result = default(SimulationResult);
            result.Timestamp = (ushort) (inInitial.Timestamp + 1);
            result.Random = random;
            result.Environment = environment;

            for(int i = 0; i < critterCount; ++i)
            {
                profiles[i].EndTick(ref dataBlock[i], inFlags, ref outDetails);
                profiles[i].CopyTo(dataBlock[i], ref result);
            }

            if (bLogging)
            {
                Log.Msg(Dump(result));
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
        static public void GenerateToBuffer(SimulationProfile inProfile, SimulationResult[] ioResults, SimulatorFlags inFlags = 0)
        {
            SimulationResult initial = inProfile.InitialState;
            ioResults[0] = initial;
            GenerateToBuffer(inProfile, initial, ioResults, 1, ioResults.Length - 1, inFlags);
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, SimulationResult[] ioResults, SimulationResultDetails[] ioDetails, SimulatorFlags inFlags = 0)
        {
            SimulationResult initial = inProfile.InitialState;
            ioResults[0] = initial;
            GenerateToBuffer(inProfile, initial, ioResults, ioDetails, 1, ioResults.Length - 1, inFlags);
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, SimulatorFlags inFlags = 0)
        {
            ioResults[0] = inInitial;
            GenerateToBuffer(inProfile, inInitial, ioResults, 1, ioResults.Length - 1, inFlags);
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, SimulationResultDetails[] ioDetails, SimulatorFlags inFlags = 0)
        {
            ioResults[0] = inInitial;
            GenerateToBuffer(inProfile, inInitial, ioResults, ioDetails, 1, ioResults.Length - 1, inFlags);
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, int inStartIdx, int inLength, SimulatorFlags inFlags = 0)
        {
            SimulationResult current = inInitial;
            for(int i = 0; i < inLength; ++i)
            {
                current = Simulator.Simulate(inProfile, current, inFlags);
                ioResults[inStartIdx + i] = current;
            }
        }

        /// <summary>
        /// Fills the given buffer with simulation results.
        /// </summary>
        static public void GenerateToBuffer(SimulationProfile inProfile, in SimulationResult inInitial, SimulationResult[] ioResults, SimulationResultDetails[] ioDetails, int inStartIdx, int inLength, SimulatorFlags inFlags = 0)
        {
            SimulationResult current = inInitial;
            inFlags |= SimulatorFlags.OutputDetails;
            for(int i = 0; i < inLength; ++i)
            {
                current = Simulator.Simulate(inProfile, current, inFlags, out ioDetails[inStartIdx + i]);
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

        static private long ToFixed(float inValue)
        {
            return (long) Math.Round(inValue * FixedOne);
        }

        static private long ToFixed(uint inValue)
        {
            return (long) inValue << FixedShift;
        }

        static private uint ToUInt(long inFixed)
        {
            return (uint) (inFixed >> FixedShift);
        }

        /// <summary>
        /// Fixed-point multiplication.
        /// </summary>
        static public uint FixedMultiply(uint inValue, float inMultiply)
        {
            long fixedA = ToFixed(inValue);
            long fixedB = ToFixed(inMultiply);
            long fixedC = (fixedA * fixedB) >> FixedShift;
            return ToUInt(fixedC);
        }

        /// <summary>
        /// Fixed-point division.
        /// </summary>
        static public uint FixedDivide(uint inValue, float inMultiply)
        {
            long fixedA = ToFixed(inValue);
            long fixedB = ToFixed(inMultiply);
            long fixedC = (fixedA << FixedShift) / fixedB;
            return ToUInt(fixedC);
        }
    }

    [Flags]
    public enum SimulatorFlags : byte
    {
        Debug = 0x01,
        OutputDetails = 0x02
    }
}