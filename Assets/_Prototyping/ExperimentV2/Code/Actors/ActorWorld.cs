using BeauUtil;
using UnityEngine;
using Aqua;
using System;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorWorld
    {
        public readonly ActorAllocator Allocator;
        public readonly Bounds WorldBounds;
        public readonly Transform ActorRoot;
        public readonly RingBuffer<ActorInstance> Actors;
        public readonly RingBuffer<ActorCountU32> ActorCounts;
        public readonly ActorInstance.GeneralDelegate OnFree;
        public WaterPropertyBlockF32 Water;
        public bool HasEnvironment;
        public object Tag;
        public float Lifetime;

        public ActorWorld(ActorAllocator inAllocator, Bounds inBounds, Transform inActorRoot, ActorInstance.GeneralDelegate inOnFree, int inExpectedSize = 0, object inTag = null)
        {
            Allocator = inAllocator;
            WorldBounds = inBounds;
            ActorRoot = inActorRoot;
            OnFree = inOnFree;
            Actors = new RingBuffer<ActorInstance>(inExpectedSize, RingBufferMode.Expand);
            ActorCounts = new RingBuffer<ActorCountU32>(8, RingBufferMode.Expand);
            Water = Services.Assets.WaterProp.DefaultValues();
            Tag = inTag;
        }

        #region Alloc/Free

        static public ActorInstance Alloc(ActorWorld inWorld, StringHash32 inActorId)
        {
            ActorInstance actor = inWorld.Allocator.Alloc(inActorId, inWorld.ActorRoot);
            inWorld.Actors.PushBack(actor);

            ActorInstance.ForceActorAction(actor, ActorActionId.Spawning, inWorld);
            ActorInstance.SetActorState(actor, inWorld.HasEnvironment ? actor.Definition.StateEvaluator.Evaluate(inWorld.Water) : ActorStateId.Alive, inWorld);
            return actor;
        }

        static public void Alloc(ActorWorld inWorld, StringHash32 inActorId, int inCount)
        {
            int startingSize = inWorld.Actors.Count;
            int length = inCount;

            ActorInstance actor;
            while(inCount-- > 0)
            {
                actor = inWorld.Allocator.Alloc(inActorId, inWorld.ActorRoot);
                inWorld.Actors.PushBack(actor);
            }

            var newActors = new ListSlice<ActorInstance>(inWorld.Actors, startingSize, length);

            for(int i = 0; i < newActors.Length; i++)
            {
                ActorInstance.ForceActorAction(newActors[i], ActorActionId.Spawning, inWorld);
            }

            if (inWorld.HasEnvironment)
            {
                UpdateActorStates(inWorld, newActors);
            }
            else
            {
                ForceActorStates(inWorld, ActorStateId.Alive, newActors);
            }
        }

        static public void AllocWithDefaultCount(ActorWorld inWorld, StringHash32 inActorId)
        {
            Alloc(inWorld, inActorId, inWorld.Allocator.Define(inActorId).SpawnCount);
        }

        static public void Free(ActorWorld inWorld, ActorInstance inInstance)
        {
            inWorld.OnFree?.Invoke(inInstance, inWorld);
            inWorld.Actors.FastRemove(inInstance);
            inWorld.Allocator.Free(inInstance);
        }

        static public void Free(ActorWorld inWorld, ref ActorInstance ioInstance)
        {
            if (ioInstance != null)
            {
                inWorld.OnFree?.Invoke(ioInstance, inWorld);
                inWorld.Actors.FastRemove(ioInstance);
                inWorld.Allocator.Free(ioInstance);
                ioInstance = null;
            }
        }

        static public void FreeAll(ActorWorld inWorld)
        {
            if (inWorld.OnFree != null)
            {
                for(int i = 0, length = inWorld.Actors.Count; i < length; i++)
                    inWorld.OnFree(inWorld.Actors[i], inWorld);
            }
            inWorld.Actors.Clear();
            inWorld.Allocator.FreeAll();
        }

        static public void FreeAll(ActorWorld inWorld, StringHash32 inActorId)
        {
            ActorInstance actor;
            RingBuffer<ActorInstance> actorList = inWorld.Actors;
            for(int i = actorList.Count - 1; i >= 0; i--)
            {
                actor = actorList[i];
                if (actor.Definition.Id == inActorId)
                {
                    inWorld.OnFree?.Invoke(actor, inWorld);
                    actorList.FastRemoveAt(i);
                    inWorld.Allocator.Free(actor);
                }
            }
        }

        #endregion // Alloc/Free

        #region Update States

        static public void SetWaterState(ActorWorld inWorld, in WaterPropertyBlockF32? inEnvironment)
        {
            if (inEnvironment.HasValue)
            {
                inWorld.Water = inEnvironment.Value;
                inWorld.HasEnvironment = true;
                UpdateActorStates(inWorld, inWorld.Actors);
            }
            else
            {
                if (inWorld.HasEnvironment)
                {
                    inWorld.HasEnvironment = false;
                    ForceActorStates(inWorld, ActorStateId.Alive, inWorld.Actors);
                }
            }
        }

        static public void UpdateActorStates(ActorWorld inWorld)
        {
            if (inWorld.HasEnvironment)
            {
                UpdateActorStates(inWorld, inWorld.Actors);
            }
            else
            {
                ForceActorStates(inWorld, ActorStateId.Alive, inWorld.Actors);
            }
        }

        static private void UpdateActorStates(ActorWorld inWorld, ListSlice<ActorInstance> inInstances)
        {
            ActorInstance instance;
            ActorStateId state;
            WaterPropertyBlockF32 water = inWorld.Water;
            for(int i = 0; i < inInstances.Length; i++)
            {
                instance = inInstances[i];
                state = instance.Definition.StateEvaluator.Evaluate(water);
                ActorInstance.SetActorState(instance, state, inWorld);
            }
        }

        static private void ForceActorStates(ActorWorld inWorld, ActorStateId inState, ListSlice<ActorInstance> inInstances)
        {
            ActorInstance instance;
            for(int i = 0; i < inInstances.Length; i++)
            {
                instance = inInstances[i];
                ActorInstance.SetActorState(instance, inState, inWorld);
            }
        }

        #endregion // Update States

        #region Actor Queries

        static public void RegenerateActorCounts(ActorWorld inWorld)
        {
            inWorld.ActorCounts.Clear();
            if (inWorld.Actors.Count == 0)
                return;

            inWorld.Actors.Sort(SortActorsByType);
            ActorCountU32 population;
            StringHash32 currentId = inWorld.Actors[0].Definition.Id;
            population.Id = currentId;
            population.Population = 1;

            for(int i = 1, count = inWorld.Actors.Count; i < count; i++)
            {
                currentId = inWorld.Actors[i].Definition.Id;
                if (currentId != population.Id)
                {
                    inWorld.ActorCounts.PushBack(population);
                    population.Id = currentId;
                    population.Population = 1;
                }
                else
                {
                    population.Population++;
                }
            }

            inWorld.ActorCounts.PushBack(population);
            inWorld.ActorCounts.SortByKey<StringHash32, uint, ActorCountU32>();
        }

        static public uint GetPopulation(ActorWorld inWorld, StringHash32 inId)
        {
            for(int i = 0, count = inWorld.ActorCounts.Count; i < count; i++)
            {
                if (inWorld.ActorCounts[i].Id == inId)
                    return inWorld.ActorCounts[i].Population;
            }

            return 0;
        }

        static public uint ModifyPopulation(ActorWorld inWorld, StringHash32 inId, int inAmount)
        {
            for(int i = 0, count = inWorld.ActorCounts.Count; i < count; i++)
            {
                if (inWorld.ActorCounts[i].Id == inId)
                {
                    uint next = (uint) Math.Max(0, inWorld.ActorCounts[i].Population + inAmount);
                    return inWorld.ActorCounts[i].Population = next;
                }
            }

            return 0;
        }

        static public readonly Comparison<ActorInstance> SortActorsByType = (x, y) => {
            return x.Definition.Id.CompareTo(y.Definition.Id);
        };

        #endregion // Actor Queries
    }
}