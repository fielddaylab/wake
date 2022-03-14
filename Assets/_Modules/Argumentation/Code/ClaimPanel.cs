using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Argumentation {
    public class ClaimPanel : SharedPanel {

        #region Inspector

        [Header("Claim")]
        [SerializeField] private LocText m_ClaimText = null;
        [SerializeField] private FactPools m_EvidencePool = null;
        [SerializeField] private Transform m_EvidenceRoot = null;
        [SerializeField] private LayoutGroup m_Layout = null;
        
        [Header("Dialog")]
        [SerializeField] private RectTransform m_DialogTransform = null;
        [SerializeField] private CanvasGroup m_DialogGroup = null;
        [SerializeField] private RectTransform m_SpeakerContainer = null;
        [SerializeField] private TMP_Text m_SpeakerLabel = null;
        [SerializeField] private Graphic m_SpeakerLabelBackground = null;
        [SerializeField] private TMP_Text m_TextDisplay = null;
        [SerializeField] private Graphic m_TextBackground = null;

        #endregion // Inspector

        [NonSerialized] private ColorPalette4 m_DefaultNamePalette;
        [NonSerialized] private ColorPalette4 m_DefaultTextPalette;
        [NonSerialized] private float m_OriginalDialogY;

        [NonSerialized] private readonly EvidenceFactDisplay[] m_FactDisplays = new EvidenceFactDisplay[ArgueConsts.MaxFactsPerClaim];
        [NonSerialized] private readonly StringHash32[] m_CurrentlyDisplayed = new StringHash32[ArgueConsts.MaxFactsPerClaim];
        
        [NonSerialized] private ArgueData m_CurrentStatus;
        [NonSerialized] private int m_TotalFactSlots;
        
        private Routine m_RootAnim;
        private Routine m_DialogAnim;

        protected override void Awake() {
            base.Awake();

            Services.Events.Register<ArgueData>(ArgueEvents.ClaimDisplay, OnClaimDisplay, this)
                .Register(ArgueEvents.ClaimHide, OnClaimHide, this)
                .Register(ArgueEvents.ClaimCancelled, OnClaimCancelled, this)
                .Register<StringHash32>(ArgueEvents.FactSubmitted, OnFactSubmitted, this)
                .Register<StringHash32>(ArgueEvents.FactRejected, OnFactRejected, this)
                .Register(ArgueEvents.FactsCleared, OnFactsCleared, this)
                .Register(ArgueEvents.FactsRefreshed, OnFactsRefreshed, this)
                .Register(ArgueEvents.Completed, OnCompleted, this)
                .Register(ArgueEvents.Unloaded, OnUnload, this)
                .Register<DialogRecord>(GameEvents.ScriptChoicePresented, PopulateDialog, this)
                .Register(GameEvents.PortableOpened, OnPortableOpened, this)
                .Register(GameEvents.PortableClosed, OnPortableClosed, this);
            
            m_DefaultNamePalette.Content = m_SpeakerLabel.color;
            m_DefaultNamePalette.Background = m_SpeakerLabelBackground.color;

            m_DefaultTextPalette.Content = m_TextDisplay.color;
            m_DefaultTextPalette.Background = m_TextBackground.color;

            m_OriginalDialogY = m_DialogTransform.anchoredPosition.y;
        }

        protected override void OnDestroy() {
            Services.Events?.DeregisterAll(this);

            base.OnDestroy();
        }

        #region Populate

        private void PopulateDialog(DialogRecord record) {
            if (!IsShowing()) {
                return;
            }

            ColorPalette4 characterPalette = m_DefaultNamePalette;
            ColorPalette4 textPalette = m_DefaultTextPalette;

            if (!record.CharacterId.IsEmpty) {
                characterPalette = Assets.Character(record.CharacterId).NamePaletteOverride() ?? characterPalette;
                textPalette = Assets.Character(record.CharacterId).TextPaletteOverride() ?? textPalette;
                
                m_SpeakerLabel.text = record.Name;
                m_SpeakerContainer.gameObject.SetActive(true);
            } else {
                m_SpeakerContainer.gameObject.SetActive(false);
                m_SpeakerLabel.text = string.Empty;
            }

            m_TextDisplay.text = record.Text;

            m_SpeakerLabel.color = characterPalette.Content;
            m_SpeakerLabelBackground.color = characterPalette.Background;

            m_TextDisplay.color = textPalette.Content;
            m_TextBackground.color = textPalette.Background;
        }

        private int GetDataSlot(StringHash32 factId) {
            return Array.IndexOf(m_CurrentStatus.SubmittedFacts, factId);
        }

        private int GetDisplayedSlot(StringHash32 factId) {
            return Array.IndexOf(m_CurrentlyDisplayed, factId);
        }

        private int InsertFact(StringHash32 id) {
            BFDiscoveredFlags flags = Save.Bestiary.GetDiscoveredFlags(id);
            int factIdx = GetDataSlot(id);
            m_CurrentlyDisplayed[factIdx] = id;
            m_FactDisplays[factIdx].Populate(Assets.Fact(id), flags);
            return factIdx;
        }

        #endregion // Populate

        #region Panel

        protected override void OnShow(bool inbInstant) {
            Root.gameObject.SetActive(true);
            CanvasGroup.alpha = 0;

            m_ClaimText.SetText(m_CurrentStatus.ClaimLabel);

            m_EvidencePool.FreeAll();
            Array.Clear(m_FactDisplays, 0, m_TotalFactSlots);

            EvidenceFactDisplay factDisplay;
            BFShapeId slot;
            StringHash32 submitted;
            BFBase fact;
            for(int i = 0; i < m_TotalFactSlots; i++)  {
                slot = m_CurrentStatus.FactSlots[i];
                factDisplay = m_EvidencePool.Alloc(slot, 0, m_EvidenceRoot).GetComponent<EvidenceFactDisplay>();
                submitted = m_CurrentStatus.SubmittedFacts[i];
                if (!submitted.IsEmpty) {
                    fact = Assets.Fact(submitted);
                    factDisplay.Populate(fact, Save.Bestiary.GetDiscoveredFlags(submitted));
                }
                m_FactDisplays[i] = factDisplay;
            }

            m_Layout.ForceRebuild();
        }

        protected override IEnumerator TransitionToShow() {
            return CanvasGroup.Show(0.2f);
        }

        protected override IEnumerator TransitionToHide() {
            return CanvasGroup.Hide(0.2f);
        }

        protected override void OnHideComplete(bool inbInstant) {
            Array.Clear(m_FactDisplays, 0, m_TotalFactSlots);
            Array.Clear(m_CurrentlyDisplayed, 0, m_TotalFactSlots);
            m_EvidencePool.FreeAll();
            m_CurrentStatus = null;
            m_TotalFactSlots = 0;
            m_DialogTransform.gameObject.SetActive(false);

            base.OnHideComplete(inbInstant);
        }

        #endregion // Panel

        #region Event Handlers

        private void OnClaimDisplay(ArgueData data) {
            m_CurrentStatus = data;
            m_TotalFactSlots = data.ExpectedFacts.Count;
            Show();
        }

        private void OnClaimHide() {
            Hide();
        }

        private void OnClaimCancelled() {
            Hide();
        }

        private void OnFactSubmitted(StringHash32 id) {
            InsertFact(id);
        }

        private void OnFactRejected(StringHash32 id) {
            int idx = GetDisplayedSlot(id);
            if (idx >= 0) {
                m_FactDisplays[idx].Clear();
            }
        }

        private void OnFactsRefreshed() {
            Array.Copy(m_CurrentStatus.SubmittedFacts, m_CurrentlyDisplayed,  m_TotalFactSlots);

            EvidenceFactDisplay factDisplay;
            StringHash32 submitted;
            BFBase fact;
            for(int i = 0; i < m_TotalFactSlots; i++)  {
                factDisplay = m_FactDisplays[i];
                submitted = m_CurrentStatus.SubmittedFacts[i];
                if (!submitted.IsEmpty) {
                    fact = Assets.Fact(submitted);
                    factDisplay.Populate(fact, Save.Bestiary.GetDiscoveredFlags(submitted));
                } else {
                    factDisplay.Clear();
                }
            }
        }

        private void OnFactsCleared() {
            for(int i = 0; i < m_TotalFactSlots; i++) {
                m_FactDisplays[i].Clear();
            }
        }

        private void OnCompleted() {
            // TODO: Animation?
            Hide();
        }

        private void OnUnload() {
            Hide();
        }

        private void OnPortableOpened() {
            if (!IsShowing()) {
                return;
            }

            m_DialogAnim.Replace(this, ShowDialog()).Tick();
        }

        private void OnPortableClosed() {
            if (!IsShowing()) {
                return;
            }

            m_DialogAnim.Replace(this, HideDialog()).Tick();
        }

        #endregion // Event Handlers

        #region Animations

        private IEnumerator ShowDialog() {
            m_DialogTransform.SetAnchorPos(m_OriginalDialogY + 32, Axis.Y);
            m_DialogGroup.alpha = 0;
            yield return Routine.Combine(
                m_DialogGroup.Show(0.2f),
                m_DialogTransform.AnchorPosTo(m_OriginalDialogY, 0.2f, Axis.Y).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator HideDialog() {
            yield return Routine.Combine(
                m_DialogGroup.Hide(0.2f),
                m_DialogTransform.AnchorPosTo(m_OriginalDialogY + 32, 0.2f, Axis.Y).Ease(Curve.CubeIn)
            );
        }

        #endregion // Animations
    }
}