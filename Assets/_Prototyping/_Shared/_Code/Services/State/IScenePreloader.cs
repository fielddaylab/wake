using System.Collections;
using BeauUtil;

namespace ProtoAqua
{
    public interface IScenePreloader
    {
        IEnumerator OnPreloadScene(SceneBinding inScene, object inContext);
    }
}