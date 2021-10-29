using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;
using System;
using BeauUtil.Debugger;
using System.Collections.Generic;

namespace Aqua
{
    public class FactPools : MonoBehaviour
    {
        private enum PoolSource : byte {
            Behavior,
            BehaviorQuantitative,
            Model,
            State,
            Property,
            PropertyHistory,
            Population,
            PopulationHistory
        }

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
        [SerializeField] private BehaviorPool m_BehaviorQuantitativeFacts = null;
        [SerializeField] private ModelPool m_ModelFacts = null;
        [SerializeField] private StatePool m_StateFacts = null;
        [SerializeField] private PropertyPool m_PropertyFacts = null;
        [SerializeField] private PropertyHistoryPool m_PropertyHistoryFacts = null;
        [SerializeField] private PopulationPool m_PopulationFacts = null;
        [SerializeField] private PopulationHistoryPool m_PopulationHistoryFacts = null;

        [SerializeField] private Transform m_TransformPool = null;

        #endregion // Inspector

        [NonSerialized] private bool m_ConfiguredPools;
        [NonSerialized] private Dictionary<MonoBehaviour, PoolSource> m_PoolSources = new Dictionary<MonoBehaviour, PoolSource>(64);

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
            m_BehaviorQuantitativeFacts.ConfigureTransforms(m_TransformPool, null, false);
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
            m_BehaviorQuantitativeFacts.Reset();
            m_ModelFacts.Reset();
            m_StateFacts.Reset();
            m_PropertyFacts.Reset();
            m_PropertyHistoryFacts.Reset();
            m_PopulationFacts.Reset();
            m_PopulationHistoryFacts.Reset();

            m_PoolSources.Clear();
        }

        public MonoBehaviour Alloc(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags, Transform inParent)
        {
            ConfigurePoolTransforms();

            switch(inFact.Type) {
                case BFTypeId.Model: {
                    ModelFactDisplay display = m_ModelFacts.Alloc(inParent);
                    display.Populate((BFModel) inFact);
                    m_PoolSources.Add(display, PoolSource.Model);
                    return display;
                }

                case BFTypeId.State: {
                    StateFactDisplay display = m_StateFacts.Alloc(inParent);
                    display.Populate((BFState) inFact);
                    m_PoolSources.Add(display, PoolSource.State);
                    return display;
                }

                case BFTypeId.WaterProperty: {
                    WaterPropertyFactDisplay display = m_PropertyFacts.Alloc(inParent);
                    display.Populate((BFWaterProperty) inFact);
                    m_PoolSources.Add(display, PoolSource.Property);
                    return display;
                }

                case BFTypeId.WaterPropertyHistory: {
                    WaterPropertyHistoryFactDisplay display = m_PropertyHistoryFacts.Alloc(inParent);
                    display.Populate((BFWaterPropertyHistory) inFact);
                    m_PoolSources.Add(display, PoolSource.PropertyHistory);
                    return display;
                }

                case BFTypeId.Population: {
                    PopulationFactDisplay display = m_PopulationFacts.Alloc(inParent);
                    display.Populate((BFPopulation) inFact);
                    m_PoolSources.Add(display, PoolSource.Population);
                    return display;
                }

                case BFTypeId.PopulationHistory: {
                    PopulationHistoryFactDisplay display = m_PopulationHistoryFacts.Alloc(inParent);
                    display.Populate((BFPopulationHistory) inFact);
                    m_PoolSources.Add(display, PoolSource.PopulationHistory);
                    return display;
                }

                default: {
                    BFBehavior behavior = inFact as BFBehavior;
                    if (behavior != null) {
                        bool isQuantitative = inFact.Type == BFTypeId.Eat && (inFlags & BFDiscoveredFlags.Rate) != 0;
                        if (m_BehaviorQuantitativeFacts.Prefab != null && isQuantitative) {
                            BehaviorFactDisplay display = m_BehaviorQuantitativeFacts.Alloc(inParent);
                            display.Populate(behavior, inReference, inFlags);
                            return display;
                        } else {
                            BehaviorFactDisplay display = m_BehaviorFacts.Alloc(inParent);
                            display.Populate(behavior, inReference, inFlags);
                            return display;
                        }
                    }

                    Assert.Fail("Unable to find suitable fact display for '{0}'", inFact.Type);
                    return null;
                }
            }
        }

        public void Free(MonoBehaviour inDisplay)
        {
            if (!m_PoolSources.TryGetValue(inDisplay, out PoolSource source)) {
                Assert.Fail("Unable to find suitable pool");
            }

            m_PoolSources.Remove(inDisplay);

            switch(source) {
                case PoolSource.Behavior: {
                    TryFree(inDisplay, m_BehaviorFacts);
                    break;
                }

                case PoolSource.BehaviorQuantitative: {
                    TryFree(inDisplay, m_BehaviorQuantitativeFacts);
                    break;
                }

                case PoolSource.Model: {
                    TryFree(inDisplay, m_ModelFacts);
                    break;
                }

                case PoolSource.Population: {
                    TryFree(inDisplay, m_PopulationFacts);
                    break;
                }

                case PoolSource.PopulationHistory: {
                    TryFree(inDisplay, m_PopulationHistoryFacts);
                    break;
                }

                case PoolSource.Property: {
                    TryFree(inDisplay, m_PropertyFacts);
                    break;
                }

                case PoolSource.PropertyHistory: {
                    TryFree(inDisplay, m_PropertyHistoryFacts);
                    break;
                }

                case PoolSource.State: {
                    TryFree(inDisplay, m_StateFacts);
                    break;
                }
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