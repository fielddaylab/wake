using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using ActorInfo = Aqua.Modeling.SimProfile.ActorInfo;
using BehaviorInfo = Aqua.Modeling.SimProfile.BehaviorInfo;
using EatInfo = Aqua.Modeling.SimProfile.EatInfo;
using ParasiteInfo = Aqua.Modeling.SimProfile.ParasiteInfo;

namespace Aqua.Modeling
{
    static public unsafe class Simulation {

        #region Consts

        public const int MaxTrackedCritters = 12;
        public const float MaxEatProportion = 0.75f;
        public const float MaxReproduceProportion = 0.75f;
        public const float MaxDeathProportion = 0.5f;
        public const uint HungerPerPopulation = 32;

        public const float UpperCarbonDioxideThreshold = 40;
        public const float LowerCarbonDioxideThreshold = 10;
        public const float IncreasePHRatio = 0.002f;
        public const float DecreasePHRatio = 0.002f;

        public const int MaxTicks = 24;

        #endregion // Consts

        #region Types

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        public struct PopulationPair {
            public uint Alive;
            public uint Stressed;

            [MethodImpl(256)]
            public uint Total() {
                return Alive + Stressed;
            }
        }

        public struct ProduceConsumePair {
            public SimProduceConsumeSnapshot Alive;
            public SimProduceConsumeSnapshot Stressed;

            [MethodImpl(256)]
            public SimProduceConsumeSnapshot Total() {
                return new SimProduceConsumeSnapshot() {
                    Oxygen = Alive.Oxygen + Stressed.Oxygen,
                    CarbonDioxide = Alive.CarbonDioxide + Stressed.CarbonDioxide
                };
            }
        }

        public class Buffer : IDisposable {
            static public readonly int BufferSize = sizeof(PopulationPair) * 4 * MaxTrackedCritters + sizeof(ProduceConsumePair) * MaxTrackedCritters;

            public PopulationPair* Populations;
            public ProduceConsumePair* Unconsumed;
            public PopulationPair* Hungers;
            public PopulationPair* PopulationsConsumed;
            public PopulationPair* MassConsumptionBuffer;
            public WaterPropertyBlockF32 Water;
            public StringBuilder DebugReport;

            private Unsafe.ArenaHandle m_Allocator;

            public Buffer() {
                m_Allocator = Unsafe.CreateArena(BufferSize);
                Populations = Unsafe.AllocArray<PopulationPair>(m_Allocator, MaxTrackedCritters);
                Unconsumed = Unsafe.AllocArray<ProduceConsumePair>(m_Allocator, MaxTrackedCritters);
                Hungers = Unsafe.AllocArray<PopulationPair>(m_Allocator, MaxTrackedCritters);
                PopulationsConsumed = Unsafe.AllocArray<PopulationPair>(m_Allocator, MaxTrackedCritters);
                MassConsumptionBuffer = Unsafe.AllocArray<PopulationPair>(m_Allocator, MaxTrackedCritters);
            }

            public Buffer(Unsafe.ArenaHandle allocator) {
                Populations = Unsafe.AllocArray<PopulationPair>(allocator, MaxTrackedCritters);
                Unconsumed = Unsafe.AllocArray<ProduceConsumePair>(allocator, MaxTrackedCritters);
                Hungers = Unsafe.AllocArray<PopulationPair>(allocator, MaxTrackedCritters);
                PopulationsConsumed = Unsafe.AllocArray<PopulationPair>(allocator, MaxTrackedCritters);
                MassConsumptionBuffer = Unsafe.AllocArray<PopulationPair>(allocator, MaxTrackedCritters);
            }

            public void Dispose() {
                Unsafe.TryDestroyArena(ref m_Allocator);
                Populations = null;
                Unconsumed = null;
                Hungers = null;
                PopulationsConsumed = null;
                MassConsumptionBuffer = null;
                if (DebugReport != null) {
                    DebugReport.Length = 0;
                    DebugReport = null;
                }
            }
        }

        #endregion // Types

        #region Structure

