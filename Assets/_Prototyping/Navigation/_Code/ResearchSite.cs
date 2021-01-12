using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;

namespace ProtoAqua.Navigation
{
    public class ResearchSite : MonoBehaviour
    {
        [SerializeField, Required] private string m_siteId;
        [SerializeField, Required] private string m_siteLabel;

        public string SiteId { get { return m_siteId; } }

        private void OnTriggerEnter2D(Collider2D other) {
            Services.UI.FindPanel<UIController>().Display(m_siteLabel, m_siteId);
            // Debug.Log("Show Button");
        }

        private void OnTriggerExit2D(Collider2D other) {
            Services.UI.FindPanel<UIController>().Hide();
            // Debug.Log("Hide Button");
        }
    }

}
