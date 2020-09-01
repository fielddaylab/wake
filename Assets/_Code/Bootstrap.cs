using System.Collections;
using System.Globalization;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Input.multiTouchEnabled = false;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Services.AutoSetup(gameObject);
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            
            foreach(var scene in SceneHelper.FindScenes(SceneCategories.AllLoaded))
            {
                scene.OnLoaded();
            }
        }
    }
}