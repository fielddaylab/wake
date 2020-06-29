using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class ProduceRules : IRuleSet
    {
        public const string Verb = "produces";

        private readonly RingBuffer<ResourceRequirement> m_Resources;

        public ProduceRules()
        {
            m_Resources = new RingBuffer<ResourceRequirement>();
        }

        #region State

        public void Reset()
        {
            m_Resources.Clear();
        }

        public ProduceRules ProduceResource(FourCC inResourceId, ushort inBase, float inMass)
        {
            ResourceRequirement req = new ResourceRequirement()
            {
                ResourceId = inResourceId,
                BaseValue = inBase,
                MassValue = inMass
            };
            m_Resources.PushBack(req);
            return this;
        }

        #endregion // State

        #region Evaluation

        public void EvaluateResources(ref ActorState ioState, in EnergySimContext inContext)
        {
            ioState.ProducingResources = default(VarState<ushort>);

            for (int reqIdx = 0; reqIdx < m_Resources.Count; ++reqIdx)
            {
                ref ResourceRequirement req = ref m_Resources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                ioState.ProducingResources[resIdx] += (ushort) (req.BaseValue + req.MassValue * ioState.Mass);
            }
        }
    
        #endregion // Evaluation
    }
}