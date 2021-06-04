using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ActorNav : ActorModule
    {
        #region Inspector

        [Header("Spawning")]
        [SerializeField] private ActorSpawnType m_SpawnType = ActorSpawnType.Floor;
        [SerializeField] private float m_FloorOffset = 0.5f;

        [SerializeField] private float m_submergeOffset = 0.5f;
        [SerializeField] private bool m_IsFixedToFloor = false;

        [Header("Traversal")]
        [SerializeField] private ActorTraversalType m_TraversalType = ActorTraversalType.Stationary;
        [SerializeField] private float m_DefaultTraversalSpeed = 5;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private ActorNavHelper m_Helper = null;
        [NonSerialized] private Routine m_MoveRoutine;

        public ActorNavHelper Helper
        {
            get { return m_Helper; }
            set { m_Helper = value; }
        }

        public bool IsAnimating()
        {
            return m_MoveRoutine;
        }

        #region Spawn

        public void Spawn(float inDelay)
        {
            Vector2 spawnOffset, targetPos;
            switch(m_SpawnType)
            {
                case ActorSpawnType.Water:
                    targetPos = m_Helper.GetWaterSpawnTarget(Actor.Body.BodyRadius, m_submergeOffset, m_FloorOffset);
                    spawnOffset = !m_IsFixedToFloor ? m_Helper.GetSpawnOffset() : default(Vector2);
                    break;
                case ActorSpawnType.Surface:
                    targetPos = m_Helper.GetSurfaceSpawnTarget(m_submergeOffset, Actor.Body.BodyRadius);
                    spawnOffset = !m_IsFixedToFloor ? m_Helper.GetSpawnOffset() : default(Vector2);
                    break;
                case ActorSpawnType.Floor:
                default:
                    targetPos = m_Helper.GetFloorSpawnTarget(Actor.Body.BodyRadius, m_FloorOffset);
                    spawnOffset = !m_IsFixedToFloor ? m_Helper.GetSpawnOffset() : default(Vector2);
                    break;
                

            }

            m_MoveRoutine.Replace(this, SpawnRoutine(inDelay, targetPos, spawnOffset)).TryManuallyUpdate(0);
        }

        private IEnumerator SpawnRoutine(float inDelay, Vector2 inPosition, Vector2 inSpawnOffset)
        {
            yield return inDelay;

            Actor.Callbacks.OnStartSpawn?.Invoke();

            Actor.Body.Show();
            m_Transform.position = inPosition + inSpawnOffset;

            if (m_IsFixedToFloor)
            {
                Actor.Body.RenderGroup.transform.SetScale(0, Axis.XY);
                yield return Actor.Body.RenderGroup.transform.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.CubeOut);
            }
            else
            {
                yield return m_Transform.MoveTo(inPosition, 0.5f, Axis.XY, Space.World).Ease(Curve.QuadIn);
            }

            Actor.Callbacks.OnFinishSpawn?.Invoke();
        }

        #endregion // Spawn

        #region Swim

        public IEnumerator SwimTo(Vector2 inPosition)
        {
            return m_MoveRoutine.Replace(this, SwimToRoutine(inPosition, m_DefaultTraversalSpeed)).Wait();
        }

        private IEnumerator SwimToRoutine(Vector2 inPosition, float inSpeed)
        {
            yield return m_Transform.MoveToWithSpeed(inPosition, inSpeed, Axis.XY, Space.World).Ease(Curve.QuadInOut);
        }

        #endregion // Swim

        #region Listeners

        private void OnContactWater(Collider2D inCollider)
        {
            Services.Audio.PostEvent("tank_water_splash");
        }

        private void OnLeaveWater(Collider2D inCollider)
        {

        }

        #endregion // Listeners

        #region IPool

        public override void OnConstruct()
        {
            base.OnConstruct();
            m_Transform = transform;
        }

        public override void OnFree()
        {
            m_MoveRoutine.Stop();
            m_Helper = null;
            base.OnFree();
        }

        #endregion // IPool
    }

    public enum ActorTraversalType
    {
        Stationary,
        Swim,
        Surface
    }

    public enum ActorSpawnType
    {
        Floor,
        Edge,
        Water,
        Surface
    }
}