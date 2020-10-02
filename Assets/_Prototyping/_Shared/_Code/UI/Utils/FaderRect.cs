using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauUtil;
using System.Collections;
using BeauPools;
using System;

namespace ProtoAqua
{
    public class FaderRect : MonoBehaviour, IPooledObject<FaderRect>
    {
        #region Inspector

        [SerializeField] private Graphic m_Graphic = null;
        
        #endregion // Inspector

        private IPool<FaderRect> m_Pool;
        private Routine m_FadeRoutine;
        private Routine m_SequenceRoutine;

        #region Animations

        public IEnumerator Show(Color inColor, float inDuration)
        {
            if (inDuration <= 0)
            {
                m_FadeRoutine.Stop();
                m_Graphic.color = inColor;
                return null;
            }

            m_FadeRoutine.Replace(this, ShowRoutine(inColor, inDuration));
            return m_FadeRoutine.Wait();
        }

        private IEnumerator ShowRoutine(Color inColor, float inDuration)
        {
            if (m_Graphic.GetAlpha() == 0)
            {
                m_Graphic.color = inColor.WithAlpha(0);
                yield return m_Graphic.FadeTo(inColor.a, inDuration);
            }
            else
            {
                yield return m_Graphic.ColorTo(inColor, inDuration, ColorUpdate.FullColor);
            }
        }

        public IEnumerator Hide(float inDuration, bool inbAutoFree = true)
        {
            if (inDuration <= 0)
            {
                m_FadeRoutine.Stop();
                if (inbAutoFree)
                    m_Pool.Free(this);
                return null;
            }

            m_FadeRoutine.Replace(this, HideRoutine(inDuration, inbAutoFree));
            return m_FadeRoutine.Wait();
        }

        private IEnumerator HideRoutine(float inDuration, bool inbAutoFree)
        {
            yield return m_Graphic.FadeTo(0, inDuration);
            if (inbAutoFree)
                m_Pool.Free(this);
        }

        #endregion // Animations

        #region Shortcuts

        public IEnumerator Flash(Color inColor, float inDuration)
        {
            return m_SequenceRoutine.Replace(this, FlashRoutine(inColor, inDuration)).Wait();
        }

        private IEnumerator FlashRoutine(Color inColor, float inDuration)
        {
            yield return Show(inColor, 0);
            yield return Hide(inDuration);
        }

        public IEnumerator FullTransition(Color inColor, float inFadeDuration, float inPause, Action inOnPause)
        {
            return m_SequenceRoutine.Replace(this, FullTransitionRoutine(inColor, inFadeDuration, inPause, inOnPause)).Wait();
        }

        private IEnumerator FullTransitionRoutine(Color inColor, float inFadeDuration, float inPause, Action inOnPause)
        {
            yield return Show(inColor, inFadeDuration);
            if (inOnPause != null)
                inOnPause();
            yield return inPause;
            yield return Hide(inFadeDuration);
        }

        #endregion // Shortcuts

        #region IPooledObject

        void IPooledObject<FaderRect>.OnAlloc()
        {
            m_Graphic.SetAlpha(0);
        }

        void IPooledObject<FaderRect>.OnConstruct(IPool<FaderRect> inPool)
        {
            m_Pool = inPool;
        }

        void IPooledObject<FaderRect>.OnDestruct()
        {
            m_Pool = null;
        }

        void IPooledObject<FaderRect>.OnFree()
        {
            m_FadeRoutine.Stop();
            m_SequenceRoutine.Stop();
        }

        #endregion // IPooledObject
    }
}