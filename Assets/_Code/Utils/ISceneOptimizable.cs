namespace Aqua
{
    public interface ISceneOptimizable
    {
        #if UNITY_EDITOR

        void Optimize();

        #endif // UNITY_EDITOR
    }
}