using UnityEngine;
using System;
using System.Collections;
using BeauUtil;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class UrchinCritter : MonoBehaviour
    {
        [SerializeField] private float m_MaxOffset = 5;
        [SerializeField] private float m_RollSpeed = 5;
        [SerializeField] private float m_Radius = 0.25f;

        private void Awake()
        {
            Routine.Start(this, Animate());
        }

        private IEnumerator Animate()
        {
            float xStart = transform.localPosition.x;
            float rollDegPerMove = 360f / (float) (m_Radius * 2 * Math.PI);

            while(true)
            {
                float targetX = RNG.Instance.NextFloat(-m_MaxOffset, m_MaxOffset) + xStart;
                float dist = targetX - transform.localPosition.x;
                float time = Math.Abs(dist) / m_RollSpeed;
                float rot = transform.localEulerAngles.z - dist * rollDegPerMove;

                yield return Routine.Combine(
                    transform.MoveTo(targetX, time, Axis.X, Space.Self),
                    transform.RotateTo(rot, time, Axis.Z, Space.Self, AngleMode.Absolute)
                );

                yield return RNG.Instance.NextFloat(1, 3);
            }
        }
    }
}