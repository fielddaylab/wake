using UnityEngine;
using ProtoCP;
using System;
using BeauRoutine;
using BeauUtil;
using System.Collections;
using BeauPools;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class BehaviorCaptureLocation : MonoBehaviour, IPooledObject<BehaviorCaptureLocation>
    {
        #region Inspector

        [SerializeField, Required] private Transform m_ScaleTransform = null;
        [SerializeField, Required] private PointerListener m_Proxy = null;
        [SerializeField, Required] private Transform m_RotateTransform = null;
        [SerializeField, Required] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector

        [NonSerialized] private IPool<BehaviorCaptureLocation> m_Pool;
        [NonSerialized] private Transform m_DefaultParent;

        [NonSerialized] private bool m_Showing = false;
        [NonSerialized] private Routine m_ShowRoutine;
        [NonSerialized] private Routine m_RotateRoutine;
        [NonSerialized] private bool m_KillQueued;

        [NonSerialized] private uint m_Magic;
        [NonSerialized] private bool m_Alive;

        private void Awake()
        {
            m_Proxy.onPointerDown.AddListener((p) => OnClick());
        }

        public uint Initialize(StringHash32 inBehaviorId, Transform inParent)
        {
            BehaviorId = inBehaviorId;
            m_ScaleTransform.SetPosition(inParent.position);
            return ++m_Magic;
        }

        public StringHash32 BehaviorId { get; set; }

        public void TryKill(uint inMagic)
        {
            if (m_Alive && m_Magic == inMagic)
            {
                Kill(false);
            }
        }

        public void Show()
        {
            if (!m_Showing)
            {
                m_Showing = true;
                m_ShowRoutine.Replace(this, ShowRoutine());
            }
        }

        private IEnumerator ShowRoutine()
        {
            m_ColorGroup.BlocksRaycasts = true;
            m_RotateRoutine.Stop();
            m_RotateRoutine = Routine.StartLoop(this, Rotate);
            yield return Routine.Combine(
                Tween.Float(m_ColorGroup.GetAlpha(), 1, m_ColorGroup.SetAlpha, 0.25f),
                m_ScaleTransform.ScaleTo(1, 0.25f).Ease(Curve.BackOut).ForceOnCancel()
            );
        }

        private void OnClick()
        {
            if (!m_Showing)
                return;
            
            Services.Events.Dispatch(ExperimentEvents.AttemptObserveBehavior, BehaviorId);
            Kill(false);
        }

        public void Hide()
        {
            if (m_Showing)
            {
                m_Showing = false;
                m_ShowRoutine.Replace(this, HideRoutine());
            }
        }

        private IEnumerator HideRoutine()
        {
            m_ColorGroup.BlocksRaycasts = false;
            yield return Routine.Combine(
                Tween.Float(m_ColorGroup.GetAlpha(), 0, m_ColorGroup.SetAlpha, 0.25f),
                m_ScaleTransform.ScaleTo(0.8f, 0.25f).Ease(Curve.CubeIn).ForceOnCancel()
            );
            m_RotateRoutine.Stop();
            if (m_KillQueued)
            {
                m_Pool.Free(this);
            }
        }

        public void Kill(bool inbInstant)
        {
            m_ScaleTransform.SetParent(m_DefaultParent, true);

            if (inbInstant || (!m_Showing && !m_ShowRoutine))
            {
                m_Pool.Free(this);
                return;
            }

            m_KillQueued = true;
            Hide();
        }

        private void Rotate()
        {
            m_RotateTransform.SetRotation(m_RotateTransform.localEulerAngles.z + 10f * Routine.DeltaTime, Axis.Z, Space.Self);
        }

        #region IPooledObject

        void IPooledObject<BehaviorCaptureLocation>.OnConstruct(IPool<BehaviorCaptureLocation> inPool)
        {
            m_Pool = inPool;
            m_DefaultParent = ((IPrefabPool<BehaviorCaptureLocation>) inPool).DefaultSpawnTransform;
        }

        void IPooledObject<BehaviorCaptureLocation>.OnDestruct()
        {
            m_Pool = null;
        }

        void IPooledObject<BehaviorCaptureLocation>.OnAlloc()
        {
            m_Alive = true;
            m_Showing = false;
            m_KillQueued = false;
            m_ColorGroup.BlocksRaycasts = false;
            m_ColorGroup.SetAlpha(0);
            m_ScaleTransform.SetScale(0.8f);
            m_RotateTransform.SetRotation(RNG.Instance.NextFloat(360f), Axis.Z, Space.Self);
        }

        void IPooledObject<BehaviorCaptureLocation>.OnFree()
        {
            m_Alive = false;
            m_ShowRoutine.Stop();
            m_RotateRoutine.Stop();
            BehaviorId = StringHash32.Null;
        }

        #endregion // IPooledObject
    }
}