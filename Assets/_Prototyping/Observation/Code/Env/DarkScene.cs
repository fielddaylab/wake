using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    public class DarkScene : MonoBehaviour, IBaked
    {
        public FlashlightRegion RegionPrefab;

        #if UNITY_EDITOR

        public int Order { get { return FlattenHierarchy.Order - 1; } }

        public bool Bake(BakeFlags flags) {
            SceneHelper.ActiveScene().Scene.ForEachComponent<ScannableRegion>(true, (scn, scan) => {
                var existingMicroscope = scan.transform.parent.GetComponentInChildren<MicroscopeRegion>(true);
                var existingFlashlight = scan.transform.parent.GetComponentInChildren<FlashlightRegion>(true); 

                if (existingFlashlight) {
                    if (!ArrayUtils.Contains(existingFlashlight.Reveal.GameObjects, scan.gameObject)) {
                        ArrayUtils.Add(ref existingFlashlight.Reveal.GameObjects, scan.gameObject);
                    }
                    return;
                }
                FlashlightRegion region = Instantiate(RegionPrefab, scan.transform.parent);
                region.TrackTransform = scan.SafeTrackedTransform;
                region.ColliderPosition.Source = region.TrackTransform;
                ArrayUtils.Add(ref region.Reveal.GameObjects, scan.gameObject);
            });

            ScriptableBake.Bake.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}