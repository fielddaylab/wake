using BeauUtil.Debugger;
using BeauUtil.Variants;
using Leaf;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    internal class ScriptExpression : ILeafExpression<ScriptNode>
    {
        private readonly string m_ExpressionString;

        public ScriptExpression(string inString)
        {
            m_ExpressionString = inString;
        }

        public Variant Evaluate(LeafThreadState<ScriptNode> inThreadState, ILeafPlugin<ScriptNode> inPlugin)
        {
            var thread = (ScriptThread) inThreadState;
            var resolver = thread.Resolver ?? Services.Data.VariableResolver;
            return resolver.TryEvaluate(thread.Context, m_ExpressionString);
        }

        public void Set(LeafThreadState<ScriptNode> inThreadState, ILeafPlugin<ScriptNode> inPlugin)
        {
            var thread = (ScriptThread) inThreadState;
            var resolver = thread.Resolver ?? Services.Data.VariableResolver;
            if (!resolver.TryModify(thread.Context, m_ExpressionString))
            {
                Log.Error("[ScriptExpression] Failed to set variables from string '{0}'", m_ExpressionString);
            }
        }
    }
}