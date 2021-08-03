using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Cameras;
using Aqua.Profile;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class BestiaryAddPanel : BasePanel, ISceneOptimizable
    {
        #region Types

        [Serializable] private class BestiaryButtonPool : SerializablePool<BestiaryButton> { }

        #endregion // Types

        #region Inspector

        [Header("Capacity")]
        [SerializeField, Range(1, 8)] private int m_MaxAllowed = 1;
        [SerializeField] private TMP_Text m_CurrentText = null;
        [SerializeField] private TMP_Text m_MaxText = null;

        [Header("List")]
        [SerializeField, AutoEnum] private BestiaryDescCategory m_Category = BestiaryDescCategory.Critter;
        [SerializeField] private GridLayoutGroup m_Layout = null;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private BestiaryButtonPool m_ButtonPool = null;
        [SerializeField] private RectTransformPool m_EmptySlotPool = null;
        [SerializeField] private int m_RowsToFill = 3;

        #endregion // Inspector

        [NonSerialized] private bool m_NeedsRebuild = true;
        [NonSerialized] private int m_SelectedCount;
        private readonly HashSet<BestiaryDesc> m_SelectedSet = new HashSet<BestiaryDesc>();
        private BestiaryButton.ToggleDelegate m_ToggleDelegate;

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

        #endregion // BasePanel

        #region Selected Set

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
                m_CurrentText.SetText("0");
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
                CollectCritters(Services.Data.Profile.Bestiary, m_Category, availableCritters);
                availableCritters.Sort(BestiaryDesc.SortByEnvironment);

                PopulateCritters(availableCritters);
            }
        }

        private void PopulateCritters(ICollection<BestiaryDesc> inCritters)
        {
            int critterCount = inCritters.Count;
            int maxPerRow = m_Layout.constraintCount;
            int minIcons = m_RowsToFill * maxPerRow;
            int emptyCount;
            if (critterCount <= minIcons)
            {
                emptyCount = minIcons - critterCount;
            }
            else
            {
                int onRow = critterCount % maxPerRow;
                if (onRow > 0)
                {
                    emptyCount = maxPerRow - onRow;
                }
                else
                {
                    emptyCount = 0;
                }
                emptyCount = maxPerRow - (critterCount - 1) % maxPerRow;
            }

            m_EmptySlotPool.Reset();
            m_ButtonPool.Reset();

            bool bIsAtCapacity = m_MaxAllowed > 1 && m_SelectedCount >= m_MaxAllowed;

            BestiaryButton button;
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
                m_CurrentText.SetText(m_SelectedCount.ToStringLookup());

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
                m_CurrentText.SetText(m_SelectedCount.ToStringLookup());

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
            m_CurrentText.SetText("0");
            m_MaxText.SetText(m_MaxAllowed.ToStringLookup());

            if (m_MaxAllowed > 1)
                m_ToggleGroup = null;
        }

        #endif // UNITY_EDITOR

        #endregion // ISceneOptimizable

        static private void CollectCritters(BestiaryData inSaveData, BestiaryDescCategory inCategory, ICollection<BestiaryDesc> outCritters)
        {
            foreach(var critter in inSaveData.GetEntities(inCategory))
            {
                if (critter.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation))
                    continue;

                outCritters.Add(critter);
            }
        }
    }
}