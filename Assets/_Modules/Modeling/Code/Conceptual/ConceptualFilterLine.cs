using UnityEngine;
using UnityEngine.UI;
using BeauUtil;

namespace Aqua.Modeling {
    public sealed class ConceptualFilterLine : MonoBehaviour {
        [AutoEnum] public WorldFilterMask Mask;
        public bool IsHeader;
        public Toggle Toggle;

        private void Awake() {
            Toggle.onValueChanged.AddListener(OnCheckboxUpdate);
        }

        public void Sync(bool inbValue) {
            Toggle.SetIsOnWithoutNotify(inbValue);
            Toggle.targetGraphic.color = inbValue ? AQColors.ContentBlue : AQColors.Teal;
        }

        private void OnCheckboxUpdate(bool inbSetting)  {
            Toggle.targetGraphic.color = inbSetting ? AQColors.ContentBlue : AQColors.Teal;
        }
    }
}