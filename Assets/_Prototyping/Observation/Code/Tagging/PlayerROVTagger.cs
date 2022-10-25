using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;
using Aqua.Entity;
using Aqua.Character;
using AquaAudio;

namespace ProtoAqua.Observation
{
    public class PlayerROVTagger : MonoBehaviour, PlayerROV.ITool
    {
        #region Inspector

        [SerializeField] private CircleCollider2D m_RangeCollider = null;
        [SerializeField] private Transform m_RangeVisuals = null;
        [SerializeField] private ParticleSystem[] m_RangeParticleSystems = null;
        [SerializeField] private float m_MaxRange = 8;

        [Header("Hinting")]
        [SerializeField] private ParticleSystem m_HintParticleSystem = null;

        #endregion // Inspector

        [NonSerialized] private bool m_On = false;
        [NonSerialized] private float m_CurrentRange;
        [NonSerialized] private Routine m_EnableRoutine;
        [NonSerialized] private TaggingSystem m_System;
        [NonSerialized] private AudioHandle m_Loop;

        #region Unity Events

        private void Start()
        {
            SetRange(0);
            m_System = TaggingSystem.Find<TaggingSystem>();
            if (m_System != null) {
                m_System.SetDetector(m_RangeCollider);
            }
        }

        #endregion // Unity Events

        #region State

        public bool IsEnabled()
        {
            return m_On;
        }

        public void Enable(PlayerBody inBody)
        {
            if (m_On)
                return;

            m_On = true;
            m_EnableRoutine.Replace(this, TurnOnAnim());
            Services.UI.FindPanel<TaggingUI>()?.Show();
            Visual2DSystem.Activate(GameLayers.CritterTag_Mask);

            m_Loop = Services.Audio.PostEvent("ROV.Tagger.Enabled");
        }

        public void Disable()
        {
            if (!m_On)
                return;

            m_On = false;
            m_EnableRoutine.Replace(this, TurnOffAnim());
            Services.UI?.FindPanel<TaggingUI>()?.Hide();
            Visual2DSystem.Deactivate(GameLayers.CritterTag_Mask);

            if (!Services.Valid) {
                return;
            }

            m_Loop.Stop(1f);
            m_Loop.OverrideLoop(false);
            m_Loop = default;
        }

        public bool UpdateTool(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody)
        {
            return false;
        }

        public void UpdateActive(in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            Vector2 myPos = m_RangeCollider.transform.position;
            Vector2 closestPos;
            if (m_System.TryGetClosestCritterGameplayPlane(out closestPos))
            {
                Vector2 delta = closestPos - myPos;
                float distFromSurface = Math.Max(0, delta.magnitude - m_MaxRange);
                float directionDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                float arc = Mathf.Lerp(8, 30, distFromSurface / 15);
                
                var shape = m_HintParticleSystem.shape;
                Vector3 shapeRotation = shape.rotation;
                shapeRotation.z = directionDeg - arc / 2;
                shape.rotation = shapeRotation;
                shape.arc = arc;

                if (!m_HintParticleSystem.isEmitting)
                {
                    m_HintParticleSystem.Play();
                }
                else
                {
                    // HACK: Force this to update even when moving?
                    shape.enabled = false;
                    shape.enabled = true;
                }
            }
            else
            {
                m_HintParticleSystem.Stop();
            }
        }

        public bool HasTarget()
        {
            return false;
        }

        public PlayerROVAnimationFlags AnimFlags() {
            return 0;
        }

        public float MoveSpeedMultiplier() { return 1; }

        public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorld, out Vector3? outCursor) {
            outWorld = outCursor = null;
        }

        #endregion // State

        #region Animation

        private IEnumerator TurnOnAnim()
        {
            return Tween.Float(m_CurrentRange, m_MaxRange, SetRange, 0.25f).Ease(Curve.CubeOut);
        }

        private IEnumerator TurnOffAnim()
        {
            return Tween.Float(m_CurrentRange, 0, SetRange, 0.1f).Ease(Curve.CubeOut);
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

                var hintShape = m_HintParticleSystem.shape;
                hintShape.radius = inRange;
            }
            else
            {
                m_RangeCollider.gameObject.SetActive(false);
                m_RangeVisuals.gameObject.SetActive(false);
                foreach(var ps in m_RangeParticleSystems)
                {
                    ps.Stop();
                }

                m_HintParticleSystem.Stop();
            }
        }

        #endregion // Animation
    }
}