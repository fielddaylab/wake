using System.Collections;
using System.Collections.Generic;
using BeauUtil;

namespace Aqua
{
    public interface ISceneSubsceneSelector
    {
        IEnumerable<string> GetAdditionalScenesNames(SceneBinding inNew, object inContext);
    }
}