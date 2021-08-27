using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [Serializable]
    public struct RotationFromOffset
    {
        public float Lerp;

        public bool Apply(Vector2 inNormalizedOffset, Transform inTransform, float inDeltaTime)
        {
            float desiredRotation = Mathf.Atan2(inNormalizedOffset.y, inNormalizedOffset.x) * Mathf.Rad2Deg;
            float currentRotation = inTransform.localEulerAngles.z;
            float delta = MathUtil.DegreeAngleDifference(currentRotation, desiredRotation);
            if (Mathf.Abs(delta) > 1)
            {
                float newRotation = currentRotation + delta * TweenUtil.Lerp(Lerp, 1, inDeltaTime);
                inTransform.SetRotation(newRotation, Axis.Z, Space.Self);
                return true;
            }

            return false;
        }
    }
}