using System;
using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua.Journal {
    [CreateAssetMenu(menuName = "Aqualab Content/Journal Entry")]
    public class JournalDesc : DBObject, IEditorOnlyData {
        #region Inspector

        [SerializeField] private SerializedHash32 m_OverridePrefabId = null;
        [SerializeField] private JournalCategoryMask m_Category = JournalCategoryMask.Personal;
        [SerializeField] private bool m_IsDefault = false;

        #endregion // Inspector

        public StringHash32 PrefabId() { return m_OverridePrefabId.IsEmpty ? Id() : m_OverridePrefabId.Hash(); }
        public JournalCategoryMask Category() { return m_Category; }
        public bool IsDefault() { return m_IsDefault; }

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref m_OverridePrefabId);
        }

        #endif // UNITY_EDITOR
    }

    public enum JournalCategoryMask : uint {
        Kelp = 0x01,
        Coral = 0x02,
        Bayou = 0x04,
        Arctic = 0x08,
        Depths = 0x10,
        Personal = 0x20
    }

    public class JournalIdAttribute : DBObjectIdAttribute {
        public JournalIdAttribute() : base(typeof(JournalDesc)) {
        }

        public override string Name(DBObject inObject) {
            JournalDesc desc = (JournalDesc) inObject;
            return string.Format("{0}/{1}", desc.Category().ToString(), desc.name);
        }
    }
}