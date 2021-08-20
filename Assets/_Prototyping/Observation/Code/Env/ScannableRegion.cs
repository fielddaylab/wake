using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScannableRegion : ScriptComponent, ISceneOptimizable
    {
        #region Inspector

        public SerializedHash32 ScanId;
        [Required] public Collider2D Collider;
        [Space]
        public ToolView ToolView;
        [SerializeField, HideInInspector] public bool InsideToolView;

        #endregion // Inspector

        public ScanData ScanData;
        [NonSerialized] public ScanIcon CurrentIcon;
        [NonSerialized] public bool InRange;

        private void OnEnable()
        {
            ScanSystem.Find<ScanSystem>().Register(this);
        }

        private void OnDisable()
        {
            ScanSystem.Find<ScanSystem>()?.Deregister(this);
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            InsideToolView = this.GetComponentInParent<ToolView>(true);
        }

        #endif // UNITY_EDITOR
    }
}