using BeauUtil;
using UnityEngine;

namespace Aqua.Animation
{
    [DisallowMultipleComponent]
    public class AmbientTransform : MonoBehaviour
    {
        #region State

        /// <summary>
        /// Transform state.
        /// </summary>
        public AmbientTransformState TransformState;

        #endregion // State

        #region Inspector

        /// <summary>
        /// Transform to animate.
        /// </summary>
        [Required] public Transform Transform;

        /// <summary>
        /// What space to update transform within.
        /// </summary>
        public Space TransformSpace = Space.Self;

        /// <summary>
        /// Position animation.
        /// </summary>
        public AmbientVec3PropertyConfig PositionAnimation;

        /// <summary>
        /// Scale animation.
        /// </summary>
        public AmbientVec3PropertyConfig ScaleAnimation;

        /// <summary>
        /// Rotation animation.
        /// </summary>
        public AmbientVec3PropertyConfig RotationAnimation;

        #endregion // Inspector

        private void OnEnable()
        {
            Services.Animation.AmbientTransforms.Register(this);
        }

        private void OnDisable()
        {
            Services.Animation.AmbientTransforms?.Deregister(this);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            Transform = transform;
        }

        #endif // UNITY_EDITOR
    }
}