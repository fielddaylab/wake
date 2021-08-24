using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/State/Range State Change")]
    public class BFState : BFBase, IOptimizableAsset
    {
        #region Inspector

        [Header("Property Range")]
        [AutoEnum, FormerlySerializedAs("m_PropertyId")] public WaterPropertyId Property = WaterPropertyId.Temperature;
        
        [Header("Alive State")]
        [SerializeField, FormerlySerializedAs("m_HasStressed")] public bool HasStressed = true;
        [SerializeField, ShowIfField("HasStressed")] private float m_MinSafe = 0;
        [SerializeField, ShowIfField("HasStressed")] private float m_MaxSafe = 0;

        [Header("Stressed State")]
        [SerializeField, FormerlySerializedAs("m_HasDeath")] public bool HasDeath = false;
        [SerializeField, ShowIfField("HasDeath")] private float m_MinStressed = float.MinValue;
        [SerializeField, ShowIfField("HasDeath")] private float m_MaxStressed = float.MaxValue;

        #endregion // Inspector

        [SerializeField, HideInInspector] public ActorStateTransitionRange Range;

        private BFState() : base(BFTypeId.State) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.State, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.State, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.State, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFState) x).Property, ((BFState) y).Property);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFState stateFact = (BFState) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(stateFact.Property);
            
            if (stateFact.HasDeath)
            {
                return Loc.Format(desc.StateChangeFormat(), stateFact.Parent.CommonName()
                    , desc.FormatValue(stateFact.m_MinSafe), desc.FormatValue(stateFact.m_MaxSafe)
                    , desc.FormatValue(stateFact.m_MinStressed), desc.FormatValue(stateFact.m_MaxStressed)
                );
            }

            if (stateFact.HasStressed)
            {
                return Loc.Format(desc.StateChangeStressOnlyFormat(), stateFact.Parent.CommonName(), desc.FormatValue(stateFact.m_MinSafe), desc.FormatValue(stateFact.m_MaxSafe));
            }

            return Loc.Format(desc.StateChangeUnaffectedFormat(), stateFact.Parent.CommonName());
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFState stateFact = (BFState) inFact;
            return BestiaryUtils.Property(stateFact.Property).Icon();
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return -9; } }

        bool IOptimizableAsset.Optimize()
        {
            ActorStateTransitionRange range = ActorStateTransitionRange.Default;

            if (HasStressed)
            {
                range.AliveMin = m_MinSafe;
                range.AliveMax = m_MaxSafe;
            }

            if (HasDeath)
            {
                range.StressedMin = m_MinStressed;
                range.StressedMax = m_MaxStressed;
            }

            return Ref.Replace(ref Range, range);
        }

        #endif // UNITY_EDITOR
    }
}