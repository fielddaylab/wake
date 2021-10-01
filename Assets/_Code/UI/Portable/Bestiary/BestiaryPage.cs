using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class BestiaryPage : MonoBehaviour
    {
        #region Inspector

        [SerializeField] public LocText ScientificName = null;
        [SerializeField] public LocText CommonName = null;
        [SerializeField] public Image Sketch = null;
        [SerializeField] public Button SelectButton = null;

        [SerializeField] public RectTransform NoFacts = null;
        [SerializeField] public RectTransform HasFacts = null;
        [SerializeField, Required] public LayoutGroup FactLayout = null;

        [SerializeField, Required] public LayoutGroupFix[] LayoutFixes = null;

        #endregion // Inspector
    }
}