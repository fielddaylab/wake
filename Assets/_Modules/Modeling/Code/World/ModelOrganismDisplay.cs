using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelOrganismDisplay : MonoBehaviour, IPoolAllocHandler {

        public struct AddRemoveResult {
            public bool CanAdd;
            public bool CanRemove;
            public int DifferenceValue;
        }

        public delegate AddRemoveResult OnAddRemoveDelegate(BestiaryDesc organism, int sign);

        #region Inspector

        public RectTransform Transform;
        public CanvasGroup CanvasGroup;

        [SerializeField] private Image m_Icon = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Graphic m_Background = null;

        [Header("Intervention")]
        [SerializeField] private CanvasGroup m_InterveneGroup = null;
        [SerializeField] private Button m_InterveneAddButton = null;
        [SerializeField] private TMP_Text m_InterveneAddLabel = null;
        [SerializeField] private Button m_InterveneRemoveButton = null;
        [SerializeField] private TMP_Text m_InterveneRemoveLabel = null;

        #endregion // Inspector

        [NonSerialized] public BestiaryDesc Organism;
        [NonSerialized] public uint Population;
        [NonSerialized] public ActorStateId State;

        [NonSerialized] public int Index;
        [NonSerialized] public WorldFilterMask Mask;

        private OnAddRemoveDelegate m_OnAddRemove;

        private void Awake() {
            m_InterveneAddButton.onClick.AddListener(() => {
                if (m_OnAddRemove != null) {
                    UpdateAddRemove(m_OnAddRemove(Organism, 1));
                }
            });

            m_InterveneRemoveButton.onClick.AddListener(() => {
                if (m_OnAddRemove != null) {
                    UpdateAddRemove(m_OnAddRemove(Organism, -1));
                }
            });
        }

        public void Initialize(BestiaryDesc organism, int index, OnAddRemoveDelegate onAddRemove, WorldFilterMask mask) {
            Organism = organism;
            State = ActorStateId.Alive;
            m_Icon.sprite = organism.Icon();
            m_Label.SetText(organism.CommonName());
            Index = index;
            Mask = mask | WorldFilterMask.Organism;
            m_OnAddRemove = onAddRemove;
            m_Background.SetColor(organism.Color() * 0.5f);
        }

        public void EnableIntervention(AddRemoveResult result) {
            m_InterveneGroup.gameObject.SetActive(true);
            UpdateAddRemove(result);
        }

        public void DisableIntervention() {
            m_InterveneGroup.gameObject.SetActive(false);
        }

        private void UpdateAddRemove(AddRemoveResult result) {
            m_InterveneAddButton.interactable = result.CanAdd;
            m_InterveneRemoveButton.interactable = result.CanRemove;

            m_InterveneAddLabel.gameObject.SetActive(result.DifferenceValue > 0);
            m_InterveneRemoveLabel.gameObject.SetActive(result.DifferenceValue < 0);

            if (result.DifferenceValue > 0) {
                string label = BestiaryUtils.FormatPopulation(Organism, (uint) result.DifferenceValue, "+");
                m_InterveneAddLabel.SetText(label);
            } else if (result.DifferenceValue < 0) {
                string label = BestiaryUtils.FormatPopulation(Organism, (uint) (-result.DifferenceValue), "-");
                m_InterveneRemoveLabel.SetText(label);
            }
        }

        void IPoolAllocHandler.OnAlloc() { }

        void IPoolAllocHandler.OnFree() {
            Organism = null;
            m_InterveneGroup.gameObject.SetActive(false);
        }
    }
}