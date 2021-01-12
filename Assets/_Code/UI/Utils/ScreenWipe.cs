using UnityEngine;
using BeauRoutine;
using System.Collections;
using System;
using BeauPools;

namespace Aqua
{
    public class ScreenWipe : MonoBehaviour, IPooledObject<ScreenWipe>
    {
        #region Inspector

        [SerializeField] private float m_Padding = 200;
        
        #endregion // Inspector

        [NonSerialized] private RectTransform m_Transform;
        private IPool<ScreenWipe> m_Pool;
        private Routine m_FadeRoutine;
        private Routine m_SequenceRoutine;

        #region Animations

        public IEnumerator Show()
        {
            return m_FadeRoutine.Replace(this, ShowRoutine()).Wait();
        }

        public void InstantShow()
        {
            m_FadeRoutine.Stop();
            SetPosition(0);
        }

        private IEnumerator ShowRoutine()
        {
            yield return Tween.Float(-1, 0, SetPosition, 0.25f);
        }

        public IEnumerator Hide(bool inbAutoFree = true)
        {
            return m_FadeRoutine.Replace(this, HideRoutine(inbAutoFree)).Wait();
        }

        public void InstantHide(bool inbAutoFree = true)
        {
            m_FadeRoutine.Stop();
            SetPosition(1);
            if (inbAutoFree)
                m_Pool.Free(this);
        }

        private IEnumerator HideRoutine(bool inbAutoFree)
        {
            yield return Routine.Inline(Tween.Float(0, 1, SetPosition, 0.25f));
            if (inbAutoFree)
                m_Pool.Free(this);
        }

        #endregion // Animations

        #region Shortcuts

        public IEnumerator FullTransition(float inPause, Action inOnPause)
        {
            return m_SequenceRoutine.Replace(this, FullTransitionRoutine(inPause, inOnPause)).Wait();
        }

        public IEnumerator FullTransition(float inPause, Func<IEnumerator> inOnPause)
        {
            return m_SequenceRoutine.Replace(this, FullTransitionRoutine(inPause, inOnPause)).Wait();
        }

        private IEnumerator FullTransitionRoutine(float inPause, Action inOnPause)
        {
            yield return Show();
            if (inOnPause != null)
                inOnPause();
            yield return inPause;
            yield return Hide();
        }

        private IEnumerator FullTransitionRoutine(float inPause, Func<IEnumerator> inOnPause)
        {
            yield return Show();
            if (inOnPause != null)
                yield return inOnPause();
            yield return inPause;
            yield return Hide();
        }

        #endregion // Shortcuts

        private void SetPosition(float inPosition)
        {
            float width = m_Transform.rect.width + m_Padding;
            m_Transform.SetPosition(width * inPosition, Axis.X, Space.Self);
        }

        #region IPooledObject

        void IPooledObject<ScreenWipe>.OnAlloc()
        {
            SetPosition(-1);
        }

        void IPooledObject<ScreenWipe>.OnConstruct(IPool<ScreenWipe> inPool)
        {
            m_Pool = inPool;
            m_Transform = (RectTransform) transform;
        }

        void IPooledObject<ScreenWipe>.OnDestruct()
        {
            m_Pool = null;
        }

        void IPooledObject<ScreenWipe>.OnFree()
        {
            m_FadeRoutine.Stop();
            m_SequenceRoutine.Stop();
        }

        #endregion // IPooledObject
    }
}