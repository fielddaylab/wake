using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aqua.WorldMap
{
    public class StationButton : MonoBehaviour, IKeyValuePair<StringHash32, StationButton>
    {
        #region Inspector

        [SerializeField] private PointerListener m_Input = null;
        [SerializeField] private SpriteRenderer m_Icon = null;
        [SerializeField] private CursorInteractionHint m_CursorHint = null;
        [SerializeField] private Sprite m_CurrentSprite = null;
        [SerializeField] private Sprite m_OpenSprite = null;
        [Space]
        [SerializeField] private SerializedHash32 m_StationId = null;
        [SerializeField] private StationLabel m_Label = null;

        #endregion // Inspector

        #region IKeyValue

        StringHash32 IKeyValuePair<StringHash32, StationButton>.Key { get { return m_StationId; } }

        StationButton IKeyValuePair<StringHash32, StationButton>.Value { get { return this; } }

        #endregion // IKeyValue

        public StringHash32 StationId() { return m_StationId; }

        public void Hide()
        {
            gameObject.SetActive(false);
            m_Label.Hide();
        }

        public void Show(MapDesc inMap, bool inbCurrent)
        {
            m_Label.Show(inMap);
            
            m_CursorHint.TooltipId = inMap.LabelId();
            m_Icon.sprite = inbCurrent ? m_CurrentSprite : m_OpenSprite;

            m_Input.onClick.AddListener(OnClick);
        }

        private void OnClick(PointerEventData unused)
        {
            Services.Events.Dispatch(WorldMapCtrl.Event_RequestChangeStation, m_StationId.Hash());
        }
    }
}