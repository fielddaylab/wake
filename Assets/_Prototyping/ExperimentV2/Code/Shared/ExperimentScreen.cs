using System;
using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System.Collections;
using BeauRoutine;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ExperimentScreen : MonoBehaviour {
        public delegate void Callback(ExperimentScreen screen, ActorWorld world);
        public delegate bool ButtonPredicate(ExperimentScreen screen, ActorWorld world);

        #region Inspector

        public SerializedHash32 Id;

        [Header("Components")]
        [Required] public ExperimentHeaderUI Header;
        [Required] public CanvasGroup Group;

        [Header("Header Buttons")]
        public bool NextButton = true;
        public TextId NextButtonLabelId;
        public bool NextButtonArrow = true;
        public bool BackButton = true;

        [Header("Extra")]
        public BestiaryAddPanel Panel;
        public Button CustomButton;

        #endregion // Inspector

        public Callback OnOpen;
        public Callback OnClose;
        public Callback OnReset;
        public ButtonPredicate CanProceed;
        public ButtonPredicate CanGoBack;

        public ActorWorld CachedWorld;

        private void Awake() {
            if (Panel) {
                Panel.OnUpdated += () => EvaluateButtons(this, this.CachedWorld);
            }
        }

        static private IEnumerator Open(ExperimentScreen screen, ActorWorld world) {
            screen.Header.NextButton.gameObject.SetActive(screen.NextButton);
            screen.Header.NextArrow.SetActive(screen.NextButtonArrow);
            screen.Header.NextLabel.SetText(screen.NextButtonLabelId);
            screen.Header.BackButton.gameObject.SetActive(screen.BackButton);

            if (!screen.Group.gameObject.activeSelf) {
                screen.Group.gameObject.SetActive(true);
                screen.Group.alpha = 0;
            }
            
            screen.CachedWorld = world;
            screen.OnOpen?.Invoke(screen, world);

            using(Script.DisableInput()) {
                yield return Routine.Combine(
                    screen.Group.FadeTo(1, 0.2f),
                    screen.Header.Group.Show(0.2f),
                    screen.Panel ? screen.Panel.Show() : null
                );
            }
        }

        static private IEnumerator Close(ExperimentScreen screen) {
            if (screen == null || !screen.Group.gameObject.activeSelf)
                yield break;
            
            screen.OnClose?.Invoke(screen, screen.CachedWorld);
            using(Script.DisableInput()) {
                yield return Routine.Combine(
                    screen.Group.FadeTo(0, 0.2f),
                    screen.Header.Group.Hide(0.2f),
                    screen.Panel ? screen.Panel.Hide() : null
                );
                screen.Group.gameObject.SetActive(false);
            }

            screen.CachedWorld = null;
        }

        static public void Reset(ExperimentScreen screen) {
            screen.OnReset?.Invoke(screen, screen.CachedWorld);
            screen.Group.Hide();
            screen.Header.Group.Hide();
            if (screen.Panel) {
                screen.Panel.ClearSelection();
                screen.Panel.InstantHide();
            }
            if (screen.CachedWorld != null) {
                screen.CachedWorld.Tank.ScreenTransition.Stop();
                screen.CachedWorld = null;
            }
        }

        static public void Reset(params ExperimentScreen[] screens) {
            foreach(var screen in screens) {
                Reset(screen);
            }
        }

        static public IEnumerator Transition(ref ExperimentScreen current, ExperimentScreen target, ActorWorld world, IEnumerator wait = null, Action onComplete = null) {
            if (current == target) {
                (wait as IDisposable).Dispose();
                onComplete?.Invoke();
                return null;
            }

            ExperimentScreen old = current;
            current = target;
            Routine transition = world.Tank.ScreenTransition.Replace(world.Tank, Transition(old, target, world, wait));
            transition.Tick();
            return transition.Wait();
        }

        static private IEnumerator Transition(ExperimentScreen a, ExperimentScreen b, ActorWorld world, IEnumerator wait = null, Action onComplete = null) {
            using(Script.DisableInput()) {
                if (a != null) {
                    yield return Close(a);
                }
                if (world.Tank.WaterTransition) {
                    yield return world.Tank.WaterTransition.Wait();
                }
                if (wait != null) {
                    yield return wait;
                }
                if (b != null) {
                    yield return Open(b, world);
                }
                onComplete?.Invoke();
            }
        }
    
        static public void EvaluateButtons(ExperimentScreen screen, ActorWorld world) {
            bool bBack = screen.CanGoBack == null ? true : screen.CanGoBack(screen, world);

            bool bProceed = screen.CanProceed == null ? true : screen.CanProceed(screen, world);
            if (screen.Panel) {
                bProceed &= screen.Panel.Selected.Count > 0;
            }
            
            screen.Header.BackButton.interactable = bBack;
            screen.Header.NextButton.interactable = bProceed;
        }

        #if UNITY_EDITOR

        private void Reset() {
            Header = transform.parent.GetComponentInChildren<ExperimentHeaderUI>();
            Group = GetComponent<CanvasGroup>();
            Panel = GetComponentInChildren<BestiaryAddPanel>(true);
        }

        #endif // UNITY_EDITOR
    }
}