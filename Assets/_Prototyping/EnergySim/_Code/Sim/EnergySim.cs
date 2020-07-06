using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class EnergySim
    {
        public const int MaxResources = 16;
        public const int MaxProperties = 16;

        #region Buffer

        private class Buffer : IPooledObject<Buffer>
        {
            public readonly EnergySimState State = new EnergySimState();
            public readonly List<ushort> TurnOrder = new List<ushort>(EnergySimState.MaxActors);
            public readonly WeightedSet<int> VarTypeChoice = new WeightedSet<int>(MaxResources);
            public readonly WeightedSet<int> ActorTypeChoice = new WeightedSet<int>(32);
            public readonly List<ushort> ActorChoice = new List<ushort>(128);

            public List<ushort>[] ActorLists;
            public uint[] ActorMasses;

            public EnergySimContext Context;

            public void Init(in EnergySimContext inContext)
            {
                Context = inContext;
                State.CopyFrom(inContext.Current);
                Context.Current = State;

                InitFromSelf();
            }

            public void InitFromSelf()
            {
                Context.RNG = new System.Random((int)State.NextSeed);
                State.Configure(Context);

                Array.Resize(ref ActorLists, State.Populations.Length);
                Array.Resize(ref ActorMasses, State.Populations.Length);
                for (int i = 0; i < State.Populations.Length; ++i)
                {
                    ref List<ushort> actorList = ref ActorLists[i];
                    ListUtils.EnsureCapacity(ref actorList, EnergySimState.MaxActors / 4);
                    actorList?.Clear();
                    ActorMasses[i] = 0;
                }

                TurnOrder.Clear();
                ActorTypeChoice.Clear();
                VarTypeChoice.Clear();
                ActorChoice.Clear();
            }

            public void Flush(ref EnergySimState outState)
            {
                outState.CopyFrom(State);
            }

            #region IPooledObject

            void IPooledObject<Buffer>.OnConstruct(IPool<Buffer> inPool)
            {
            }

            void IPooledObject<Buffer>.OnDestruct()
            {
            }

            void IPooledObject<Buffer>.OnAlloc()
            {
            }

            void IPooledObject<Buffer>.OnFree()
            {
                Context = default(EnergySimContext);
            }

            #endregion // IPooledObject
        }

        #endregion // Buffer

        private readonly IPool<Buffer> m_BufferPool = new DynamicPool<Buffer>(1, Pool.DefaultConstructor<Buffer>());

        #region Advance

        public void Setup(ref EnergySimContext ioContext)
        {
            if (ioContext.Start == null)
            {
                ioContext.Start = new EnergySimState();
            }
            
            if (ioContext.Current == null)
            {
                ioContext.Current = new EnergySimState();
            }

            AcquireBufferAndSetup(ref ioContext);
        }

        public bool Scrub(ref EnergySimContext ioContext, uint inDesiredTick)
        {
            if (ioContext.Current == null)
            {
                ioContext.Current = new EnergySimState();
                ioContext.Current.CopyFrom(ioContext.Start);
            }

            int distance = (int) inDesiredTick - (int)ioContext.Current.Timestamp;
            if (distance == 0)
                return false;

            if (distance < 0)
            {
                distance = (int) inDesiredTick;
                ioContext.Current.CopyFrom(ioContext.Start);
            }

            AcquireBufferAndPerformTicks(ref ioContext, (uint)distance);

            return true;
        }

        public bool Tick(ref EnergySimContext ioContext, uint inIncrement = 1)
        {
            if (ioContext.Current == null)
            {
                ioContext.Current = new EnergySimState();
                ioContext.Current.CopyFrom(ioContext.Start);
            }

            if (inIncrement == 0)
                return false;

            AcquireBufferAndPerformTicks(ref ioContext, inIncrement);

            return true;
        }

        private void AcquireBufferAndPerformTicks(ref EnergySimContext ioContext, uint inIncrements)
        {
            Buffer buffer = m_BufferPool.Alloc();
            try
            {
                PerformTicksOnBuffer(ref ioContext, buffer, inIncrements);
            }
            finally
            {
                m_BufferPool.Free(buffer);
            }
        }

        private void AcquireBufferAndSetup(ref EnergySimContext ioContext)
        {
            Buffer buffer = m_BufferPool.Alloc();
            try
            {
                SetupBuffer(ref ioContext, buffer);
            }
            finally
            {
                m_BufferPool.Free(buffer);
            }
        }

        #endregion // Advance

        #region Setup Logic

        static private void SetupBuffer(ref EnergySimContext ioContext, Buffer inBuffer)
        {
            ioContext.Current.CopyFrom(ioContext.Start);
            
            inBuffer.Init(ioContext);

            inBuffer.State.Reset(ioContext);
            ioContext.Scenario.Initialize(inBuffer.State, ioContext.Database);

            CollectPopulationStats(inBuffer);

            inBuffer.Flush(ref ioContext.Current);
            inBuffer.Flush(ref ioContext.Start);
        }

        #endregion // Setup Logic

        #region Tick Logic

        static private void PerformTicksOnBuffer(ref EnergySimContext ioContext, Buffer inBuffer, uint inTicks)
        {
            inBuffer.Context.Logger?.Reset();

            inBuffer.Init(ioContext);

            if (inTicks == 0)
            {
                inBuffer.Context.Logger?.Log("PERFORMING NO TICKS");
                CollectPopulationStats(inBuffer);
            }

            for (int tickCount = 0; tickCount < inTicks; ++tickCount)
            {
                if (tickCount > 0)
                {
                    inBuffer.InitFromSelf();
                }

                TickBufferPrep(inBuffer);
                TickBuffer(inBuffer);
                TickBufferPost(inBuffer);
            }

            inBuffer.Flush(ref ioContext.Current);

            inBuffer.Context.Logger?.Flush();
        }

        static private void CollectPopulationStats(Buffer inBuffer)
        {
            // initial population count
            for (int actorIdx = inBuffer.State.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                inBuffer.TurnOrder.Add((ushort)actorIdx);
                ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                FourCC actorType = actor.Type;
                int typeIdx = inBuffer.Context.Database.Actors.IdToIndex(actorType);
                inBuffer.ActorLists[typeIdx].Add((ushort)actorIdx);
                inBuffer.ActorMasses[typeIdx] += actor.Mass;
            }

            // copy population counts
            for (int i = 0; i < inBuffer.ActorLists.Length; ++i)
            {
                inBuffer.State.Populations[i] = (ushort)inBuffer.ActorLists[i].Count;
                inBuffer.State.Masses[i] = inBuffer.ActorMasses[i];
            }
        }

        static private void TickBufferPrep(Buffer inBuffer)
        {
            // advance Timestamp
            ++inBuffer.State.Timestamp;

            inBuffer.Context.Logger?.Log("<--- TICK {0} --->", inBuffer.State.Timestamp);

            inBuffer.Context.Logger?.Log("-- PRE-TICK ---");

            // initial population count
            for (int actorIdx = inBuffer.State.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                inBuffer.TurnOrder.Add((ushort)actorIdx);
                ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                FourCC actorType = actor.Type;
                int typeIdx = inBuffer.Context.Database.Actors.IdToIndex(actorType);
                inBuffer.ActorLists[typeIdx].Add((ushort)actorIdx);
                inBuffer.ActorMasses[typeIdx] += actor.Mass;
            }

            // copy population counts
            for (int i = 0; i < inBuffer.ActorLists.Length; ++i)
            {
                inBuffer.State.Populations[i] = (ushort)inBuffer.ActorLists[i].Count;
                inBuffer.State.Masses[i] = inBuffer.ActorMasses[i];
                inBuffer.Context.Logger?.Log("Population {0}: pop {1} mass {2}", inBuffer.Context.Database.Actors.IndexToId(i), inBuffer.State.Populations[i], inBuffer.State.Masses[i]);
            }

            // environment tick
            ref EnvironmentState env = ref inBuffer.State.Environment;
            EnvironmentPreTick(inBuffer, ref env, inBuffer.Context.Database.Envs[env.Type]);

            // actor type tick
            for (int i = 0, length = inBuffer.Context.Database.Actors.Count(); i < length; ++i)
            {
                ActorType type = inBuffer.Context.Database.Actors[i];
                // TODO: modify environment resources based on population counts?
            }

            // log resources
            if (inBuffer.Context.Logger != null)
            {
                for (int i = 0; i < inBuffer.Context.Database.Resources.Count(); ++i)
                {
                    inBuffer.Context.Logger.Log("Resource {0}: {1}", inBuffer.Context.Database.Resources.IndexToId(i), inBuffer.State.Environment.OwnedResources[i]);
                }

                for (int i = 0; i < inBuffer.Context.Database.Properties.Count(); ++i)
                {
                    inBuffer.Context.Logger.Log("Property {0}: {1}", inBuffer.Context.Database.Properties.IndexToId(i), inBuffer.State.Environment.Properties[i]);
                }
            }

            // actor pre-tick
            for (int actorIdx = inBuffer.State.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                ActorPreTick(inBuffer, ref actor, inBuffer.Context.Database.Actors[actor.Type]);
            }

            // shuffle turn order
            inBuffer.Context.RNG.Shuffle(inBuffer.TurnOrder);

            inBuffer.Context.Logger?.Log("Shuffled turn order");
        }

        static private void TickBuffer(Buffer inBuffer)
        {
            inBuffer.Context.Logger?.Log("-- TICK -- ");

            int actionCount = inBuffer.Context.Scenario.TickActionCount();

            int actorCount = inBuffer.State.ActorCount;
            int unfinishedCount = actorCount;
            bool bTookAction = true;
            while (unfinishedCount > 0 || bTookAction)
            {
                unfinishedCount = 0;
                bTookAction = false;

                for (int shuffleIdx = 0; shuffleIdx < actorCount; ++shuffleIdx)
                {
                    int actorIdx = inBuffer.TurnOrder[shuffleIdx];
                    ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                    if ((actor.Flags & ActorStateFlags.Alive) == ActorStateFlags.Alive)
                    {
                        bTookAction |= ActorTick(inBuffer, (ushort) actorIdx, ref actor, inBuffer.Context.Database.Actors[actor.Type], ref inBuffer.State.Environment, actionCount);

                        if ((actor.Flags & ActorStateFlags.DoneForTick) != ActorStateFlags.DoneForTick)
                        {
                            ++unfinishedCount;
                        }
                    }
                }
            }
        }

        static private void TickBufferPost(Buffer inBuffer)
        {
            inBuffer.Context.Logger?.Log("-- POST-TICK --");

            // process deaths
            for (int actorIdx = inBuffer.State.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                if ((actor.Flags & ActorStateFlags.Alive) == ActorStateFlags.Alive)
                    ActorPostTick_Death(inBuffer, (ushort)actorIdx, ref actor, inBuffer.Context.Database.Actors[actor.Type]);
            }

            // cleanup dead actors
            CleanupDeadActors(inBuffer.State, inBuffer.Context.Logger);

            // process growth/reproduction
            for (int actorIdx = inBuffer.State.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                ref ActorState actor = ref inBuffer.State.Actors[actorIdx];
                // Debug.LogFormat("living actor at {0} {1}:{2}", actorIdx, actor.Type, actor.Id);
                ActorPostTick_Life(inBuffer, ref actor, inBuffer.Context.Database.Actors[actor.Type]);
            }

            // copy population stats
            for (int i = 0; i < inBuffer.ActorLists.Length; ++i)
            {
                inBuffer.State.Populations[i] = (ushort)inBuffer.ActorLists[i].Count;
                inBuffer.State.Masses[i] = inBuffer.ActorMasses[i];
                inBuffer.Context.Logger?.Log("Population {0}: pop {1} mass {2}", inBuffer.Context.Database.Actors.IndexToId(i), inBuffer.State.Populations[i], inBuffer.State.Masses[i]);
            }

            // log resources
            if (inBuffer.Context.Logger != null)
            {
                for (int i = 0; i < inBuffer.Context.Database.Resources.Count(); ++i)
                {
                    inBuffer.Context.Logger.Log("Resource {0}: {1}", inBuffer.Context.Database.Resources.IndexToId(i), inBuffer.State.Environment.OwnedResources[i]);
                }

                for (int i = 0; i < inBuffer.Context.Database.Properties.Count(); ++i)
                {
                    inBuffer.Context.Logger.Log("Property {0}: {1}", inBuffer.Context.Database.Properties.IndexToId(i), inBuffer.State.Environment.Properties[i]);
                }
            }

            // copy seed
            inBuffer.State.NextSeed = (uint)inBuffer.Context.RNG.Next();
            inBuffer.Context.Logger?.Log("Finishing tick with random seed {0}", inBuffer.State.NextSeed);

            inBuffer.Context.Logger?.Log("<--- FINISHED TICK {0} --->", inBuffer.State.Timestamp);
        }

        #endregion // Tick Logic

        #region Environment Ticks

        static private void EnvironmentPreTick(Buffer inBuffer, ref EnvironmentState ioState, EnvironmentType inType)
        {
            inType.AddResources(ref ioState, inBuffer.Context);
            inType.DefaultProperties(ref ioState, inBuffer.Context);
        }

        #endregion // Environment Ticks

        #region Actor Ticks

        static private void ActorPreTick(Buffer inBuffer, ref ActorState ioState, ActorType inType)
        {
            ioState.MetPropertyRequirements = 0;
            ioState.Flags &= ~ActorStateFlags.TempMask;

            inType.SetupResourceExchange(ref ioState, inBuffer.Context);
            inType.EvaluateProperties(ref ioState, inBuffer.Context);

            if (inType.ShouldReproduce(ioState, inBuffer.Context))
            {
                inBuffer.Context.Logger?.Log("Actor {0}:{1} queued to reproduce at end of tick", ioState.Type, ioState.Id);
                ioState.Flags |= ActorStateFlags.QueuedToReproduce;
            }
        }

        static private bool ActorTick(Buffer inBuffer, ushort inStateIdx, ref ActorState ioState, ActorType inType, ref EnvironmentState ioEnv, int inAllowedActions)
        {
            bool bTookAction = false;
            bool bDone = false;

            int resCount = inBuffer.Context.Database.Resources.Count();
            int actionsRemaining = inAllowedActions;

            while (actionsRemaining > 0 && !bDone)
            {
                bDone = true;

                inBuffer.VarTypeChoice.Clear();

                // produce a resource
                for (int i = 0; i < resCount; ++i)
                {
                    if (ioState.ProducingResources[i] > 0)
                        inBuffer.VarTypeChoice.SetWeight(i, ioState.ProducingResources[i]);
                }

                if (inBuffer.VarTypeChoice.Count > 0)
                {
                    int toProduce = inBuffer.Context.RNG.Choose(inBuffer.VarTypeChoice);
                    --ioState.ProducingResources[toProduce];
                    ++ioEnv.OwnedResources[toProduce];
                    bDone = false;
                    bTookAction = true;
                    --actionsRemaining;

                    inBuffer.Context.Logger?.Log("Actor {0}:{1} produced 1 resource {2}",
                        ioState.Type, ioState.Id, inBuffer.Context.Database.Resources.IndexToId(toProduce));
                }

                if (actionsRemaining <= 0)
                    break;

                // consume a resource

                inBuffer.VarTypeChoice.Clear();

                for (int i = 0; i < resCount; ++i)
                {
                    ushort supply = ioEnv.OwnedResources[i];
                    ushort demand = ioState.DesiredResources[i];
                    if (supply > 0 && demand > 0)
                        inBuffer.VarTypeChoice.SetWeight(i, supply * demand);
                }

                if (inBuffer.VarTypeChoice.Count > 0)
                {
                    int toConsume = inBuffer.Context.RNG.Choose(inBuffer.VarTypeChoice);
                    --ioState.DesiredResources[toConsume];
                    --ioEnv.OwnedResources[toConsume];
                    bDone = false;
                    bTookAction = true;
                    --actionsRemaining;

                    inBuffer.Context.Logger?.Log("Actor {0}:{1} consumed 1 resource {2}",
                        ioState.Type, ioState.Id, inBuffer.Context.Database.Resources.IndexToId(toConsume));
                }

                if (actionsRemaining <= 0)
                    break;

                // eat something

                if ((ioState.Flags & ActorStateFlags.FailedToEat) == 0)
                {
                    bool bNeedsToEat = false;
                    for (int i = 0; i < resCount && !bNeedsToEat; ++i)
                    {
                        ushort demand = ioState.DesiredResources[i];
                        if (demand > 0 && inBuffer.Context.Database.Resources[i].HasFlags(VarTypeFlags.ConvertFromMass))
                        {
                            bNeedsToEat = true;
                            break;
                        }
                    }

                    if (bNeedsToEat)
                    {
                        inBuffer.ActorTypeChoice.Clear();

                        int actCount = inBuffer.Context.Database.Actors.Count();
                        for (int i = 0; i < actCount; ++i)
                        {
                            ushort popCount = (ushort)inBuffer.ActorLists[i].Count;
                            uint mass = inBuffer.ActorMasses[i];

                            FourCC actorType = inBuffer.Context.Database.Actors.IndexToId(i);
                            if (actorType == ioState.Type)
                            {
                                --popCount;
                                mass -= ioState.Mass;
                            }

                            if (popCount <= 0 || mass <= 0)
                                continue;

                            float weightCo = inType.GetEatTargetWeight(ioState, actorType, inBuffer.Context);
                            if (weightCo <= 0)
                                continue;

                            float totalWeight = mass * weightCo;
                            inBuffer.ActorTypeChoice.Add(i, totalWeight);
                        }

                        if (inBuffer.ActorTypeChoice.Count > 0)
                        {
                            ushort biteSize, maxSize;
                            inType.GetEatSize(ioState, inBuffer.Context, out biteSize, out maxSize);

                            bool bEaten = false;
                            do
                            {
                                int actorIdx = inBuffer.Context.RNG.Choose(inBuffer.ActorTypeChoice);
                                inBuffer.ActorTypeChoice.Remove(actorIdx);

                                FourCC actorType = inBuffer.Context.Database.Actors.IndexToId(actorIdx);

                                inBuffer.ActorChoice.Clear();
                                inBuffer.ActorChoice.AddRange(inBuffer.ActorLists[actorIdx]);

                                inBuffer.Context.RNG.Shuffle(inBuffer.ActorChoice);

                                for(int i = inBuffer.ActorChoice.Count - 1; i >= 0; --i)
                                {
                                    ushort targetIdx = inBuffer.ActorChoice[i];
                                    if (inBuffer.State.Actors[targetIdx].Id == ioState.Id)
                                        continue;
                                    
                                    if (EatActor(inBuffer, ref ioState, inType, targetIdx, biteSize, maxSize))
                                    {
                                        bEaten = true;
                                        break;
                                    }
                                }
                            }
                            while (!bEaten && inBuffer.ActorTypeChoice.Count > 0);

                            if (!bEaten)
                            {
                                inBuffer.Context.Logger?.Log("Actor {0}:{1} tried and failed to eat",
                                    ioState.Type, ioState.Id);
                                ioState.Flags |= ActorStateFlags.FailedToEat;
                            }
                            else
                            {
                                bTookAction = true;
                                bDone = false;
                                --actionsRemaining;
                            }
                        }
                        else
                        {
                            inBuffer.Context.Logger?.Log("Actor {0}:{1} wanted to eat but nothing was around",
                                ioState.Type, ioState.Id);
                            ioState.Flags |= ActorStateFlags.FailedToEat;
                        }
                    }
                }
            }

            if (bDone)
            {
                ioState.Flags |= ActorStateFlags.DoneForTick;
            }

            return bTookAction;
        }

        static private void ActorPostTick_Death(Buffer inBuffer, ushort inActorIdx, ref ActorState ioState, ActorType inType)
        {
            // aging
            ++ioState.Age;

            // starvation counters
            for (int i = inBuffer.Context.Database.Resources.Count() - 1; i >= 0; --i)
            {
                if (ioState.DesiredResources[i] > 0)
                {
                    ++ioState.ResourceStarvation[i];
                    inBuffer.Context.Logger?.Log("Actor {0}:{1} could not satisfy resource {2} by {3} ({4} ticks)",
                        ioState.Type, ioState.Id,
                        inBuffer.Context.Database.Resources.IndexToId(i), ioState.DesiredResources[i], ioState.ResourceStarvation[i]);
                }
                else
                {
                    ioState.ResourceStarvation[i] = 0;
                }
            }

            for (int i = inBuffer.Context.Database.Properties.Count() - 1; i >= 0; --i)
            {
                ushort mask = (ushort)(1U << i);
                if ((ioState.MetPropertyRequirements & mask) == 0)
                {
                    ++ioState.PropertyStarvation[i];
                    inBuffer.Context.Logger?.Log("Actor {0}:{1} could not satisfy property {2} ({3} ticks)",
                        ioState.Type, ioState.Id,
                        inBuffer.Context.Database.Properties.IndexToId(i), ioState.PropertyStarvation[i]);
                }
                else
                {
                    ioState.PropertyStarvation[i] = 0;
                }
            }

            if (inType.ShouldDie(ioState, inBuffer.Context))
            {
                KillActor(inBuffer, inActorIdx);
            }
        }

        static private void ActorPostTick_Life(Buffer inBuffer, ref ActorState ioState, ActorType inType)
        {
            // growth
            ushort growth = inType.PerformGrowth(ref ioState, inBuffer.Context);
            if (growth > 0)
            {
                inBuffer.Context.Logger?.Log("Actor {0}:{1} grew {2}", ioState.Type, ioState.Id, growth);
            }

            // reproduction
            if ((ioState.Flags & ActorStateFlags.QueuedToReproduce) == ActorStateFlags.QueuedToReproduce)
            {
                int actorTypeIdx = inBuffer.Context.Database.Actors.IdToIndex(inType.Id());
                int desiredChildCount = inType.ReproductionSettings().Count;
                int allowedChildCount = Math.Min(inType.MaxActors() - inBuffer.State.Populations[actorTypeIdx], EnergySimState.MaxActors - inBuffer.State.ActorCount);
                if (desiredChildCount > allowedChildCount)
                {
                    int newChildCount = allowedChildCount;
                    if (newChildCount > 0)
                    {
                        inBuffer.Context.Logger?.Log("Actor {0}:{1} wanted to reproduce to make {2} children, but could only make {3} due to simulation capacity",
                            ioState.Type, ioState.Type, desiredChildCount, newChildCount);
                    }
                    else
                    {
                        inBuffer.Context.Logger?.Log("Actor {0}:{1} wanted to reproduce to make {2} children, but couldn't make any due to simulation capacity",
                            ioState.Type, ioState.Type, desiredChildCount);
                    }
                    desiredChildCount = allowedChildCount;
                }
                for (int i = 0; i < desiredChildCount; ++i)
                {
                    ref ActorState child = ref CreateActor(inBuffer, ioState.Type);
                    ++inBuffer.State.Populations[actorTypeIdx];
                    inBuffer.State.Masses[actorTypeIdx] += child.Mass;
                }
                if (desiredChildCount > 0)
                {
                    inBuffer.Context.Logger?.Log("Actor {0}:{1} reproduced to make {2} children", ioState.Type, ioState.Id, desiredChildCount);
                }

                ioState.Flags &= ~ActorStateFlags.QueuedToReproduce;
            }
        }

        static private bool KillActor(Buffer inBuffer, ushort inActorIdx)
        {
            ref ActorState actor = ref inBuffer.State.Actors[inActorIdx];
            if ((actor.Flags & ActorStateFlags.Alive) != ActorStateFlags.Alive)
                return false;

            actor.Flags &= ~ActorStateFlags.Alive;
            int actorTypeIdx = inBuffer.Context.Database.Actors.IdToIndex(actor.Type);
            inBuffer.ActorLists[actorTypeIdx].Remove(inActorIdx);
            inBuffer.ActorMasses[actorTypeIdx] -= actor.Mass;

            inBuffer.Context.Logger?.Log("Actor {0}:{1} killed", actor.Type, actor.Id);
            return true;
        }

        static private bool EatActor(Buffer inBuffer, ref ActorState ioState, ActorType inActorType, ushort inTargetIdx, ushort inBiteSize, uint inMaxBite)
        {
            ref ActorState targetActor = ref inBuffer.State.Actors[inTargetIdx];

            // if dead, do not eat
            if ((targetActor.Flags & ActorStateFlags.Alive) != ActorStateFlags.Alive)
                return false;

            // if no nutritional value, do not eat
            float conversion = inActorType.GetEatTargetWeight(ioState, targetActor.Type, inBuffer.Context);
            if (conversion <= 0)
                return false;

            ushort targetMass = inBiteSize;

            bool bCanEatPartial = (inBuffer.Context.Database.Actors[targetActor.Type].Flags() & ActorTypeFlags.AllowPartialConsumption) != 0;
            if (!bCanEatPartial)
            {
                // if we can't take partial bites, and this is too big, don't eat it
                if (targetActor.Mass > inMaxBite)
                    return false;

                targetMass = targetActor.Mass;
            }
            else if (targetMass > targetActor.Mass)
            {
                targetMass = targetActor.Mass;
            }

            int resCount = inBuffer.Context.Database.Resources.Count();

            for (int i = 0; i < resCount; ++i)
            {
                VarType resType = inBuffer.Context.Database.Resources[i];
                if (resType.HasFlags(VarTypeFlags.ConvertFromMass))
                {
                    ushort contribution = (ushort) Math.Ceiling(conversion * targetMass);
                    ioState.DesiredResources[i] = (ushort) Math.Max(ioState.DesiredResources[i] - contribution, 0);
                }
            }

            targetActor.Mass -= targetMass;
            int targetActorTypeIdx = inBuffer.Context.Database.Actors.IdToIndex(targetActor.Type);

            if (targetActor.Mass == 0)
            {
                targetActor.Flags &= ~ActorStateFlags.Alive;
                inBuffer.ActorLists[targetActorTypeIdx].Remove(inTargetIdx);

                inBuffer.Context.Logger?.Log("Actor {0}:{1} ate {2} mass of {3}:{4} (killed)", ioState.Type, ioState.Id, targetMass, targetActor.Type, targetActor.Id);
            }
            else
            {
                inBuffer.Context.Logger?.Log("Actor {0}:{1} ate {2} mass of {3}:{4} (remaining mass {5})", ioState.Type, ioState.Id, targetMass, targetActor.Type, targetActor.Id, targetActor.Mass);
            }

            inBuffer.ActorMasses[targetActorTypeIdx] -= targetMass;
            return true;
        }

        static private ref ActorState CreateActor(Buffer inBuffer, FourCC inActorType)
        {
            ref ActorState actor = ref inBuffer.State.AddActor(inBuffer.Context.Database.Actors[inActorType]);
            actor.OffsetA = (byte)inBuffer.Context.RNG.Next(3);
            actor.OffsetB = (byte)inBuffer.Context.RNG.Next(17);
            inBuffer.Context.Logger?.Log("Actor {0}:{1} created", actor.Type, actor.Id);
            return ref actor;
        }

        #endregion // Actor Ticks

        #region Cleanup Steps

        static public void CleanupDeadActors(EnergySimState inState, ILogger inLogger = null)
        {
            for (int actorIdx = inState.ActorCount - 1; actorIdx >= 0; --actorIdx)
            {
                ref ActorState actor = ref inState.Actors[actorIdx];
                if ((actor.Flags & ActorStateFlags.Alive) == 0)
                {
                    inLogger?.Log("Cleaning up dead actor {0}:{1}", actor.Type, actor.Id);
                    inState.DeleteActor((ushort) actorIdx);
                }
            }
        }

        #endregion // Cleanup Steps

        #region Error

        static public float CalculateError(in EnergySimState inStateA, in EnergySimState inStateB, ISimDatabase inDatabase)
        {
            int actorTypeCount = inDatabase.Actors.Count();
            int resTypeCount = inDatabase.Resources.Count();
            int propTypeCount = inDatabase.Properties.Count();

            float errorAccum = 0;
            int errorCounter = 0;
            for(int i = 0; i < actorTypeCount; ++i)
            {
                errorAccum += RPD(inStateA.Populations[i], inStateB.Populations[i]);
                errorAccum += RPD(inStateA.Masses[i], inStateB.Masses[i]);
                errorCounter += 2;
            }

            for(int i = 0; i < resTypeCount; ++i)
            {
                errorAccum += RPD(inStateA.Environment.OwnedResources[i], inStateB.Environment.OwnedResources[i]);
                ++errorCounter;
            }

            for(int i = 0; i < propTypeCount; ++i)
            {
                errorAccum += RPD(inStateA.Environment.Properties[i], inStateB.Environment.Properties[i]);
                ++errorCounter;
            }

            if (errorCounter == 0)
                return 0;
            
            return errorAccum / errorCounter;
        }

        static public float RPD(float inA, float inB)
        {
            float delta = Math.Abs(inA - inB);
            float avg = (Math.Abs(inA) + Math.Abs(inB)) / 2f;
            if (avg == 0)
                return 0;
            return delta / avg;
        }

        #endregion // Error
    }
}