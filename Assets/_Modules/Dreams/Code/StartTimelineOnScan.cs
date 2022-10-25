using System.Collections;
using System.Collections.Generic;
using Aqua.Scripting;
using BeauUtil;
using ProtoAqua.Observation;
using UnityEngine;
using UnityEngine.Playables;

namespace Aqua.Dreams {
    public class StartTimelineOnScan : MonoBehaviour, IScenePreloader {
        [Required] public PlayableDirector playableDirector;
        [Required] public ScannableRegion Scan;

        private void Awake() {
            Scan.OnScanComplete = (s) => {
                if (s != ScanResult.NoChange) {
                    PlayTimeline();
                }
            };
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            playableDirector.RebuildGraph();
            yield return null;
        }

        private void PlayTimeline() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("timelineId", playableDirector.playableAsset.name);
                table.Set("timelineDuration", (float) playableDirector.playableAsset.duration);
                Services.Script.TriggerResponse(GameTriggers.TimelineStarted, table);
            }
            playableDirector.Play();
            Scan.gameObject.SetActive(false);
        }
    }
}