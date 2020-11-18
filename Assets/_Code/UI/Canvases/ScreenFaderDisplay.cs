using UnityEngine;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using BeauPools;
using System;

namespace Aqua
{
    public class ScreenFaderDisplay : MonoBehaviour
    {
        #region Types

        [Serializable]
        private class RectPool : SerializablePool<FaderRect> { }

        [Serializable]
        private class WipePool : SerializablePool<ScreenWipe> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private RectPool m_RectPool = null;
        [SerializeField] private WipePool m_WipePool = null;

        #endregion // Inspector

        #region Unity Events

        private void Awake()
        {
            m_RectPool.Initialize();
            m_WipePool.Initialize();
        }

        #endregion // Unity Events

        #region Operations

        public void StopAll()
        {
            m_RectPool.Reset();
            m_WipePool.Reset();
        }

        #endregion // Operations

        #region Faders

        public IEnumerator Flash(Color inColor, float inDuration)
        {
            return m_RectPool.Alloc().Flash(inColor, inDuration);
        }

        public IEnumerator FadeTransition(Color inColor, float inFadeDuration, float inPause, Action inOnPause)
        {
            return m_RectPool.Alloc().FullTransition(inColor, inFadeDuration, inPause, inOnPause);
        }

        public TempAlloc<FaderRect> AllocFader()
        {
            return m_RectPool.TempAlloc();
        }

        #endregion // Faders

        #region Screen Wipe

        public IEnumerator WipeTransition(float inPause, Action inOnPause)
        {
            return m_WipePool.Alloc().FullTransition(inPause, inOnPause);
        }

        public TempAlloc<ScreenWipe> AllocWipe()
        {
            return m_WipePool.TempAlloc();
        }

        #endregion // Screen Wipe
    }

    public enum ScreenFaderLayer
    {
        World,
        Screen
    }
}