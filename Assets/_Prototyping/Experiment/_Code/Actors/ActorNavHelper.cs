using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class ActorNavHelper : MonoBehaviour
    {
        [SerializeField] private float m_SpawnHeight = 8;
        [SerializeField] private BoxCollider2D m_Region = null;
        [NonSerialized] private Transform m_Transform;

        private void Awake()
        {
            this.CacheComponent(ref m_Transform);
        }

        public Vector2 GetSpawnOffset()
        {
            return new Vector2(0, m_SpawnHeight);
        }

        public Vector2 GetFloorSpawnTarget(float inSideOffset, float inFloorOffset)
        {
            Rect r = Rect();
            Vector2 left = new Vector2(r.xMin + inSideOffset, r.yMin + inFloorOffset);
            Vector2 right = new Vector2(r.xMax - inSideOffset, left.y);
            return RNG.Instance.NextVector2(left, right);
        }

        public Vector2 GetWaterSpawnTarget(float inSideOffset, float submergeOffset, float inFloorOffset)
        {
            Rect r = Rect();
            Vector2 left = new Vector2(r.xMin + inSideOffset, r.yMax - submergeOffset);
            Vector2 right = new Vector2(r.xMax - inSideOffset, r.yMin + inFloorOffset);
            return RNG.Instance.NextVector2(left, right);
        }

        public Vector2 GetRandomSwimTarget(float inSideOffset, float inFloorOffset, float inCeilingOffset)
        {
            Rect r = Rect();
            Vector2 left = new Vector2(r.xMin + inSideOffset, r.yMin + inFloorOffset);
            Vector2 right = new Vector2(r.xMax - inSideOffset, r.yMax - inCeilingOffset);
            return RNG.Instance.NextVector2(left, right);
        }

        public Vector2 GetClimb(float root, float inSideOffset, float heightOffset, float currHeight, bool descend=false) {
            float climbOffset = heightOffset;
            if (descend) climbOffset *= -1;
            Vector2 left = new Vector2(root + inSideOffset, currHeight + climbOffset);
            Vector2 right = new Vector2(root - inSideOffset, currHeight + climbOffset);
            return RNG.Instance.NextVector2(left, right);

        }

        public bool ReachedTheFloor(Vector2 position, float inFloorOffset) {
            Rect r = Rect();
            return position.y <= (r.yMin + inFloorOffset);
        }

        public bool ReachedTheTop(Vector2 position, float height)
        {
            Rect r = Rect();
            return position.y >= (r.yMin + height);
        }

        private Rect Rect()
        {
            Rect rect = new Rect();
            rect.size = m_Region.size;
            rect.center = (Vector2) m_Transform.position + m_Region.offset;
            return rect;
        }
    }
}