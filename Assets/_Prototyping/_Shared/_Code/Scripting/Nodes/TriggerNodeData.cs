using System;
using BeauUtil.Blocks;
using System.Collections.Generic;
using UnityEngine.Scripting;
using BeauUtil;
using BeauUtil.Variants;

namespace ProtoAqua.Scripting
{
    public class TriggerNodeData
    {
        public StringHash TriggerId;
        public StringHash TargetId;

        public VariantComparison[] Conditions;
        public int Score;

        public PersistenceLevel OnceLevel = PersistenceLevel.Untracked;
        public int RepeatDuration = 0;
    }
}