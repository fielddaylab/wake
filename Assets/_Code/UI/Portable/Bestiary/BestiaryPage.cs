using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class BestiaryPage : MonoBehaviour
    {
        #region Inspector

        [Header("Header")]
        public AppearAnimSet HeaderAnim;
        public LocText ScientificName;
        public LocText CommonName;
        public LocText Description;
        public StreamedImageSetDisplay Sketch;
        public Button SelectButton;

        [Header("No Facts")]
        public GameObject NoFacts;
        
        [Header("Has Facts")]
        public GameObject HasFacts;
        [Required] public CanvasGroup FactGroup;
        [Required] public FactPools FactPools;
        [Required] public LayoutGroup FactLayout;
        [Required] public ScrollRect FactScroll;

        [Required] public LayoutGroupFix[] LayoutFixes;

        #endregion // Inspector
    }
}