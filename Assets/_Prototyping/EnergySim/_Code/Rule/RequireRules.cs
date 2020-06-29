using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class RequireRules : IRuleSet
    {
        public const string Verb = "requires";

        private readonly RingBuffer<ResourceRequirement> m_Resources;
        private readonly RingBuffer<PropertyRequirement> m_Properties;

        public RequireRules()
        {
            m_Resources = new RingBuffer<ResourceRequirement>();
            m_Properties = new RingBuffer<PropertyRequirement>();
        }

        #region State

        public void Reset()
        {
            m_Resources.Clear();
            m_Properties.Clear();
        }

        public RequireRules RequireResource(FourCC inResourceId, ushort inBase, float inMass)
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

        public RequireRules RequireProperty(FourCC inPropertyId, CompareOp inComparison, float inBase, float inMass)
        {
            PropertyRequirement req = new PropertyRequirement()
            {
                PropertyId = inPropertyId,
                Comparison = inComparison,
                BaseValue = inBase,
                MassValue = inMass
            };
            m_Properties.PushBack(req);
            return this;
        }

        #endregion // State

        #region Evaluation

        public void EvaluateResources(ref ActorState ioState, in EnergySimContext inContext)
        {
            ioState.DesiredResources = default(VarState<ushort>);

            for (int reqIdx = 0; reqIdx < m_Resources.Count; ++reqIdx)
            {
                ref ResourceRequirement req = ref m_Resources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                ioState.DesiredResources[resIdx] += (ushort) (req.BaseValue + req.MassValue * ioState.Mass);
            }
        }

        public void EvaluateProperties(ref ActorState ioState, in EnergySimContext inContext)
        {
            ioState.MetPropertyRequirements = ushort.MaxValue;

            for (int reqIdx = 0; reqIdx < m_Properties.Count; ++reqIdx)
            {
                ref PropertyRequirement req = ref m_Properties[reqIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(req.PropertyId);
                if (!req.Evaluate(ioState.Mass, inContext))
                {
                    ioState.MetPropertyRequirements &= (ushort) (~(1 << propIdx));
                }
            }
        }
    
        #endregion // Evaluation
    }
}