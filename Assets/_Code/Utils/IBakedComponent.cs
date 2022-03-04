namespace Aqua
{
    public interface IBakedComponent
    {
        #if UNITY_EDITOR

        void Bake();

        #endif // UNITY_EDITOR
    }
}