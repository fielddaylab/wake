using System.Collections;
using BeauUtil;

namespace Aqua
{
    public interface IScenePreloader
    {
        IEnumerator OnPreloadScene(SceneBinding inScene, object inContext);
    }
}