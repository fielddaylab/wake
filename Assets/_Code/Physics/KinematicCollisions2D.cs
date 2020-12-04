using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class KinematicCollisions2D : MonoBehaviour
    {
        private const float SeparatorIncrement = 1f / 64;

        #region Inspector

        [SerializeField, Required] private Rigidbody2D m_Rigidbody = null;
        [SerializeField, Required] private KinematicObject2D m_KinematicObject = null;
        [SerializeField, Required] private LayerMask m_SolidMask = 0;

        #endregion // Inspector

        private void Awake()
        {
            m_KinematicObject.MoveCallback = TryMoveObject;
        }

        private void OnDestroy()
        {
            m_KinematicObject.MoveCallback = null;
        }

        private bool TryMoveObject(Vector2 inOffset, ref KinematicPropertyBlock2D ioProperties, float inDeltaTime)
        {
            Physics2D.autoSyncTransforms = false;

            RaycastHit2D xHit, yHit;
            bool bHitX, bHitY;
            Vector2 actualOffset = inOffset;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(m_SolidMask);

            Vector2 position = m_Rigidbody.position;

            bHitX = TryMoveX(inOffset.x, inDeltaTime, ref actualOffset, filter, out xHit);
            position.x += actualOffset.x;
            m_Rigidbody.position = position;

            bHitY = TryMoveY(inOffset.y, inDeltaTime, ref actualOffset, filter, out yHit);
            position.y += actualOffset.y;
            m_Rigidbody.position = position;

            if (bHitX)
                ioProperties.Velocity.x = 0;
            if (bHitY)
                ioProperties.Velocity.y = 0;

            Physics2D.autoSyncTransforms = true;

            Array.Clear(s_Casts, 0, s_Casts.Length);
            return true;
        }

        private bool TryMoveX(float inDeltaX, float inDeltaTime, ref Vector2 ioOffset, in ContactFilter2D inContactFilter, out RaycastHit2D outXHit)
        {
            if (Mathf.Approximately(inDeltaX, 0))
            {
                ioOffset.x = 0;
                outXHit = default(RaycastHit2D);
                return false;
            }

            float dist = inDeltaX;
            if (dist < 0)
                dist = -dist;

            Vector2 dir;
            dir.y = 0;
            if (inDeltaX < 0)
                dir.x = -1;
            else
                dir.x = 1;

            int collisionCount = m_Rigidbody.Cast(dir, inContactFilter, s_Casts, dist);
            if (collisionCount <= 0)
            {
                ioOffset.x = inDeltaX;
                outXHit = default(RaycastHit2D);
                return false;
            }

            float minFraction = 1;
            RaycastHit2D minHit = default(RaycastHit2D);
            for(int i = 0; i < collisionCount; ++i)
            {
                if (s_Casts[i].fraction < minFraction)
                {
                    minHit = s_Casts[i];
                    minFraction = minHit.fraction;
                }
            }

            dist = dist * minFraction - SeparatorIncrement;
            while(dist > 0 && m_Rigidbody.Cast(dir, inContactFilter, s_Casts, dist) > 0)
            {
                dist -= SeparatorIncrement;
            }

            ioOffset.x = dist * dir.x;

            outXHit = minHit;
            return true;
        }

        private bool TryMoveY(float inDeltaY, float inDeltaTime, ref Vector2 ioOffset, in ContactFilter2D inContactFilter, out RaycastHit2D outYHit)
        {
            if (Mathf.Approximately(inDeltaY, 0))
            {
                ioOffset.y = 0;
                outYHit = default(RaycastHit2D);
                return false;
            }

            float dist = inDeltaY;
            if (dist < 0)
                dist = -dist;

            Vector2 dir;
            dir.x = 0;
            if (inDeltaY < 0)
                dir.y = -1;
            else
                dir.y = 1;

            int collisionCount = m_Rigidbody.Cast(dir, inContactFilter, s_Casts, dist);
            if (collisionCount <= 0)
            {
                ioOffset.y = inDeltaY;
                outYHit = default(RaycastHit2D);
                return false;
            }

            float minFraction = 1;
            RaycastHit2D minHit = default(RaycastHit2D);
            for(int i = 0; i < collisionCount; ++i)
            {
                if (s_Casts[i].fraction < minFraction)
                {
                    minHit = s_Casts[i];
                    minFraction = minHit.fraction;
                }
            }

            dist = dist * minFraction - SeparatorIncrement;
            while(dist > 0 && m_Rigidbody.Cast(dir, inContactFilter, s_Casts, dist) > 0)
            {
                dist -= SeparatorIncrement;
            }

            ioOffset.y = dist * dir.y;

            outYHit = minHit;
            return true;
        }

        static private readonly RaycastHit2D[] s_Casts = new RaycastHit2D[16];
    }
}