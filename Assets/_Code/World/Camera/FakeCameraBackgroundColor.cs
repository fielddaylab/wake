using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Cameras {
    public sealed class FakeCameraBackgroundColor : MonoBehaviour, IBaked {
        static public Color Current { get; private set; }

        [SerializeField] private Color m_Color = ColorBank.Aqua;

        private void OnEnable() {
            Current = m_Color;
        }

#if UNITY_EDITOR

        int IBaked.Order => -1000;

        private void AutoSetColor() {
            if (gameObject.TryGetComponent(out ColorGroup group)) {
                m_Color = group.Color;
                if (isActiveAndEnabled) {
                    Current = m_Color;
                }
            }
        }

        private void Reset() {
            AutoSetColor();
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            AutoSetColor();
            return true;
        }

#endif // UNITY_EDITOR
    }
}