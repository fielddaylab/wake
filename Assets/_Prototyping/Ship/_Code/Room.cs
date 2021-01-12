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
            Services.State.Camera.transform.SetPosition(new Vector3(m_NewCameraTransform.position.x, m_NewCameraTransform.position.y), Axis.XY, Space.World);
        }

        private IEnumerator FadeRoutine()
        {
            if (m_NewCameraTransform != null)
            {
                yield return Services.UI.WorldFaders.FadeTransition(Color.white, 0.5f, 0.25f, MoveCameraToRoom);
            }
            else if (m_SceneToLoad != null)
            {
                if (updateSceneToLoad)
                {
                    m_SceneToLoad = Services.Data.Profile.Map.getStationId();
                }
                yield return StateUtil.LoadSceneWithFader(m_SceneToLoad);
            }
            else
            {
                Debug.Log("New Camera Transform or Scene not defined");
            }
        }
    }
}

