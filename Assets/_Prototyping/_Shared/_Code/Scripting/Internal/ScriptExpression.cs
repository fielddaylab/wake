using BeauUtil.Variants;
using Leaf;
using Leaf.Runtime;

namespace ProtoAqua.Scripting
{
    public class ScriptExpression : ILeafExpression<ScriptNode>
    {
        private readonly string m_ExpressionString;

        public ScriptExpression(string inString)
        {
            m_ExpressionString = inString;
        }

        public Variant Evaluate(LeafThreadState<ScriptNode> inThreadState, ILeafPlugin<ScriptNode> inPlugin)
        {
            ScriptThread thread = (ScriptThread) inThreadState;
            return thread.Resolver.TryEvaluate(thread.Context, m_ExpressionString);
        }
    }
}