using BeauUtil;

namespace Aqua
{
    [LabeledEnum]
    public enum BFGraphType : byte
    {
        Flat,
        Increase,
        Decrease,
        Cycle,

        [Hidden]
        MAX
    }
}