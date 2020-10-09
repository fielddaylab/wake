using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;

namespace ProtoAqua.Experiment
{
    public class ActorNav : MonoBehaviour, IPoolAllocHandler, IPoolConstructHandler
    {
        #region Inspector

        [SerializeField] private ActorTraversalType m_TraversalType = ActorTraversalType.Stationary;

        [Header("Spawning")]
        [SerializeField] private ActorSpawnType m_SpawnType = ActorSpawnType.Floor;
        [SerializeField] private float m_FloorOffset = 0.5f;
        [SerializeField] private bool m_IsFixedToFloor = false;

        #endregion // Inspector

        [NonSerialized] private ActorCtrl m_Actor;
        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private ActorNavHelper m_Helper = null;
        [NonSerialized] private Routine m_MoveRoutine;

        public void SetHelper(ActorNavHelper inHelper)
        {
            m_Helper = inHelper;
        }

        #region Spawn

        public void Spawn(float inDelay)
        {
            Vector2 spawnOffset, targetPos;
            switch(m_SpawnType)
            {
                case ActorSpawnType.Floor:
                default:
                    targetPos = m_Helper.GetFloorSpawnTarget(m_Actor.Body.BodyRadius, m_FloorOffset);
                    spawnOffset = !m_IsFixedToFloor ? m_Helper.GetSpawnOffset() : default(Vector2);
                    break;
            }

            m_MoveRoutine.Replace(this, SpawnRoutine(inDelay, targetPos, spawnOffset)).TryManuallyUpdate(0);
        }

        private IEnumerator SpawnRoutine(float inDelay, Vector2 inPosition, Vector2 inSpawnOffset)
        {
            yield return inDelay;

            m_Actor.Body.Show();
            m_Transform.position = inPosition + inSpawnOffset;

            if (m_IsFixedToFloor)
            {
                m_Actor.Body.RenderGroup.transform.SetScale(0, Axis.XY);
                yield return m_Actor.Body.RenderGroup.transform.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.CubeOut);
            }
            else
            {
                yield return m_Transform.MoveTo(inPosition, 0.5f, Axis.XY, Space.World).Ease(Curve.QuadIn);
            }
        }

        #endregion // Spawn

        public bool IsAnimating()
        {
            return m_MoveRoutine;
        }

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

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolConstructHandler.OnConstruct()
        {
            m_Actor = GetComponent<ActorCtrl>();
            m_Transform = transform;
        }

        void IPoolConstructHandler.OnDestruct()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            m_MoveRoutine.Stop();
            m_Helper = null;
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
        Water
    }
}