using System;
using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System.Collections;
using BeauRoutine;
using ScriptableBake;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ExperimentScreen : MonoBehaviour, IBaked {
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
                if (Panel.Category == BestiaryDescCategory.Critter) {
                    Panel.OnAdded += (b) => Services.Events.Dispatch(ExperimentEvents.ExperimentAddCritter, b.Id());
                    Panel.OnRemoved += (b) => Services.Events.Dispatch(ExperimentEvents.ExperimentRemoveCritter, b.Id());
                    Panel.OnCleared += () => Services.Events.Dispatch(ExperimentEvents.ExperimentCrittersCleared);
                } else {
                    Panel.OnAdded += (b) => Services.Events.Dispatch(ExperimentEvents.ExperimentAddEnvironment, b.Id());
                    Panel.OnRemoved += (b) => Services.Events.Dispatch(ExperimentEvents.ExperimentRemoveEnvironment, b.Id());
                    Panel.OnCleared += () => Services.Events.Dispatch(ExperimentEvents.ExperimentEnvironmentCleared);
                }
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

            ExperimentUtil.TriggerExperimentScreenViewed(world.Tank, screen.Id);

            using(Script.DisableInput()) {
                Async.InvokeAsync(() => EvaluateButtons(screen, world));
                yield return Routine.Combine(
                    screen.Group.FadeTo(1, 0.2f),
                    screen.BackButton || screen.NextButton || screen.Panel ? screen.Header.Group.Show(0.2f) : null,
                    screen.Panel ? screen.Panel.Show() : null
                );

                screen.Group.blocksRaycasts = true;
            }
        }

        static private IEnumerator Close(ExperimentScreen screen) {
            if (screen == null || !screen.Group.gameObject.activeSelf)
                yield break;
            
            screen.OnClose?.Invoke(screen, screen.CachedWorld);
            ExperimentUtil.TriggerExperimentScreenExited(screen.CachedWorld.Tank, screen.Id);

            using(Script.DisableInput()) {
                screen.Group.blocksRaycasts = false;
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

        static public IEnumerator Transition(ExperimentScreen target, ActorWorld world, IEnumerator wait = null, Action onComplete = null) {
            if (world.Tank.CurrentScreen == target) {
                (wait as IDisposable).Dispose();
                onComplete?.Invoke();
                return null;
            }

            ExperimentScreen old = world.Tank.CurrentScreen;
            world.Tank.CurrentScreen = target;
            Routine transition = world.Tank.ScreenTransition.Replace(world.Tank, Transition(old, target, world, wait, onComplete));
            transition.Tick();
            return transition.Wait();
        }

        static private IEnumerator Transition(ExperimentScreen a, ExperimentScreen b, ActorWorld world, IEnumerator wait, Action onComplete) {
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

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            Reset();
            return true;
        }

        #endif // UNITY_EDITOR
    }
}