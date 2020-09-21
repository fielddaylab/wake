using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Experiment
{
    public class ActorStats : MonoBehaviour
    {
        #region Inspector

        #endregion // Inspector

        [NonSerialized] private float m_CurrentEnergy = 0;
        [NonSerialized] private float m_CurrentSleepiness = 0;
        [NonSerialized] private float m_CurrentOxygen = 0;

        [NonSerialized] private float m_CurrentFear = 0;
        [NonSerialized] private float m_CurrentDistraction = 0;
    }
}