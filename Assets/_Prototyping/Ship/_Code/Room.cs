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
        [SerializeField] bool updateSceneToLoad = false;


        private Routine fadeRoutine;

        public void OnPointerClick(PointerEventData eventData)
        {
            fadeRoutine.Replace(this, FadeRoutine());
        }


        private void MoveCameraToRoom()
        {
            m_MainCamera.transform.position = new Vector3(m_NewCameraTransform.position.x, m_NewCameraTransform.position.y, m_MainCamera.transform.position.z);
        }

        private IEnumerator ChangeScene()
        {
            using (var fader = Services.UI.WorldFaders.AllocFader())
            {
                yield return fader.Object.Show(Color.white, 0.25f);
                if (updateSceneToLoad)
                {
                    m_SceneToLoad = Services.Data.Profile.Map.getStationId();
                }
                yield return Services.State.LoadScene(m_SceneToLoad, null, SceneLoadFlags.NoLoadingScreen);
                yield return fader.Object.Hide(0.25f, false);
            }
        }

        private IEnumerator FadeRoutine()
        {
            if (m_NewCameraTransform != null)
            {
                yield return Services.UI.WorldFaders.FadeTransition(Color.white, 0.25f, .5f, MoveCameraToRoom);
            }
            else if (m_SceneToLoad != null)
            {
                yield return Routine.Start(ChangeScene());
            }
            else
            {
                Debug.Log("New Camera Transform or Scene not defined");
            }
        }
    }
}

