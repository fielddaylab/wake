using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [Serializable]
    public class MouseDistanceInputFilter
    {
        public struct Output
        {
            public Vector2? Target;
            public Vector2 RawOffset;
            public Vector2 ClampedOffset;
            public Vector2 NormalizedOffset;
        }

        public float MinDistance = 1;
        public float MaxDistance = 8;
        public Curve DistanceCurve = Curve.Linear;

        public bool Process(DeviceInput inInput, Transform inReference, Vector3? inTargetOverride, Plane? inPlaneOverride, out Output outOutput)
        {
            if (!inInput.IsActive())
            {
                outOutput = default(Output);
                return false;
            }

            if (inTargetOverride.HasValue)
            {
                outOutput.Target = inTargetOverride.Value;
            }
            else if (inPlaneOverride.HasValue)
            {
                outOutput.Target = inInput.WorldMousePosition(inPlaneOverride.Value);
            }
            else
            {
                outOutput.Target = inInput.WorldMousePosition();
            }

            Vector2 currentPos = inReference.position;
            Vector2 rawOffset = outOutput.Target.GetValueOrDefault(currentPos) - currentPos;

            float magnitude = rawOffset.magnitude;
            float normalizedMagnitude = DistanceCurve.Evaluate(Mathf.Clamp01(MathUtils.Remap(magnitude, MinDistance, MaxDistance, 0, 1)));
            outOutput.RawOffset = rawOffset;

            Vector2 clamped = outOutput.RawOffset;
            clamped.Normalize();
            clamped *= Mathf.Clamp(magnitude, MinDistance, MaxDistance);
            outOutput.ClampedOffset = clamped;

            Vector2 normalized = outOutput.RawOffset;
            normalized.Normalize();
            normalized *= normalizedMagnitude;

            outOutput.NormalizedOffset = normalized;

            return true;
        }
    }
}