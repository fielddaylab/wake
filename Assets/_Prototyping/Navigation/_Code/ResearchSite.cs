using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;

namespace ProtoAqua.Navigation
{


    public class ResearchSite : MonoBehaviour
    {
        private Routine fadeRoutine;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            fadeRoutine.Replace(this, FadeRoutine());
        }

        private void ChangeScene()
        {
            SceneManager.LoadScene("SeaSceneTest");
        }

        private IEnumerator FadeRoutine()
        {
            yield return Services.UI.WorldFaders.FadeTransition(Color.white, 1, .2f, ChangeScene);
        }
    }

}
