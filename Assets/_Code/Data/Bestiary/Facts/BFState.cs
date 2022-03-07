using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/State Change")]
    public class BFState : BFBase, IBakedAsset
    {
        #region Inspector

        [Header("State Change")]
        [AutoEnum] public WaterPropertyId Property = WaterPropertyId.Temperature;
        
        [Header("Alive State")]
        public bool HasStressed = true;
        [SerializeField, ShowIfField("HasStressed")] internal float m_MinSafe = 0;
        [SerializeField, ShowIfField("HasStressed")] internal float m_MaxSafe = 0;

        [Header("Stressed State")]
        public bool HasDeath = false;
        [SerializeField, ShowIfField("HasDeath")] internal float m_MinStressed = float.MinValue;
        [SerializeField, ShowIfField("HasDeath")] internal float m_MaxStressed = float.MaxValue;

        #endregion // Inspector

        [SerializeField, HideInInspector] public ActorStateTransitionRange Range;

        private BFState() : base(BFTypeId.State) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.State, BFShapeId.State, 0, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.State, null, GenerateDetails, null, null, (f) => ((BFState) f).Property);
            BFType.DefineEditor(BFTypeId.State, DefaultIcon, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            return WaterPropertyDB.SortByVisualOrder(((BFState) x).Property, ((BFState) y).Property);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFState stateFact = (BFState) inFact;
            WaterPropertyDesc desc = BestiaryUtils.Property(stateFact.Property);
            BFDetails details;

            details.Header = Loc.Find("fact.state.header");
            details.Image = desc.ImageSet();
            
            if (stateFact.HasDeath)
            {
                details.Description = Loc.Format(desc.StateChangeFormat(), stateFact.Parent.CommonName()
                    , desc.FormatValue(stateFact.m_MinSafe), desc.FormatValue(stateFact.m_MaxSafe)
                    , desc.FormatValue(stateFact.m_MinStressed), desc.FormatValue(stateFact.m_MaxStressed)
                );
            }
            else if (stateFact.HasStressed)
            {
                details.Description = Loc.Format(desc.StateChangeStressOnlyFormat(), stateFact.Parent.CommonName(), desc.FormatValue(stateFact.m_MinSafe), desc.FormatValue(stateFact.m_MaxSafe));
            }
            else
            {
                details.Description = Loc.Format(desc.StateChangeUnaffectedFormat(), stateFact.Parent.CommonName());
            }

            return details;
        }

        static private Sprite DefaultIcon(BFBase inFact)
        {
            BFState stateFact = (BFState) inFact;
            return BestiaryUtils.Property(stateFact.Property).Icon();
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        int IBakedAsset.Order { get { return -9; } }

        bool IBakedAsset.Bake()
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