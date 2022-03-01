using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Death Rate")]
    public class BFDeath : BFBehavior
    {
        #region Inspector

        [Header("Death Rate")]
        [Range(0, 1)] public float Proportion = 0;
        [SerializeField, HideInInspector] private QualCompare m_Relative;

        #endregion // Inspector

        private BFDeath() : base(BFTypeId.Death) { }

        #region Behavior

        static public readonly TextId DeathVerb = "words.death";
        static private readonly TextId DeathSentence = "factFormat.death";
        static private readonly TextId DeathSentenceStressed = "factFormat.death.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Death, BFShapeId.Behavior, BFFlags.IsBehavior, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Death, null, null, null, null, null);
            BFType.DefineEditor(BFTypeId.Death, null, BFMode.Internal);
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        public override bool Optimize()
        {
            if (OnlyWhenStressed)
            {
                var pair = FindPairedFact<BFDeath>();
                if (pair != null)
                {
                    float compare = Proportion - pair.Proportion;
                    return Ref.Replace(ref m_Relative, MapDescriptor(compare, QualCompare.Slower, QualCompare.Faster, QualCompare.SameRate));
                }
            }

            return Ref.Replace(ref m_Relative, QualCompare.Null);
        }

        #endif // UNITY_EDITOR
    }
}