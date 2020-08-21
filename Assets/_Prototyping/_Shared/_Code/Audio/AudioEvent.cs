using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace ProtoAudio
{
    [CreateAssetMenu(menuName = "Prototype/Audio/Audio Event")]
    public class AudioEvent : ScriptableObject, IKeyValuePair<string, AudioEvent>
    {
        #region Inspector

        [SerializeField] private AudioClip[] m_Clips = null;

        [Header("Playback Settings")]
        [SerializeField] private FloatRange m_Volume = new FloatRange(1);
        [SerializeField] private FloatRange m_Pitch = new FloatRange(1);
        [SerializeField] private FloatRange m_Delay = new FloatRange(0);
        [SerializeField] private bool m_Loop = false;
        [SerializeField, ShowIfField("m_Loop")] private bool m_RandomizeStartingPosition = false;

        #endregion // Inspector

        [NonSerialized] private string m_Id;
        [NonSerialized] private int[] m_Order;
        [NonSerialized] private int m_OrderIdx = -1;

        #region IKeyValuePair

        string IKeyValuePair<string, AudioEvent>.Key { get { return Id(); } }

        AudioEvent IKeyValuePair<string, AudioEvent>.Value { get { return this; } }

        #endregion // IKeyValuePair

        public string Id() { return m_Id ?? (m_Id = name); }

        public bool CanPlay()
        {
            return m_Clips.Length > 0;
        }

        private AudioClip GetNextClip(System.Random inRandom)
        {
            if (m_Clips.Length <= 0)
                return null;

            if (m_Order == null)
            {
                m_Order = new int[m_Clips.Length];
                for(int i = 0; i < m_Order.Length; ++i)
                    m_Order[i] = i;
            }

            if (m_OrderIdx < 0)
            {
                inRandom.Shuffle(m_Order);
                m_OrderIdx = 0;
            }
            else if (++m_OrderIdx >= m_Order.Length)
            {
                inRandom.Shuffle(m_Order, 0, m_Order.Length - 1);
                inRandom.Shuffle(m_Order, 1, m_Order.Length - 1);
                m_OrderIdx = 0;
            }

            int clipIdx = m_Order[m_OrderIdx];
            return m_Clips[clipIdx];
        }

        public bool Load(AudioSource inSource, System.Random inRandom, out AudioPropertyBlock outProperties, out float outDelay)
        {
            if (m_Clips.Length <= 0)
            {
                outProperties = AudioPropertyBlock.Default;
                outDelay = 0;
                return false;
            }

            inSource.clip = GetNextClip(inRandom);
            inSource.loop = m_Loop;

            if (m_Loop && m_RandomizeStartingPosition)
                inSource.time = inRandom.NextFloat(inSource.clip.length);
            else
                inSource.time = 0;
            
            outProperties.Volume = m_Volume.Generate(inRandom);
            outProperties.Pitch = m_Pitch.Generate(inRandom);
            outDelay = m_Delay.Generate(inRandom);

            outProperties.Mute = false;
            outProperties.Pause = false;
            return true;
        }

        public void ResetOrdering()
        {
            m_OrderIdx = -1;
        }
    }
}