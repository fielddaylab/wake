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

        public int Order { get { return FlattenHierarchy.Order - 15; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            SceneHelper.ActiveScene().Scene.ForEachComponent<ScannableRegion>(true, (scn, scan) => {
                if (scan.gameObject.name == "MicroscopeHint") {
                    return;
                }

                if (scan == null || scan.transform == null || scan.transform.parent == null) {
                    return;
                }

                var region = scan.transform.parent.GetComponent<FlashlightRegion>(); 
                
                if (!region) {
                    region = Instantiate(RegionPrefab, scan.transform.parent);
                    region.TrackTransform = scan.SafeTrackedTransform;
                    region.ColliderPosition.Source = region.TrackTransform;
                }

                region.AutoConfigure();
            });

            Baking.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}