using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;
using System;
using BeauUtil.Debugger;
using System.Collections.Generic;
using System.Collections;

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
            InitializeAllPools();
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

        private void InitializeAllPools() {
            if (m_BehaviorFacts.Prefab != null) {
                m_BehaviorFacts.TryInitialize();
            }

            if (m_ModelFacts.Prefab != null) {
                m_ModelFacts.TryInitialize();
            }

            if (m_StateFacts.Prefab != null) {
                m_StateFacts.TryInitialize();
            }

            if (m_PropertyFacts.Prefab != null) {
                m_PropertyFacts.TryInitialize();
            }

            if (m_PropertyHistoryFacts.Prefab != null) {
                m_PropertyHistoryFacts.TryInitialize();
            }

            if (m_PopulationFacts.Prefab != null) {
                m_PopulationFacts.TryInitialize();
            }

            if (m_PopulationHistoryFacts.Prefab != null) {
                m_PopulationHistoryFacts.TryInitialize();
            }
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

        public MonoBehaviour Alloc(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference, Transform inParent)
        {
            ConfigurePoolTransforms();

            switch(BFType.Shape(inFact)) {
                case BFShapeId.Model: {
                    ModelFactDisplay display = m_ModelFacts.Alloc(inParent);
                    display.Populate((BFModel) inFact);
                    return display;
                }

                case BFShapeId.State: {
                    StateFactDisplay display = m_StateFacts.Alloc(inParent);
                    display.Populate((BFState) inFact, inFlags);
                    return display;
                }

                case BFShapeId.WaterProperty: {
                    WaterPropertyFactDisplay display = m_PropertyFacts.Alloc(inParent);
                    display.Populate((BFWaterProperty) inFact);
                    return display;
                }

                case BFShapeId.WaterPropertyHistory: {
                    WaterPropertyHistoryFactDisplay display = m_PropertyHistoryFacts.Alloc(inParent);
                    display.Populate((BFWaterPropertyHistory) inFact);
                    return display;
                }

                case BFShapeId.Population: {
                    PopulationFactDisplay display = m_PopulationFacts.Alloc(inParent);
                    display.Populate((BFPopulation) inFact);
                    return display;
                }

                case BFShapeId.PopulationHistory: {
                    PopulationHistoryFactDisplay display = m_PopulationHistoryFacts.Alloc(inParent);
                    display.Populate((BFPopulationHistory) inFact);
                    return display;
                }

                case BFShapeId.Behavior: {
                    BFBehavior behavior = (BFBehavior) inFact;
                    BehaviorFactDisplay display = m_BehaviorFacts.Alloc(inParent);
                    display.Populate(behavior, inFlags, inReference);
                    return display;
                }

                default: {
                    Assert.Fail("Unable to find suitable fact display for '{0}'", inFact.Type);
                    return null;
                }
            }
        }

        public MonoBehaviour Alloc(BFTypeId inFactType, BFDiscoveredFlags inFlags, Transform inParent)
        {
            return Alloc(BFType.Shape(inFactType), inFlags, inParent);
        }

        public MonoBehaviour Alloc(BFShapeId inShape, BFDiscoveredFlags inFlags, Transform inParent)
        {
            ConfigurePoolTransforms();

            switch(inShape) {
                case BFShapeId.Model: {
                    ModelFactDisplay display = m_ModelFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.State: {
                    StateFactDisplay display = m_StateFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.WaterProperty: {
                    WaterPropertyFactDisplay display = m_PropertyFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.WaterPropertyHistory: {
                    WaterPropertyHistoryFactDisplay display = m_PropertyHistoryFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.Population: {
                    PopulationFactDisplay display = m_PopulationFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.PopulationHistory: {
                    PopulationHistoryFactDisplay display = m_PopulationHistoryFacts.Alloc(inParent);
                    return display;
                }

                case BFShapeId.Behavior: {
                    BehaviorFactDisplay display = m_BehaviorFacts.Alloc(inParent);
                    return display;
                }

                default: {
                    Assert.Fail("Unable to find suitable fact display for '{0}'", inShape);
                    return null;
                }
            }
        }

        public void Free(MonoBehaviour inDisplay)
        {
            if (!Pool.TryFree(inDisplay)) {
                Assert.Fail("Unable to find suitable pool");
            }
        }

        static public void Populate(MonoBehaviour inBehavior, BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference) {
            switch(BFType.Shape(inFact)){
                case BFShapeId.Model: {
                    ((ModelFactDisplay) inBehavior).Populate((BFModel) inFact);
                    break;
                }

                case BFShapeId.State: {
                    ((StateFactDisplay) inBehavior).Populate((BFState) inFact, inFlags);
                    break;
                }

                case BFShapeId.WaterProperty: {
                    ((WaterPropertyFactDisplay) inBehavior).Populate((BFWaterProperty) inFact);
                    break;
                }

                case BFShapeId.WaterPropertyHistory: {
                    ((WaterPropertyHistoryFactDisplay) inBehavior).Populate((BFWaterPropertyHistory) inFact);
                    break;
                }

                case BFShapeId.Population: {
                    ((PopulationFactDisplay) inBehavior).Populate((BFPopulation) inFact);
                    break;
                }

                case BFShapeId.PopulationHistory: {
                    ((PopulationHistoryFactDisplay) inBehavior).Populate((BFPopulationHistory) inFact);
                    break;
                }

                default: {
                    if (BFType.IsBehavior(inFact.Type)) {
                        ((BehaviorFactDisplay) inBehavior).Populate((BFBehavior) inFact, inFlags, inReference);
                        return;
                    }

                    Assert.Fail("Unable to populate fact type '{0}'", inFact.Id);
                    break;
                }
            }
        }
    }
}