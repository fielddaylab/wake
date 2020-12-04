using BeauRoutine;
using UnityEngine;

namespace Aqua
{
    static public class KinematicMath2D
    {
        #region Limits

        static private float s_MinSpeed = 0.001f;
        static private float s_MinSpeedSq = s_MinSpeed * s_MinSpeed;

        static public float MinSpeed() { return s_MinSpeed; }
        static public void SetMinSpeed(float inMinSpeed)
        {
            s_MinSpeed = inMinSpeed;
            s_MinSpeedSq = inMinSpeed * inMinSpeed;
        }

        #endregion // Limits

        #region Integration

        static public Vector2 Integrate(ref KinematicPropertyBlock2D ioProperties, float inDeltaTime, bool inbApplyGravity = true)
        {
            IntegrateLimits(ref ioProperties);

            Vector2 offset = IntegratePosition(ref ioProperties, inDeltaTime, inbApplyGravity);
            IntegrateVelocity(ref ioProperties, inDeltaTime, inbApplyGravity);
            
            IntegrateDrag(ref ioProperties, inDeltaTime);
            IntegrateLimits(ref ioProperties);
            
            return offset;
        }

        static public Vector2 IntegratePosition(ref KinematicPropertyBlock2D ioProperties, float inDeltaTime, bool inbApplyGravity = true)
        {
            float timeSq = inDeltaTime * inDeltaTime;

            float dx = ioProperties.Velocity.x * inDeltaTime + ioProperties.Acceleration.x * timeSq * 05f;
            float dy = ioProperties.Velocity.y * inDeltaTime + ioProperties.Acceleration.y * timeSq * 05f;

            if (inbApplyGravity)
            {
                dx += ioProperties.Gravity.x * ioProperties.GravityMultiplier * timeSq * 0.5f;
                dy += ioProperties.Gravity.y * ioProperties.GravityMultiplier * timeSq * 0.5f;
            }

            return new Vector2(dx, dy);
        }

        static public void IntegrateVelocity(ref KinematicPropertyBlock2D ioProperties, float inDeltaTime, bool inbApplyGravity = true)
        {
            float dx = ioProperties.Acceleration.x;
            float dy = ioProperties.Acceleration.y;

            if (inbApplyGravity)
            {
                dx += ioProperties.Gravity.x * ioProperties.GravityMultiplier;
                dy += ioProperties.Gravity.y * ioProperties.GravityMultiplier;
            }

            ioProperties.Velocity.x += dx * inDeltaTime;
            ioProperties.Velocity.y += dy * inDeltaTime;

            if (ioProperties.Friction.x > 0)
            {
                if (ioProperties.Velocity.x > 0)
                    ioProperties.Velocity.x = Mathf.Max(ioProperties.Velocity.x - ioProperties.Friction.x * inDeltaTime, 0);
                else
                    ioProperties.Velocity.x = Mathf.Min(ioProperties.Velocity.x + ioProperties.Friction.x * inDeltaTime, 0);
            }

            if (ioProperties.Friction.y > 0)
            {
                if (ioProperties.Velocity.y > 0)
                    ioProperties.Velocity.y = Mathf.Max(ioProperties.Velocity.y - ioProperties.Friction.y * inDeltaTime, 0);
                else
                    ioProperties.Velocity.y = Mathf.Min(ioProperties.Velocity.y + ioProperties.Friction.y * inDeltaTime, 0);
            }
        }

        static public void IntegrateLimits(ref KinematicPropertyBlock2D ioProperties)
        {
            float speedSq = ioProperties.Velocity.sqrMagnitude;
            float maxSq = ioProperties.MaxSpeed * ioProperties.MaxSpeed;

            if (speedSq < s_MinSpeedSq)
            {
                ioProperties.Velocity.x = ioProperties.Velocity.y = 0;
            }
            else if (maxSq > 0)
            {
                if (speedSq > maxSq)
                {
                    ioProperties.Velocity.Normalize();
                    ioProperties.Velocity.x *= ioProperties.MaxSpeed;
                    ioProperties.Velocity.y *= ioProperties.MaxSpeed;
                }
            }
        }

        static public void IntegrateDrag(ref KinematicPropertyBlock2D ioProperties, float inDeltaTime)
        {
            float drag = ioProperties.Drag;

            if (drag > 0)
            {
                float dragMult = TweenUtil.LerpDecay(drag, 1, inDeltaTime);
                ioProperties.Velocity.x *= dragMult;
                ioProperties.Velocity.y *= dragMult;
            }
        }

        #endregion // Integration
    }
}