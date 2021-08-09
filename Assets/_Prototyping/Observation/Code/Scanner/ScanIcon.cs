using System;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using BeauPools;

namespace ProtoAqua.Observation
{
    public class ScanIcon : MonoBehaviour, IPooledObject<ScanIcon>
    {
        #region Inspector

        [SerializeField] private Transform m_Root = null;
        [SerializeField] private SpriteRenderer m_Fill = null;
        [SerializeField] private SpriteRenderer m_Border = null;
        [SerializeField] private SpriteRenderer m_Icon = null;

        #endregion // Inspector

        [NonSerialized] private float m_FillSize;
        [NonSerialized] private Transform m_FillTransform;
        [NonSerialized] private Transform m_IconTransform;
        [NonSerialized] private bool m_Showing;
        private IPool<ScanIcon> m_Pool;
        private Routine m_SpinRoutine;
        private Routine m_RootRoutine;

        public void Show()
        {
            if (!m_Showing)
            {
                m_Showing = true;
                m_RootRoutine.Replace(this, ToOn(this));
            }
        }

        public void Hide()
        {
            if (m_Showing)
            {
                SetSpinning(false);
                m_Showing = false;
                m_RootRoutine.Replace(this, ToOff(this));
            }
        }

        public void SetIcon(Sprite inIcon)
        {
            m_Icon.sprite = inIcon;
        }

        public void SetColor(Color inLineColor, Color inFillColor)
        {
            m_Border.color = m_Icon.color = inLineColor;
            m_Fill.color = inFillColor;
        }

        public void SetFill(float inFill)
        {
            m_FillTransform.SetScale(inFill * m_FillSize, Axis.XY);
            SetSpinning(inFill > 0 && inFill < 1);
        }

        public void SetSpinning(bool inbSpinning)
        {
            if (inbSpinning)
            {
                if (!m_SpinRoutine)
                {
                    m_SpinRoutine = Routine.Start(this, Spin(this));
                }
            }
            else
            {
                if (m_SpinRoutine)
                {
                    m_SpinRoutine.Stop();
                    m_IconTransform.SetRotation(0, Axis.Y, Space.Self);
                }
            }
        }

        #region Animations

        static private IEnumerator ToOn(ScanIcon inIcon)
        {
            yield return inIcon.m_Root.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.BackOut);
        }

        static private IEnumerator ToOff(ScanIcon inIcon)
        {
            yield return inIcon.m_Root.ScaleTo(0, 0.2f, Axis.XY);
            inIcon.m_Pool.Free(inIcon);
        }

        static private IEnumerator Spin(ScanIcon inIcon)
        {
            return inIcon.m_IconTransform.RotateTo(360f, 1.5f, Axis.Y, Space.Self, AngleMode.Absolute).Loop();
        }

        #endregion // Animations

        #region IPooledObject

        void IPooledObject<ScanIcon>.OnConstruct(IPool<ScanIcon> inPool)
        {
            m_FillTransform = m_Fill.transform;
            m_FillSize = m_FillTransform.localScale.x;
            m_FillTransform.SetScale(0, Axis.XY);

            m_IconTransform = m_Icon.transform;

            m_Pool = inPool;
        }

        void IPooledObject<ScanIcon>.OnDestruct() { }
        
        void IPooledObject<ScanIcon>.OnAlloc()
        {
            m_Root.SetScale(0, Axis.XY);
        }

        void IPooledObject<ScanIcon>.OnFree()
        {
            m_RootRoutine.Stop();
            m_SpinRoutine.Stop();
            m_IconTransform.SetRotation(0, Axis.Y, Space.Self);
            m_FillTransform.transform.SetScale(0, Axis.XY);
            m_Showing = false;
        }

        #endregion // IPoolAllocHandler
    }
}