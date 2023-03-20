using System;
using System.Collections;
using Aqua.Animation;
using Aqua.Cameras;
using Aqua.Profile;
using AquaAudio;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua.Title
{
    public class TitleController : MonoBehaviour, ISceneLoadHandler
    {
        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private TitleInteractions m_Menu = null;

        [Header("UI")]
        [SerializeField] private TMP_Text m_BuildIdText = null;
        [SerializeField] private TitleCard[] m_Cards = null;
        [SerializeField] private CanvasGroup m_LogoGroup = null;
        [SerializeField] private CanvasGroup m_SubtitleGroup = null;
        [SerializeField] private CanvasGroup m_CreatedByGroup = null;
        [SerializeField] private CanvasGroup m_ControlsGroup = null;

        [Header("Audio")]
        [SerializeField] private SerializedHash32 m_WhaleCallAudioId = null;
        [SerializeField] private AudioBGMTrigger m_TitleBGM = null;

        [Header("Renderers")]
        [SerializeField] private Material m_OtterGlitchMaterial = null;
        
        #endregion // Inspector

        [NonSerialized] private TitleConfig m_Config;
        [NonSerialized] private TempAlloc<FaderRect> m_CurrentFader;
        [NonSerialized] private Routine m_IntroCutscene;
        [NonSerialized] private Routine m_SkipRoutine;
        [NonSerialized] private bool m_AllowSkip;
        [NonSerialized] private Vector3[] m_OriginalOtterPositions;

        private void Start()
        {
            m_Canvas.enabled = false;

            Services.Secrets.RegisterCheat("title_k", SecretService.CheatType.Single, "title", "UUDDLRLRba", () => {
                foreach(var otter in m_Config.Otters) {
                    otter.sharedMaterial = m_OtterGlitchMaterial;
                }
                Routine.Start(this, SpawnOtters());
            });
        }

        private void OnDestroy()
        {
            Services.Secrets?.DeregisterCheat("title_k");

            Services.UI?.StopSkipCutscene();
            Services.Events?.DeregisterAll(this);
            Services.UI?.StopSkipCutscene();
            Services.Secrets?.DisallowCheats("title");
        }

        private void LateUpdate() {
            if (m_AllowSkip && Time.timeScale > 0) {
                if (Script.SkipPressed()) {
                    m_AllowSkip = false;
                    m_SkipRoutine.Replace(this, Skip()).Tick();
                }
            }
        }

        private void InitializeFromAnotherScene()
        {
            Services.Camera.SnapToPose(m_Config.FullPose);

            m_Config.Drift.Scale = 1;
            m_Config.Drift.PushChanges();

            Routine.Start(this, WhaleNoises());
            Services.Data.UnloadProfile();

            m_TitleBGM.Play();

            m_Menu.gameObject.SetActive(true);

            m_LogoGroup.Show();
            m_SubtitleGroup.Show();
            m_ControlsGroup.Show();
            m_CreatedByGroup.Show();
            Services.Secrets.AllowCheats("title");
        }

        private void InitializeFromBootScene()
        {
            Services.Camera.SnapToPose(m_Config.LoadingPose);

            m_Config.Drift.Scale = 0;
            m_Config.Drift.PushChanges();

            m_CurrentFader = Services.UI.WorldFaders.AllocFader();
            m_CurrentFader.Object.Show(Color.black, 0);
            m_IntroCutscene.Replace(this, BootupSequence());

            m_Menu.gameObject.SetActive(false);

            m_LogoGroup.Hide();
            m_SubtitleGroup.Hide();
            m_ControlsGroup.Hide();
            m_CreatedByGroup.Hide();
        }

        private IEnumerator BootupSequence()
        {
            yield return 0.5f;

            m_AllowSkip = true;
            PlayWhaleSound();
            yield return CameraSweepPhase(0, 2, 1, null);
            yield return CameraSweepPhase(1, 4, 2, () => PlayWhaleSound());
            Routine.Start(this, WhaleNoises());
            m_TitleBGM.Play();
            
            m_CurrentFader.Object.Hide(0.3f, false);
            Services.Input.PauseAll();

            yield return Routine.Combine(
                CameraTransition(m_Config.IntroPosePairs[4], m_Config.IntroPosePairs[5], 5, Curve.QuadOut),
                Tween.Float(m_Config.Drift.Scale, 1, (f) => {
                    m_Config.Drift.Scale = f;
                    m_Config.Drift.PushChanges();
                }, 5),
                DisplayMenu(3)
            );

            Services.Input.ResumeAll();
            Services.Secrets.AllowCheats("title");
            m_AllowSkip = false;
        }

        private IEnumerator DisplayMenu(float delay) {
            yield return delay;
            m_AllowSkip = false;
            m_Menu.gameObject.SetActive(true);
            yield return Routine.Combine(
                m_LogoGroup.Show(1f),
                Routine.Delay(m_SubtitleGroup.Show(0.5f), 0.8f),
                Routine.Delay(m_CreatedByGroup.Show(0.3f), 1.2f),
                Routine.Delay(m_ControlsGroup.Show(0.3f), 1.5f)
            );
        }

        #region Routines

        private IEnumerator SpawnOtters() {
            yield return RNG.Instance.NextFloat(2, 3);
            for(int i = 0; i < m_Config.OtterPrefabs.Length; i++) {
                RandomAnimationDelay randomDelay = m_Config.OtterPrefabs[i].GetComponent<RandomAnimationDelay>();
                randomDelay.delayMin *= 0.1f;
                randomDelay.delayMax *= 0.1f;
            }
            for(int i = 0; i < 30; i++) {
                int idx = i % m_Config.OtterPrefabs.Length;
                GameObject newOtter = Instantiate(m_Config.OtterPrefabs[idx]);
                newOtter.transform.position = m_OriginalOtterPositions[idx] + RNG.Instance.NextVector3(1, 5);
                newOtter.transform.SetScale(newOtter.transform.localScale * RNG.Instance.NextFloat(0.8f, 2f));
                yield return 0.1f;
            }
        }

        private void PlayWhaleSound() {
            if (m_SkipRoutine) {
                return;
            }
            Services.Audio.PostEvent("TitleWhale");
        }

        private IEnumerator WhaleNoises()
        {
            while(true)
            {
                yield return RNG.Instance.NextFloat(12, 24);
                if (!m_SkipRoutine) {
                    Services.Audio.PostEvent("TitleWhale");
                }
            }
        }

        private IEnumerator CameraSweepPhase(int phaseIdx, float whaleTime, float cardUpDuration, Action onCardShow) {
            float cameraSweepDuration;
            IEnumerator cameraSweep = CameraSweep(m_Config.IntroPosePairs[phaseIdx * 2], m_Config.IntroPosePairs[phaseIdx * 2 + 1], 2f, out cameraSweepDuration);
            Services.Animation.AmbientTransforms.SyncTransform(m_Config.WhaleTransform, whaleTime);
            m_CurrentFader.Object.Hide(0.3f, false);
            yield return cameraSweepDuration - 0.6f;
            yield return m_CurrentFader.Object.Show(Color.black, 0.3f);
            yield return 0.3f;
            yield return ShowCard(m_Cards[phaseIdx], cardUpDuration, onCardShow);
        }

        static private IEnumerator CameraSweep(CameraPose poseA, CameraPose poseB, float speed, out float duration)
        {
            Services.Camera.SnapToPose(poseA);
            float distance = Vector3.Distance(poseA.transform.position, poseB.transform.position);
            return Services.Camera.MoveToPose(poseB, duration = (distance / speed), Curve.Linear, CameraPoseProperties.All);
        }

        static private IEnumerator CameraTransition(CameraPose poseA, CameraPose poseB, float duration, Curve curve)
        {
            Services.Camera.SnapToPose(poseA);
            return Services.Camera.MoveToPose(poseB, duration, curve, CameraPoseProperties.All);
        }

        static private IEnumerator ShowCard(TitleCard card, float duration, Action onShow)
        {
            card.Group.gameObject.SetActive(true);
            card.Group.alpha = 1;
            if (card.Text != null) {
                card.Text.maxVisibleCharacters = 0;
            }
            if (card.Text2 != null) {
                card.Text2.maxVisibleCharacters = 0;
            }
            if (card.Logo != null) {
                card.Logo.Alpha = 0;
            }

            IEnumerator[] routines = new IEnumerator[3];
            int routineCount = 0;

            float delay = 0;

            if (card.Text != null) {
                int maxCharacters = card.Text.textInfo.characterCount;
                if (maxCharacters == 0) {
                    LocText loc = card.Text.GetComponent<LocText>();
                    if (loc != null) {
                        maxCharacters = loc.Metrics.VisibleCharCount;
                    } else {
                        maxCharacters = card.Text.text.Length;
                    }
                }
                routines[routineCount++] = Tween.Int(0, maxCharacters, (i) => card.Text.maxVisibleCharacters = i, 1).DelayBy(delay);
                delay += 1.1f;
            }

            if (card.Logo != null) {
                routines[routineCount++] = Tween.Float(card.Logo.Alpha, 1, (f) => card.Logo.Alpha = f, 0.4f).DelayBy(delay);
                delay += 0.5f;
            }

            if (card.Text2 != null) {
                int maxCharacters = card.Text2.textInfo.characterCount;
                if (maxCharacters == 0) {
                    LocText loc = card.Text2.GetComponent<LocText>();
                    if (loc != null) {
                        maxCharacters = loc.Metrics.VisibleCharCount;
                    } else {
                        maxCharacters = card.Text2.text.Length;
                    }
                }
                routines[routineCount++] = Tween.Int(0, maxCharacters, (i) => card.Text2.maxVisibleCharacters = i, 1).DelayBy(delay);
            }

            yield return Routine.Combine(routines);
            onShow?.Invoke();
            yield return duration;

            yield return card.Group.FadeTo(0, 0.3f);
            card.Group.gameObject.SetActive(false);
        }

        #endregion // Routines

        #region Skip

        private IEnumerator Skip() {
            yield return Services.UI.StartSkipCutscene();
            float oldTimeScale = Time.timeScale;
            Time.timeScale = 100;
            m_IntroCutscene.SetTimeScale(1000);
            while(m_IntroCutscene) {
                yield return null;
            }
            Time.timeScale = 1;
            Services.UI.StopSkipCutscene();
        }

        #endregion // Skip

        private void ShowSecretsForSaveData(SaveSummaryData summary) {
            for(byte i = 0; i < summary.SpecterCount; i++) {
                m_Config.Specters[i].gameObject.SetActive(RNG.Instance.Chance(0.25f));
            }

            if ((summary.Flags & SaveSummaryFlags.CompletedFinalJob) == 0) {
                for(int i = 0; i < m_Config.Dreams.Length; i++) {
                    Transform dream = m_Config.Dreams[i];
                    if (dream && (summary.DreamMask & (1 << i)) != 0) {
                        dream.gameObject.SetActive(RNG.Instance.Chance(0.15f));
                    }
                }
            }
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            Services.Assets.CancelPreload("Scene/Title");

            m_Config = FindObjectOfType<TitleConfig>();
            m_BuildIdText.SetText(string.Format("Build: {0} ({1})", BuildInfo.Id(), BuildInfo.Date()));
            m_Menu.LoadConfig(m_Config);

            m_OriginalOtterPositions = new Vector3[m_Config.OtterPrefabs.Length];
            for(int i = 0; i < m_OriginalOtterPositions.Length; i++) {
                m_OriginalOtterPositions[i] = m_Config.OtterPrefabs[i].transform.position;
            }

            foreach(var card in m_Cards) {
                card.Group.gameObject.SetActive(false);
            }

            ShowSecretsForSaveData(Services.Data.LastProfileSummary());

            m_Canvas.enabled = true;

            // if returning from another scene
            if (Services.State.PreviousScene().BuildIndex >= GameConsts.GameSceneIndexStart)
            {
                InitializeFromAnotherScene();
            }
            else
            {
                InitializeFromBootScene();
            }
        }
    }
}