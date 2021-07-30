using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;

namespace ProtoAqua.Observation
{
    public class TaggableCritter : ScriptComponent
    {
        #region Inspector

        public SerializedHash32 CritterId;
        [Required] public Collider2D Collider;

        #endregion // Inspector

        private void OnEnable()
        {
            ScanSystem.Find<TaggingSystem>().Register(this);
        }

        private void OnDisable()
        {
            ScanSystem.Find<TaggingSystem>()?.Deregister(this);
        }
    }
}