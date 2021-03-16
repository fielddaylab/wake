using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    /// <summary>
    /// Animates a sequence of sprites.
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        [ServiceReference] static private SpriteAnimatorService Manager { get; set; }

        #region Types

        public delegate void AnimationChangeDelegate(SpriteAnimation inAnimation);
        public delegate void FrameChangeDelegate(SpriteAnimation inAnimation, int inFrameIndex);
        public delegate void FrameEventDelegate(TagData inData);

        #endregion // Types

        #region Inspector

        [SerializeField, HideInInspector] private SpriteRenderer m_WorldRenderer = null;
        [SerializeField, HideInInspector] private Image m_ImageRenderer = null;
        
        [SerializeField] private SpriteAnimation m_Animation = null;
        
        [Header("Settings")]
        [SerializeField] private float m_TimeScale = 1;
        [SerializeField, ShowIfField("m_ImageRenderer")] private bool m_SyncPivot = false;

        #endregion // Inspector

        [NonSerialized] private int m_CurrentFrame = -1;
        [NonSerialized] private float m_CurrentFrameDelay = 0;

        [NonSerialized] private int m_LastAppliedFrame = -1;
        [NonSerialized] private bool m_Playing;

        public event AnimationChangeDelegate OnAnimationChange;
        public event FrameChangeDelegate OnFrameChange;
        public event FrameEventDelegate OnFrameEvent;

        public SpriteRenderer SpriteRenderer { get { return m_WorldRenderer; } }
        public Image ImageRenderer { get { return m_ImageRenderer; } }

        #region Unity Events

        private void Start()
        {
            if (m_Animation != null && m_CurrentFrame == -1)
                Play(m_Animation, true);
        }

        private void OnEnable()
        {
            Manager.RegisterAnimator(this);
        }

        private void OnDisable()
        {
            Manager?.DeregisterAnimator(this);
        }

        #endregion // Unity Events

        #region Operations

        public void Play(SpriteAnimation inAnimation, bool inbRestart = false)
        {
            TrySetAnimation(inAnimation, inbRestart);
            m_Playing = true;
        }

        public void Stop()
        {
            TrySetAnimation(null, false);
            m_Playing = false;
        }

        public void Restart()
        {
            TrySetAnimation(m_Animation, true);
            m_Playing = true;
        }

        public void Pause()
        {
            m_Playing = false;
        }

        public void Resume()
        {
            m_Playing = true;
        }

        #endregion // Operations

        #region Accessors / Setters

        public SpriteAnimation Animation
        {
            get { return m_Animation; }
            set { TrySetAnimation(value, false); }
        }

        public int FrameIndex
        {
            get { return m_CurrentFrame; }
            set
            {
                ApplyFrame(value, false, false);
                m_CurrentFrameDelay = m_Animation != null ? m_Animation.FrameDuration() : 0;
                m_Playing = false;
            }
        }

        #endregion // Accessors / Setters

        #region Animation

        public void AdvanceAnimation(float inDeltaTime)
        {
            if (m_TimeScale <= 0 || !IsAnimated(m_Animation) || !m_Playing)
                return;
            
            float dt = inDeltaTime * m_TimeScale;
            float frameDelay = m_Animation.FrameDuration();
            m_CurrentFrameDelay -= dt;
            while(m_CurrentFrameDelay < 0 && m_Playing)
            {
                m_CurrentFrameDelay += frameDelay;
                AdvanceFrame();
            }
        }

        static private bool IsAnimated(SpriteAnimation inAnimation)
        {
            return inAnimation != null && inAnimation.IsAnimated();
        }

        private void AdvanceFrame()
        {
            if (++m_CurrentFrame >= m_Animation.FrameCount())
            {
                if (m_Animation.HasTransition())
                {
                    SpriteAnimation nextAnim = m_Animation.NextAnim(RNG.Instance);
                    if (TrySetAnimation(nextAnim, false))
                        return;
                }
                else
                {
                    if (m_Animation.IsOneShot())
                    {
                        m_CurrentFrameDelay = 0;
                        m_Playing = false;
                        return;
                    }
                    
                    m_CurrentFrame = 0;
                }
            }
            
            ApplyFrame(m_CurrentFrame, true, false);
        }

        private bool TrySetAnimation(SpriteAnimation inAnimation, bool inbForce)
        {
            if (!inbForce && m_Animation == inAnimation)
                return false;

            m_Animation = inAnimation;
            if (!IsAnimated(m_Animation))
                m_Playing = false;

            if (m_Animation)
            {
                m_CurrentFrame = m_Animation.FirstFrame(RNG.Instance, out m_CurrentFrameDelay);
                
                if (OnAnimationChange != null)
                    OnAnimationChange(m_Animation);
                
                ApplyFrame(m_CurrentFrame, true, true);
            }
            else
            {
                m_CurrentFrameDelay = 0;

                if (OnAnimationChange != null)
                    OnAnimationChange(m_Animation);
            }

            return true;
        }

        private bool ApplyFrame(int inFrameIdx, bool inbProcessEvents, bool inbForce)
        {
            if (!inbForce && m_LastAppliedFrame == inFrameIdx)
                return false;

            m_LastAppliedFrame = inFrameIdx;

            if (m_Animation == null || inFrameIdx >= m_Animation.FrameCount())
                return false;

            SpriteFrame frame = m_Animation.Frame(inFrameIdx);
            Sprite frameSprite = frame.Sprite;

            if (inbProcessEvents)
            {
                Services.Audio.PostEvent(frame.AudioEvent);
            }
            
            if (m_WorldRenderer != null)
            {
                m_WorldRenderer.sprite = frameSprite;
            }
            if (m_ImageRenderer != null)
            {
                m_ImageRenderer.sprite = frameSprite;
                m_ImageRenderer.canvasRenderer.SetAlpha(frameSprite ? 1 : 0);
                if (m_SyncPivot)
                {
                    m_ImageRenderer.rectTransform.pivot = frameSprite.pivot / frameSprite.rect.size;
                }
            }

            if (OnFrameChange != null)
            {
                OnFrameChange(m_Animation, inFrameIdx);
            }

            if (!string.IsNullOrEmpty(frame.ExtraEvents) && OnFrameEvent != null)
            {
                foreach(var evtSlice in StringSlice.EnumeratedSplit(frame.ExtraEvents, SpriteFrame.EventSplitChars, StringSplitOptions.RemoveEmptyEntries))
                {
                    TagData evtData = TagData.Parse(evtSlice, TagStringParser.RichTextDelimiters);
                    OnFrameEvent(evtData);
                }
            }

            return true;
        }

        #endregion // Animation

        #if UNITY_EDITOR

        private void Reset()
        {
            m_ImageRenderer = GetComponent<Image>();
            m_WorldRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnValidate()
        {
            m_ImageRenderer = GetComponent<Image>();
            m_WorldRenderer = GetComponent<SpriteRenderer>();
        }

        #endif // UNITY_EDITOR
    }
}