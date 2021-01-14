using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua;
using UnityEngine.UI;
using BeauUtil;

namespace ProtoAqua.Navigation
{
    public class UIController : SharedPanel
    {
        static public readonly StringHash32 Event_Dive = "nav:dive";


        [SerializeField] private Button m_DiveButton = null;
        [SerializeField] private LocText m_DiveLabel = null;
        
        public string currentSiteId {get; set;} 

        private string sceneToLoad = "";

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            m_DiveButton.onClick.AddListener(beginDiveScene);
        }

        private void beginDiveScene()
        {
            Hide();
            Routine.Start(FadeRoutine());
        }

        public void Display(string inLabel, string inScene)
        {
            sceneToLoad = inScene;
            m_DiveLabel.SetText(inLabel);
            Show();
        }

        private IEnumerator FadeRoutine()
        {
            Services.UI.ShowLetterbox();
            Services.Data.SetVariable(GameVars.DiveSite, sceneToLoad);
            Services.Events.Dispatch(Event_Dive, sceneToLoad);
            yield return 2;
            StateUtil.LoadSceneWithWipe(sceneToLoad);
            yield return 0.3f;
            Services.UI.HideLetterbox();
        }
    }
}
