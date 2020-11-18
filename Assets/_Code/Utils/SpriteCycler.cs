using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class SpriteCycler : MonoBehaviour
    {
        #region Inspector

        [SerializeField, HideInInspector] private SpriteRenderer m_WorldRenderer = null;
        [SerializeField, HideInInspector] private Image m_ImageRenderer = null;
        
        [SerializeField] private Sprite[] m_Frames = null;
        [SerializeField] private float m_FrameRate = 10;

        #endregion // Inspector

        [NonSerialized] private Routine m_Playback;
        [NonSerialized] private int m_CurrentFrame = -1;
        [NonSerialized] private float m_CurrentFrameDelay = 0;

        public void Restart()
        {
            ApplyFrame(0);
            m_CurrentFrame = 0;
            m_CurrentFrameDelay = 0;
        }

        private void OnEnable()
        {
            Restart();
            m_Playback = Routine.StartLoop(this, Animate);
        }

        private void OnDisable()
        {
            m_Playback.Stop();
        }

        private void Animate()
        {
            float dt = Routine.DeltaTime;
            float frameDelay = 1f / m_FrameRate;
            m_CurrentFrameDelay -= dt;
            while(m_CurrentFrameDelay < 0)
            {
                m_CurrentFrameDelay += frameDelay;
                m_CurrentFrame = (m_CurrentFrame + 1) % m_Frames.Length;
            }

            ApplyFrame(m_CurrentFrame);
        }

        private void ApplyFrame(int inFrameIdx)
        {
            Sprite spr = m_Frames[inFrameIdx];
            if (m_WorldRenderer != null)
                m_WorldRenderer.sprite = spr;
            if (m_ImageRenderer != null)
                m_ImageRenderer.sprite = spr;
        }

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