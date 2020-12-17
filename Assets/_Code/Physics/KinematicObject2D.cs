using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class KinematicObject2D : MonoBehaviour
    {
        public delegate bool MoveDelegate(Vector2 inOffset, ref KinematicPropertyBlock2D ioProperties, float inDeltaTime);

        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public KinematicPropertyBlock2D Properties;

        #endregion // Inspector

        [NonSerialized] private MoveDelegate m_MoveCallback;
        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private Routine m_TickRoutine;

        public MoveDelegate MoveCallback
        {
            get { return m_MoveCallback; }
            set { m_MoveCallback = value; }
        }

        private void OnEnable()
        {
            this.CacheComponent(ref m_Transform);
            m_TickRoutine = Routine.StartLoop(this, Tick).SetPhase(RoutinePhase.FixedUpdate).SetPriority(1000);
        }

        private void OnDisable()
        {
            m_TickRoutine.Stop();
        }

        private void Tick()
        {
            float deltaTime = Routine.DeltaTime;
            if (deltaTime <= 0)
                return;

            Vector2 offset = KinematicMath2D.Integrate(ref Properties, deltaTime);
            bool bMoveDirect = m_MoveCallback == null;
            if (!bMoveDirect)
            {
                bMoveDirect = m_MoveCallback(offset, ref Properties, deltaTime);
            }
            
            if (!bMoveDirect)
            {
                Vector3 pos = m_Transform.localPosition;
                pos.x += offset.x;
                pos.y += offset.y;
                m_Transform.localPosition = pos;
            }
        }
    }
}