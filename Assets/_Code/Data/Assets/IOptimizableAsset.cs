namespace Aqua
{
    public interface IOptimizableAsset
    {
        #if UNITY_EDITOR

        int Order { get; }
        bool Optimize();

        #endif // UNITY_EDITOR
    }
}