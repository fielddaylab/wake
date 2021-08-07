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

        [NonSerialized] private Phase m_Phase = Phase.Spawning;
        [NonSerialized] private ActorWorld m_World;

        public ActorWorld World()
        {
            return m_World ?? (m_World = new ActorWorld(m_Allocator, m_Tank.Bounds, null, 16));
        }

        #region Critters

        public void Alloc(StringHash32 inId)
        {
            ActorWorld.AllocWithDefaultCount(World(), inId);
        }

        public void FreeAll(StringHash32 inId)
        {
            ActorWorld.FreeAll(m_World, inId);
        }

        #endregion // Critters

        #region Env State

        public void UpdateEnvState(WaterPropertyBlockF32 inEnvironment)
        {
            ActorWorld.SetWaterState(m_World, inEnvironment);
        }

        public void ClearEnvState()
        {
            ActorWorld.SetWaterState(m_World, null);
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
                foreach(var critter in m_World.Actors)
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
            for(int i = m_World.Actors.Count - 1; i >= 0; i--)
            {
                critter = m_World.Actors[i];
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
            m_World.Water = default(WaterPropertyBlockF32);
            if (m_World != null)
            {
                ActorWorld.FreeAll(m_World);
            }
            m_Phase = Phase.Spawning;
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