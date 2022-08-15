using System.Collections;
using System.Collections.Generic;
using BeauUtil;

namespace Aqua
{
    public interface ISceneSubsceneSelector
    {
        IEnumerable<SceneImportSettings> GetAdditionalScenesNames(SceneBinding inNew, object inContext);
    }

    public struct SceneImportSettings {
        public string SceneName;
        public bool ImportLighting;

        static public implicit operator SceneImportSettings(string sceneName) {
            return new SceneImportSettings() {
                SceneName = sceneName,
                ImportLighting = false
            };
        }
    }
}