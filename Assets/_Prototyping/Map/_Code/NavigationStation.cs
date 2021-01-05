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

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Services.Data.Profile.Map.setStationId(stationId);
             fadeRoutine.Replace(this, FadeRoutine());
            
        }
         private void ChangeScene() 
         {
            SceneManager.LoadScene("Ship");
        }

        private IEnumerator FadeRoutine() {
            yield return Services.UI.WorldFaders.FadeTransition(Color.white, 1, .2f, ChangeScene);
        }

    }
}