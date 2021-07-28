using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class ObservationBehaviorSystem : MonoBehaviour, ISceneOptimizable
    {
        private enum Phase
        {
            Spawning,
            Executing
        }

        [SerializeField] private SelectableTank m_Tank = null;
        [SerializeField] private ActorAllocator m_Allocator = null;

        [NonSerialized] private WaterPropertyBlockF32 m_EnvironmentState;
        [NonSerialized] private bool m_HasEnvironment;
        [NonSerialized] private RingBuffer<ActorInstance> m_AllocatedActors = new RingBuffer<ActorInstance>();
        [NonSerialized] private Phase m_Phase = Phase.Spawning;
        [NonSerialized] private ActorWorld m_World;

        #region Critters

        public void Alloc(StringHash32 inId)
        {
            if (m_World == null)
            {
                m_World = new ActorWorld(m_Tank.Bounds, FreeCritter);
            }

            ActorDefinition define = m_Allocator.Define(inId);
            int spawnCount = define.SpawnCount;
            ActorDefinition.SpawnConfiguration spawnConfig = define.Spawning;
            ActorInstance instance;

            int countStart = m_AllocatedActors.Count;
            int countEnd = countStart + spawnCount;

            // spawn everything first
            while(spawnCount-- > 0)
            {
                instance = m_Allocator.Alloc(inId, null);
                m_AllocatedActors.PushBack(instance);
            }

            // then set spawn states
            for(int i = countStart; i < countEnd; i++)
            {
                instance = m_AllocatedActors[i];
                ActorInstance.ForceActorAction(instance, ActorActionId.Spawning, m_World, ActorInstance.StartSpawning);
                if (m_HasEnvironment)
                    ActorInstance.SetActorState(instance, instance.Definition.StateEvaluator.Evaluate(m_EnvironmentState), m_World);
            }
        }

        public void Free(StringHash32 inId)
        {
            ActorInstance instance;
            for(int i = m_AllocatedActors.Count - 1; i >= 0; i--)
            {
                instance = m_AllocatedActors[i];
                if (instance.Definition.Id == inId)
                {
                    m_AllocatedActors.FastRemoveAt(i);
                    m_Allocator.Free(instance);
                }
            }
        }

        #endregion // Critters

        #region Env State

        public void UpdateEnvState(WaterPropertyBlockF32 inEnvironment)
        {
            m_EnvironmentState = inEnvironment;
            m_HasEnvironment = true;
            foreach(var critter in m_AllocatedActors)
                ActorInstance.SetActorState(critter, critter.Definition.StateEvaluator.Evaluate(inEnvironment), m_World);
        }

        public void ClearEnvState()
        {
            m_EnvironmentState = Services.Assets.WaterProp.DefaultValues();
            m_HasEnvironment = false;
            foreach(var critter in m_AllocatedActors)
                ActorInstance.SetActorState(critter, ActorStateId.Alive, m_World);
        }

        #endregion // Env State

        #region Tick

        public void TickBehaviors(float inDeltaTime)
        {
            if (!IsSpawningComplete())
                return;

            
        }

        private bool IsSpawningComplete()
        {
            if (m_Phase == Phase.Spawning)
            {
                foreach(var critter in m_AllocatedActors)
                {
                    if (critter.CurrentAction == ActorActionId.Spawning)
                        return false;
                }

                FinalizeCritterInitialization();
                m_Phase = Phase.Executing;
                return true;
            }
            else
            {
                return true;
            }
        }

        private void FinalizeCritterInitialization()
        {
            ActorInstance critter;
            for(int i = m_AllocatedActors.Count - 1; i >= 0; i--)
            {
                critter = m_AllocatedActors[i];
                if (critter.CurrentState == ActorStateId.Dead)
                {
                    ActorInstance.SetActorAction(critter, ActorActionId.Dying, m_World, OnActorDyingStart);
                }
                else
                {
                    ActorInstance.SetActorAction(critter, ActorActionId.Idle, m_World, OnActorIdleStart);
                }
            }
        }

        private void FreeCritter(ActorInstance inInstance)
        {
            m_AllocatedActors.FastRemove(inInstance);
            m_Allocator.Free(inInstance);
        }

        #endregion // Tick

        #region Actor States

        static private void OnActorDyingStart(ActorInstance inActor, ActorWorld inSystem)
        {

        }

        static private void OnActorIdleStart(ActorInstance inActor, ActorWorld inSystem)
        {

        }

        #endregion // Actor States

        public void ClearAll()
        {
            m_EnvironmentState = default(WaterPropertyBlockF32);
            m_AllocatedActors.Clear();
            m_Allocator.FreeAll();
            m_Phase = Phase.Spawning;
            m_HasEnvironment = false;
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Allocator = FindObjectOfType<ActorAllocator>();
            m_Tank = GetComponentInParent<SelectableTank>();
        }

        #endif // UNITY_EDITOR
    }
}