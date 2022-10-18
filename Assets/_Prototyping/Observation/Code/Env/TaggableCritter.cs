using System;
using Aqua;
using Aqua.Entity;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class TaggableCritter : ToolRegion, IEditorOnlyData {

        #region Inspector

        [Header("Taggable")]
        [FilterBestiaryId(BestiaryDescCategory.Critter)] public SerializedHash32 CritterId;

        #endregion // Inspector

        [NonSerialized] public bool WasTagged;

        private void OnEnable() {
            ScanSystem.Find<TaggingSystem>().Register(this);
        }

        private void OnDisable() {
            if (!Services.Valid || !Collider) {
                return;
            }
            ScanSystem.Find<TaggingSystem>()?.Deregister(this);
        }

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref CritterId);
        }

        protected override bool CustomBake() {
            if (!Assets.Has(CritterId)) {
                Log.Error("[TaggableCritter] Critter Id not found: '{0}'", CritterId);
            }

            return false;
        }

        #endif // UNITY_EDITOR
    }
}