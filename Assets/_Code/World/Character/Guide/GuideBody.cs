using Aqua.Scripting;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;

namespace Aqua.Character {
    public class GuideBody : ScriptComponent {
        #region Inspector

        [SerializeField] private GuideCursorFollow m_CursorFollow = null;
        [SerializeField] private ScriptEmotes m_Emotes = null;

        [Header("Bones")]
        [SerializeField] private Transform m_RootTransform = null;
        [SerializeField] private Transform m_FacePivotTransform = null;
        [SerializeField] private Transform m_LookTransform = null;

        #endregion // Inspector

        private Routine m_MoveRoutine;

        public void MoveTo(Transform target) {
            m_MoveRoutine.Replace(this, MoveGuide(m_RootTransform, target));
        }

        public void SnapTo(Transform target) {
            m_MoveRoutine.Stop();
            m_RootTransform.transform.position = target.position;
        }

        static private IEnumerator MoveGuide(Transform v1ctorCurrent, Transform target) {
            yield return v1ctorCurrent.MoveTo(target.position, 0.5f, Axis.X, Space.World).Ease(Curve.CubeOut);
        }
    }
}