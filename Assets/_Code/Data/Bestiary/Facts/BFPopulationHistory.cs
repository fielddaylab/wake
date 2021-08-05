using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Property/Population History")]
    public class BFPopulationHistory : BFBase
    {
        // TODO: Finish

        static private readonly TextId PopulationSentence = "factFormat.populationHistory";

        #region Inspector

        [Header("Population History")]
        [SerializeField, FilterBestiary(BestiaryDescCategory.Critter)] private BestiaryDesc m_Critter = null;
        [SerializeField, AutoEnum] private BFGraphType m_Graph = BFGraphType.Flat;

        #endregion // Inspector

        public BestiaryDesc Critter() { return m_Critter; }
        public BFGraphType Graph() { return m_Graph; }

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
            return Loc.Format(PopulationSentence, m_Critter.CommonName(), Parent().CommonName(), BestiaryUtils.GraphTypeToTextId(m_Graph), Parent().HistoricalRecordDuration());
        }
    }
}