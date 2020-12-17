using System;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    public class PlayerFactParams
    {
        [NonSerialized] private BestiaryFactBase m_CachedFact;
        
        // base fact id
        private StringHash32 m_FactId;

        // basic data
        public Variant Value;
        
        // advanced data
        public StringHash32 SubjectVariantId;
        public StringHash32 TargetVariantId;
        public Condition ConditionData;

        public StringHash32 FactId { get { return m_FactId; } }
        
        public BestiaryFactBase Fact
        {
            get
            {
                if (m_CachedFact.IsReferenceNull())
                {
                    m_CachedFact = Services.Assets.Bestiary.Fact(m_FactId);
                }

                return m_CachedFact;
            }
        }

        public PlayerFactParams() { }

        public PlayerFactParams(StringHash32 inFactId)
        {
            m_FactId = inFactId;
        }
    }
}