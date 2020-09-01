// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
// using BeauRoutine;
// using BeauRoutine.Extensions;
// using BeauUtil;
// using TMPro;
// using System.Collections;
// using System;
// using ProtoAudio;
// using BeauUtil.Tags;
// using BeauUtil.UI;

// namespace ProtoAqua
// {
//     public class FaderDisplay : BasePanel
//     {
//         #region Inspector

//         [Header("Loading")]
//         [SerializeField] private Canvas m_Canvas = null;
//         [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;

//         [Header("Shared Elements")]
//         [SerializeField] private CanvasGroup m_Group = null;
//         [SerializeField] private Image m_Image = null;
//         [SerializeField] private TweenSettings m_TransitionSettings = new TweenSettings(0.2f);
        
//         #endregion // Inspector

//         protected override void InstantTransitionToShow()
//         {
//             m_Group.alpha = 1;
//             m_Group.gameObject.SetActive(true);

//             m_BackgroundGroup.alpha = 1;
//             m_BackgroundGroup.gameObject.SetActive(true);
//         }

//         protected override void InstantTransitionToHide()
//         {
//             m_Group.gameObject.SetActive(false);
//             m_Group.alpha = 0;

//             m_BackgroundGroup.gameObject.SetActive(false);
//             m_BackgroundGroup.alpha = 0;
//         }

//         protected override void OnShow(bool inbInstant)
//         {
//             m_Canvas.enabled = true;
//             m_RaycastBlocker.ClearOverride();
//             if (!WasShowing())
//             {
//                 Services.Input.PushPriority(m_RaycastBlocker);
//             }
//         }

//         protected override void OnHide(bool inbInstant)
//         {
//         }

//         protected override void OnHideComplete(bool inbInstant)
//         {
//             m_Canvas.enabled = false;
//             m_RaycastBlocker.OverrideState(false);
//             if (WasShowing())
//             {
//                 Services.Input?.PopPriority();
//             }
//         }

//         protected override IEnumerator TransitionToShow()
//         {
//             m_Group.gameObject.SetActive(true);
//             m_BackgroundGroup.gameObject.SetActive(true);

//             yield return Routine.Combine(
//                 m_Group.FadeTo(1, m_TransitionSettings),
//                 m_BackgroundGroup.FadeTo(1, m_TransitionSettings)
//             );
//         }

//         protected override IEnumerator TransitionToHide()
//         {
//             yield return Routine.Combine(
//                 m_Group.FadeTo(0, m_TransitionSettings),
//                 m_BackgroundGroup.FadeTo(0, m_TransitionSettings)
//             );

//             m_Group.gameObject.SetActive(false);
//             m_BackgroundGroup.gameObject.SetActive(false);
//         }
//     }
// }