using UnityEngine;
using BeauUtil;
using UnityEngine.UI;

namespace Aqua
{
    public class ModelFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private GameObject m_ImageGroup = null;
        [SerializeField, Required] private Image m_Image = null;
        [SerializeField, Required] private LocText m_Description = null;

        #endregion // Inspector

        public void Populate(BFModel inFact)
        {
            m_ImageGroup.SetActive(inFact.Image);
            m_Image.sprite = inFact.Image;
            
            m_Description.SetText(inFact.DescriptionId);
        }
    }
}