using BeauUtil;

namespace Aqua
{
    // TODO: Implement renaming utility
    public interface IRenameTarget
    {
        #if UNITY_EDITOR

        bool Rename(StringHash32 inPrev, StringHash32 inNew);

        #endif // UNITY_EDITOR
    }
}