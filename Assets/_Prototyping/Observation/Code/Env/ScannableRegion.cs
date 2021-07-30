using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;

namespace ProtoAqua.Observation
{
    public class ScannableRegion : ScriptComponent
    {
        #region Inspector

        public SerializedHash32 ScanId;
        [Required] public Collider2D Collider;

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
    }
}