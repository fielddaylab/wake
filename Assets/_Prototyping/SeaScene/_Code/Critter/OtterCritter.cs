using UnityEngine;
using System;
using System.Collections;
using BeauUtil;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class OtterCritter : MonoBehaviour
    {
        [SerializeField] private float m_MaxOffset = 5;
        [SerializeField] private TweenSettings m_AnimSettings = new TweenSettings(1f);

        private void Awake()
        {
            Routine.Start(this, Animate());
        }

        private IEnumerator Animate()
        {
            yield return transform.MoveTo(transform.localPosition.y + m_MaxOffset, m_AnimSettings, Axis.Y, Space.Self)
                .Loop().Wave(Wave.Function.Sin, 1);
        }
    }
}