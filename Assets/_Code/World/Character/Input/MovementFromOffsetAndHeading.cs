using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [Serializable]
    public struct MovementFromOffsetAndHeading
    {
        public float MaxSpeed;
        public Curve SpeedCurve;
        public float Acceleration;
        public float TurnRate;
        public float InPlaceRotationSpeedThreshold;
        public float InPlaceRotationMultiplier;

        public bool Apply(Vector2 inNormalizedOffset, KinematicObject2D inKinematics, float inDeltaTime, float inMultiplier)
        {
            float desiredSpeed = SpeedCurve.Evaluate(inNormalizedOffset.magnitude) * MaxSpeed * inMultiplier;
            Vector2 desiredVelocity = inNormalizedOffset * desiredSpeed;
            Vector2 deltaVelocity = desiredVelocity - inKinematics.State.Velocity;
            float currentSpeed = inKinematics.State.Velocity.magnitude;
            float deltaSpeed = desiredSpeed - currentSpeed;

            if (deltaVelocity.sqrMagnitude > 0)
            {
                float desiredRotation = Mathf.Atan2(inNormalizedOffset.y, inNormalizedOffset.x) * Mathf.Rad2Deg;
                float currentRotation = inKinematics.Transform.localEulerAngles.z;
                float delta = MathUtils.DegreeAngleDifference(currentRotation, desiredRotation);
                bool inPlace = currentSpeed < InPlaceRotationSpeedThreshold && (delta < -TurnRate || delta > TurnRate);
                float turnRate = TurnRate;
                if (inPlace && InPlaceRotationMultiplier > 0) {
                    turnRate *= InPlaceRotationMultiplier;
                }
                float newRotation = currentRotation + Mathf.Clamp(delta, -turnRate, turnRate) * inDeltaTime;

                if (inPlace)
                {
                    inKinematics.transform.SetRotation(newRotation, Axis.Z, Space.Self);
                    return true;
                }

                float vecSpeed = deltaSpeed * (inDeltaTime * Acceleration * inMultiplier);
                Vector2 vector = new Vector2(vecSpeed, 0);
                Geom.Rotate(ref vector, newRotation * Mathf.Deg2Rad);
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
                inKinematics.transform.SetRotation(newRotation, Axis.Z, Space.Self);
                return true;
            }

            return false;
        }
    }
}