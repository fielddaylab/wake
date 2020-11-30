using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeauRoutine;

namespace ProtoAqua.Ship
{
    public class Room : MonoBehaviour
    {
        [Header("Room Declarations")]
        [SerializeField] string m_RoomId = null;
        [SerializeField] string m_RoomText = null;
        [SerializeField] string m_SceneToLoad = null;

        [Header("Room Dependencies")]
        [SerializeField] Camera m_MainCamera = null;
        [SerializeField] TextMeshProUGUI m_TextBox = null;
        [SerializeField] CanvasGroup m_CanvasGroup = null;
        [SerializeField] Transform m_RoomTransform = null;

        private Routine fadeRoutine;

        // Start is called before the first frame update
        void Start()
        {
            //m_TextBox.text = m_RoomText;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void RoomClick()
        {
            
            //SceneManager.LoadScene(m_SceneToLoad);
            fadeRoutine.Replace(this, FadeRoutine());
            Debug.Log("clicked me");
        }
        public void OnMouseUpAsButton() { //Pointer Listener
            RoomClick();
        }

        private void MoveCameraToRoom() {
            m_MainCamera.transform.position = new Vector3(m_RoomTransform.position.x, m_RoomTransform.position.y, m_MainCamera.transform.position.z);
        }
        private IEnumerator FadeRoutine() {
            yield return m_CanvasGroup.FadeTo(1f, 1f).Ease(Curve.Smooth);
            MoveCameraToRoom();
            yield return m_CanvasGroup.FadeTo(0f, 1f).Ease(Curve.Smooth);
        }
        

    }
}

