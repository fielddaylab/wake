namespace Aqua
{
    public interface IBakedAsset
    {
        #if UNITY_EDITOR

        int Order { get; }
        bool Bake();

        #endif // UNITY_EDITOR
    }
}