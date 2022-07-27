using System;
using BeauUtil.Blocks;
using System.Collections.Generic;
using UnityEngine.Scripting;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    internal class TriggerNodeData
    {
        public TriggerPriority TriggerPriority;

        public int Score;

        public PersistenceLevel OnceLevel = PersistenceLevel.Untracked;
        public int RepeatDuration = 0;
    }
}