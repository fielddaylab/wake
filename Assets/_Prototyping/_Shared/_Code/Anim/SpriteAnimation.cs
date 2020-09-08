using System;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua
{
    /// <summary>
    /// Animated sequence of sprites.
    /// </summary>
    [CreateAssetMenu(menuName = "Prototype/SpriteAnim/Animation")]
    public class SpriteAnimation : ScriptableObject
    {
        #region Inspector

        [Header("Frames")]
        [SerializeField] private SpriteFrame[] m_Frames = null;
        [SerializeField] private float m_FrameRate = 10;

        [Header("Playback")]
        [SerializeField] private SpriteAnimPlayMode m_PlaybackMode = SpriteAnimPlayMode.Loop;
        [SerializeField, KeyValuePair("Animation", "Weight")] private SpriteAnimTransition[] m_NextAnimations = null;

        #endregion // Inspector

        public int FrameCount() { return m_Frames.Length; }
        public float FrameRate() { return m_FrameRate; }

        public float FrameDuration() { return m_Frames.Length == 0 ? 0 : 1f / m_FrameRate; }
        public float TotalDuration() { return m_Frames.Length == 0 ? 0 : m_Frames.Length / m_FrameRate; }

        public SpriteFrame Frame(int inIndex)
        {
            return m_Frames[inIndex];
        }

        public bool HasTransition()
        {
            return m_PlaybackMode == SpriteAnimPlayMode.Transition && m_NextAnimations.Length > 0;
        }

        public bool IsAnimated()
        {
            if (m_Frames.Length == 0)
                return false;

            switch(m_PlaybackMode)
            {
                case SpriteAnimPlayMode.StillFrames:
                default:
                    return false;
                
                case SpriteAnimPlayMode.Loop:
                case SpriteAnimPlayMode.LoopRandom:
                case SpriteAnimPlayMode.OneShot:
                    return m_Frames.Length > 1;

                case SpriteAnimPlayMode.Transition:
                    return m_NextAnimations.Length > 0;
            }
        }

        public bool IsOneShot()
        {
            return m_PlaybackMode == SpriteAnimPlayMode.OneShot;
        }

        public SpriteAnimation NextAnim(System.Random ioRandom)
        {
            switch(m_PlaybackMode)
            {
                case SpriteAnimPlayMode.OneShot:
                default:
                    return null;

                case SpriteAnimPlayMode.Loop:
                case SpriteAnimPlayMode.LoopRandom:
                    return this;

                case SpriteAnimPlayMode.Transition:
                    {
                        if (m_NextAnimations == null || m_NextAnimations.Length == 0)
                        {
                            return null;
                        }

                        SpriteAnimation anim = null;

                        if (m_NextAnimations.Length == 1)
                        {
                            anim = m_NextAnimations[0].Animation;
                        }
                        else
                        {
                            WeightedSet<SpriteAnimation> weightedSet = m_NextSelector ?? (m_NextSelector = new WeightedSet<SpriteAnimation>(m_NextAnimations.Length));
                            weightedSet.Clear();
                            foreach(var choice in m_NextAnimations)
                            {
                                weightedSet.Add(choice.Animation, choice.Weight);
                            }

                            if (weightedSet.Count > 0)
                            {
                                anim = weightedSet.GetItemNormalized(ioRandom.NextFloat());
                            }
                        }

                        return anim;
                    }
            }
        }

        public int FirstFrame(System.Random ioRandom, out float outDelay)
        {
            if (m_PlaybackMode == SpriteAnimPlayMode.LoopRandom)
            {
                outDelay = ioRandom.NextFloat(FrameDuration());
                return ioRandom.Next(FrameCount());
            }

            outDelay = FrameDuration();
            return 0;
        }

        private WeightedSet<SpriteAnimation> m_NextSelector = null;
    }

    /// <summary>
    /// Indicates how a SpriteAnimation will play back.
    /// </summary>
    public enum SpriteAnimPlayMode
    {
        OneShot,
        Transition,
        Loop,
        LoopRandom,
        StillFrames
    }

    /// <summary>
    /// Transition to another animation.
    /// </summary>
    [Serializable]
    public class SpriteAnimTransition
    {
        public SpriteAnimation Animation;
        [Range(0, 100)] public float Weight = 1;
    }
}