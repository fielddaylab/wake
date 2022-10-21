using Aqua;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Playables;

public class StartTimelineOnLoad : MonoBehaviour, IBaked
{
    [Required] public PlayableDirector Director;

    private void Awake() {
        Script.OnSceneLoad(PlayTimeline);
    }

    private void PlayTimeline() {
        Director.Play();
        Destroy(this);
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
