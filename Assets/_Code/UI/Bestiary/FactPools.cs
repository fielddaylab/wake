using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;
using System;
using BeauUtil.Debugger;

namespace Aqua
{
    public class FactPools : MonoBehaviour
    {
        #region Types

        [Serializable] private class BehaviorPool : SerializablePool<BehaviorFactDisplay> { }
        [Serializable] private class ModelPool : SerializablePool<ModelFactDisplay> { }
        [Serializable] private class StatePool : SerializablePool<StateFactDisplay> { }
        [Serializable] private class PropertyPool : SerializablePool<WaterPropertyFactDisplay> { }
        [Serializable] private class PopulationPool : SerializablePool<PopulationFactDisplay> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private BehaviorPool m_BehaviorFacts = null;
        [SerializeField] private ModelPool m_ModelFacts = null;
        [SerializeField] private StatePool m_StateFacts = null;
        [SerializeField] private PropertyPool m_PropertyFacts = null;
        [SerializeField] private PopulationPool m_PopulationFacts = null;

        [SerializeField] private Transform m_TransformPool = null;

        #endregion // Inspector

        [NonSerialized] private bool m_ConfiguredPools;

        private void Awake()
        {
            ConfigurePoolTransforms();
        }

        private void OnDisable()
        {
            if (!Services.Valid)
                return;
            
            FreeAll();
        }

        private void ConfigurePoolTransforms()
        {
            if (m_ConfiguredPools)
                return;

            m_BehaviorFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_ModelFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_StateFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_PropertyFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_PopulationFacts.ConfigureTransforms(m_TransformPool, null, false);

            m_ConfiguredPools = true;
        }

        public void FreeAll()
        {
            m_BehaviorFacts.Reset();
            m_ModelFacts.Reset();
            m_StateFacts.Reset();
            m_PropertyFacts.Reset();
            m_PopulationFacts.Reset();
        }

        public MonoBehaviour Alloc(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags, Transform inParent)
        {
            ConfigurePoolTransforms();

            BFBehavior behavior = inFact as BFBehavior;
            if (behavior != null)
            {
                BehaviorFactDisplay display = m_BehaviorFacts.Alloc(inParent);
                display.Populate(behavior, inReference, inFlags);
                return display;
            }

            BFModel model = inFact as BFModel;
            if (model != null)
            {
                ModelFactDisplay display = m_ModelFacts.Alloc(inParent);
                display.Populate(model);
                return display;
            }

            BFState state = inFact as BFState;
            if (state != null)
            {
                StateFactDisplay display = m_StateFacts.Alloc(inParent);
                display.Populate(state);
                return display;
            }

            BFWaterProperty waterProp = inFact as BFWaterProperty;
            if (waterProp != null)
            {
                WaterPropertyFactDisplay display = m_PropertyFacts.Alloc(inParent);
                display.Populate(waterProp);
                return display;
            }

            BFPopulation populationProp = inFact as BFPopulation;
            if (populationProp != null)
            {
                PopulationFactDisplay display = m_PopulationFacts.Alloc(inParent);
                display.Populate(populationProp);
                return display;
            }

            Assert.Fail("Unable to find suitable fact");
            return null;
        }

        public void Free(MonoBehaviour inDisplay)
        {
            if (!TryFree(inDisplay, m_BehaviorFacts)
                && !TryFree(inDisplay, m_ModelFacts)
                && !TryFree(inDisplay, m_StateFacts)
                && !TryFree(inDisplay, m_PopulationFacts)
                && !TryFree(inDisplay, m_PropertyFacts))
            {
                Assert.Fail("Unable to find suitable pool");
            }
        }

        static private bool TryFree<T>(MonoBehaviour inBehavior, IPool<T> inPool) where T : MonoBehaviour
        {
            T asType = inBehavior as T;
            if (asType != null)
            {
                inPool.Free(asType);
                return true;
            }

            return false;
        }
    }
}