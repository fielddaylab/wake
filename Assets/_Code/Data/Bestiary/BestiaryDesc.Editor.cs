#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil.Debugger;
using UnityEngine;
#endif // UNITY_EDITOR

namespace Aqua
{
    public partial class BestiaryDesc : DBObject, IOptimizableAsset
    {
        #if UNITY_EDITOR

        #region Optimize

        int IOptimizableAsset.Order { get { return (int) m_Type; } }

        bool IOptimizableAsset.Optimize()
        {
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
                        m_EnvState = ValidationUtils.FindAsset<WaterPropertyDB>().DefaultValues();
                        foreach(var fact in m_Facts)
                        {
                            BFWaterProperty waterProp = fact as BFWaterProperty;
                            if (waterProp != null)
                            {
                                m_EnvState[waterProp.Property] = waterProp.Value;
                            }
                        }
                        break;
                    }

                case BestiaryDescCategory.Critter:
                    {
                        m_StateTransitions.Reset();
                        foreach(var fact in m_Facts)
                        {
                            BFState state = fact as BFState;
                            if (state != null)
                            {
                                m_StateTransitions[state.Property] = state.Range;
                            }
                        }
                        break;
                    }
            }

            return true;
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

        #endif // UNITY_EDITOR
    }
}