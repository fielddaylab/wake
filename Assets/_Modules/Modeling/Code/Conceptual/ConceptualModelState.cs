using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauUtil;

namespace Aqua.Modeling {
    public class ConceptualModelState {
        
        public enum StatusId {
            MissingData,
            PendingImport,
            ExportReady,
            UpToDate
        }

        public StatusId Status;
        public ModelMissingReasons MissingReasons;
        public readonly RingBuffer<MissingFactRecord> MissingFacts = new RingBuffer<MissingFactRecord>(24);

        public readonly HashSet<BestiaryDesc> GraphedEntities = new HashSet<BestiaryDesc>();
        public readonly HashSet<BFBase> GraphedFacts = new HashSet<BFBase>();

        public readonly HashSet<BestiaryDesc> PendingEntities = new HashSet<BestiaryDesc>();
        public readonly HashSet<BFBase> PendingFacts = new HashSet<BFBase>();

        public readonly HashSet<BestiaryDesc> SimulatedEntities = new HashSet<BestiaryDesc>();
        public readonly HashSet<BFBase> SimulatedFacts = new HashSet<BFBase>();

        public void LoadFrom(SiteSurveyData siteData) {
            GraphedEntities.Clear();
            GraphedFacts.Clear();
            PendingEntities.Clear();
            PendingFacts.Clear();
            SimulatedEntities.Clear();
            SimulatedFacts.Clear();

            foreach(var critterId in siteData.GraphedCritters) {
                GraphedEntities.Add(Assets.Bestiary(critterId));
            }

            foreach(var factId in siteData.GraphedFacts) {
                GraphedFacts.Add(Assets.Fact(factId));
            }
        }

        public void Reset() {
            GraphedEntities.Clear();
            GraphedFacts.Clear();
            PendingEntities.Clear();
            PendingFacts.Clear();
            SimulatedEntities.Clear();
            SimulatedFacts.Clear();
            Status = StatusId.UpToDate;
            MissingReasons = 0;
        }
    }
}