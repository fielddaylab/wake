using BeauUtil;
using UnityEngine;

namespace Aqua.Animation
{
    [DisallowMultipleComponent]
    public class AmbientRenderer : MonoBehaviour
    {
        #region State

        /// <summary>
        /// Renderer state.
        /// </summary>
        public AmbientColorState ColorState;

        #endregion // State

        #region Inspector

        /// <summary>
        /// Renderer to animate.
        /// </summary>
        [Required] public ColorGroup Group;

        /// <summary>
        /// Color channel.
        /// </summary>
        public ColorGroup.Channel Channel = ColorGroup.Channel.Main;

        /// <summary>
        /// Color animation.
        /// </summary>
        public AmbientColorPropertyConfig ColorAnimation;

        #endregion // Inspector

        private void OnEnable()
        {
            Services.Animation.AmbientRenderers.Register(this);
        }

        private void OnDisable()
        {
            Services.Animation.AmbientRenderers?.Deregister(this);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            Group = GetComponent<ColorGroup>();
            ColorAnimation.MinAlpha = new Fraction16(1f);
            ColorAnimation.MaxAlpha = new Fraction16(1f);
            ColorAnimation.MinColor = Group?.Color ?? Color.white;
            ColorAnimation.MaxColor = Color.white;
        }

        #endif // UNITY_EDITOR
    }
}