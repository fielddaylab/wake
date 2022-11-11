using System;
using System.Collections;
using Aqua.Compression;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public sealed class PopupLayout : MonoBehaviour {
        
        public delegate void OptionSelectedDelegate(StringHash32 inOptionId);

        [Serializable]
        private struct ButtonConfig {
            public Transform Root;
            public TMP_Text Text;
            public Button Button;
            public CursorInteractionHint Tooltip;

            [NonSerialized] public StringHash32 OptionId;
        }

        #region Inspector

        [Header("Contents")]
        [SerializeField] private LayoutGroup m_Layout = null;
        [SerializeField] private LocText m_HeaderText = null;
        [SerializeField] private LocText m_SubheaderText = null;
        [SerializeField] private StreamedImageSetDisplay m_ImageDisplay = null;
        [SerializeField] private Graphic m_ImageBG = null;
        [SerializeField] private LocText m_ContentsText = null;
        [SerializeField] private FactPools m_FactPools = null;
        [SerializeField] private RectTransformPool m_KnownFactBadgePool = null;
        [SerializeField] private VerticalLayoutGroup m_VerticalFactLayout = null;
        [SerializeField] private GridLayoutGroup m_GridFactLayout = null;
        [SerializeField] private int m_CustomModuleSiblingIndex = 0;
        [SerializeField] private RectTransform m_ExtraBackground = null;
        [SerializeField] private GameObject m_CompressedLayoutContainer = null;
        [SerializeField] private GameObject m_CompressedLayoutRoot = null;
        [SerializeField] private LayoutDecompressor m_LayoutDecompressor = null;
        [SerializeField] private LayoutElement m_DividerGroup = null;
        [SerializeField] private ButtonConfig[] m_Buttons = null;
        [SerializeField] private Button m_CloseButton = null;

        #endregion // Inspector

        [NonSerialized] private BaseInputLayer m_Input;

        [NonSerialized] private Color m_DefaultHeaderColor;
        [NonSerialized] private Color m_DefaultSubheaderColor;
        [NonSerialized] private Color m_DefaultTextColor;

        [NonSerialized] private StringHash32 m_SelectedOption;
        [NonSerialized] private bool m_OptionWasSelected;
        [NonSerialized] private int m_OptionCount;

        [NonSerialized] private BFBase[] m_CachedFactsSet = new BFBase[1];
        [NonSerialized] private BFDiscoveredFlags[] m_CachedFlagsSet = new BFDiscoveredFlags[1];

        [NonSerialized] private RectTransform m_CurrentCustomModule;
        [NonSerialized] private Transform m_OldCustomModuleParent;
        [NonSerialized] private StringHash32 m_CurrentLayoutId;

        public OptionSelectedDelegate OnOptionSelected;

        #region Unity Events

        public void Initialize() {
            for (int i = 0; i < m_Buttons.Length; ++i) {
                int cachedIdx = i;
                m_Buttons[i].Button.onClick.AddListener(() => OnButtonClicked(cachedIdx));
            }

            m_CloseButton.onClick.AddListener(OnCloseClicked);

            if (m_HeaderText != null) {
                m_DefaultHeaderColor = m_HeaderText.Graphic.color;
            }
            if (m_SubheaderText != null) {
                m_DefaultSubheaderColor = m_SubheaderText.Graphic.color;
            }
            if (m_ContentsText != null) {
                m_DefaultTextColor = m_ContentsText.Graphic.color;
            }

            m_Input = BaseInputLayer.Find(this);
        }

        private void OnEnable() {
            Async.InvokeAsync(() => m_Layout.ForceRebuild(true));
        }

        private void OnDisable() {
            m_KnownFactBadgePool.Reset();
            if (m_FactPools) {
                m_FactPools.FreeAll();
            }
            if (m_ImageDisplay) {
                m_ImageDisplay.Clear();
            }
            SetCustomModule(null);
            SetCustomLayout(null);

            m_CachedFactsSet[0] = default;
            m_CachedFlagsSet[0] = default;
        }

        #endregion // Unity Events

        #region Display

        public PopupFacts TempFacts(BFBase fact, BFDiscoveredFlags flags) {
            m_CachedFactsSet[0] = fact;
            m_CachedFlagsSet[0] = flags;
            return new PopupFacts() {
                Facts = m_CachedFactsSet,
                Flags = m_CachedFlagsSet
            };
        }

        public void Configure(ref PopupContent inContent, PopupFlags inFlags) {
            ConfigureText(ref inContent, inFlags);
            ConfigureOptions(ref inContent, inFlags);
            ConfigureFacts(ref inContent.Facts);
            SetCustomModule(inContent.CustomModule);
            SetCustomLayout(inContent.CustomLayout);
        }

        public void ConfigureText(ref PopupContent inContent, PopupFlags inFlags) {
            if (m_HeaderText) {
                if (!string.IsNullOrEmpty(inContent.Header)) {
                    m_HeaderText.SetTextFromString(inContent.Header);
                    m_HeaderText.Graphic.color = inContent.HeaderColorOverride.GetValueOrDefault(m_DefaultHeaderColor);
                    m_HeaderText.gameObject.SetActive(true);
                } else {
                    m_HeaderText.gameObject.SetActive(false);
                    m_HeaderText.SetTextFromString(string.Empty);
                }
            }

            if (m_SubheaderText) {
                if (!string.IsNullOrEmpty(inContent.Subheader)) {
                    m_SubheaderText.SetTextFromString(inContent.Subheader);
                    m_SubheaderText.Graphic.color = inContent.SubheaderColorOverride.GetValueOrDefault(m_DefaultSubheaderColor);
                    m_SubheaderText.gameObject.SetActive(true);
                } else {
                    m_SubheaderText.gameObject.SetActive(false);
                    m_SubheaderText.SetTextFromString(string.Empty);
                }
            }

            if (m_ContentsText) {
                if (!string.IsNullOrEmpty(inContent.Text)) {
                    m_ContentsText.SetTextFromString(inContent.Text);
                    m_ContentsText.Graphic.color = inContent.TextColorOverride.GetValueOrDefault(m_DefaultTextColor);
                    m_ContentsText.gameObject.SetActive(true);
                } else {
                    m_ContentsText.gameObject.SetActive(false);
                    m_ContentsText.SetTextFromString(string.Empty);
                }
            }

            if (m_ImageDisplay) {
                if (inContent.Image.IsEmpty) {
                    m_ImageDisplay.gameObject.SetActive(false);
                } else {
                    m_ImageDisplay.gameObject.SetActive(true);
                    if ((inFlags & PopupFlags.TallImage) != 0) {
                        m_ImageDisplay.Layout.preferredHeight = 260;
                    } else {
                        m_ImageDisplay.Layout.preferredHeight = 160;
                    }
                    m_ImageDisplay.Display(inContent.Image);
                    if (m_ImageBG) {
                        m_ImageBG.enabled = (inFlags & PopupFlags.ImageBG) != 0;
                    }
                }
            }

            if (m_ExtraBackground) {
                if ((inFlags & PopupFlags.ImageTextBG) != 0) {
                    float top = 20;
                    if (m_HeaderText && m_HeaderText.gameObject.activeSelf) {
                        top += m_HeaderText.Graphic.preferredHeight;
                    }
                    if (m_ImageDisplay && m_ImageDisplay.gameObject.activeSelf) {
                        top += m_ImageDisplay.Layout.preferredHeight * 0.6f;
                    }
                    float bottom = 20;
                    if (inContent.Options.Length > 0) {
                        bottom += 72;
                    }
                    Vector2 offsetMin = m_ExtraBackground.offsetMin,
                        offsetMax = m_ExtraBackground.offsetMax;
                    offsetMin.y = bottom;
                    offsetMax.y = -top;
                    m_ExtraBackground.offsetMin = offsetMin;
                    m_ExtraBackground.offsetMax = offsetMax;
                    m_ExtraBackground.gameObject.SetActive(true);
                } else {
                    m_ExtraBackground.gameObject.SetActive(false);
                }
            }
        }

        private void ConfigureOptions(ref PopupContent inContent, PopupFlags ioPopupFlags) {
            m_OptionCount = inContent.Options.Length;
            if (m_OptionCount == 0) {
                ioPopupFlags |= PopupFlags.ShowCloseButton;
            }
            for (int i = 0; i < m_Buttons.Length; ++i) {
                ref ButtonConfig config = ref m_Buttons[i];

                if (i < m_OptionCount) {
                    NamedOption option = inContent.Options[i];
                    config.Text.SetText(Loc.Find(option.TextId));
                    config.OptionId = option.Id;
                    config.Root.gameObject.SetActive(true);
                    config.Tooltip.TooltipOverride = config.Text.text;
                    config.Button.interactable = option.Enabled;
                } else {
                    config.OptionId = null;
                    config.Root.gameObject.SetActive(false);
                    config.Tooltip.TooltipOverride = null;
                }
            }

            if (m_DividerGroup) {
                m_DividerGroup.gameObject.SetActive(m_OptionCount > 0);
                m_DividerGroup.preferredHeight = (ioPopupFlags & PopupFlags.ImageTextBG) != 0 ? 16 : 4;
            }
            if (m_CloseButton) {
                m_CloseButton.gameObject.SetActive((ioPopupFlags & PopupFlags.ShowCloseButton) != 0);
            }
        }

        private void ConfigureFacts(ref PopupFacts inFacts) {
            if (!m_VerticalFactLayout || !m_GridFactLayout || !m_FactPools) {
                return;
            }

            var factList = inFacts.Facts;

            m_FactPools.FreeAll();
            m_KnownFactBadgePool.Reset();

            Vector2 gridCellSize = m_GridFactLayout.cellSize;
            gridCellSize.y = 0;

            if (factList.IsEmpty) {
                m_VerticalFactLayout.gameObject.SetActive(false);
                m_GridFactLayout.gameObject.SetActive(false);
                return;
            }

            bool bUsedGrid = false, bUsedVertical = false, bCurrentIsGrid = false;

            Transform target;
            MonoBehaviour factView;
            RectTransform factTransform;

            for (int i = 0; i < factList.Length; i++) {
                BFBase fact = factList[i];
                BFDiscoveredFlags flags = i >= inFacts.Flags.Length ? BFType.DefaultDiscoveredFlags(factList[i]) : inFacts.Flags[i];
                if (factList.Length > 1) {
                    switch (fact.Type) {
                        case BFTypeId.WaterProperty:
                        case BFTypeId.WaterPropertyHistory:
                        case BFTypeId.Population:
                        case BFTypeId.PopulationHistory:
                            target = m_GridFactLayout.transform;
                            bUsedGrid = true;
                            bCurrentIsGrid = true;
                            break;

                        default: {
                                target = m_VerticalFactLayout.transform;
                                bUsedVertical = true;
                                bCurrentIsGrid = false;
                                break;
                            }
                    }
                } else {
                    target = m_VerticalFactLayout.transform;
                    bUsedVertical = true;
                }

                factView = m_FactPools.Alloc(factList[i], flags, inFacts.Reference, target);
                factTransform = (RectTransform)factView.transform;
                if (bCurrentIsGrid) {
                    gridCellSize.y = Mathf.Max(gridCellSize.y, factTransform.sizeDelta.y);
                }

                if (inFacts.IsNew != null) {
                    bool isNew = inFacts.IsNew(factList[i]);
                    if (!isNew) {
                        m_KnownFactBadgePool.Alloc(factTransform);
                    }
                }
            }

            if (bUsedVertical) {
                m_VerticalFactLayout.gameObject.SetActive(true);
                m_VerticalFactLayout.ForceRebuild();
            } else {
                m_VerticalFactLayout.gameObject.SetActive(false);
            }

            if (bUsedGrid) {
                m_GridFactLayout.cellSize = gridCellSize;
                m_GridFactLayout.gameObject.SetActive(true);
                m_GridFactLayout.ForceRebuild();
            } else {
                m_GridFactLayout.gameObject.SetActive(false);
            }
        }

        public IEnumerator WaitForInput(PopupContent inContent, Future<StringHash32> ioFuture) {
            m_SelectedOption = StringHash32.Null;
            m_OptionWasSelected = false;

            var options = inContent.Options;
            while (!m_OptionWasSelected) {
                if (options.Length <= 1 && m_Input.Device.KeyPressed(KeyCode.Space)) {
                    if (options.Length == 1) {
                        OnButtonClicked(0);
                    } else {
                        OnCloseClicked();
                    }
                    break;
                }

                yield return null;
            }

            ioFuture?.Complete(m_SelectedOption);
            m_SelectedOption = StringHash32.Null;
            m_OptionWasSelected = false;
        }

        #endregion // Display

        #region Custom Modules

        private void SetCustomModule(RectTransform inCustomModule) {
            if (m_CurrentCustomModule == inCustomModule) {
                return;
            }

            if (m_CurrentCustomModule) {
                m_CurrentCustomModule.SetParent(m_OldCustomModuleParent);
                m_CurrentCustomModule.gameObject.SetActive(false);
            }

            m_CurrentCustomModule = inCustomModule;

            if (inCustomModule) {
                m_OldCustomModuleParent = inCustomModule.parent;
                inCustomModule.SetParent(m_Layout.transform);
                inCustomModule.SetSiblingIndex(m_CustomModuleSiblingIndex);
                inCustomModule.gameObject.SetActive(true);
            }
        }

        private void SetCustomLayout(StringHash32 inLayoutId) {
            if (m_CurrentLayoutId == inLayoutId || !m_LayoutDecompressor) {
                return;
            }

            m_LayoutDecompressor.ClearAll();
            m_CurrentLayoutId = inLayoutId;

            if (!m_CurrentLayoutId.IsEmpty) {
                GameObject layout = m_LayoutDecompressor.Decompress(Services.UI.CompressedLayouts, m_CurrentLayoutId, m_CompressedLayoutRoot);
                m_CompressedLayoutContainer.SetActive(true);
                layout.SetActive(true);
            } else {
                m_CompressedLayoutContainer.SetActive(false);
            }
        }

        #endregion // Custom Modules

        #region Callbacks

        private void OnButtonClicked(int inIndex) {
            m_SelectedOption = m_Buttons[inIndex].OptionId;
            m_OptionWasSelected = true;
            OnOptionSelected?.Invoke(m_SelectedOption);
        }

        private void OnCloseClicked() {
            m_SelectedOption = StringHash32.Null;
            m_OptionWasSelected = true;
            OnOptionSelected?.Invoke(m_SelectedOption);
        }

        #endregion // Callbacks

        static public PopupContent TempContent(string inHeader, string inText, StreamedImageSet inImage, PopupFacts inFacts, NamedOption[] inOptions) {
            PopupContent content = new PopupContent() {
                Text = inText,
                Header = inHeader,
                Image = inImage,
                Facts = inFacts,
                Options = inOptions
            };
            return content;
        }

        static public void AttemptTTS(ref PopupContent inContent) {
            if (!Accessibility.TTSFull) {
                return;
            }
            
            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                if (!string.IsNullOrEmpty(inContent.Header)) {
                    psb.Builder.Append(inContent.Header).Append("\n\n");
                }
                if (!inContent.Image.IsEmpty && !string.IsNullOrEmpty(inContent.Image.Tooltip)) {
                    psb.Builder.Append(inContent.Image.Tooltip).Append("\n\n");
                }
                if (!string.IsNullOrEmpty(inContent.Text)) {
                    psb.Builder.Append(inContent.Text);
                }
                if (psb.Builder.Length > 0) {
                    Services.TTS.Text(psb.Builder.Flush());
                }
            }
        }
    }

    public struct PopupContent {
        public string Header;
        public string Subheader;
        public string Text;
        public Color? HeaderColorOverride;
        public Color? SubheaderColorOverride;
        public Color? TextColorOverride;
        public StreamedImageSet Image;
        public RectTransform CustomModule;
        public StringHash32 CustomLayout;
        public PopupFacts Facts;
        public ListSlice<NamedOption> Options;
    }

    public struct PopupFacts {
        public ListSlice<BFBase> Facts;
        public ListSlice<BFDiscoveredFlags> Flags;
        public Predicate<BFBase> IsNew;
        public BestiaryDesc Reference;

        public PopupFacts(ListSlice<BFBase> facts) {
            Facts = facts;
            Flags = default;
            IsNew = null;
            Reference = null;
        }

        public PopupFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags) {
            Facts = facts;
            Flags = flags;
            IsNew = null;
            Reference = null;
        }
    }

    [Flags]
    public enum PopupFlags {
        ShowCloseButton = 0x01,
        TallImage = 0x02,
        ImageTextBG = 0x04,
        ImageBG = 0x08
    }
}