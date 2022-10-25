using Aqua;
using UnityEngine;
using BeauUtil;
using System.Collections;
using ProtoAqua.Observation;

namespace Aqua.Dreams {
    public class DreamUISettings : MonoBehaviour, IScenePreloader, ISceneUnloadHandler {
        public bool EnableFlashlight;

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            Services.Events.Dispatch(GameEvents.HotbarHide);
            yield return 0.3f;
            if (!Save.Inventory.HasUpgrade(ItemIds.ROVScanner)) {
                Save.Inventory.AddUpgrade(ItemIds.ROVScanner);
            }
            if (EnableFlashlight) {
                Script.WriteVariable(PlayerROV.Var_LastFlashlightState, true);
                if (!Save.Inventory.HasUpgrade(ItemIds.Flashlight)) {
                    Save.Inventory.AddUpgrade(ItemIds.Flashlight);
                }
            }
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext) {
            Services.Events.Dispatch(GameEvents.HotbarShow);
        }
    }
}