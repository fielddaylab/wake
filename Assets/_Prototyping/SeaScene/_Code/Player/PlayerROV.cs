using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;

namespace ProtoAqua.Observation
{
    public class PlayerROV : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private KinematicObject2D m_Kinematic = null;
        [SerializeField] private ColorGroup m_MoveArrow = null;

        [Header("Movement Params")]

        [SerializeField] private float m_TargetVectorScaleFactor = 0.5f;
        [SerializeField] private float m_TargetVectorMinDistance = 0.2f;
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;
        
        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private bool m_MoveOn;
        [NonSerialized] private AudioHandle m_EngineSound;

        private void Start()
        {
            this.CacheComponent(ref m_Transform);
            ObservationServices.Audio.PostEvent("underwater_loop");

            SetEngineState(false, true);
        }

        private void Update()
        {
            if (m_EngineSound.Exists())
            {
                m_EngineSound.SetPitch(m_Kinematic.Properties.Velocity.magnitude / m_Kinematic.Properties.MaxSpeed);
            }

            CheckInput();
        }

        private void CheckInput()
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 targetPos = ObservationServices.Camera.ScreenToWorldOnPlane(Input.mousePosition, m_Transform);
                Vector2 vector = targetPos - (Vector2) m_Transform.position;

                if (vector.sqrMagnitude > m_TargetVectorMinDistance * m_TargetVectorMinDistance)
                {
                    SetEngineState(true);
                    m_Kinematic.Properties.Velocity += vector * m_TargetVectorScaleFactor;
                }
                else
                {
                    SetEngineState(false);
                }
            }
            else
            {
                SetEngineState(false);
            }
        }

        private void SetEngineState(bool inbOn, bool inbForce = false)
        {
            if (!inbForce && m_MoveOn == inbOn)
                return;

            m_MoveOn = inbOn;
            if (inbOn)
            {
                m_EngineSound = ObservationServices.Audio.PostEvent("rov_engine_loop");
                m_EngineSound.SetVolume(0).SetVolume(1, 0.5f);

                m_Kinematic.Properties.Drag = m_DragEngineOn;
            }
            else
            {
                m_EngineSound.Stop(0.5f);

                m_Kinematic.Properties.Drag = m_DragEngineOff;
            }
        }
    }
}