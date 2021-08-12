using System.Collections.Generic;
using Aqua;
using Aqua.Debugging;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Initial state and critter profiles.
    /// </summary>
    public sealed class SimulationProfile : IFactVisitor
    {
        private RingBuffer<CritterProfile> m_Profiles = new RingBuffer<CritterProfile>(Simulator.MaxTrackedCritters, RingBufferMode.Fixed);
        private SimulationResult m_InitialState;
        
        private BestiaryDesc m_Environment;
        private readonly HashSet<BestiaryDesc> m_DiscoveredCritters = new HashSet<BestiaryDesc>();
        private readonly HashSet<BFBase> m_DiscoveredFacts = new HashSet<BFBase>();

        public void Clear()
        {
            m_InitialState = default(SimulationResult);
            for(int i = 0; i < m_Profiles.Count; i++)
                m_Profiles[i].Clear();
            m_Profiles.Clear();
            m_DiscoveredCritters.Clear();
            m_DiscoveredFacts.Clear();
        }

        private void LimitedClear()
        {
            m_InitialState.Environment = Services.Assets.WaterProp.DefaultValues();
            for(int i = 0; i < m_Profiles.Count; ++i)
                m_Profiles[i].Clear();
            m_DiscoveredCritters.Clear();
            m_DiscoveredFacts.Clear();
        }

        public void Construct(BestiaryDesc inEnvironment, IEnumerable<BestiaryDesc> inCritters)
        {
            LimitedClear();

            foreach(var critter in inCritters)
            {
                DiscoverCritter(critter);
                foreach(var fact in critter.Facts)
                    DiscoverFact(fact);
            }

            foreach(var fact in inEnvironment.Facts)
                DiscoverFact(fact);

            for(int i = 0; i < m_Profiles.Count; i++)
                m_Profiles[i].PostProcess(this);
        }

        public void Construct(BestiaryDesc inEnvironment, IEnumerable<BestiaryDesc> inCritters, IEnumerable<BFBase> inFacts)
        {
            LimitedClear();

            foreach(var critter in inCritters)
                DiscoverCritter(critter);

            foreach(var fact in inFacts)
                DiscoverFact(fact);

            foreach(var fact in inEnvironment.Facts)
                DiscoverFact(fact);

            for(int i = 0; i < m_Profiles.Count; i++)
                m_Profiles[i].PostProcess(this);
        }

        #region Profiles

        public IReadOnlyList<CritterProfile> Critters() { return m_Profiles; }

        public CritterProfile FindProfile(BestiaryDesc inCritter)
        {
            CritterProfile profile;
            m_Profiles.TryBinarySearch(inCritter.Id(), out profile);
            return profile;
        }

        private CritterProfile FindOrCreateProfile(BestiaryDesc inCritter)
        {
            CritterProfile profile;
            if (!m_Profiles.TryBinarySearch(inCritter.Id(), out profile))
            {
                DebugService.Log(LogMask.Modeling, "[SimulationProfile] Creating new profile for '{0}'", inCritter.Id());
                profile = new CritterProfile(inCritter);
                m_Profiles.PushBack(profile);
                m_Profiles.SortByKey<StringHash32, CritterProfile>();
            }
            return profile;
        }

        public int CritterIndex(StringHash32 inCritterId)
        {
            for(int i = 0; i < m_Profiles.Count; ++i)
            {
                if (m_Profiles[i].Id() == inCritterId)
                    return i;
            }

            return -1;
        }

        #endregion // Profiles
        
        #region InitialState

        public ref SimulationResult InitialState
        {
            get { return ref m_InitialState; }
        }

        #endregion // InitialState

        #region Facts

        private void DiscoverCritter(BestiaryDesc inCritter)
        {
            if (m_DiscoveredCritters.Add(inCritter))
            {
                DebugService.Log(LogMask.Modeling, "[SimulationProfile] Importing critter '{0}'", inCritter.Id());
                foreach(var internalFact in inCritter.InternalFacts)
                    DiscoverFact(internalFact);
            }
        }

        private void DiscoverFact(BFBase inFact)
        {
            DiscoverCritter(inFact.Parent());
            if (m_DiscoveredFacts.Add(inFact))
            {
                DebugService.Log(LogMask.Modeling, "[SimulationProfile] Importing fact '{0}'", inFact.Id());
                inFact.Accept(this);
            }
        }

        #endregion // Facts

        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact)
        {
            // Default
        }

        void IFactVisitor.Visit(BFBody inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFWaterProperty inFact)
        {
            m_InitialState.Environment[inFact.PropertyId()] = inFact.Value();
        }

        void IFactVisitor.Visit(BFWaterPropertyHistory inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFPopulation inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFPopulationHistory inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFEat inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
            DiscoverCritter(inFact.Target());
        }

        void IFactVisitor.Visit(BFGrow inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFReproduce inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFProduce inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFConsume inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFState inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFDeath inFact)
        {
            inFact.Accept(FindOrCreateProfile(inFact.Parent()));
        }

        void IFactVisitor.Visit(BFModel inModel)
        {
            // nothing...
        }

        #endregion // IFactVisitor

        #region Utils

        static public IEnumerable<BFBase> GatherFacts(ListSlice<BestiaryDesc> inCritters)
        {
            foreach(var critter in inCritters)
            {
                foreach(var fact in critter.Facts)
                    yield return fact;
            }
        }

        #endregion // Utils
    }
}