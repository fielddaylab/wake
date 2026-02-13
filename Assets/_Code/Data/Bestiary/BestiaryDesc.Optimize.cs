#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
#endif // UNITY_EDITOR
using ScriptableBake;

namespace Aqua
{
    public partial class BestiaryDesc : DBObject, IBaked, IEditorOnlyData
    {
        #if UNITY_EDITOR

        #region Optimize

        int IBaked.Order { get { return (int) m_Type; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            FindAllFacts();

            if ((m_Flags & BestiaryDescFlags.IsSpecter) != 0) {
                m_Flags |= BestiaryDescFlags.DoNotUseInExperimentation;
            }
            if ((m_Flags & BestiaryDescFlags.IsMicroscopic) != 0) {
                m_Flags |= BestiaryDescFlags.TreatAsHerd;
            }

            foreach(var fact in m_Facts)
            {
                Assert.NotNull(fact, "Null fact on BestiaryDesc '{0}'", name);
                fact.BakeProperties(this);
            }

            if (m_StationId.IsEmpty) {
                m_StationSortingOrder = -1;
            } else {
                MapDesc map = ValidationUtils.FindAsset<MapDesc>(m_StationId.ToDebugString());
                Assert.NotNull(map, "Map with id '{0}' was unable to be found on BestiaryDesc '{1}'", m_StationId, name);
                m_StationSortingOrder = map.SortingOrder();
            }

            switch(m_Type)
            {
                case BestiaryDescCategory.Environment:
                    {
                        HashSet<StringHash32> populationSet = new HashSet<StringHash32>();
                        m_EnvState = ValidationUtils.FindAsset<WaterPropertyDB>().DefaultValues();
                        foreach(var fact in m_Facts)
                        {
                            switch(fact.Type)
                            {
                                case BFTypeId.WaterProperty:
                                {
                                    BFWaterProperty waterProp = (BFWaterProperty) fact;
                                    m_EnvState[waterProp.Property] = waterProp.Value;
                                    break;
                                }

                                case BFTypeId.Population:
                                {
                                    BFPopulation population = (BFPopulation) fact;
                                    populationSet.Add(population.Critter.Id());
                                    break;
                                }

                                case BFTypeId.PopulationHistory:
                                {
                                    BFPopulationHistory population = (BFPopulationHistory) fact;
                                    populationSet.Add(population.Critter.Id());
                                    break;
                                }

                                case BFTypeId.Sim:
                                {
                                    BFSim sim = (BFSim) fact;
                                    m_HistoricalRecordDuration = sim.SyncTickCount;
                                    break;
                                }
                            }

                            m_InhabitingOrganisms = new StringHash32[populationSet.Count];
                            populationSet.CopyTo(m_InhabitingOrganisms);
                        }
                        break;
                    }

                case BestiaryDescCategory.Critter:
                    {
                        m_StateTransitions.Reset();
                        m_FirstStressFactId = null;
                        bool hasTempStress = false;
                        bool hasPHStress = false;
                        bool hasLightStress = false;
                        foreach(var fact in m_Facts)
                        {
                            if (fact.Type == BFTypeId.State)
                            {
                                BFState state = (BFState) fact;
                                m_StateTransitions[state.Property] = state.Range;
                                if (m_FirstStressFactId.IsEmpty) {
                                    m_FirstStressFactId = fact.Id;
                                }
                                switch(state.Property) {
                                    case WaterPropertyId.Temperature:
                                        hasTempStress = true;
                                        break;
                                    case WaterPropertyId.Light:
                                        hasLightStress = true;
                                        break;
                                    case WaterPropertyId.PH:
                                        hasPHStress = true;
                                        break;
                                }
                            }
                        }

                        if (!name.StartsWith("[")) {
                            if (CanShowUpInStressTank(this)) {
                                Assert.True(!m_FirstStressFactId.IsEmpty, "Critter '{0}' can be added to stress tank but has no stress facts", name);
                                Assert.True(hasTempStress && hasPHStress && hasLightStress, "Critter '{0}' is missing a stress rule", name);
                                Assert.True(CanHitRangeInStressTank(m_StateTransitions.Light, WaterPropertyId.Light), "Critter '{0}' has light ranges that cannot be hit in the stress tank", name);
                                Assert.True(CanHitRangeInStressTank(m_StateTransitions.Temperature, WaterPropertyId.Temperature), "Critter '{0}' has temperature ranges that cannot be hit in the stress tank", name);
                                Assert.True(CanHitRangeInStressTank(m_StateTransitions.PH, WaterPropertyId.PH), "Critter '{0}' has PH ranges that cannot be hit in the stress tank", name);
                            }
                        }
                        break;
                    }
            }

            return true;
        }

        static private bool CanHitRangeInStressTank(ActorStateTransitionRange range, WaterPropertyId propertyId) {
            WaterPropertyDesc property = Assets.Property(propertyId);
            bool bHasMin = !float.IsInfinity(range.AliveMin) && range.AliveMin >= property.MinValue();
            bool bHasMax = !float.IsInfinity(range.AliveMax) && range.AliveMax <= property.MaxValue();
            float min = bHasMin ? range.AliveMin : property.MinValue();
            float max = bHasMax ? range.AliveMax : property.MaxValue();

            return min >= property.MinValue() && max <= property.MaxValue() && max != min;
        }

        static private bool CanShowUpInStressTank(BestiaryDesc desc) {
            return !desc.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation | BestiaryDescFlags.DoNotUseInStressTank | BestiaryDescFlags.IsNotLiving);
        }

