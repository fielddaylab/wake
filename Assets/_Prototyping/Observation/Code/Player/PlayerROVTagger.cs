using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVTagger : MonoBehaviour, PlayerROV.ITool
    {
        #region Inspector

        [SerializeField] private CircleCollider2D m_RangeCollider = null;
        [SerializeField] private Transform m_RangeVisuals = null;
        [SerializeField] private ParticleSystem[] m_RangeParticleSystems = null;
        [SerializeField] private float m_MaxRange = 8;

        #endregion // Inspector

        [NonSerialized] private bool m_On = false;
        [NonSerialized] private float m_CurrentRange;
        [NonSerialized] private Routine m_EnableRoutine;

        #region Unity Events

        private void Start()
        {
            SetRange(0);
            TaggingSystem.Find<TaggingSystem>().SetDetector(m_RangeCollider);
        }

        #endregion // Unity Events

        #region State

        public void Enable()
        {
            if (m_On)
                return;

            m_On = true;
            m_EnableRoutine.Replace(this, TurnOnAnim());
            Services.UI.FindPanel<TaggingUI>().Show();
        }

        public void Disable()
        {
            if (!m_On)
                return;

            m_On = false;
            m_EnableRoutine.Replace(this, TurnOffAnim());
            Services.UI?.FindPanel<TaggingUI>()?.Hide();
        }

        public bool UpdateTool(in PlayerROV.InputData inInput)
        {
            return false;
        }

        public bool HasTarget()
        {
            return false;
        }

        public Vector3? GetTargetPosition()
        {
            return null;
        }

        #endregion // State

        #region Animation

        private IEnumerator TurnOnAnim()
        {
            yield return Tween.Float(m_CurrentRange, m_MaxRange, SetRange, 0.25f).Ease(Curve.CubeOut);
        }

        private IEnumerator TurnOffAnim()
        {
            yield return Tween.Float(m_CurrentRange, 0, SetRange, 0.1f).Ease(Curve.CubeOut);
        }

        private void SetRange(float inRange)
        {
            m_CurrentRange = inRange;

            if (inRange > 0)
            {
                m_RangeCollider.gameObject.SetActive(true);
                m_RangeCollider.radius = inRange;

                m_RangeVisuals.gameObject.SetActive(true);
                m_RangeVisuals.SetScale(inRange * 2, Axis.XY);

                foreach(var ps in m_RangeParticleSystems)
                {
                    var shape = ps.shape;
                    shape.radius = inRange;

                    if (m_On && !ps.isEmitting)
                    {
                        ps.Play();
                    }
                    else if (!m_On)
                    {
                        ps.Stop();
                    }
                }
            }
            else
            {
                m_RangeCollider.gameObject.SetActive(false);
                m_RangeVisuals.gameObject.SetActive(false);
                foreach(var ps in m_RangeParticleSystems)
                {
                    ps.Stop();
                }
            }
        }

        #endregion // Animation
    }
}