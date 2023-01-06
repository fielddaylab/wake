using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using ScriptableBake;

namespace Aqua {
    public class CreditsCtrl : MonoBehaviour, IBaked, IScenePreloader, ISceneUnloadHandler {
        public Transform[] CreditsNodes;

        private Routine m_Play;

        private void Start() {
            Script.OnSceneLoad(() => m_Play.Replace(this, Play()));
        }

        private IEnumerator Play() {
            yield return 3;

            for(int i = 0; i < CreditsNodes.Length; i++) {
                if (i == CreditsNodes.Length - 1) {
                    yield return 1;
                    Services.Script.TriggerResponse("CreditsOver");
                }
                yield return PlayNode(CreditsNodes[i]);
            }
        }

        private IEnumerator PlayNode(Transform t) {
            ColorGroup color;
            if (t.TryGetComponent(out color)) {
                Routine.Start(color, Tween.Float(0, 1, color.SetAlpha, 1));
            }

            yield return Services.Camera.MoveToPosition(t.position, null, null, 1.5f, Curve.SineInOut);
            yield return 2;

            if (color) {
                Routine.Start(color, Tween.Float(1, 0, color.SetAlpha, 1));
            }
        }

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            Save.Map.SetCurrentStationId("KelpStation");
            Services.Events.Dispatch(GameEvents.HotbarHide);
            yield break;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)  {
            Services.Events.Dispatch(GameEvents.HotbarShow);
        }

        #if UNITY_EDITOR

        public int Order { get { return 0; } }

        public bool Bake(BakeFlags flags, BakeContext context)
        {
            foreach(var node in CreditsNodes) {
                ColorGroup color;
                if (node.TryGetComponent(out color)) {
                    color.SetAlpha(0);
                }
            }
            return true;
        }

#endif // UNITY_EDITOR
    }
}