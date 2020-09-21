using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace ProtoAudio
{
    [CreateAssetMenu(menuName = "Prototype/Audio/Audio Event")]
    public class AudioEvent : ScriptableObject, IKeyValuePair<StringHash, AudioEvent>
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

        [NonSerialized] private StringHash m_Id;
        [NonSerialized] private RandomDeck<AudioClip> m_ClipDeck;

        #region IKeyValuePair

        StringHash IKeyValuePair<StringHash, AudioEvent>.Key { get { return Id(); } }

        AudioEvent IKeyValuePair<StringHash, AudioEvent>.Value { get { return this; } }

        #endregion // IKeyValuePair

        public StringHash Id() { return !m_Id.IsEmpty ? m_Id : (m_Id = name); }

        public bool CanPlay()
        {
            return m_Clips.Length > 0;
        }

        private AudioClip GetNextClip(System.Random inRandom)
        {
            if (m_ClipDeck == null)
                m_ClipDeck = new RandomDeck<AudioClip>(m_Clips);

            return m_ClipDeck.Next(inRandom);
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
            if (m_ClipDeck != null)
                m_ClipDeck.Reset();
        }

        #if UNITY_EDITOR

        // TODO:    Add commands to generate AudioEvent from an AudioClip,
        //          or combine multiple AudioClips into a single AudioEvent

        #endif // UNITY_EDITOR
    }
}