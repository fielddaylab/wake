using BeauUtil;

namespace Aqua
{
    [LabeledEnum]
    public enum BFRateLabel : byte
    {
        // normal labels
        [Label("Amount/Tiny")]
        Tiny,
        [Label("Amount/Small")]
        Small,
        [Label("Amount/Medium")]
        Medium,
        [Label("Amount/Large")]
        Large,
        [Label("Amount/Huge")]
        Huge,

        [Label("Rate/Slowly")]
        Slowly,
        [Label("Rate/Moderate")]
        ModerateRate,
        [Label("Rate/Quickly")]
        Quickly,

        // relative labels
        [Label("Relative/Less")]
        Less,
        [Label("Relative/More")]
        More,
        [Label("Relative/Slower")]
        Slower,
        [Label("Relative/Faster")]
        Faster
    }
}