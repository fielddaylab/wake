using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using ScriptableBake;
using BeauRoutine.Extensions;

namespace Aqua {
    public class CreditsCtrl : MonoBehaviour, IBaked, IScenePreloader, ISceneUnloadHandler {
        public WorldCreditsElement[] WorldNodes;
        public BasePanel ScrollPanel;
        public CreditsScroll Scroll;
        public ParticleSystem SpecterParticles;
        public Transform FinalNode;
        public float FinalAscentDuration = 50;

        private Routine m_Play;

        private void Start() {
            Script.OnSceneLoad(() => m_Play.Replace(this, Play()));

            Scroll.OnCompleted = OnScrollCompleted;
        }

        private IEnumerator Play() {
            SpecterParticles.Play();
            yield return 3;

            for(int i = 0; i < WorldNodes.Length; i++) {
                yield return PlayNode(WorldNodes[i]);
            }

            yield return 0.1f;

            ScrollPanel.Show(2);
            Services.Camera.MoveToPosition(FinalNode.position, null, null, FinalAscentDuration, Curve.SineInOut);
        }

        private void OnScrollCompleted() {
            ScrollPanel.InstantHide();
            Services.Script.TriggerResponse("CreditsOver");
        }

        private IEnumerator PlayNode(WorldCreditsElement t) {
            t.FadeRoutine.Replace(t, Tween.Float(0, 1, t.Fade.SetAlpha, 1).DelayBy(t.MoveTime - t.FadeInOffset));

            yield return Services.Camera.MoveToPosition(t.transform.position, null, null, t.MoveTime, Curve.SineInOut);
            yield return t.LingerTime;

            t.FadeRoutine.Replace(t, Tween.Float(1, 0, t.Fade.SetAlpha, 1));
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

        public bool Bake(BakeFlags flags, BakeContext context) {
            foreach(var node in WorldNodes) {
                node.Fade.SetAlpha(0);
            }
            return true;
        }

#endif // UNITY_EDITOR
    }
}