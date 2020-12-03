using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    public class Condition
    {
        public StringHash32 Id;
        public ConditionOperator Operator;
        public Variant Operand;
    }

    public enum ConditionOperator
    {
        GreaterThan,
        LessThan,
        Is,
        True,
        False
    }
}