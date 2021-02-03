using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using BeauRoutine;
using BeauUtil;
using Aqua;
using ProtoCP;

namespace ProtoAqua.Map {

    public class NavigationStation : MonoBehaviour
    {
        [SerializeField] private string stationId = null;
        [SerializeField] private Transform m_ShipMount = null;
        [SerializeField] private PointerListener m_ClickZone = null;

        private Routine fadeRoutine;

        private void Awake()
        {
            m_ClickZone.onClick.AddListener(OnClick);
        }

        public StringHash32 Id() { return stationId; }
        public Transform Mount() { return m_ShipMount; }

        private void OnClick(PointerEventData eventData)
        {
            Services.State.FindManager<StationManager>().SetStation(stationId, m_ShipMount);
        }
    }
}