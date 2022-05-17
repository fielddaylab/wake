using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using ScriptableBake;

namespace Aqua.Modeling {
    public sealed class ConceptualFilterBox : MonoBehaviour, IBaked {
        public Toggle Toggle;
        public ConceptualFilterLine[] Lines;
        public WorldFilterMask RepresentedMask;
        public Button Close;

        #if UNITY_EDITOR
        int IBaked.Order { get { return 20; } }

        bool IBaked.Bake(BakeFlags flags) {
            Lines = GetComponentsInChildren<ConceptualFilterLine>(true);
            RepresentedMask = 0;
            foreach(var line in Lines) {
                RepresentedMask |= line.Mask;
            }
            return true;
        }

        #endif // UNITY_EDITOR
    }
}