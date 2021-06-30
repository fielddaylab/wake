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
                inKinematics.State.Velocity += deltaVelocity * inDeltaTime * Acceleration;
                return true;
            }

            return false;
        }
    }
}