using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [Serializable]
    public struct MovementFromOffset
    {
        public float MaxSpeed;
        public Curve SpeedCurve;
        public float Acceleration;

        public bool Apply(Vector2 inNormalizedOffset, KinematicObject2D inKinematics, float inDeltaTime)
        {
            float desiredSpeed = SpeedCurve.Evaluate(inNormalizedOffset.magnitude) * MaxSpeed;
            Vector2 desiredVelocity = inNormalizedOffset * desiredSpeed;
            Vector2 deltaVelocity = desiredVelocity - inKinematics.State.Velocity;
            
            if (deltaVelocity.sqrMagnitude > 0)
            {
                Vector2 vector = deltaVelocity * inDeltaTime * Acceleration;
                vector = PhysicsService.SmoothVelocity(vector);

                if (vector.x == 0 && vector.y == 0)
                    return false;

                Vector2 solidCheck = PhysicsService.UnitOffset(vector);

                Vector2 collideNormal;
                if (inKinematics.CheckSolid(solidCheck, out collideNormal))
                {
                    vector = PhysicsService.SmoothDeflect(vector, collideNormal, 0);
                }
                
                inKinematics.State.Velocity += vector;
                return true;
            }

            return false;
        }
    }
}