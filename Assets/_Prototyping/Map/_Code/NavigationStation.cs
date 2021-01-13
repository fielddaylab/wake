using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using BeauRoutine;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Map {

    public class NavigationStation : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] string stationId = null;

        private Routine fadeRoutine;

        public void OnPointerClick(PointerEventData eventData)
        {
            Services.Data.Profile.Map.SetCurrentStationId(stationId);
            fadeRoutine.Replace(this, FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            yield return StateUtil.LoadPreviousSceneWithWipe();
        }

    }
}