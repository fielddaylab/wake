using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Experiment
{
    public class ActorTicker : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private float m_TimeScale = 1;

        #endregion // Inspector

        [NonSerialized] private double m_TimeAsDouble = 0;

        public uint CurrentTimeMS()
        {
            return (uint) (m_TimeAsDouble * 1000);
        }

        public void Advance(double inDeltaTime)
        {
            m_TimeAsDouble += inDeltaTime * m_TimeScale;
        }

        public void ResetTime()
        {
            m_TimeAsDouble = 0;
        }
    }
}