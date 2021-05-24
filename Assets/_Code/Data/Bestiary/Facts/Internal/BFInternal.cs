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

        public override string GenerateSentence()
        {
            throw new System.NotSupportedException();
        }
    }
}