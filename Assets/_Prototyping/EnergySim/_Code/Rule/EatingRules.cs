using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class EatingRules : IRuleSet
    {
        public const string Verb_Eats = "eats";
        public const string Verb_BiteSize = "bites";

        public const float DefaultBiteStretch = 1.25f;

        private readonly RingBuffer<ActorConversion> m_Actors;
        private ushort m_BaseBiteSize;
        private float m_MassBiteSize;
        private float m_BiteStretch;

        public EatingRules()
        {
            m_Actors = new RingBuffer<ActorConversion>();
            m_BiteStretch = DefaultBiteStretch;
        }

        #region State

        public void Reset()
        {
            m_Actors.Clear();
            m_BaseBiteSize = 0;
            m_MassBiteSize = 0;
            m_BiteStretch = DefaultBiteStretch;
        }

        public EatingRules Eats(FourCC inActorId, float inConversion = 1)
        {
            return Eats(inActorId, inConversion, inConversion);
        }

        public EatingRules Eats(FourCC inActorId, float inConversion, float inWeight)
        {
            ActorConversion conversion = new ActorConversion()
            {
                ActorId = inActorId,
                Conversion = inConversion,
                Weight = inWeight
            };
            m_Actors.PushBack(conversion);
            return this;
        }

        public EatingRules Bites(ushort inBaseBiteSize, float inMassBite = 0, float inBiteStretch = DefaultBiteStretch)
        {
            m_BaseBiteSize = inBaseBiteSize;
            m_MassBiteSize = inMassBite;
            m_BiteStretch = inBiteStretch;
            return this;
        }

        #endregion // State

        #region Evaluation

        public void GetEatSize(in ActorState inState, out ushort outBiteSize, out ushort outMaxSize)
        {
            outBiteSize = (ushort) (m_BaseBiteSize + inState.Mass * m_MassBiteSize);
            outMaxSize = (ushort) (outBiteSize * m_BiteStretch);
        }

        public bool CanEat(in FourCC inActorId, out ActorConversion outConversion)
        {
            for(int i = m_Actors.Count - 1; i >= 0; --i)
            {
                ref ActorConversion entry = ref m_Actors[i];
                if (entry.ActorId == inActorId)
                {
                    outConversion = entry;
                    return entry.Weight > 0;
                }
            }

            outConversion = default(ActorConversion);
            return false;
        }
    
        #endregion // Evaluation
    }
}