using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    static public class CameraConstraints
    {
        public delegate float HintWeightFunction(Hint inHint, Vector2 inTargetPos);

        public class Bounds
        {
            public string Name;
            public Rect Region;
            public RectEdges SoftEdges = RectEdges.All;
            public RectEdges HardEdges = RectEdges.All;

            public void ConstrainSoft(ref Vector2 ioCameraCenter, in Vector2 inCameraSize)
            {
                Geom.Constrain(ref ioCameraCenter, inCameraSize, Region, SoftEdges);
            }

            public void ConstrainHard(ref Vector2 ioCameraCenter, in Vector2 inCameraSize)
            {
                Geom.Constrain(ref ioCameraCenter, inCameraSize, Region, HardEdges);
            }
        }

        public class Hint : Target
        {
            protected float m_FixedWeight;
            protected HintWeightFunction m_DynamicWeight;

            public void SetWeight(float inWeight)
            {
                m_FixedWeight = inWeight;
                m_DynamicWeight = null;
            }

            public void SetWeight(HintWeightFunction inFunction, float inOffset = 0)
            {
                m_FixedWeight = inOffset;
                m_DynamicWeight = inFunction;
            }

            public float Weight(in Vector2 inTargetPos)
            {
                if (m_DynamicWeight != null)
                    return m_DynamicWeight(this, inTargetPos) + m_FixedWeight;
                return m_FixedWeight;
            }

            public void SetWeightAsDistanceToTarget(in Vector2 inTargetPos, float inFarRadius, float inCloseRadius = 0, float inMaxWeight = 1, Curve inCurve = Curve.Linear)
            {
                Vector2 vec = Position();
                float dist = Vector2.Distance(vec, inTargetPos);
                m_FixedWeight = CalculateWeightForDistance(dist, inFarRadius, inCloseRadius, inMaxWeight, inCurve);
            }

            static public float CalculateWeightForDistance(float inDistance, float inFarRadius, float inCloseRadius = 0, float inMaxWeight = 1, Curve inCurve = Curve.Linear)
            {
                if (inDistance > inFarRadius)
                {
                    return 0;
                }
                else if (inDistance <= inCloseRadius)
                {
                    return inMaxWeight;
                }
                else
                {
                    return inCurve.Evaluate(1 - Mathf.InverseLerp(inCloseRadius, inFarRadius, inDistance)) * inMaxWeight;
                }
            }

            public void Contribute(in Vector2 inTargetPos, ref HintAccumulator ioHintAccumulator)
            {
                float weight = Weight(inTargetPos);
                if (Mathf.Approximately(weight, 0))
                    return;

                Vector2 vec = Position();
                VectorUtil.Subtract(ref vec, inTargetPos);
                VectorUtil.Multiply(ref vec, weight);

                float absWeight = Math.Abs(weight);

                ioHintAccumulator.Offset += vec;
                ioHintAccumulator.Lerp += LerpFactor * absWeight;
                ioHintAccumulator.Zoom += Zoom * absWeight;
                ioHintAccumulator.TotalWeight += absWeight;
            }
        }

        public struct HintAccumulator
        {
            public Vector2 Offset;
            public float Lerp;
            public float Zoom;

            public float TotalWeight;

            public void Calculate()
            {
                Offset /= TotalWeight;
                Lerp /= TotalWeight;
                Zoom /= TotalWeight;
            }
        }

        public class Target
        {
            protected Vector2 m_FixedPosition;
            protected Transform m_DynamicPosition;

            public string Name;
            public float Zoom = 1;
            public float LerpFactor = 1;

            public void PositionAt(Vector2 inPosition)
            {
                m_FixedPosition = inPosition;
                m_DynamicPosition = null;
            }

            public void PositionAt(Transform inTransform, Vector2 inOffset = default(Vector2))
            {
                m_FixedPosition = inOffset;
                m_DynamicPosition = inTransform;
            }

            public Vector2 Position()
            {
                if (m_DynamicPosition)
                    return (Vector2) m_DynamicPosition.position + m_FixedPosition;
                return m_FixedPosition;
            }
        }
    
        public class Drift
        {
            public string Name;
            public Vector2 Distance;
            public Vector2 Period;
            public Vector2 Offset;
        }
    }
}