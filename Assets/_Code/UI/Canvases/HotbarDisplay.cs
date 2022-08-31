using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class HotbarDisplay : MonoBehaviour {
        [SerializeField, Required] private CanvasGroup m_CanvasGroup = null;

        private Routine m_FadeRoutine;
        private int m_HideCount;

        private void Awake() {
            Services.Events.Register(GameEvents.HotbarHide, () => {
                if (++m_HideCount == 1) {
                    m_FadeRoutine.Replace(this, m_CanvasGroup.FadeTo(0, 0.2f));
                    m_CanvasGroup.blocksRaycasts = false;
                }
            }, this)
                .Register(GameEvents.HotbarShow, () => {
                    if (--m_HideCount == 0) {
                        m_FadeRoutine.Replace(this, m_CanvasGroup.FadeTo(1, 0.2f));
                        m_CanvasGroup.blocksRaycasts = true;
                    }
                });
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }
    }
}