        internal BFBase[] OwnedFacts { get { return m_Facts; } }

        internal void OptimizeSecondPass(List<BFBase> inReciprocalFacts)
        {
            if (inReciprocalFacts != null && inReciprocalFacts.Count > 0)
            {
                m_AllFacts = new BFBase[m_Facts.Length + inReciprocalFacts.Count];
                Array.Copy(m_Facts, 0, m_AllFacts, 0, m_Facts.Length);
                inReciprocalFacts.CopyTo(0, m_AllFacts, m_Facts.Length, inReciprocalFacts.Count);
            }
            else
            {
                m_AllFacts = (BFBase[]) m_Facts.Clone();
            }

            Array.Sort(m_AllFacts, BFBase.SortByMode);
            m_PlayerFactCount = 0;
            m_InternalFactCount = 0;
            m_AlwaysFactCount = 0;

            for(int i = 0; i < m_AllFacts.Length; i++)
            {
                switch(m_AllFacts[i].Mode)
                {
                    case BFMode.Player:
                        m_PlayerFactCount++;
                        break;

                    case BFMode.Internal:
                        m_InternalFactCount++;
                        break;

                    case BFMode.Always:
                        m_AlwaysFactCount++;
                        break;
                }
            }

            m_InternalFactOffset = (ushort) (m_AlwaysFactCount + m_PlayerFactCount);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            switch(m_Type)
            {
                case BestiaryDescCategory.Critter:
                    {
                        if (m_Size == BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Large;
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        if (m_Size != BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Ecosystem;
                        break;
                    }
            }
        }

        [ContextMenu("Load All In Directory")]
        internal void FindAllFacts()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            m_Facts = ValidationUtils.FindAllAssets<BFBase>(myDirectory);
            Array.Sort(m_Facts, BFType.SortByVisualOrder);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endregion // Optimize

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            m_Facts = null;

            ValidationUtils.StripDebugInfo(ref m_CommonNameId);
            ValidationUtils.StripDebugInfo(ref m_PluralCommonNameId);
            ValidationUtils.StripDebugInfo(ref m_DescriptionId);
            ValidationUtils.StripDebugInfo(ref m_EncodedMessageId);
        }

        #endif // UNITY_EDITOR
    }
}