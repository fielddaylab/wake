using System;
using System.Collections;
using Aqua.Animation;
using AquaAudio;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aqua
{
    public class TitleController : MonoBehaviour, ISceneLoadHandler
    {
        static public readonly StringHash32 Event_StartGame = "title:start-game"; // String userCode

        #region Inspector

        [Header("Camera")]
        [SerializeField] private CameraFOVPlane m_CameraFOV = null;
        [SerializeField] private AmbientTransform m_CameraDrift = null;
        [SerializeField] private float m_CloseZoom = 10;

        [Header("World")]
        [SerializeField] private TMP_Text m_LoadingText = null;

        [Header("UI")]
        [SerializeField] private CanvasGroup m_FieldDayCard = null;
        [SerializeField] private CanvasGroup m_TitleCard = null;
        [SerializeField] private TMP_InputField m_UsercodeInput = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private TMP_Text m_BuildIdText = null;
        
        [Header("Settings")]
        [SerializeField] private string m_LoadSceneName = null;
        [SerializeField] private SerializedHash32 m_AmbienceEvent = null;

        [Header("Animation")]
        [SerializeField] private float m_InitialDelay = 0.5f;
        [SerializeField] private float m_LoadTextFadeDelay = 0.2f;
        [SerializeField] private float m_CardFadeDuration = 0.5f;
        [SerializeField] private float m_ZoomDuration = 7;
        [SerializeField] private float m_ZoomDelay = 1;
        [SerializeField] private Curve m_ZoomCurve = Curve.Linear;
        [SerializeField] private float m_FieldDayCardDelay = 2;
        [SerializeField] private float m_FieldDayCardSustain = 2;
        [SerializeField] private float m_TitleCardDelay = 6.5f;
        
        #endregion // Inspector

        private AudioHandle m_WaterAmbience;

        private void Awake()
        {
            m_CameraFOV.Zoom = m_CloseZoom;
            m_NextButton.onClick.AddListener(OnNextButton);
        }

        private void OnDisable()
        {
            m_WaterAmbience.Stop();
        }

        private void OnNextButton()
        {
            Services.Data.LoadProfile(m_UsercodeInput.text);
            Services.Events.Dispatch(Event_StartGame, m_UsercodeInput.text);
            Services.Audio.StopMusic();
            StateUtil.LoadSceneWithWipe(m_LoadSceneName);
        }

        private void InitializeFromAnotherScene()
        {
            m_CameraFOV.Zoom = 1;
            m_CameraDrift.AnimationScale = 1;

            m_TitleCard.gameObject.SetActive(true);
            m_TitleCard.alpha = 1;
            m_TitleCard.blocksRaycasts = true;

            m_LoadingText.gameObject.SetActive(false);

            m_WaterAmbience = Services.Audio.PostEvent(m_AmbienceEvent);
            Routine.Start(this, WhaleNoises());
        }

        private void InitializeFromBootScene()
        {
            m_WaterAmbience = Services.Audio.PostEvent(m_AmbienceEvent).SetVolume(0).SetVolume(1, 1);
            Routine.Start(this, WhaleNoises());

            m_CameraFOV.Zoom = m_CloseZoom;
            m_CameraDrift.AnimationScale = 0;
            Routine.Start(this, BootupSequence());
        }

        private IEnumerator BootupSequence()
        {
            yield return m_InitialDelay;
            yield return Routine.Combine(
                m_LoadingText.FadeTo(0, m_CardFadeDuration).DelayBy(m_LoadTextFadeDelay),
                Tween.Float(m_CameraFOV.ZoomedHeight(), m_CameraFOV.ZoomedHeight(1), SetCameraZoomByHeight, m_ZoomDuration).Ease(m_ZoomCurve).DelayBy(m_ZoomDelay),
                Tween.Float(m_CameraDrift.AnimationScale, 1, (f) => m_CameraDrift.AnimationScale = f, m_ZoomDuration).DelayBy(m_ZoomDelay),
                ShowFieldDaySequence(m_FieldDayCardDelay),
                ShowTitleScreenSequence(m_TitleCardDelay)
            );
            m_LoadingText.gameObject.SetActive(false);
        }

        private IEnumerator ShowFieldDaySequence(float inDelay)
        {
            yield return inDelay;

            m_FieldDayCard.gameObject.SetActive(true);
            m_FieldDayCard.alpha = 0;
            yield return m_FieldDayCard.FadeTo(1, m_CardFadeDuration);
            yield return m_FieldDayCardSustain;
            yield return m_FieldDayCard.FadeTo(0, m_CardFadeDuration);
            m_FieldDayCard.gameObject.SetActive(false);
        }

        private IEnumerator ShowTitleScreenSequence(float inDelay)
        {
            yield return inDelay;

            m_TitleCard.gameObject.SetActive(true);
            m_TitleCard.alpha = 0;
            m_TitleCard.blocksRaycasts = false;
            yield return m_TitleCard.FadeTo(1, m_CardFadeDuration);
            m_TitleCard.blocksRaycasts = true;
        }

        private IEnumerator WhaleNoises()
        {
            while(true)
            {
                yield return RNG.Instance.NextFloat(12, 24);
                Services.Audio.PostEvent("TitleWhale");
            }
        }

        private void SetCameraZoomByHeight(float inHeight)
        {
            m_CameraFOV.Zoom = m_CameraFOV.Height / inHeight;
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_BuildIdText.SetText(string.Format("Build: {0} ({1})", BuildInfo.Id(), BuildInfo.Date()));

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