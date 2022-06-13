using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauPools;
using System;

namespace Aqua.Modeling {
    public sealed class SimFilterLine : MonoBehaviour, IPoolAllocHandler {
        [Serializable] public class Pool : SerializablePool<SimFilterLine> { }
        public delegate void ToggleHandler(SimFilterLine line, bool active);

        public HeaderType IsHeader;
        public Toggle Toggle;
        public LocText Label;
        public Image Image;

        [NonSerialized] public StringHash32 OrganismId;
        [NonSerialized] public WaterPropertyId PropertyId;

        public ToggleHandler OnToggled;

        private void Awake() {
            Toggle.onValueChanged.AddListener(OnCheckboxUpdate);
        }

        public void Sync(bool inbValue) {
            Toggle.SetIsOnWithoutNotify(inbValue);
            Toggle.targetGraphic.color = inbValue ? AQColors.ContentBlue : AQColors.Teal;
        }

        private void OnCheckboxUpdate(bool inbSetting)  {
            Toggle.targetGraphic.color = inbSetting ? AQColors.ContentBlue : AQColors.Teal;
            OnToggled?.Invoke(this, inbSetting);
        }

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            OrganismId = default;
            PropertyId = default;
        }

        public enum HeaderType {
            NotHeader,
            Organism,
            WaterChem
        }
    }
}