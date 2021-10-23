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
        [Serializable] private class PropertyHistoryPool : SerializablePool<WaterPropertyHistoryFactDisplay> { }
        [Serializable] private class PopulationPool : SerializablePool<PopulationFactDisplay> { }
        [Serializable] private class PopulationHistoryPool : SerializablePool<PopulationHistoryFactDisplay> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private BehaviorPool m_BehaviorFacts = null;
        [SerializeField] private ModelPool m_ModelFacts = null;
        [SerializeField] private StatePool m_StateFacts = null;
        [SerializeField] private PropertyPool m_PropertyFacts = null;
        [SerializeField] private PropertyHistoryPool m_PropertyHistoryFacts = null;
        [SerializeField] private PopulationPool m_PopulationFacts = null;
        [SerializeField] private PopulationHistoryPool m_PopulationHistoryFacts = null;

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
            m_PropertyHistoryFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_PopulationFacts.ConfigureTransforms(m_TransformPool, null, false);
            m_PopulationHistoryFacts.ConfigureTransforms(m_TransformPool, null, false);

            m_ConfiguredPools = true;
        }

        public void FreeAll()
        {
            m_BehaviorFacts.Reset();
            m_ModelFacts.Reset();
            m_StateFacts.Reset();
            m_PropertyFacts.Reset();
            m_PropertyHistoryFacts.Reset();
            m_PopulationFacts.Reset();
            m_PopulationHistoryFacts.Reset();
        }

        public MonoBehaviour Alloc(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags, Transform inParent)
        {
            ConfigurePoolTransforms();

            switch(inFact.Type) {
                case BFTypeId.Model: {
                    ModelFactDisplay display = m_ModelFacts.Alloc(inParent);
                    display.Populate((BFModel) inFact);
                    return display;
                }

                case BFTypeId.State: {
                    StateFactDisplay display = m_StateFacts.Alloc(inParent);
                    display.Populate((BFState) inFact);
                    return display;
                }

                case BFTypeId.WaterProperty: {
                    WaterPropertyFactDisplay display = m_PropertyFacts.Alloc(inParent);
                    display.Populate((BFWaterProperty) inFact);
                    return display;
                }

                case BFTypeId.WaterPropertyHistory: {
                    WaterPropertyHistoryFactDisplay display = m_PropertyHistoryFacts.Alloc(inParent);
                    display.Populate((BFWaterPropertyHistory) inFact);
                    return display;
                }

                case BFTypeId.Population: {
                    PopulationFactDisplay display = m_PopulationFacts.Alloc(inParent);
                    display.Populate((BFPopulation) inFact);
                    return display;
                }

                case BFTypeId.PopulationHistory: {
                    PopulationHistoryFactDisplay display = m_PopulationHistoryFacts.Alloc(inParent);
                    display.Populate((BFPopulationHistory) inFact);
                    return display;
                }

                default: {
                    BFBehavior behavior = inFact as BFBehavior;
                    if (behavior != null) {
                        BehaviorFactDisplay display = m_BehaviorFacts.Alloc(inParent);
                        display.Populate(behavior, inReference, inFlags);
                        return display;
                    }

                    Assert.Fail("Unable to find suitable fact display for '{0}'", inFact.Type);
                    return null;
                }
            }
        }

        public void Free(MonoBehaviour inDisplay)
        {
            if (!TryFree(inDisplay, m_BehaviorFacts)
                && !TryFree(inDisplay, m_ModelFacts)
                && !TryFree(inDisplay, m_StateFacts)
                && !TryFree(inDisplay, m_PopulationFacts)
                && !TryFree(inDisplay, m_PropertyFacts)
                && !TryFree(inDisplay, m_PropertyHistoryFacts)
                && !TryFree(inDisplay, m_PopulationHistoryFacts))
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