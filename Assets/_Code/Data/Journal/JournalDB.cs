using UnityEngine;

namespace Aqua.Journal {
    [CreateAssetMenu(menuName = "Aqualab System/Journal Database")]
    public class JournalDB : DBObjectCollection<JournalDesc> {
        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(JournalDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}