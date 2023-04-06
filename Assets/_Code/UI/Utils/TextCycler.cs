using System;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class TextCycler : MonoBehaviour
    {
        #region Inspector

        [SerializeField, HideInInspector] private TMP_Text m_Renderer = null;
        
        [SerializeField] private string[] m_Frames = null;
        [SerializeField] private float m_FrameRate = 10;

        #endregion // Inspector

        [NonSerialized] private Routine m_Playback;
        [NonSerialized] private int m_CurrentFrame = -1;
        [NonSerialized] private float m_CurrentFrameDelay = 0;

        public void Restart()
        {
            ApplyFrame(0);
            m_CurrentFrame = 0;
            m_CurrentFrameDelay = 1f / m_FrameRate;
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
            string str = m_Frames[inFrameIdx];
            if (m_Renderer != null) {
                m_Renderer.SetText(str);
            }
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Renderer = GetComponent<TMP_Text>();
        }

        private void OnValidate()
        {
            m_Renderer = GetComponent<TMP_Text>();
        }

        #endif // UNITY_EDITOR
    }
}