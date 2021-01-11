using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua;

namespace ProtoAqua.Navigation
{
    public class UIController : MonoBehaviour
    {

        [SerializeField] GameObject m_DiveUI = null;
        
        public string currentSiteId {get; set;} 
        private Routine fadeRoutine;

        private string sceneToLoad = "";

        // Start is called before the first frame update
        void Start()
        {
            m_DiveUI.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void returnToShip() {
            sceneToLoad = "Ship";
            fadeRoutine.Replace(this, FadeRoutine());
        }
        
        public void displayDiveUI() {
            m_DiveUI.SetActive(true);
        }

        public void hideDiveUI() {
            m_DiveUI.SetActive(false);
        }

        public void beginDiveScene() {
            sceneToLoad = "SeaSceneTest"; //CurrentSiteId
            fadeRoutine.Replace(this, FadeRoutine());
        }
        private void ChangeScene()
        {
            SceneManager.LoadScene(sceneToLoad);
        }

        private IEnumerator FadeRoutine()
        {
            yield return Services.UI.WorldFaders.FadeTransition(Color.white, 1, .2f, ChangeScene);
        }

        
    }
}
