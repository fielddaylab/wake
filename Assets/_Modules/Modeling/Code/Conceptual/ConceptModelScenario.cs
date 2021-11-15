using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {

    [CreateAssetMenu(menuName = "Aqualab Content/Conceptual Model Scenario", fileName = "NewConceptualModelScenario")]
    public class ConceptModelScenario : ScriptableObject {
        #region Inspector

        [SerializeField, FilterBestiaryId(BestiaryDescCategory.Environment)] private StringHash32 m_EnvironmentId = default;

        [SerializeField, FilterBestiaryId(BestiaryDescCategory.Critter)] private StringHash32[] m_CritterIds = null;
        [SerializeField, FactId(typeof(BFBehavior))] private StringHash32[] m_BehaviorIds = null;
        [SerializeField, FactId(typeof(BFModel))] private StringHash32 m_ModelId = null;

        #endregion // Inspector

        public StringHash32 EnvironmentId() { return m_EnvironmentId; }
        public ListSlice<StringHash32> CritterIds() { return m_CritterIds; }
        public ListSlice<StringHash32> BehaviorIds() { return m_BehaviorIds; }
        public StringHash32 ModelId() { return m_ModelId; }
    }
}