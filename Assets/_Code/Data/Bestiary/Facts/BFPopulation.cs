using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Population")]
    public class BFPopulation : BFBase, IOptimizableAsset
    {
        static private readonly TextId PopulationSentence = "factFormat.population";
        static private readonly TextId PopulationHerdSentence = "factFormat.population.herd";

        #region Inspector

        [Header("Population")]
        [SerializeField, FilterBestiary(BestiaryDescCategory.Critter)] private BestiaryDesc m_Critter = null;
        [SerializeField] private uint m_Value = 0;
        [SerializeField] private byte m_SiteVersion = 0;

        #endregion // Inspector

        [SerializeField] private BFBody m_Body;

        public BestiaryDesc Critter() { return m_Critter; }
        public uint Population() { return m_Value; }
        public byte SiteVersion() { return m_SiteVersion; }

        public string FormattedPopulation()
        {
            return m_Body.FormatPopulation(m_Value);
        }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        internal override int GetSortingOrder()
        {
            return 0;
        }

        public override BFMode Mode()
        {
            return BFMode.Player;
        }

        protected override Sprite DefaultIcon()
        {
            return m_Critter.Icon();
        }

        public override string GenerateSentence()
        {
            TextId format = PopulationSentence;
            if (m_Critter.HasFlags(BestiaryDescFlags.TreatAsHerd))
                format = PopulationHerdSentence;
            return Loc.Format(format, m_Body.FormatPopulation(m_Value), m_Critter.PluralCommonName(), Parent().CommonName());
        }

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return 10; } }

        bool IOptimizableAsset.Optimize()
        {
            return Ref.Replace(ref m_Body, m_Critter.FactOfType<BFBody>());
        }

        #endif // UNITY_EDITOR
    }
}