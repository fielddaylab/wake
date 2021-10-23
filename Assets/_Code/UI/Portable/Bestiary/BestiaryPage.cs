using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class BestiaryPage : MonoBehaviour
    {
        #region Inspector

        public LocText ScientificName;
        public LocText CommonName;
        public LocText Description;
        public StreamedRawImage Sketch;
        public Button SelectButton;

        public GameObject NoFacts;
        
        public GameObject HasFacts;
        [Required] public FactPools FactPools;
        [Required] public LayoutGroup FactLayout;
        [Required] public ScrollRect FactScroll;

        [Required] public LayoutGroupFix[] LayoutFixes;

        #endregion // Inspector
    }
}