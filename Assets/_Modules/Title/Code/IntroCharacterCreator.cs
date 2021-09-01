using UnityEngine;
using BeauUtil;
using TMPro;
using UnityEngine.UI;
using System;
using Aqua.Profile;
using Aqua.Scripting;
using Leaf.Runtime;
using System.Collections;
using BeauRoutine;

namespace Aqua.Title
{
    public class IntroCharacterCreator : SharedPanel
    {
        #region Inspector

        [Header("Name")]
        [SerializeField] private TMP_InputField m_NameInput = null;
        [SerializeField] private Button m_NameRandomizeButton = null;

        [Header("Pronouns")]
        [SerializeField] private Toggle m_MalePronounToggle = null;
        [SerializeField] private Toggle m_NeutralPronounToggle = null;
        [SerializeField] private Toggle m_FemalePronounToggle = null;
        [SerializeField] private LocText m_PronounLabel = null;

        [Header("Confirm")]
        [SerializeField] private Button m_ConfirmButton = null;

        [Header("Data")]
        [SerializeField] private string[] m_RandomNames = null;

        #endregion // Inspector

        [NonSerialized] private RandomDeck<string> m_RandomNameDeck;
        [NonSerialized] private string m_CurrentName;
        [NonSerialized] private Pronouns? m_CurrentPronouns;

        protected override void Awake()
        {
            base.Awake();

            m_NameInput.onValueChanged.AddListener(OnNameChanged);
            m_NameRandomizeButton.onClick.AddListener(OnNameRandomize);

            m_MalePronounToggle.onValueChanged.AddListener((b) => OnPronounToggled(Pronouns.Masculine, b));
            m_NeutralPronounToggle.onValueChanged.AddListener((b) => OnPronounToggled(Pronouns.Neutral, b));
            m_FemalePronounToggle.onValueChanged.AddListener((b) => OnPronounToggled(Pronouns.Feminine, b));
        }

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            m_RandomNameDeck = new RandomDeck<string>(m_RandomNames);

            m_CurrentPronouns = null;
            m_CurrentName = null;

            m_NameInput.SetTextWithoutNotify(string.Empty);
            m_MalePronounToggle.SetIsOnWithoutNotify(false);
            m_NeutralPronounToggle.SetIsOnWithoutNotify(false);
            m_FemalePronounToggle.SetIsOnWithoutNotify(false);
            m_PronounLabel.SetText(null);

            TryUpdateConfirm();
        }

        protected override IEnumerator TransitionToShow()
        {
            yield return CanvasGroup.Show(0.2f, true);
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return CanvasGroup.Hide(0.2f, false);
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnNameChanged(string inNewText)
        {
            m_CurrentName = inNewText;
            TryUpdateConfirm();
        }

        private void OnNameRandomize()
        {
            m_NameInput.text = m_RandomNameDeck.Next();
        }

        private void OnPronounToggled(Pronouns inPronoun, bool inbToggled)
        {
            if (inbToggled)
            {
                if (Ref.Replace(ref m_CurrentPronouns, inPronoun))
                {
                    switch(inPronoun)
                    {
                        case Pronouns.Masculine:
                            m_PronounLabel.SetText("He/Him");
                            break;

                        case Pronouns.Neutral:
                            m_PronounLabel.SetText("They/Them");
                            break;

                        case Pronouns.Feminine:
                            m_PronounLabel.SetText("She/Her");
                            break;
                    }

                    TryUpdateConfirm();
                }
            }
            else
            {
                if (Ref.CompareExchange(ref m_CurrentPronouns, inPronoun, null))
                {
                    m_PronounLabel.SetText(null);
                    TryUpdateConfirm();
                }
            }
        }

        #endregion // Callbacks

        private void TryUpdateConfirm()
        {
            m_ConfirmButton.interactable = !String.IsNullOrEmpty(m_CurrentName) && m_CurrentPronouns.HasValue;
        }

        private void WriteCharacterData()
        {
            CharacterProfile profile = Services.Data.Profile.Character;
            profile.DisplayName = m_CurrentName;
            profile.Pronouns = m_CurrentPronouns.Value;
        }

        #region Leaf

        [LeafMember("RunActivity")]
        private IEnumerator Run()
        {
            if (!IsShowing())
            {
                yield return Show();
            }
            else
            {
                yield return CanvasGroup.FadeTo(1, 0.5f);
                SetInputState(true);
            }

            yield return m_ConfirmButton.onClick.WaitForInvoke();
            WriteCharacterData();
            SetInputState(false);
            yield return CanvasGroup.FadeTo(0.5f, 0.5f);
        }

        [LeafMember("CompleteActivity")]
        private IEnumerator Complete()
        {
            WriteCharacterData();
            yield return Hide();
        }

        #endregion // Leaf
    }
}