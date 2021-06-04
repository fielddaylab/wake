using UnityEngine;
using BeauUtil;
using UnityEngine.UI;

namespace Aqua
{
    public class ModelFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private LocText m_Description = null;

        #endregion // Inspector

        public void Populate(BFModel inFact)
        {
            m_Icon.sprite = inFact.Icon();
            m_Label.SetText(inFact.NameId());
            m_Description.SetText(inFact.DescriptionId());
        }
    }
}