using System;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Modeling {
    public unsafe class SimProfile : IDisposable {

        #region Consts

        private const int MaxActors = Simulation.MaxTrackedCritters;
        private const int MaxEating = Simulation.MaxTrackedCritters * 4;
        private const int MaxParasites = Simulation.MaxTrackedCritters / 2;

        private const uint NotAssignedU32 = uint.MaxValue;
        private const float NotAssignedF32 = -1;

        static private readonly StringHash32 DetritusId = "Detritus";

        static public readonly int BufferSize = sizeof(ActorInfo) * MaxActors + sizeof(EatInfo) * MaxEating + sizeof(ParasiteInfo) * MaxParasites + sizeof(WorkingEatInfo) * MaxEating;

        #endregion // Consts

        #region Types

        // information about an actor
        public struct ActorInfo {
            public StringHash32 Id;
            public ActorStateTransitionSet StateTransitions;
            public uint MassPerPopulation;
            public uint ScarcityLevel;
            public ActorFlags Flags;
            public BehaviorInfo AliveBehavior;
            public BehaviorInfo StressedBehavior;
            public ushort EatOffset;
            public ushort EatCount;
            public uint PopulationCap;
            public byte ActionOrder;
        }

        // information about an organism's behavior for a particular state
        public struct BehaviorInfo {
            public float ProduceOxygen;
            public float ProduceCarbonDioxide;
            public float ConsumeOxygen;
            public float ConsumeCarbonDioxide;
            public float ConsumeLight;
            public uint Growth;
            public float Reproduce;
            public float Death;
        }

        [Flags]
        public enum ActorFlags : byte {
            Herd = 0x01,
            IgnoreStarvation = 0x02,

            Alive_DoesNotEat = 0x04,
            Stressed_DoesNotEat = 0x08
        }

        // information about eating
        public struct EatInfo {
            public byte Parent;
            public byte Target;
            public ActorStateId State;
            public float Proportion;
            public float FoodToMass;
            public float MassToFood;
        }

        // information about parasitism
        public struct ParasiteInfo {
            public byte Index;
            public byte Target;
            public float Affected;
        }

        // working info about a potential pair of eating facts
        private struct WorkingEatInfo {
            public byte Parent;
            public byte Target;
            public ushort SortingOrder;
            public float AmountWhenAlive;
            public float AmountWhenStressed;
        }

        // total mass eaten accumulator
        private struct WorkingEatAccumulator {
            public float Alive;
            public float Stressed;
        }

        private enum Phase {
            ImportingActors,
            ImportingFacts,
            Done
        }

        #endregion // Types

        public WaterPropertyBlockF32 Water;
        public float OxygenPerTick;
        public float CarbonDioxidePerTick;

        public ActorInfo* Actors;
        public int ActorCount;

        public int DetritusIndex;

        public EatInfo* Eats;
        public int EatCount;

        public ParasiteInfo* Parasites;
        public int ParasiteCount;

        private WorkingEatInfo* WorkingEatBuffer;
        private int WorkingEatCount;
        private Phase m_CurrentPhase;
        private Unsafe.ArenaHandle m_Allocator;

        public SimProfile() {
            m_Allocator = Unsafe.CreateArena(BufferSize);
            Actors = Unsafe.AllocArray<ActorInfo>(m_Allocator, MaxActors);
            Eats = Unsafe.AllocArray<EatInfo>(m_Allocator, MaxEating);
            Parasites = Unsafe.AllocArray<ParasiteInfo>(m_Allocator, MaxParasites);
            WorkingEatBuffer = Unsafe.AllocArray<WorkingEatInfo>(m_Allocator, MaxEating);
            DetritusIndex = -1;
        }

        public SimProfile(Unsafe.ArenaHandle allocator) {
            Actors = Unsafe.AllocArray<ActorInfo>(allocator, MaxActors);
            Eats = Unsafe.AllocArray<EatInfo>(allocator, MaxEating);
            Parasites = Unsafe.AllocArray<ParasiteInfo>(allocator, MaxParasites);
            WorkingEatBuffer = Unsafe.AllocArray<WorkingEatInfo>(allocator, MaxEating);
            DetritusIndex = -1;
        }

        // static SimulationProfile() {
        //     LogSizeDifferences<ActorInfo>();
        //     LogSizeDifferences<BehaviorInfo>();
        //     LogSizeDifferences<EatInfo>();
        //     LogSizeDifferences<ParasiteInfo>();
        //     LogSizeDifferences<WorkingEatInfo>();
        //     LogSizeDifferences<WorkingEatAccumulator>();
        //     LogSizeDifferences<ActorStateTransitionSet>();
        //     LogSizeDifferences<ActorStateTransitionRange>();
        //     LogSizeDifferences<SimSnapshot>();
        // }

        // static private void LogSizeDifferences<T>() where T : unmanaged {
        //     Log.Msg("sizeof({0})={1}; Marshal.SizeOf({0})={2}", typeof(T).Name, sizeof(T), Marshal.SizeOf<T>());
        // }

        public void Dispose() {
            Unsafe.TryFreeArena(ref m_Allocator);
            Actors = null;
            Eats = null;
            Parasites = null;
            WorkingEatBuffer = null;
            DetritusIndex = -1;
        }

        public void Clear() {
            ActorCount = 0;
            EatCount = 0;
            ParasiteCount = 0;
            WorkingEatCount = 0;
            m_CurrentPhase = Phase.ImportingActors;
            DetritusIndex = -1;
            Water = default;
            OxygenPerTick = 0;
            CarbonDioxidePerTick = 0;
        }

        #region Actors

        private ActorInfo* NewActor(BestiaryDesc desc) {
            Assert.True(ActorCount < MaxActors, "Cannot alloc more than {0} actors", MaxActors);
            
            int newIdx = ActorCount++;
            ActorInfo* actor = &Actors[newIdx];
            actor->Id = desc.Id();
            actor->MassPerPopulation = 0;
            actor->PopulationCap = 0;
            actor->ScarcityLevel = 0;

            ActorFlags flags = 0;
            if (desc.HasFlags(BestiaryDescFlags.TreatAsHerd)) {
                flags |= ActorFlags.Herd;
            }
            if (desc.HasFlags(BestiaryDescFlags.IgnoreStarvation)) {
                flags |= ActorFlags.IgnoreStarvation;
            }
            actor->Flags = flags;
            actor->ActionOrder = (byte) desc.Size();

            actor->EatOffset = 0;
            actor->EatCount = 0;

            actor->StateTransitions.Reset();

            ResetBehavior(&actor->AliveBehavior);
            ResetBehavior(&actor->StressedBehavior);

            if (desc.Id() == DetritusId) {
                DetritusIndex = newIdx;
            }

            return actor;
        }

        private void ResetBehavior(BehaviorInfo* behavior) {
            behavior->ProduceOxygen = behavior->ProduceCarbonDioxide = NotAssignedF32;
            behavior->ConsumeOxygen = behavior->ConsumeCarbonDioxide = NotAssignedF32;
            behavior->ConsumeLight = NotAssignedF32;
            behavior->Growth = NotAssignedU32;
            behavior->Reproduce = NotAssignedF32;
            behavior->Death = NotAssignedF32;
        }

        private ActorInfo* FindActor(StringHash32 id) {
            for(int i = 0; i < ActorCount; i++) {
                if (Actors[i].Id == id) {
                    return &Actors[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the index of the given actor type.
        /// </summary>
        public int IndexOfActorType(StringHash32 id) {
            for(int i = 0; i < ActorCount; i++) {
                if (Actors[i].Id == id) {
                    return i;
                }
            }
            return -1;
        }

        #endregion // Actors

        #region Eating

        private EatInfo* NewEat() {
            return &Eats[EatCount++];
        }

        private WorkingEatInfo* AllocWorkingEat(byte parent, byte target) {
            WorkingEatInfo* eat;
            for(int i = 0; i < WorkingEatCount; i++) {
                eat = &WorkingEatBuffer[i];
                if (eat->Parent == parent && eat->Target == target) {
                    return eat;
                }
            }
            
            eat = &WorkingEatBuffer[WorkingEatCount++];
            eat->Parent = parent;
            eat->SortingOrder = (ushort) (Actors[parent].ActionOrder * MaxActors + parent);
            eat->Target = target;
            eat->AmountWhenAlive = NotAssignedF32;
            eat->AmountWhenStressed = NotAssignedF32;
            return eat;
        }

        #endregion // Eating

        #region Importing

        public void ImportSim(BFSim sim) {
            Assert.True(m_CurrentPhase == Phase.ImportingActors, "Cannot import sim during '{0}' phase", m_CurrentPhase);
            Water = sim.InitialWater;
            OxygenPerTick = sim.OxygenPerTick;
            CarbonDioxidePerTick = sim.CarbonDioxidePerTick;
        }

        public void ImportActor(BestiaryDesc desc) {
            Assert.True(m_CurrentPhase == Phase.ImportingActors, "Cannot import actors during '{0}' phase", m_CurrentPhase);
            Assert.True(IndexOfActorType(desc.Id()) < 0, "Actor '{0}' already imported", desc.Id().ToDebugString());
            NewActor(desc);
        }

        public void ImportFact(BFBase fact, BFDiscoveredFlags flags) {
            Assert.True(m_CurrentPhase == Phase.ImportingFacts, "Cannot import facts during '{0}' phase", m_CurrentPhase);
            BestiaryDesc parent = fact.Parent;

            if (parent.Category() == BestiaryDescCategory.Environment) {
                return;
            }

            int index = IndexOfActorType(parent.Id());
            if (index < 0) {
                return;
            }

            ActorInfo* info = &Actors[index];
            
            switch(fact.Type) {
                case BFTypeId.Body: {
                    BFBody body = (BFBody) fact;
                    info->MassPerPopulation = body.MassPerPopulation;
                    info->ScarcityLevel = body.ScarcityLevel;
                    info->PopulationCap = body.PopulationHardCap;
                    break;
                }

                case BFTypeId.Death: {
                    BFDeath death = (BFDeath) fact;
                    FindBehaviorBlock(info, death)->Death = death.Proportion;
                    break;
                }

                case BFTypeId.Grow: {
                    BFGrow grow = (BFGrow) fact;
                    FindBehaviorBlock(info, grow)->Growth = grow.Amount;
                    break;
                }

                case BFTypeId.Reproduce: {
                    BFReproduce repro = (BFReproduce) fact;
                    FindBehaviorBlock(info, repro)->Reproduce = repro.Amount;
                    break;
                }

                case BFTypeId.State: {
                    BFState state = (BFState) fact;
                    info->StateTransitions[state.Property] = state.Range;
                    break;
                }

                case BFTypeId.Eat: {
                    BFEat eat = (BFEat) fact;
                    int targetIdx = IndexOfActorType(eat.Critter.Id());
                    if (targetIdx >= 0) {
                        WorkingEatInfo* workingInfo = AllocWorkingEat((byte) index, (byte) targetIdx);
                        if (eat.OnlyWhenStressed) {
                            workingInfo->AmountWhenStressed = eat.Amount;
                        } else {
                            workingInfo->AmountWhenAlive = eat.Amount;
                        }
                    }
                    break;
                }

                case BFTypeId.Consume: {
                    BFConsume consume = (BFConsume) fact;
                    switch(consume.Property) {
                        case WaterPropertyId.Oxygen: {
                            FindBehaviorBlock(info, consume)->ConsumeOxygen = consume.Amount;
                            break;
                        }
                        case WaterPropertyId.CarbonDioxide: {
                            FindBehaviorBlock(info, consume)->ConsumeCarbonDioxide = consume.Amount;
                            break;
                        }
                        case WaterPropertyId.Light: {
                            FindBehaviorBlock(info, consume)->ConsumeLight = consume.Amount;
                            break;
                        }
                    }
                    break;
                }

                case BFTypeId.Produce: {
                    BFProduce produce = (BFProduce) fact;
                    switch(produce.Property) {
                        case WaterPropertyId.Oxygen: {
                            FindBehaviorBlock(info, produce)->ProduceOxygen = produce.Amount;
                            break;
                        }
                        case WaterPropertyId.CarbonDioxide: {
                            FindBehaviorBlock(info, produce)->ProduceCarbonDioxide = produce.Amount;
                            break;
                        }
                    }
                    break;
                }
            
                case BFTypeId.Parasites: {
                    // TODO: add parasite relationship
                    break;
                }
            }
        }

        static private BehaviorInfo* FindBehaviorBlock(ActorInfo* actor, BFBehavior behavior) {
            if (behavior.OnlyWhenStressed) {
                return &actor->StressedBehavior;
            } else {
                return &actor->AliveBehavior;
            }
        }

        #endregion // Facts

        #region Post Process

        public void FinishActors() {
            Assert.True(m_CurrentPhase == Phase.ImportingActors, "Actor import is already completed - in '{0}' phase", m_CurrentPhase);
            Quicksort(Actors, 0, ActorCount - 1);
            m_CurrentPhase = Phase.ImportingFacts;
        }

        public void FinishFacts() {
            Assert.True(m_CurrentPhase == Phase.ImportingFacts, "Facts import is already completed - in '{0}' phase", m_CurrentPhase);
            PostProcessEating();
            PostProcessActorBehavior();
            m_CurrentPhase = Phase.Done;
        }

        private void PostProcessActorBehavior() {
            ActorInfo* actor;
            for(int i = 0; i < ActorCount; i++) {
                actor = &Actors[i];
                PostProcessValuePair(ref actor->AliveBehavior.ProduceOxygen, ref actor->StressedBehavior.ProduceOxygen);
                PostProcessValuePair(ref actor->AliveBehavior.ProduceCarbonDioxide, ref actor->StressedBehavior.ProduceCarbonDioxide);
                PostProcessValuePair(ref actor->AliveBehavior.ConsumeOxygen, ref actor->StressedBehavior.ConsumeOxygen);
                PostProcessValuePair(ref actor->AliveBehavior.ConsumeCarbonDioxide, ref actor->StressedBehavior.ConsumeCarbonDioxide);
                PostProcessValuePair(ref actor->AliveBehavior.ConsumeLight, ref actor->StressedBehavior.ConsumeLight);

                PostProcessValuePair(ref actor->AliveBehavior.Growth, ref actor->StressedBehavior.Growth);
                PostProcessValuePair(ref actor->AliveBehavior.Reproduce, ref actor->StressedBehavior.Reproduce);
                PostProcessValuePair(ref actor->AliveBehavior.Death, ref actor->StressedBehavior.Death);
            }
        }

        private void PostProcessEating() {
            Quicksort(WorkingEatBuffer, 0, WorkingEatCount - 1);

            WorkingEatAccumulator* totalEatAccumulationBuffer = stackalloc WorkingEatAccumulator[MaxActors];
            for(int i = 0; i < ActorCount; i++) {
                totalEatAccumulationBuffer[i] = default;
            }

            WorkingEatInfo* info;
            int actorIdx = -1;
            ActorInfo* actor = null;
            WorkingEatAccumulator* accum = null;
            EatInfo* newEat;
            
            // first pass - process alive/stressed pairs
            for(int i = 0; i < WorkingEatCount; i++) {
                info = &WorkingEatBuffer[i];
                if (actorIdx != info->Parent) {
                    actorIdx = info->Parent;
                    actor = &Actors[actorIdx];
                    accum = &totalEatAccumulationBuffer[actorIdx];
                }

                PostProcessValuePair(ref info->AmountWhenAlive, ref info->AmountWhenStressed);
                accum->Alive += info->AmountWhenAlive;
                accum->Stressed += info->AmountWhenStressed;
            }

            // second pass - generate eating rules
            for(int i = 0; i < WorkingEatCount; i++) {
                info = &WorkingEatBuffer[i];
                if (actorIdx != info->Parent) {
                    actorIdx = info->Parent;
                    actor = &Actors[actorIdx];
                    accum = &totalEatAccumulationBuffer[actorIdx];
                }

                if (info->AmountWhenAlive > 0) {
                    IncrementOffsetCount(ref actor->EatOffset, ref actor->EatCount, (ushort) EatCount);

                    newEat = NewEat();
                    newEat->Parent = (byte) actorIdx;
                    newEat->State = ActorStateId.Alive;
                    newEat->Target = info->Target;
                    newEat->Proportion = info->AmountWhenAlive / accum->Alive;
                    newEat->FoodToMass = info->AmountWhenAlive / Simulation.HungerPerPopulation;
                    newEat->MassToFood = 1f / newEat->FoodToMass;
                }

                if (info->AmountWhenStressed > 0) {
                    IncrementOffsetCount(ref actor->EatOffset, ref actor->EatCount, (ushort) EatCount);

                    newEat = NewEat();
                    newEat->Parent = (byte) actorIdx;
                    newEat->State = ActorStateId.Stressed;
                    newEat->Target = info->Target;
                    newEat->Proportion = info->AmountWhenStressed / accum->Alive;
                    newEat->FoodToMass = info->AmountWhenStressed / Simulation.HungerPerPopulation;
                    newEat->MassToFood = 1f / newEat->FoodToMass;
                }
            }

            // process whether or not these actors are eating
            for(int i = 0; i < ActorCount; i++) {
                actor = &Actors[i];
                accum = &totalEatAccumulationBuffer[i];

                if (accum->Alive == 0) {
                    actor->Flags |= ActorFlags.Alive_DoesNotEat;
                }
                if (accum->Stressed == 0) {
                    actor->Flags |= ActorFlags.Stressed_DoesNotEat;
                }
            }
        }

        static private void IncrementOffsetCount(ref ushort offset, ref ushort count, ushort current) {
            if (count == 0) {
                offset = current;
            }
            count++;
        }

        static private float PostProcessValuePair(ref float alive, ref float stressed) {
            if (alive == NotAssignedF32) {
                alive = 0;
            }
            if (stressed == NotAssignedF32) {
                stressed = alive;
                return alive;
            }

            return 0;
        }

        static private uint PostProcessValuePair(ref uint alive, ref uint stressed) {
            if (alive == NotAssignedU32) {
                alive = 0;
            }
            if (stressed == NotAssignedU32) {
                stressed = alive;
                return alive;
            }

            return 0;
        }

        #endregion // Post Process

        #region Quicksort

        static private void Quicksort(WorkingEatInfo* buffer, int lower, int higher) {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher);
                Quicksort(buffer, lower, pivot);
                Quicksort(buffer, pivot + 1, higher);
            }
        }

        static private void Quicksort(ActorInfo* buffer, int lower, int higher) {
            if (lower >= 0 && higher >= 0 && lower < higher) {
                int pivot = Partition(buffer, lower, higher);
                Quicksort(buffer, lower, pivot);
                Quicksort(buffer, pivot + 1, higher);
            }
        }

        static private int Partition(WorkingEatInfo* buffer, int lower, int higher) {
            int center = (lower + higher) >> 1;
            ushort pivotVal = buffer[center].SortingOrder;

            int i = lower - 1;
            int j = higher + 1;

            while(true) {
                do {
                    i++;
                } while (buffer[i].SortingOrder < pivotVal);
                do {
                    j--;
                } while (buffer[j].SortingOrder > pivotVal);

                if (i >= j) {
                    return j;
                }

                Ref.Swap(ref buffer[i], ref buffer[j]);
            }
        }

        static private int Partition(ActorInfo* buffer, int lower, int higher) {
            int center = (lower + higher) >> 1;
            ushort pivotVal = buffer[center].ActionOrder;

            int i = lower - 1;
            int j = higher + 1;

            while(true) {
                do {
                    i++;
                } while (buffer[i].ActionOrder < pivotVal);
                do {
                    j--;
                } while (buffer[j].ActionOrder > pivotVal);

                if (i >= j) {
                    return j;
                }

                Ref.Swap(ref buffer[i], ref buffer[j]);
            }
        }

        #endregion // Quicksort
    
        #region Debug

        static internal string Dump(SimProfile profile) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(profile.ActorCount).Append(" actor types");
            for(int i = 0; i < profile.ActorCount; i++) {
                ActorInfo actor = profile.Actors[i];
                sb.Append('\n').Append(actor.Id.ToDebugString());
                sb.Append("\n\tMass: ").Append(actor.MassPerPopulation);
                sb.Append("\n\tPopulation Cap: ").Append(actor.PopulationCap);
                sb.Append("\n\tScarcity Level: ").Append(actor.ScarcityLevel);
                sb.Append("\n\tFlags: ").Append(actor.Flags);
                sb.Append("\n\tTemperature Range: ").Append(actor.StateTransitions.Temperature);
                sb.Append("\n\tPH Range: ").Append(actor.StateTransitions.PH);
                sb.Append("\n\tLight Range: ").Append(actor.StateTransitions.Light);
                sb.Append("\n\tEat Entry Count: ").Append(actor.EatCount);
                sb.Append("\n\tAction Priority: ").Append(actor.ActionOrder);
                
                sb.Append("\n\tAlive Behavior:");
                DumpBehaviorBlock(sb, actor.AliveBehavior);
                sb.Append("\n\tStressed Behavior:");
                DumpBehaviorBlock(sb, actor.StressedBehavior);
            }
            sb.Append('\n').Append(profile.EatCount).Append(" eat entries");
            for(int i = 0; i < profile.EatCount; i++) {
                EatInfo info = profile.Eats[i];
                sb.Append('\n').Append(info.State).Append(' ').Append(profile.Actors[info.Parent].Id.ToDebugString())
                    .Append(" eats ").Append(profile.Actors[info.Target].Id.ToDebugString());
            }
            return sb.Flush();
        }

        static private void DumpBehaviorBlock(System.Text.StringBuilder sb, BehaviorInfo info) {
            if (info.ProduceOxygen > 0) {
                sb.Append("\n\t\tProduces Oxygen: ").Append(info.ProduceOxygen);
            }
            if (info.ProduceCarbonDioxide > 0) {
                sb.Append("\n\t\tProduces CarbonDioxide: ").Append(info.ProduceCarbonDioxide);
            }
            if (info.ConsumeOxygen > 0) {
                sb.Append("\n\t\tConsumes Oxygen: ").Append(info.ConsumeOxygen);
            }
            if (info.ConsumeCarbonDioxide > 0) {
                sb.Append("\n\t\tConsumes CarbonDioxide: ").Append(info.ConsumeCarbonDioxide);
            }
            if (info.ConsumeLight > 0) {
                sb.Append("\n\t\tConsumes Light: ").Append(info.ConsumeLight);
            }
            if (info.Growth > 0) {
                sb.Append("\n\t\tGrowth: ").Append(info.Growth);
            }
            if (info.Reproduce > 0) {
                sb.Append("\n\t\tReproduction: ").Append(info.Reproduce);
            }
            if (info.Death > 0) {
                sb.Append("\n\t\tDeath: ").Append(info.Death);
            }
        }

        #endregion // Debug
    }
}