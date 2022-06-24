using System;
using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;

namespace Aqua
{
    public class LocNode : IDataBlock
    {
        public StringHash32 Id;
        [BlockContent, UnityEngine.Scripting.Preserve] public string Content = string.Empty;
    }
}