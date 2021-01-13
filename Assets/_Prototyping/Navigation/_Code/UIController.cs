using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua;
using UnityEngine.UI;

namespace ProtoAqua.Navigation
{
    public class UIController : SharedPanel
    {

        [SerializeField] private Button m_DiveButton = null;
        [SerializeField] private LocText m_DiveLabel = null;
        
        public string currentSiteId {get; set;} 
        private Routine fadeRoutine;

        private string sceneToLoad = "";

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            m_DiveButton.onClick.AddListener(beginDiveScene);
        }

        private void beginDiveScene() {
            fadeRoutine.Replace(this, FadeRoutine());
        }

        public void Display(string inLabel, string inScene)
        {
            sceneToLoad = inScene;
            m_DiveLabel.SetText(inLabel);
            Show();
        }

        private IEnumerator FadeRoutine()
        {
            Services.Data.SetVariable(GameVars.DiveSite, sceneToLoad);
            yield return StateUtil.LoadSceneWithWipe(sceneToLoad);
        }
    }
}
