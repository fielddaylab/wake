using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Model")]
    public class BFModel : BFBase // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateNoun(Title());
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            return Services.Loc.MaybeLocalize(Description());
        }
    }
}