using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using BeauPools;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class BestiaryAddPanel : BasePanel, ISceneOptimizable
    {
        #region Types

        [Serializable] private class BestiaryButtonPool : SerializablePool<BestiarySelectButton> { }

        #endregion // Types

        #region Inspector

        [Header("Capacity")]
        [SerializeField, Range(1, 8)] private int m_MaxAllowed = 1;
        [SerializeField] private TickDisplay m_CurrentDisplay = null;

        [Header("List")]
        [SerializeField, AutoEnum] private BestiaryDescCategory m_Category = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_IgnoreFlags = 0;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private BestiaryButtonPool m_ButtonPool = null;
        [SerializeField] private RectTransformPool m_EmptySlotPool = null;
        [SerializeField] private int m_MinIcons = 30;
        [SerializeField] private int m_PerRow = 0;

        #endregion // Inspector

        [NonSerialized] private bool m_NeedsRebuild = true;
        [NonSerialized] private int m_SelectedCount;
        private readonly HashSet<BestiaryDesc> m_SelectedSet = new HashSet<BestiaryDesc>();
        private BestiarySelectButton.ToggleDelegate m_ToggleDelegate;

        public Predicate<BestiaryDesc> Filter;
        public Action<BestiaryDesc> OnAdded;
        public Action<BestiaryDesc> OnRemoved;
        public Action OnCleared;

        #region Unity Events

        protected override void Awake()
        {
            base.Awake();

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, InvalidateListFromBestiaryUpdate, this)
                .Register(GameEvents.ProfileRefresh, InvalidateListAndClearSet, this);
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
                PopulateCritters();
                m_NeedsRebuild = false;
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
            return CanvasGroup.Show(0.2f, null);
        }

        protected override IEnumerator TransitionToHide()
        {
            return CanvasGroup.Hide(0.2f, null);
        }

        #endregion // BasePanel

        #region Selected Set

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
                PopulateCritters();
                m_NeedsRebuild = false;
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
                return true;
            }

            return false;
        }

        #endregion // Selected Set

        #region Population

        private void PopulateCritters()
        {
            using(PooledList<BestiaryDesc> availableCritters = PooledList<BestiaryDesc>.Create())
            {
                CollectEntities(Save.Bestiary, m_Category, m_IgnoreFlags, Filter, availableCritters);
                availableCritters.Sort(BestiaryDesc.SortByEnvironment);

                PopulateCritters(availableCritters);
            }
        }

        private void PopulateCritters(ICollection<BestiaryDesc> inCritters)
        {
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
                button.Toggle.group = m_ToggleGroup;
                button.Icon.sprite = critter.Icon();
                button.Tooltip.TooltipId = critter.CommonName();
                button.Label.SetText(critter.CommonName());
                button.Critter = critter;
                button.OnToggle = m_ToggleDelegate ?? (m_ToggleDelegate = OnToggleSelected);
            }

            while(emptyCount-- > 0)
            {
                m_EmptySlotPool.Alloc();
            }
        }

        private void OnToggleSelected(BestiaryDesc inCritter, bool inbOn)
        {
            if (inbOn && m_SelectedSet.Add(inCritter))
            {
                m_SelectedCount++;
                if (m_CurrentDisplay) {
                    m_CurrentDisplay.Display(m_SelectedCount);
                }

                if (m_MaxAllowed > 1 && m_SelectedCount == m_MaxAllowed)
                {
                    SetAtCapacityMode(true);
                }

                OnAdded?.Invoke(inCritter);
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
            }
        }

        private void SetAtCapacityMode(bool inbAtCapacity)
        {
            foreach(var button in m_ButtonPool.ActiveObjects)
            {
                button.Toggle.interactable = !inbAtCapacity || button.Toggle.isOn;
            }
        }

        #endregion // Population

        #region ISceneOptimizable

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            if (m_CurrentDisplay) {
                m_CurrentDisplay.Display(0);
            }
            
            if (m_MaxAllowed > 1)
                m_ToggleGroup = null;
        }

        #endif // UNITY_EDITOR

        #endregion // ISceneOptimizable

        static private void CollectEntities(BestiaryData inSaveData, BestiaryDescCategory inCategory, BestiaryDescFlags inIgnore, Predicate<BestiaryDesc> inFilter, ICollection<BestiaryDesc> outCritters)
        {
            foreach(var entity in inSaveData.GetEntities(inCategory))
            {
                if (entity.HasFlags(inIgnore) || (inFilter != null && !inFilter(entity)))
                    continue;

                outCritters.Add(entity);
            }
        }
    }
}