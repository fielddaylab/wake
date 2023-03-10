using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class BestiaryAddPanel : BasePanel, IBaked
    {
        #region Types

        [Serializable] private class BestiaryButtonPool : SerializablePool<BestiarySelectButton> { }

        #endregion // Types

        #region Inspector

        [Header("Capacity")]
        [SerializeField, Range(1, 32)] private int m_MaxAllowed = 1;
        [SerializeField] private TickDisplay m_CurrentDisplay = null;

        [Header("List")]
        [SerializeField, AutoEnum] private BestiaryDescCategory m_Category = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_IgnoreFlags = 0;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private ScrollRect m_ScrollLayout = null;
        [SerializeField] private LayoutGroup m_ScrollLayoutGroup = null;
        [SerializeField] private CanvasGroup m_ScrollCanvasGroup = null;
        [SerializeField] private BestiaryButtonPool m_ButtonPool = null;
        [SerializeField] private RectTransformPool m_EmptySlotPool = null;
        [SerializeField] private int m_MinIcons = 30;
        [SerializeField] private int m_PerRow = 0;
        [SerializeField] private float m_AnimateIntervalMultiplier = 0.2f;

        #endregion // Inspector

        [NonSerialized] private bool m_NeedsRebuild = true;
        [NonSerialized] private int m_SelectedCount;
        private readonly HashSet<BestiaryDesc> m_SelectedSet = Collections.NewSet<BestiaryDesc>(4);
        private BestiarySelectButton.ToggleDelegate m_ToggleDelegate;
        private Routine m_PopulateRoutine;

        public Predicate<BestiaryDesc> Filter;
        public Action<BestiaryDesc> OnAdded;
        public Action<BestiaryDesc> OnRemoved;
        public Action OnCleared;
        public Action OnUpdated;

        public Predicate<BestiaryDesc> HighlightFilter;
        public Predicate<BestiaryDesc> MarkerFilter;
        public Predicate<BestiaryDesc> HistoryFilter;
        public Func<BestiaryDesc, Color> ColorFilter;

        #region Unity Events

        protected override void Awake()
        {
            base.Awake();

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, InvalidateListFromBestiaryUpdate, this)
                .Register(GameEvents.ProfileRefresh, InvalidateListAndClearSet, this)
                .Register(GameEvents.JobSwitched, InvalidateList, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);
            if (m_NeedsRebuild)
            {
                m_PopulateRoutine.Replace(this, PopulateCritters());
            }
        }

        protected override void InstantTransitionToShow()
        {
            CanvasGroup.Show(null);
        }

        protected override void InstantTransitionToHide()
        {
            CanvasGroup.Hide(null);
        }

        protected override IEnumerator TransitionToShow()
        {
            CanvasGroup.alpha = 0;
            CanvasGroup.gameObject.SetActive(true);
            yield return null;
            yield return m_PopulateRoutine;
            AnimateButtons(0.15f);
            yield return CanvasGroup.Show(0.2f, null);
        }

        protected override IEnumerator TransitionToHide()
        {
            return CanvasGroup.Hide(0.2f, null);
        }

        #endregion // BasePanel

        #region Selected Set

        public BestiaryDescCategory Category
        {
            get { return m_Category; }
        }

        public IReadOnlyCollection<BestiaryDesc> Selected
        {
            get { return m_SelectedSet; }
        }

        public bool IsSelected(BestiaryDesc inEntry)
        {
            return m_SelectedSet.Contains(inEntry);
        }

        public void ClearSelection()
        {
            if (ClearSelectedSet())
            {
                foreach(var button in m_ButtonPool.ActiveObjects)
                {
                    button.Toggle.SetIsOnWithoutNotify(false);
                    button.Toggle.interactable = true;
                }
            }
        }

        private void InvalidateListFromBestiaryUpdate(BestiaryUpdateParams inUpdate)
        {
            switch(inUpdate.Type)
            {
                case BestiaryUpdateParams.UpdateType.Entity:
                    InvalidateList();
                    break;

                case BestiaryUpdateParams.UpdateType.RemovedEntity:
                case BestiaryUpdateParams.UpdateType.Unknown:
                    InvalidateListAndClearSet();
                    break;

                case BestiaryUpdateParams.UpdateType.Fact:{
                    BFBase fact = Assets.Fact(inUpdate.Id);
                    if (fact.Type == BFTypeId.State && (MarkerFilter != null || ColorFilter != null)) {
                        InvalidateList();
                    }
                    break;
                }
            }
        }

        private void InvalidateListAndClearSet()
        {
            ClearSelectedSet();
            InvalidateList();
        }

        private void InvalidateList()
        {
            if (IsShowing())
            {
                m_PopulateRoutine.Replace(this, PopulateCritters());
            }
            else
            {
                m_NeedsRebuild = true;
            }
        }

        private bool ClearSelectedSet()
        {
            if (m_SelectedCount > 0)
            {
                m_SelectedCount = 0;
                m_SelectedSet.Clear();
                if (m_CurrentDisplay) {
                    m_CurrentDisplay.Display(0);
                }
                OnCleared?.Invoke();
                OnUpdated?.Invoke();
                return true;
            }

            return false;
        }

        #endregion // Selected Set

        #region Population

        public void Refresh()
        {
            InvalidateList();
        }

        private IEnumerator PopulateCritters()
        {
            using(PooledList<BestiaryDesc> availableCritters = PooledList<BestiaryDesc>.Create())
            {
                CollectEntities(Save.Bestiary, m_Category, m_IgnoreFlags, Filter, availableCritters);
                yield return null;
                
                availableCritters.Sort(BestiaryDesc.SortByEnvironment);

                yield return Routine.Amortize(PopulateCritters(availableCritters), 6);
            }
        }

        private IEnumerator PopulateCritters(ICollection<BestiaryDesc> inCritters)
        {
            Vector2 prevScroll = m_ScrollLayout.normalizedPosition;
            m_ScrollLayout.enabled = false;
            m_ScrollLayoutGroup.enabled = false;
            m_ScrollCanvasGroup.alpha = 0;
            m_ScrollCanvasGroup.blocksRaycasts = false;

            int critterCount = inCritters.Count;
            int emptyCount;
            if (critterCount <= m_MinIcons)
            {
                emptyCount = m_MinIcons - critterCount;
            }
            else if (m_PerRow > 0)
            {
                int onRow = critterCount % m_PerRow;
                emptyCount = (m_PerRow - onRow) % m_PerRow;
            }
            else
            {
                emptyCount = 0;
            }

            m_EmptySlotPool.Reset();
            m_ButtonPool.Reset();

            bool bIsAtCapacity = m_MaxAllowed > 1 && m_SelectedCount >= m_MaxAllowed;

            BestiarySelectButton button;
            foreach(var critter in inCritters)
            {
                button = m_ButtonPool.Alloc();
                if (m_SelectedSet.Contains(critter))
                {
                    button.Toggle.SetIsOnWithoutNotify(true);
                    button.Toggle.interactable = true;
                }
                else
                {
                    button.Toggle.SetIsOnWithoutNotify(false);
                    button.Toggle.interactable = !bIsAtCapacity;
                }

                string name = BestiaryUtils.FullLabel(critter);
                button.Toggle.group = m_ToggleGroup;
                button.Icon.sprite = critter.Icon();
                button.Tooltip.TooltipOverride = name;
                button.Label.SetTextFromString(name);
                button.Critter = critter;
                button.OnToggle = m_ToggleDelegate ?? (m_ToggleDelegate = OnToggleSelected);
                
                bool highlight = HighlightFilter != null && HighlightFilter(critter);
                bool marker = MarkerFilter != null && MarkerFilter(critter);
                bool history = HistoryFilter != null && HistoryFilter(critter);

                button.Highlight.SetActive(highlight);
                button.Marker.gameObject.SetActive(marker);
                button.Historical.SetActive(history);

                if (ColorFilter != null) {
                    button.Color.Color = ColorFilter(critter);
                } else {
                    button.Color.Color = Color.white;
                }

                if (marker) {
                    if (highlight) {
                        button.Marker.SetAnchorPos(-14, Axis.Y);
                    } else {
                        button.Marker.SetAnchorPos(0, Axis.Y);
                    }
                }

                yield return null;
            }

            while(emptyCount-- > 0)
            {
                m_EmptySlotPool.Alloc();
                yield return null;
            }

            m_ScrollCanvasGroup.alpha = 1;
            m_ScrollCanvasGroup.blocksRaycasts = true;
            m_ScrollLayout.normalizedPosition = prevScroll;
            m_ScrollLayout.enabled = true;
            m_ScrollLayoutGroup.enabled = true;
            m_NeedsRebuild = false;
        }

        private void OnToggleSelected(BestiaryDesc inCritter, BestiarySelectButton inButton, bool inbOn)
        {
            if (inbOn && m_SelectedSet.Add(inCritter))
            {
                m_SelectedCount++;
                if (m_CurrentDisplay) {
                    m_CurrentDisplay.Display(m_SelectedCount);
                }

                inButton.Flash.Ping();

                if (m_MaxAllowed > 1 && m_SelectedCount == m_MaxAllowed)
                {
                    SetAtCapacityMode(true);
                }

                OnAdded?.Invoke(inCritter);
                OnUpdated?.Invoke();
            }
            else if (!inbOn && m_SelectedSet.Remove(inCritter))
            {
                if (m_MaxAllowed > 1 && m_SelectedCount == m_MaxAllowed)
                {
                    SetAtCapacityMode(false);
                }

                m_SelectedCount--;
                if (m_CurrentDisplay) {
                    m_CurrentDisplay.Display(m_SelectedCount);
                }

                OnRemoved?.Invoke(inCritter);
                OnUpdated?.Invoke();
            }
        }

        private void SetAtCapacityMode(bool inbAtCapacity)
        {
            foreach(var button in m_ButtonPool.ActiveObjects)
            {
                button.Toggle.interactable = !inbAtCapacity || button.Toggle.isOn;
            }
        }

        private void AnimateButtons(float delay) {
            foreach(var anim in m_ButtonPool.ActiveObjects) {
                if (m_ScrollLayout.IsVisible((RectTransform) anim.transform)) {
                    delay += anim.Anim.Ping(delay) * m_AnimateIntervalMultiplier;
                }
            }
            foreach(var anim in m_EmptySlotPool.ActiveObjects) {
                if (m_ScrollLayout.IsVisible(anim)) {
                    delay += anim.GetComponent<AppearAnim>().Ping(delay) * m_AnimateIntervalMultiplier;
                }
            }
        }

        #endregion // Population

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            if (m_CurrentDisplay) {
                m_CurrentDisplay.Display(0);
            }
            
            if (m_MaxAllowed > 1)
                m_ToggleGroup = null;

            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked

        static private void CollectEntities(BestiaryData inSaveData, BestiaryDescCategory inCategory, BestiaryDescFlags inIgnore, Predicate<BestiaryDesc> inFilter, ICollection<BestiaryDesc> outCritters)
        {
            bool fullyDecrypted = Save.Science.FullyDecrypted();
            foreach(var entity in inSaveData.GetEntities(inCategory))
            {
                if (entity.HasFlags(inIgnore) || (inFilter != null && !inFilter(entity)))
                    continue;

                if (entity.HasFlags(BestiaryDescFlags.IsSpecter) && !fullyDecrypted)
                    continue;

                outCritters.Add(entity);
            }
        }
    }
}