        /// <summary>
        /// Generates the initial snapshot for the given sim and profile.
        /// </summary>
        static public SimSnapshot GenerateInitialSnapshot(SimProfile profile, BFSim sim, Predicate<StringHash32> organismFilter = null) {
            SimSnapshot snapshot = default;
            snapshot.Water = profile.Water;

            ActorCountU32 actorPop;
            int actorIdx;
            for(int i = 0; i < sim.InitialActors.Length; i++) {
                actorPop = sim.InitialActors[i];
                if (organismFilter != null && !organismFilter(actorPop.Id)) {
                    continue;
                }
                
                actorIdx = profile.IndexOfActorType(actorPop.Id);
                if (actorIdx >= 0) {
                    snapshot.Populations[actorIdx] = actorPop.Population;
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Prepares the given buffer for simulation.
        /// </summary>
        static public void Prepare(Buffer buffer, SimProfile profile, SimSnapshot start) {
            PopulationPair* p;
            float stressed;
            for(int i = 0; i < profile.ActorCount; i++) {
                // copy populations
                p = &buffer.Populations[i];
                stressed = start.StressedRatio[i] / 128f;
                p->Stressed = SimMath.FixedMultiply(start.Populations[i], stressed);
                p->Alive = start.Populations[i] - p->Stressed;

                // reset temporary buffers
                buffer.MassConsumptionBuffer[i] = default;
                buffer.Unconsumed[i] = default;
                buffer.Hungers[i] = default;
                buffer.PopulationsConsumed[i] = default;
            }

            buffer.Water = start.Water;
        }

        /// <summary>
        /// Simulates one tick.
        /// </summary>
        static public SimSnapshot Simulate(Buffer buffer, SimProfile profile, SimSnapshot start, uint inTimestamp, SimulationFlags flags = 0) {
            bool bLogging = (flags & SimulationFlags.Debug) != 0;
            bool bDetails = (flags & SimulationFlags.DetailedOutput) != 0;

            ActorInfo* actorInfo;

            if (buffer.DebugReport != null) {
                buffer.DebugReport.Length = 0;
            }

            if (bLogging) {
                Report(buffer, "--- Beginning Tick {0}", inTimestamp);
            }

            // reset light
            buffer.Water.Light = profile.Water.Light;

            if (bLogging) {
                Report(buffer, "Initial environment conditions: {0}", buffer.Water.ToString());
            }

            // light reductions
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];
                float lightReduced = PerformLightAbsorption(actorInfo, &actorInfo->AliveBehavior, buffer.Populations[i].Alive, ref buffer.Water);
                lightReduced += PerformLightAbsorption(actorInfo, &actorInfo->StressedBehavior, buffer.Populations[i].Stressed, ref buffer.Water);

                if (bLogging && lightReduced > 0) {
                    Report(buffer, "{0} reduced light by {1}", actorInfo->Id.ToDebugString(), lightReduced);
                }
            }
            
            // water stress eval
            for(int i = 0; i < profile.ActorCount; i++) {
                EvaluateWaterStress(&profile.Actors[i], ref buffer.Populations[i].Alive, ref buffer.Populations[i].Stressed, buffer.Water);
            }

            // parasite eval
            for(int i = 0; i < profile.ParasiteCount; i++) {
                ParasiteInfo parasite = profile.Parasites[i];
                PopulationPair* source = &buffer.Populations[parasite.Index];
                PopulationPair* target = &buffer.Populations[parasite.Target];

                uint totalSource = source->Total();
                uint totalTarget = target->Total();

                if (totalSource == 0 || totalTarget == 0) {
                    continue;
                }

                uint desiredAffected = SimMath.FixedMultiply(totalSource, parasite.Affected);
                uint newAffected = 0;
                if (desiredAffected > target->Stressed) {
                    newAffected = Math.Min(desiredAffected - target->Stressed, target->Alive);
                }

                target->Alive -= newAffected;
                target->Stressed += newAffected;

                if (bLogging) {
                    Report(buffer, "{0} parasited {1} {2}", profile.Actors[parasite.Index].Id.ToDebugString(), newAffected, profile.Actors[parasite.Target].Id.ToDebugString());
                }
            }

            // report final states
            if (bLogging) {
                for(int i = 0; i < profile.ActorCount; i++) {
                    actorInfo = &profile.Actors[i];
                    Report(buffer, "{0} population: {1} alive, {2} stressed", actorInfo->Id.ToDebugString(), buffer.Populations[i].Alive, buffer.Populations[i].Stressed);
                }
            }

            // produce (environment)
            buffer.Water.Oxygen += profile.OxygenPerTick;
            buffer.Water.CarbonDioxide += profile.CarbonDioxidePerTick;

            Report(buffer, "Environment added added {0} oxygen and {1} carbon dioxide", profile.OxygenPerTick, profile.CarbonDioxidePerTick);

            // produce (actor)
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];
                
                SimProduceConsumeSnapshot alive = PerformProduce(actorInfo, &actorInfo->AliveBehavior, buffer.Populations[i].Alive, ref buffer.Water);
                SimProduceConsumeSnapshot stress = PerformProduce(actorInfo, &actorInfo->StressedBehavior, buffer.Populations[i].Stressed, ref buffer.Water);

                if (bLogging) {
                    Report(buffer, "{0} produced {1} oxygen and {2} carbon dioxide", actorInfo->Id.ToDebugString(), alive.Oxygen + stress.Oxygen, alive.CarbonDioxide + stress.CarbonDioxide);
                }
            }

