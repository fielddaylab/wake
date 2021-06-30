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

        public void Apply(Vector2 inNormalizedOffset, Transform inTransform, float inDeltaTime)
        {
            float desiredRotation = Mathf.Atan2(inNormalizedOffset.y, inNormalizedOffset.x) * Mathf.Rad2Deg;
            float currentRotation = inTransform.localEulerAngles.z;
            float delta = MathUtil.DegreeAngleDifference(currentRotation, desiredRotation);
            float newRotation = currentRotation + delta * TweenUtil.Lerp(Lerp, 1, inDeltaTime);
            inTransform.SetRotation(newRotation, Axis.Z, Space.Self);
        }
    }
}