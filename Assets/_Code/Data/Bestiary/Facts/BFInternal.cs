using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public abstract class BFInternal : BFBase
    {
        public override BFMode Mode()
        {
            return BFMode.Internal;
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            throw new System.NotSupportedException();
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            throw new System.NotSupportedException();
        }
    }
}