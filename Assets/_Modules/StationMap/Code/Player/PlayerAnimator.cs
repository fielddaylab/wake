using System.Collections;
using UnityEngine;
using BeauRoutine;
using BeauUtil;
using Aqua.Cameras;

namespace Aqua.StationMap
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Transform m_BoatRenderer = null;
        [SerializeField] private Color m_DiveColor = Color.black;

        private Routine m_DiveRoutine;

        private void OnEnable()
        {
            Services.Events.Register(NavigationUI.Event_Dive, StartDiving, this);
        }

        private void OnDisable()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void StartDiving()
        {
            m_DiveRoutine.Replace(this, DiveRoutine());
        }

        private IEnumerator DiveRoutine()
        {
            ColorGroup group = m_BoatRenderer.GetComponent<ColorGroup>();
            CameraTarget target = m_BoatRenderer.GetComponentInParent<CameraTarget>();

            yield return Routine.Combine(
                m_BoatRenderer.MoveTo(5, 3, Axis.Z, Space.Self).Ease(Curve.QuadIn),
                Tween.Color(group.Color, m_DiveColor, group.SetColor, 3).Ease(Curve.QuadIn),
                Tween.Float(target.Zoom, 3, (f) => { target.Zoom = f; target.PushChanges(); }, 3).Ease(Curve.QuadIn).DelayBy(0.5f)
            );
        }
    }
}