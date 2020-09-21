using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Experiment
{
    public class ActorSense : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TriggerListener2D m_Listener = null;

        [Header("Reporting")]
        [SerializeField, Range(0, 1)] private float m_Accuracy = 1;
        [SerializeField, Range(0, 10)] private float m_Intensity = 1;
        // TODO: Response time?

        #endregion // Inspector
    }
}