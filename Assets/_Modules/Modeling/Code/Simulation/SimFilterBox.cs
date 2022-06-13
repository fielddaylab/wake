using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using ScriptableBake;

namespace Aqua.Modeling {
    public sealed class SimFilterBox : MonoBehaviour {
        public Toggle Toggle;
        public Button Close;
        public SimFilterLine OrganismSection;
        public SimFilterLine WaterChemSection;
    }
}