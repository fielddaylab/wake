using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using BeauUtil;
using UnityEngine.EventSystems;
using Aqua;

namespace ProtoAqua.Ship
{
    public class Room : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Camera m_MainCamera = null;
        [SerializeField] Transform m_NewCameraTransform = null;
        [SerializeField] string m_SceneToLoad = null;


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
            fadeRoutine.Replace(this, FadeRoutine());
        }


        private void MoveCameraToRoom() {
            m_MainCamera.transform.position = new Vector3(m_NewCameraTransform.position.x, m_NewCameraTransform.position.y, m_MainCamera.transform.position.z);
        }

        private void ChangeScene() {
            SceneManager.LoadScene(m_SceneToLoad);
        }

        private IEnumerator FadeRoutine() {
            if(m_NewCameraTransform != null) {
                yield return Services.UI.WorldFaders.FadeTransition(Color.white, 1, .5f, MoveCameraToRoom);
            }
            else if(m_SceneToLoad != null) {
                yield return Services.UI.WorldFaders.FadeTransition(Color.white, 1, .2f, ChangeScene);
            } else {
                Debug.Log("New Camera Transform or Scene not defined");
            }
        }

      
    }
}

