using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    public class BehaviorCaptureCircle : MonoBehaviour
    {
        public struct TempAlloc : IDisposable
        {
            private BehaviorCaptureCircle m_Circle;
            private ushort m_UseCount;

            public TempAlloc(BehaviorCaptureCircle inCircle)
            {
                m_Circle = inCircle;
                m_UseCount = inCircle.UseCount;
            }

            public void Dispose()
            {
                if (IsValid())
                {
                    m_Circle.OnDispose?.Invoke(m_Circle);
                    m_Circle = null;
                }
            }

            public bool IsValid()
            {
                return m_Circle != null && m_Circle.Active && m_Circle.UseCount == m_UseCount;
            }
        }

        #region Inspector

        [Required] public Transform Scale;
        [Required] public PointerListener Pointer;
        [Required] public ColorGroup Color;

        #endregion // Inspector

        [NonSerialized] public StringHash32 FactId;
        [NonSerialized] public Routine Animation;
        [NonSerialized] public Action<BehaviorCaptureCircle> OnClick;
        [NonSerialized] public Action<BehaviorCaptureCircle> OnDispose;
        [NonSerialized] public ushort UseCount = 0;
        [NonSerialized] public bool Active;

        private void Awake()
        {
            Pointer.onClick.AddListener((p) => OnClick?.Invoke(this));
        }
    }
}