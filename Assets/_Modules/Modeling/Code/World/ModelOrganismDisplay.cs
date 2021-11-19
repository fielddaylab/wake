using System;
using BeauPools;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelOrganismDisplay : MonoBehaviour, IPoolAllocHandler {

        #region Inspector

        public RectTransform Transform;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector

        [NonSerialized] public BestiaryDesc Organism;
        [NonSerialized] public uint Population;
        [NonSerialized] public ActorStateId State;

        [NonSerialized] public int Index;

        public void Initialize(BestiaryDesc organism, int index) {
            Organism = organism;
            State = ActorStateId.Alive;
            m_Icon.sprite = organism.Icon();
            m_Label.SetText(organism.CommonName());
            Index = index;
        }

        void IPoolAllocHandler.OnAlloc() { }

        void IPoolAllocHandler.OnFree() {
            Organism = null;
        }
    }
}