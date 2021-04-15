using System;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua.Scripting
{
    /// <summary>
    /// Bundled dispatch parameters.
    /// </summary>
    public struct TriggerDispatchParams
    {
        public StringHash32 Target;
        public IScriptContext Context;
        public VariantTable Table;
        public TriggerDispatchFlags Flags;
    }
}