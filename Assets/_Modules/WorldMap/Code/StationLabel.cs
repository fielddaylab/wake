using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap
{
    public class StationLabel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Image m_Icon = null;
    
        #endregion // Inspector

        public void Show(MapDesc inMap)
        {
            m_Label.SetText(inMap.LabelId());
            m_Icon.sprite = inMap.Icon();
            
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}