using BeauUtil;
using UnityEngine;
using Aqua;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorWorld
    {
        public delegate void ActorAction(ActorWorld inWorld, ActorInstance inInstance);

        public readonly ActorAllocator Allocator;
        public readonly Bounds WorldBounds;
        public readonly Transform ActorRoot;
        public readonly RingBuffer<ActorInstance> Actors;
        public WaterPropertyBlockF32 Water;
        public bool HasEnvironment;

        public ActorWorld(ActorAllocator inAllocator, Bounds inBounds, Transform inActorRoot, int inExpectedSize = 0)
        {
            Allocator = inAllocator;
            WorldBounds = inBounds;
            ActorRoot = inActorRoot;
            Actors = new RingBuffer<ActorInstance>(inExpectedSize, RingBufferMode.Expand);
            Water = Services.Assets.WaterProp.DefaultValues();
        }

        #region Alloc/Free

        static public ActorInstance Alloc(ActorWorld inWorld, StringHash32 inActorId)
        {
            ActorInstance actor = inWorld.Allocator.Alloc(inActorId, inWorld.ActorRoot);
            inWorld.Actors.PushBack(actor);

            ActorInstance.ForceActorAction(actor, ActorActionId.Spawning, inWorld, ActorInstance.StartSpawning);
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
                ActorInstance.ForceActorAction(newActors[i], ActorActionId.Spawning, inWorld, ActorInstance.StartSpawning);
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
            inWorld.Actors.FastRemove(inInstance);
            inWorld.Allocator.Free(inInstance);
        }

        static public void Free(ActorWorld inWorld, ref ActorInstance ioInstance)
        {
            if (ioInstance != null)
            {
                inWorld.Actors.FastRemove(ioInstance);
                inWorld.Allocator.Free(ioInstance);
                ioInstance = null;
            }
        }

        static public void FreeAll(ActorWorld inWorld)
        {
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
    }
}