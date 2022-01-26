using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class StationLabel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_Transform = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private GameObject m_NewStationBadge = null;
        [SerializeField] private GameObject m_ActiveBadge = null;
        [SerializeField] private TMP_Text m_AvailableJobCount = null;
        [SerializeField] private TMP_Text m_InProgressJobCount = null;
        [SerializeField] private TMP_Text m_CompletedJobCount = null;
    
        #endregion // Inspector

        public void Show(MapDesc inMap, bool inbCurrent, bool inbSeen, JobProgressSummary inSummary)
        {
            m_Label.SetText(inMap.LabelId());
            m_Icon.sprite = inMap.Icon();
            
            m_ActiveBadge.SetActive(inbCurrent);
            m_NewStationBadge.SetActive(!inbSeen);

            m_AvailableJobCount.SetText(LookupTables.ToStringLookup(inSummary.Available));
            m_InProgressJobCount.SetText(LookupTables.ToStringLookup(inSummary.InProgress));
            m_InProgressJobCount.transform.parent.gameObject.SetActive(inSummary.InProgress > 0);

            m_CompletedJobCount.SetText(LookupTables.ToStringLookup(inSummary.Completed));
            m_CompletedJobCount.transform.parent.gameObject.SetActive(inSummary.Completed > 0);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}