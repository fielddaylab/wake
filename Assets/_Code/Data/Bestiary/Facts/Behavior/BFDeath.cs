using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Death Rate")]
    public class BFDeath : BFBehavior
    {
        #region Inspector

        [Header("Death Rate")]
        [Range(0, 1)] public float Proportion = 0;

        #endregion // Inspector

        private BFDeath() : base(BFTypeId.Death) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Death, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.HasRate, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Death, null, null, null, null, null);
            BFType.DefineEditor(BFTypeId.Death, null, BFMode.Internal);
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Bake(BakeFlags flags, BakeContext context)
        {
            return false;
        }

        #endif // UNITY_EDITOR
    }
}