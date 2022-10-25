using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Playables;

public class StartTimelineOnLoad : MonoBehaviour, IBaked, IScenePreloader
{
    [Required] public PlayableDirector Director;

    private void Awake() {
        Script.OnSceneLoad(PlayTimeline);
    }

    private void PlayTimeline() {
        Director.Play();
        using(var table = TempVarTable.Alloc()) {
            table.Set("timelineId", Director.playableAsset.name);
            table.Set("timelineDuration", (float) Director.playableAsset.duration);
            Services.Script.TriggerResponse(GameTriggers.TimelineStarted, table);
        }
        Destroy(this);
    }

    IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
        Director.RebuildGraph();
        yield return null;
    }

    #if UNITY_EDITOR

    int IBaked.Order { get { return 0; } }

    bool IBaked.Bake(BakeFlags flags, BakeContext context) {
        if (Director.playOnAwake) {
            Baking.SetDirty(Director);
            Director.playOnAwake = false;
            return true;
        }

        return false;
    }

    #endif // UNITY_EDITOR
}
