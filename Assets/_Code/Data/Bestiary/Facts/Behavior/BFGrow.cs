using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Grow")]
    public class BFGrow : BFBehavior
    {
        #region Inspector

        [Header("Growth")]
        public uint Amount = 0;

        #endregion // Inspector

        private BFGrow() : base(BFTypeId.Grow) { }

        #region Behavior

        static public readonly TextId GrowVerb = "words.grow";
        static private readonly TextId GrowSentence = "factFormat.grow";
        static private readonly TextId GrowSentenceStressed = "factFormat.grow.stressed";

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Grow, BFShapeId.Behavior, BFFlags.IsBehavior | BFFlags.SelfTarget, BFDiscoveredFlags.All, CompareStressedPair);
            BFType.DefineMethods(BFTypeId.Grow, null, GenerateDetails, GenerateFragments, null, null);
            BFType.DefineEditor(BFTypeId.Grow, null, BFMode.Player);
        }

        static private IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFGrow fact = (BFGrow) inFact;

            if (Services.Loc.IsCurrentLanguageGendered()) {
                yield return BFFragment.CreateGenderedLocNoun(fact.Parent.CommonName(), fact.Parent.Gender());
            }
            else {
                yield return BFFragment.CreateLocNoun(fact.Parent.CommonName());
            }
            yield return BFFragment.CreateLocVerb(GrowVerb);
            yield return BFFragment.CreateAmount(BestiaryUtils.FormatMass(fact.Amount));
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFGrow fact = (BFGrow) inFact;

            BFDetails details;
            details.Header = Loc.Find(DetailsHeader);
            details.Image = fact.Parent.ImageSet();

            if (fact.OnlyWhenStressed)
            {
                details.Description = Loc.Format(GrowSentenceStressed, inFact.Parent.CommonName(), BestiaryUtils.FormatMass(fact.Amount));
            }
            else
            {
                details.Description = Loc.Format(GrowSentence, inFact.Parent.CommonName(), BestiaryUtils.FormatMass(fact.Amount));
            }

            return details;
        }

        #endregion // Behavior
    }
}