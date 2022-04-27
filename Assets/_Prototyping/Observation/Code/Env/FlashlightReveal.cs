using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation {

    public class FlashlightReveal : MonoBehaviour {
        [SerializeField, Required] private FlashlightRegion m_Region = null;
        [SerializeField, Required] private ColorGroup m_Color = null;
        [SerializeField] private Color m_LitColor = Color.white;
        [SerializeField] private Color m_UnlitColor = Color.black.WithAlpha(0.1f);

        private Routine m_ColorFadeRoutine;

        private void Awake() {
            m_Region.OnLit += (f) => {
                m_ColorFadeRoutine.Replace(this, Tween.Color(m_Color.Color, m_LitColor, m_Color.SetColor, 0.5f));
            };
            m_Region.OnUnlit += (f) => {
                m_ColorFadeRoutine.Replace(this, Tween.Color(m_Color.Color, m_UnlitColor, m_Color.SetColor, 0.5f));
            };
            m_Color.SetColor(m_UnlitColor);
        }
    }
}