            // consume (actor)
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];

                SimProduceConsumeSnapshot alive = PerformConsume(actorInfo, &actorInfo->AliveBehavior, buffer.Populations[i].Alive, ref buffer.Water, out buffer.Unconsumed[i].Alive);
                SimProduceConsumeSnapshot stress = PerformConsume(actorInfo, &actorInfo->StressedBehavior, buffer.Populations[i].Stressed, ref buffer.Water, out buffer.Unconsumed[i].Stressed);

                if (bLogging) {
                    Report(buffer, "{0} consumed {1} oxygen and {2} carbon dioxide", actorInfo->Id.ToDebugString(), alive.Oxygen + stress.Oxygen, alive.CarbonDioxide + stress.CarbonDioxide);
                }
            }

            // carbon dioxide / ph exchange
            float phAdjust;
            ProcessCarbonDioxideExchange(ref buffer.Water, out phAdjust);
            if (bLogging && phAdjust != 0) {
                Report(buffer, "Carbon Dioxide levels adjusted PH by {0}; final values {1} carbon dioxide, {2} ph", phAdjust, buffer.Water.CarbonDioxide, buffer.Water.PH);
            }

            if (bLogging) {
                Report(buffer, "Current environment conditions: {0}", buffer.Water.ToString());
            }

            // stress eval (the second) + hunger values
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];
                EvaluateWaterStress(actorInfo, ref buffer.Populations[i].Alive, ref buffer.Populations[i].Stressed, buffer.Water);
                if (bLogging) {
                    Report(buffer, "{0} population: {1} alive, {2} stressed", actorInfo->Id.ToDebugString(), buffer.Populations[i].Alive, buffer.Populations[i].Stressed);
                }

                buffer.PopulationsConsumed[i] = default;
                
                if ((actorInfo->Flags & SimProfile.ActorFlags.Alive_DoesNotEat) == 0) {
                    buffer.Hungers[i].Alive = buffer.Populations[i].Alive * profile.HungerPerPopulation;
                } else {
                    buffer.Hungers[i].Alive = 0;
                }
                
                if ((actorInfo->Flags & SimProfile.ActorFlags.Stressed_DoesNotEat) == 0) {
                    buffer.Hungers[i].Stressed = buffer.Populations[i].Stressed * profile.HungerPerPopulation;
                } else {
                    buffer.Hungers[i].Stressed = 0;
                }
            }

            // eating
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];
                int eatCount = actorInfo->EatCount;
                if (eatCount == 0) {
                    continue;
                }

                EatInfo* eatStart = &profile.Eats[actorInfo->EatOffset];

                // treat these as uint pointers instead, allows nice indexing instead of branching
                uint* sourcePopulation = (uint*) (&buffer.Populations[i]);
                uint* sourceHunger = (uint*) &buffer.Hungers[i];

                // make sure to clean this up so creatures that have already been eaten have the correct hunger values
                ProcessEatenReductions(actorInfo, &actorInfo->AliveBehavior, ref sourceHunger[0], ref buffer.Unconsumed[i].Alive, ref buffer.PopulationsConsumed[i].Alive);
                ProcessEatenReductions(actorInfo, &actorInfo->StressedBehavior, ref sourceHunger[1], ref buffer.Unconsumed[i].Stressed, ref buffer.PopulationsConsumed[i].Stressed);

                // clear eat mass buffer
                for(int j = 0; j < eatCount; j++) {
                    buffer.MassConsumptionBuffer[j] = default;
                }

                ActorInfo* targetActorInfo;
                EatInfo* eat;
                int eatTargetIndex;
                int sourceIndex;
                ulong foodToAllocateBacking = 0;
                uint* foodToAllocate = (uint*) &foodToAllocateBacking;

                uint hungerEffect, consumedMass;

                foodToAllocate[0] = sourceHunger[0];
                foodToAllocate[1] = sourceHunger[1];

                // initial pass, proportional allocation
                for(int j = 0; j < eatCount; j++) {
                    eat = &eatStart[j];
                    sourceIndex = (int) eat->State;
                    eatTargetIndex = eat->Target;
                    targetActorInfo = &profile.Actors[eatTargetIndex];

                    EvaluatePotentialEat(targetActorInfo, eat, buffer.Populations[eatTargetIndex], sourceHunger[sourceIndex], eat->Proportion, out hungerEffect, out consumedMass);
                    ((uint*) &buffer.MassConsumptionBuffer[j])[sourceIndex] = consumedMass;
                    foodToAllocate[sourceIndex] -= Math.Min(foodToAllocate[sourceIndex], hungerEffect);
                }

                // second pass to allocate remainder
                for(int j = 0; j < eatCount && foodToAllocateBacking > 0; j++) {
                    eat = &eatStart[j];
                    sourceIndex = (int) eat->State;
                    eatTargetIndex = eat->Target;
                    targetActorInfo = &profile.Actors[eatTargetIndex];

                    EvaluatePotentialEat(targetActorInfo, eat, buffer.Populations[eatTargetIndex], foodToAllocate[sourceIndex], 1, out hungerEffect, out consumedMass);
                    ((uint*) &buffer.MassConsumptionBuffer[j])[sourceIndex] += consumedMass;
                    foodToAllocate[sourceIndex] -= Math.Min(foodToAllocate[sourceIndex], hungerEffect);
                }

                // actually perform eating
                for(int j = 0; j < eatCount; j++) {
                    eat = &eatStart[j];
                    sourceIndex = (int) eat->State;
                    eatTargetIndex = eat->Target;
                    targetActorInfo = &profile.Actors[eatTargetIndex];

                    consumedMass = (uint) Math.Min(sourceHunger[sourceIndex] * eat->FoodToMass, ((uint*) &buffer.MassConsumptionBuffer[j])[sourceIndex]);

                    PerformEaten(targetActorInfo, ref buffer.Populations[eatTargetIndex], consumedMass, out consumedMass, ref buffer.PopulationsConsumed[eatTargetIndex]);
                    hungerEffect = SimMath.FixedMultiply(consumedMass, eat->MassToFood);
                    sourceHunger[sourceIndex] -= Math.Min(sourceHunger[sourceIndex], hungerEffect);

                    if (bLogging && consumedMass > 0) {
                        Report(buffer, "{0} ate {1} mass of {2}", actorInfo->Id.ToDebugString(), consumedMass, targetActorInfo->Id.ToDebugString());
                    }
                }
            }

            // death
            uint detritusProduced = 0;
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];

                // clean up hunger and consumed populations
                ProcessEatenReductions(actorInfo, &actorInfo->AliveBehavior, ref buffer.Hungers[i].Alive, ref buffer.Unconsumed[i].Alive, ref buffer.PopulationsConsumed[i].Alive);
                ProcessEatenReductions(actorInfo, &actorInfo->StressedBehavior, ref buffer.Hungers[i].Stressed, ref buffer.Unconsumed[i].Stressed, ref buffer.PopulationsConsumed[i].Stressed);

                if (bLogging) {
                    SimProduceConsumeSnapshot unconsumed = buffer.Unconsumed[i].Total();
                    Report(buffer, "{0} entering death phase with {1} food, {2} oxygen, and {3} carbondioxide unconsumed", actorInfo->Id.ToDebugString(), buffer.Hungers[i].Total(), unconsumed.Oxygen, unconsumed.CarbonDioxide);
                }

                PopulationPair starve;
                starve.Alive = EvaluateStarvation(actorInfo, &actorInfo->AliveBehavior, buffer.Populations[i].Alive, buffer.Hungers[i].Alive, buffer.Unconsumed[i].Alive);
                starve.Stressed = EvaluateStarvation(actorInfo, &actorInfo->StressedBehavior, buffer.Populations[i].Stressed, buffer.Hungers[i].Stressed, buffer.Unconsumed[i].Stressed);
                
                uint totalDeath = PerformDeath(actorInfo, ref buffer.Populations[i], starve);

                detritusProduced += totalDeath * actorInfo->MassPerPopulation;

                if (bLogging && totalDeath > 0) {
                    Report(buffer, "{0} of {1} died ({2} requested starvation)", totalDeath, actorInfo->Id.ToDebugString(), starve.Total());
                }
            }

            // add to detritus population, if it's being tracked
            if (profile.DetritusIndex >= 0 && detritusProduced > 0) {
                actorInfo = &profile.Actors[profile.DetritusIndex];

                detritusProduced /= actorInfo->MassPerPopulation;
                detritusProduced = PerformGenericPopulationIncrease(actorInfo, ref buffer.Populations[profile.DetritusIndex], detritusProduced);

                if (bLogging) {
                    Report(buffer, "Dead organisms converted to {0} units of detritus ({1} now)", detritusProduced, buffer.Populations[profile.DetritusIndex].Total());
                }
            }

            // reproduction and growth
            for(int i = 0; i < profile.ActorCount; i++) {
                actorInfo = &profile.Actors[i];

                uint growth = PerformReproductionAndGrowth(actorInfo, ref buffer.Populations[i]);

                if (bLogging && growth > 0) {
                    Report(buffer, "{0} grew population by {1}", actorInfo->Id.ToDebugString(), growth);
                }
            }

            if (bLogging) {
                for(int i = 0; i < profile.ActorCount; i++) {
                    actorInfo = &profile.Actors[i];
                    Report(buffer, "{0} population: {1} alive, {2} stressed", actorInfo->Id.ToDebugString(), buffer.Populations[i].Alive, buffer.Populations[i].Stressed);
                }

                Report(buffer, "--- Tick {0} finished!", inTimestamp);
            }

            return Export(buffer, profile);
        }

        /// <summary>
        /// Exports the given buffer to a snapshot.
        /// </summary>
        static public SimSnapshot Export(Buffer buffer, SimProfile profile) {
            SimSnapshot snapshot = default;
            snapshot.Water = buffer.Water;

            PopulationPair* p;
            uint u;
            for(int i = 0; i < profile.ActorCount; i++) {
                p = &buffer.Populations[i];
                u = p->Total();
                snapshot.Populations[i] = u;
                snapshot.StressedRatio[i] = (byte) (128 * ((float) p->Stressed / u));
            }
            return snapshot;
        }

        #endregion // Structure

        #region Evaluations

        /// <summary>
        /// Evaluates stress state based on water properties.
        /// </summary>
        static private ActorStateId EvaluateWaterStress(ActorInfo* actorInfo, ref uint alive, ref uint stressed, WaterPropertyBlockF32 water) {
            uint total = alive + stressed;
            if (total == 0) {
                return ActorStateId.Dead;
            }

            ActorStateId state = actorInfo->StateTransitions.Evaluate(water);
            switch(state) {
                case ActorStateId.Alive: {
                    alive = total;
                    stressed = 0;
                    break;
                }
                case ActorStateId.Stressed: {
                    alive = 0;
                    stressed = total;
                    break;
                }
                case ActorStateId.Dead: {
                    alive = 0;
                    stressed = 0;
                    break;
                }
            }
            return state;
        }

        /// <summary>
        /// Returns how many to kill of starvation.
        /// </summary>
        static private uint EvaluateStarvation(ActorInfo* actorInfo, BehaviorInfo* behavior, uint population, uint hunger, SimProduceConsumeSnapshot unconsumed) {
            if ((actorInfo-> Flags & SimProfile.ActorFlags.IgnoreStarvation) != 0) {
                return 0;
            }
            uint toKill = hunger / HungerPerPopulation;
            if (behavior->ConsumeOxygen > 0) {
                toKill = Math.Max(toKill, (uint) (unconsumed.Oxygen / behavior->ConsumeOxygen));
            }
            if (behavior->ConsumeCarbonDioxide > 0) {
                toKill = Math.Max(toKill, (uint) (unconsumed.CarbonDioxide / behavior->ConsumeCarbonDioxide));
            }
            return toKill;
        }

        /// <summary>
        /// Calculates the hunger and mass effect of eating this specific actor. 
        /// </summary>
        static private void EvaluatePotentialEat(ActorInfo* actorInfo, EatInfo* eat, PopulationPair population, uint sourceHunger, float proportion, out uint hungerEffect, out uint massToConsume) {
            uint totalMass = population.Total() * actorInfo->MassPerPopulation;
            if (totalMass == 0 || sourceHunger == 0) {
                hungerEffect = 0;
                massToConsume = 0;
                return;
            }

            uint mass = SimMath.FixedMultiply(sourceHunger, eat->FoodToMass);
            mass = SimMath.FixedMultiply(mass, proportion);
            if (mass > totalMass) {
                mass = totalMass;
            }

            massToConsume = mass;
            hungerEffect = SimMath.FixedMultiply(mass, eat->MassToFood);
        }

        #endregion // Evaluations

        #region Actions

        /// <summary>
        /// Process carbon dioxide and ph relationship
        /// </summary>
        static private void ProcessCarbonDioxideExchange(ref WaterPropertyBlockF32 water, out float phAdjust) {
            if (water.CarbonDioxide < LowerCarbonDioxideThreshold) {
                water.PH += (phAdjust = IncreasePHRatio * (LowerCarbonDioxideThreshold - water.CarbonDioxide));
                water.CarbonDioxide = LowerCarbonDioxideThreshold;
            } else if (water.CarbonDioxide > UpperCarbonDioxideThreshold) {
                water.PH += (phAdjust = -DecreasePHRatio * (water.CarbonDioxide - UpperCarbonDioxideThreshold));
                water.CarbonDioxide = UpperCarbonDioxideThreshold;
            } else {
                phAdjust = 0;
            }
        }

        /// <summary>
        /// Performs oxygen and carbon dioxide production.
        /// </summary>
        static private SimProduceConsumeSnapshot PerformProduce(ActorInfo* actorInfo, BehaviorInfo* behavior, uint population, ref WaterPropertyBlockF32 water) {
            if (population == 0) {
                return default;
            }

            SimProduceConsumeSnapshot snapshot;
            
            float oxygen = behavior->ProduceOxygen * (float) population;
            water.Oxygen += oxygen;
            snapshot.Oxygen = oxygen;

            float carbonDioxide = behavior->ProduceCarbonDioxide * (float) population;
            water.CarbonDioxide += carbonDioxide;
            snapshot.CarbonDioxide = carbonDioxide;

            return snapshot;
        }

        /// <summary>
        /// Performs oxygen and carbon dioxide consumption.
        /// </summary>
        static private SimProduceConsumeSnapshot PerformConsume(ActorInfo* actorInfo, BehaviorInfo* behavior, uint population, ref WaterPropertyBlockF32 water, out SimProduceConsumeSnapshot remaining) {
            if (population == 0) {
                remaining.Oxygen = 0;
                remaining.CarbonDioxide = 0;
                return default;
            }

            remaining.Oxygen = behavior->ConsumeOxygen * (float) population;
            remaining.CarbonDioxide = behavior->ConsumeCarbonDioxide * (float) population;

            SimProduceConsumeSnapshot snapshot;
            
            snapshot.Oxygen = Math.Min(remaining.Oxygen, water.Oxygen);
            snapshot.CarbonDioxide = Math.Min(remaining.CarbonDioxide, water.CarbonDioxide);

            water.Oxygen -= snapshot.Oxygen;
            water.CarbonDioxide -= snapshot.CarbonDioxide;

            remaining.Oxygen -= snapshot.Oxygen;
            remaining.CarbonDioxide -= snapshot.CarbonDioxide;
            return snapshot;
        }

        /// <summary>
        /// Performs light absorption.
        /// </summary>
        static private float PerformLightAbsorption(ActorInfo* actorInfo, BehaviorInfo* behavior, uint population, ref WaterPropertyBlockF32 water) {
            if (population == 0) {
                return 0;
            }

            float lightAbsorbed = Math.Min(behavior->ConsumeLight * (float) population, water.Light);
            water.Light -= lightAbsorbed;
            return lightAbsorbed;
        }

        /// <summary>
        /// Performs being eaten.
        /// </summary>
        static private void PerformEaten(ActorInfo* actorInfo, ref PopulationPair population, uint desiredMass, out uint consumedMass, ref PopulationPair consumedPopulation) {
            uint localConsumedPopulation = desiredMass / actorInfo->MassPerPopulation;
            uint totalPopulation = population.Total();
            
            if (localConsumedPopulation == 0 || totalPopulation == 0) {
                consumedMass = 0;
                return;
            }

            uint scarcityLevel = actorInfo->ScarcityLevel;
            if (scarcityLevel > 0) {
                float ratio = (float) totalPopulation / (float) scarcityLevel;
                if (ratio < 1) {
                    localConsumedPopulation = SimMath.FixedMultiply(localConsumedPopulation, ratio);
                }
            }

            uint max = SimMath.FixedMultiply(totalPopulation, MaxEatProportion);
            if (localConsumedPopulation > max) {
                localConsumedPopulation = max;
            }

            // eat stressed critters first, i guess?
            uint stressDecrease = Math.Min(population.Stressed, localConsumedPopulation);
            uint aliveDecrease = localConsumedPopulation - stressDecrease;

            population.Alive -= aliveDecrease;
            population.Stressed -= stressDecrease;

            consumedPopulation.Alive += aliveDecrease;
            consumedPopulation.Stressed += stressDecrease;

            consumedMass = localConsumedPopulation * actorInfo->MassPerPopulation;
        }

        /// <summary>
        /// Reduces hunger and unconsumed resource counts.
        /// </summary>
        static private void ProcessEatenReductions(ActorInfo* actorInfo, BehaviorInfo* behavior, ref uint hunger, ref SimProduceConsumeSnapshot unconsumed, ref uint totalEaten) {
            // reduce hunger, oxygen, and carbon dioxide to account for eaten critters
            if (totalEaten > 0) {
                uint hungerDecrease = Math.Min(hunger, totalEaten * HungerPerPopulation);
                float oxygenDecrease = Math.Min(unconsumed.Oxygen, totalEaten * behavior->ConsumeOxygen);
                float carbonDioxideDecrease = Math.Min(unconsumed.CarbonDioxide, totalEaten * behavior->ConsumeCarbonDioxide);
                hunger -= hungerDecrease;
                unconsumed.Oxygen -= oxygenDecrease;
                unconsumed.CarbonDioxide -= carbonDioxideDecrease;
                totalEaten = 0;
            }
        }

        /// <summary>
        /// Performs reproduction and growth.
        /// </summary>
        static private uint PerformReproductionAndGrowth(ActorInfo* actorInfo, ref PopulationPair population) {
            BehaviorInfo* alive = &actorInfo->AliveBehavior;
            BehaviorInfo* stress = &actorInfo->StressedBehavior;
            
            uint totalPopulation = population.Total();
            if (totalPopulation == 0 || (alive->Growth + stress->Growth == 0 && alive->Reproduce + stress->Reproduce <= 0)) {
                return 0;
            }

            uint increase = SimMath.FixedMultiply(population.Alive, alive->Reproduce) + SimMath.FixedMultiply(population.Stressed, stress->Reproduce);
            if (population.Alive > 0) {
                increase += (alive->Growth / actorInfo->MassPerPopulation);
            }
            if (population.Stressed > 0) {
                increase += (stress->Growth / actorInfo->MassPerPopulation);
            }
            uint max = SimMath.FixedMultiply(actorInfo->PopulationCap - totalPopulation, MaxReproduceProportion);

            if (increase > max) {
                increase = max;
            }

            population.Alive += increase;
            return increase;
        }

        /// <summary>
        /// Performs a generic population increase
        /// </summary>
        static private uint PerformGenericPopulationIncrease(ActorInfo* actorInfo, ref PopulationPair population, uint increase) {
            uint max = actorInfo->PopulationCap - population.Total();
            if (increase > max) {
                increase = max;
            }

            population.Alive += increase;
            return increase;
        }

        /// <summary>
        /// Performs death.
        /// </summary>
        static private uint PerformDeath(ActorInfo* actorInfo, ref PopulationPair population, PopulationPair excess) {
            BehaviorInfo* alive = &actorInfo->AliveBehavior;
            BehaviorInfo* stress = &actorInfo->StressedBehavior;

            uint totalPopulation = population.Total();
            if (totalPopulation == 0 || (alive->Death + stress->Death <= 0 && excess.Total() == 0)) {
                return 0;
            }

            uint aliveDecrease = SimMath.FixedMultiply(population.Alive, alive->Death) + excess.Alive;
            uint stressDecrease = SimMath.FixedMultiply(population.Stressed, stress->Death) + excess.Stressed;

            if ((aliveDecrease + stressDecrease) >= totalPopulation) {
                aliveDecrease = SimMath.FixedMultiply(population.Alive, MaxDeathProportion);
                stressDecrease = SimMath.FixedMultiply(population.Stressed, MaxDeathProportion);
            }

            population.Alive -= aliveDecrease;
            population.Stressed -= stressDecrease;
            return aliveDecrease + stressDecrease;
        }
    
        #endregion // Actions

        #region Reporting

        static private void Report(Buffer buffer, string message) {
            if (buffer.DebugReport == null) {
                buffer.DebugReport = new StringBuilder(1024);
            }

            if (buffer.DebugReport.Length > 0) {
                buffer.DebugReport.Append('\n');
            }

            buffer.DebugReport.Append(message);
        }

        static private void Report(Buffer buffer, string format, object arg0) {
            if (buffer.DebugReport == null) {
                buffer.DebugReport = new StringBuilder(1024);
            }

            if (buffer.DebugReport.Length > 0) {
                buffer.DebugReport.Append('\n');
            }

            buffer.DebugReport.AppendFormat(format, arg0);
        }

        static private void Report(Buffer buffer, string format, object arg0, object arg1) {
            if (buffer.DebugReport == null) {
                buffer.DebugReport = new StringBuilder(1024);
            }

            if (buffer.DebugReport.Length > 0) {
                buffer.DebugReport.Append('\n');
            }

            buffer.DebugReport.AppendFormat(format, arg0, arg1);
        }

        static private void Report(Buffer buffer, string format, object arg0, object arg1, object arg2) {
            if (buffer.DebugReport == null) {
                buffer.DebugReport = new StringBuilder(1024);
            }

            if (buffer.DebugReport.Length > 0) {
                buffer.DebugReport.Append('\n');
            }

            buffer.DebugReport.AppendFormat(format, arg0, arg1, arg2);
        }

        static private void Report(Buffer buffer, string format, params object[] args) {
            if (buffer.DebugReport == null) {
                buffer.DebugReport = new StringBuilder(1024);
            }

            if (buffer.DebugReport.Length > 0) {
                buffer.DebugReport.Append('\n');
            }

            buffer.DebugReport.AppendFormat(format, args);
        }

        #endregion // Reporting
    
        #region Snapshots

        /// <summary>
        /// Copies the given snapshot with a given profile to another snapshot with a different profile.
        /// </summary>
        static public void CopyTo(SimSnapshot* source, SimProfile sourceProfile, SimSnapshot* target, SimProfile targetProfile) {
            *target = default;
            target->Water = source->Water;
            for(int i = 0; i < sourceProfile.ActorCount; i++) {
                StringHash32 actorId = sourceProfile.Actors[i].Id;
                int remap = targetProfile.IndexOfActorType(actorId);
                if (remap >= 0) {
                    target->Populations[remap] = source->Populations[i];
                    target->StressedRatio[remap] = source->StressedRatio[i];
                }
            }
        }

        /// <summary>
        /// Sets the population of a specific actor.
        /// </summary>
        static public void SetPopulation(SimSnapshot* target, SimProfile targetProfile, StringHash32 id, uint population, float stressedRatio) {
            int index = targetProfile.IndexOfActorType(id);
            if (index >= 0) {
                target->Populations[index] = population;
                target->StressedRatio[index] = (byte) (stressedRatio * 128);
            }
        }

        /// <summary>
        /// Gets the population of a specific actor.
        /// </summary>
        static public void GetPopulation(SimSnapshot* target, SimProfile targetProfile, StringHash32 id, out uint population, out float stressedRatio) {
            int index = targetProfile.IndexOfActorType(id);
            if (index >= 0) {
                population = target->Populations[index];
                stressedRatio = target->StressedRatio[index] / 128f;
            } else {
                population = 0;
                stressedRatio = 0;
            }
        }

        /// <summary>
        /// Calculates the average error between the given source and target snapshots over a particular set of organisms.
        /// </summary>
        static public float CalculateAverageError(SimSnapshot* sources, SimProfile sourceProfile, SimSnapshot* targets, SimProfile targetProfile, uint snapshotCount, Predicate<StringHash32> organismFilter, int expectedOrganismCount, WaterPropertyMask waterPropertyMask) {
            if (snapshotCount == 0) {
                return 0;
            }

            // build remap
            int* remap = stackalloc int[MaxTrackedCritters];
            int unsyncedOrganisms = expectedOrganismCount;
            for(int i = 0; i < sourceProfile.ActorCount; i++) {
                StringHash32 actorId = sourceProfile.Actors[i].Id;
                if (organismFilter(actorId)) {
                    remap[i] = targetProfile.IndexOfActorType(actorId);
                    if (remap[i] != -1) {
                        unsyncedOrganisms--;
                    }
                } else{
                    remap[i] = -2;
                }
            }

            float accum = 0;
            for(int i = 0; i < snapshotCount; i++) {
                // prefetch stuff
                Unsafe.Prefetch(&sources[i + 1]);
                Unsafe.Prefetch(&targets[i + 1]);
                accum += CalculateSingleError(&sources[i], &targets[i], sourceProfile.ActorCount, remap, unsyncedOrganisms, unsyncedOrganisms, waterPropertyMask);
            }

            return accum / snapshotCount;
        }

        /// <summary>
        /// Calculates the average error for the given 
        /// </summary>
        static private float CalculateSingleError(SimSnapshot* source, SimSnapshot* target, int sourcePopulationCount, int* indexRemap, float extraError, int extraErrorCounter, WaterPropertyMask waterPropertyMask) {
            float accum = extraError;
            int counter = extraErrorCounter;

            WaterPropertyBlockF32 sourceWater = source->Water;
            WaterPropertyBlockF32 targetWater = target->Water;

            if (waterPropertyMask[WaterPropertyId.Temperature]) {
                counter++;
                accum += GraphingUtils.RPD(sourceWater.Temperature, targetWater.Temperature);
            }

            if (waterPropertyMask[WaterPropertyId.PH]) {
                counter++;
                accum += GraphingUtils.RPD(sourceWater.PH, targetWater.PH);
            }

            if (waterPropertyMask[WaterPropertyId.Light]) {
                counter++;
                accum += GraphingUtils.RPD(sourceWater.Light, targetWater.Light);
            }
            
            counter += sourcePopulationCount;
            for(int i = 0; i < sourcePopulationCount; i++) {
                int remap = indexRemap[i];
                if (remap >= 0) {
                    accum += GraphingUtils.RPD(source->Populations[i], target->Populations[remap]);
                } else {
                    if (remap != -2) {
                        accum += 1;
                    }
                }
            }

            return counter == 0 ? 0 : accum / counter;
        }

        #endregion // Snapshots
    }

    public enum SimulationFlags {
        Debug = 0x01,
        DetailedOutput = 0x02,
    }
}