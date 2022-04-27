using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauPools;
using BeauUtil;

namespace Aqua.Modeling {
    static public class FactUtil {
        
        static public void GatherImportableFacts(BestiaryDesc environment, RingBuffer<BestiaryDesc> importableEntities, RingBuffer<BFBase> importableFacts) {
            BestiaryDesc critter;
            
            using(PooledSet<BestiaryDesc> critters = PooledSet<BestiaryDesc>.Create())
            using(PooledSet<BFBase> potentialFacts = PooledSet<BFBase>.Create()) {
                foreach(var critterId in environment.Organisms()) {
                    critter = Assets.Bestiary(critterId);
                    critters.Add(critter);
                    importableEntities.PushBack(critter);

                    foreach(var fact in critter.PlayerFacts) {
                        if (fact.Parent == critter) {
                            potentialFacts.Add(fact);
                        }
                    }
                }

                foreach(var fact in environment.PlayerFacts) {
                    potentialFacts.Add(fact);
                }

                foreach(var fact in potentialFacts) { 
                    switch(fact.Type) {
                        case BFTypeId.Eat:
                        case BFTypeId.Parasite: {
                            if (critters.Contains(BFType.Target(fact))) {
                                importableFacts.PushBack(fact);
                            }
                            break;
                        }
                        
                        case BFTypeId.Population:
                        case BFTypeId.WaterProperty:
                        case BFTypeId.Sim:
                        case BFTypeId.Model: {
                            break;
                        }

                        default: {
                            importableFacts.PushBack(fact);
                            break;
                        }
                    }
                }
            }
        }

        static public void GatherSimulatedSubset(HashSet<BestiaryDesc> filter, HashSet<BestiaryDesc> graphedEntities, HashSet<BFBase> graphedFacts, HashSet<BestiaryDesc> simulatedEntities, HashSet<BFBase> simulatedFacts) {
            foreach(var graphed in graphedEntities) {
                if (filter.Contains(graphed)) {
                    simulatedEntities.Add(graphed);
                }
            }

            foreach(var graphed in graphedFacts) {
                BestiaryDesc target = BFType.Target(graphed);
                if (filter.Contains(graphed.Parent) && (target == null || filter.Contains(target))) {
                    simulatedFacts.Add(graphed);
                }
            }
        }

        static public void GatherInterventionSubset(BestiaryDesc additional, BestiaryData data, HashSet<BestiaryDesc> simulatedEntities, HashSet<BestiaryDesc> additionalEntities, HashSet<BFBase> additionalFacts) {
            if (additional != null && !simulatedEntities.Contains(additional)) {
                additionalEntities.Add(additional);

                foreach(var fact in additional.PlayerFacts) {
                    if (fact.Parent != additional || !data.HasFact(fact.Id)) {
                        continue;
                    }
                    BestiaryDesc target = BFType.Target(fact);
                    if (target == null || simulatedEntities.Contains(target)) {
                        additionalFacts.Add(fact);
                    }
                }

                foreach(var entity in simulatedEntities) {
                    foreach(var fact in entity.PlayerFacts) {
                        if (fact.Parent != entity || !data.HasFact(fact.Id)) {
                            continue;
                        }

                        BestiaryDesc target = BFType.Target(fact);
                        if (target == additional) {
                            additionalFacts.Add(fact);
                        }
                    }
                }
            }
        }

        static public void GatherPendingEntities(RingBuffer<BestiaryDesc> all, BestiaryData filter, HashSet<BestiaryDesc> current, HashSet<BestiaryDesc> pending) {
            foreach(var entity in all) {
                if (!current.Contains(entity) && filter.HasEntity(entity.Id())) {
                    pending.Add(entity);
                }
            }
        }

        static public void GatherPendingFacts(RingBuffer<BFBase> all, BestiaryData filter, HashSet<BestiaryDesc> currentEntities, HashSet<BestiaryDesc> pendingEntities, HashSet<BFBase> current, HashSet<BFBase> pending) {
            foreach(var fact in all) {
                if (!current.Contains(fact) && filter.HasFact(fact.Id)) {
                    bool add = true;
                    BestiaryDesc target = BFType.Target(fact);
                    if (target != null && !currentEntities.Contains(target) && !pendingEntities.Contains(target)) {
                        add = false;
                    }
                    if (add) {
                        pending.Add(fact);
                    }
                }
            }
        }

        static public void GatherMissingFacts(BestiaryDesc environment, Predicate<StringHash32> organismFilter, Predicate<WaterPropertyId> propertyFilter, RingBuffer<BFBase> requiredFacts, HashSet<BestiaryDesc> graphedEntities, HashSet<BFBase> graphedFacts, RingBuffer<MissingFactRecord> missingRecords) {
            // check facts directly
            foreach(var fact in requiredFacts) {
                if (!graphedFacts.Contains(fact)) {
                    if (!fact.Parent.HasCategory(BestiaryDescCategory.Critter) || graphedEntities.Contains(fact.Parent)) {
                        RecordMissingFact(missingRecords, fact);
                    }
                }
            }

            // check for missing population history
            foreach(var propHistory in environment.FactsOfType<BFPopulationHistory>()) {
                if (organismFilter(propHistory.Critter.Id()) && graphedEntities.Contains(propHistory.Critter) && !graphedFacts.Contains(propHistory)) {
                    RecordMissingFact(missingRecords, propHistory);
                }
            }

            // check for missing water chem history
            foreach(var chemHistory in environment.FactsOfType<BFWaterPropertyHistory>()) {
                if (propertyFilter(chemHistory.Property) && !graphedFacts.Contains(chemHistory)) {
                    RecordMissingFact(missingRecords, chemHistory);
                }
            }
        }

        static private void RecordMissingFact(RingBuffer<MissingFactRecord> missingRecords, BFBase fact) {
            bool onlyStressed = BFType.OnlyWhenStressed(fact);
            switch(fact.Type) {
                case BFTypeId.WaterPropertyHistory: {
                    FindMissingRecord(missingRecords, BFType.WaterProperty(fact)).FactTypes |= MissingFactTypes.WaterChemHistory;
                    break;
                }

                case BFTypeId.Eat: {
                    FindMissingRecord(missingRecords, fact.Parent.Id()).FactTypes |= onlyStressed ? MissingFactTypes.Eat_Stressed : MissingFactTypes.Eat;
                    break;
                }

                case BFTypeId.Grow:
                case BFTypeId.Reproduce: {
                    FindMissingRecord(missingRecords, fact.Parent.Id()).FactTypes |= onlyStressed ? MissingFactTypes.Repro_Stressed : MissingFactTypes.Repro;
                    break;
                }

                case BFTypeId.Parasite: {
                    FindMissingRecord(missingRecords, fact.Parent.Id()).FactTypes |= MissingFactTypes.Parasite;
                    break;
                }

                case BFTypeId.Produce:
                case BFTypeId.Consume: {
                    FindMissingRecord(missingRecords, BFType.WaterProperty(fact)).FactTypes |= onlyStressed ? MissingFactTypes.WaterChem_Stressed : MissingFactTypes.WaterChem;
                    break;
                }

                case BFTypeId.PopulationHistory: {
                    FindMissingRecord(missingRecords, BFType.Target(fact).Id()).FactTypes |= MissingFactTypes.PopulationHistory;
                    break;
                }
            }
        }

        static private ref MissingFactRecord FindMissingRecord(RingBuffer<MissingFactRecord> records, StringHash32 organismId) {
            for(int i = 0; i < records.Count; i++) {
                if (records[i].OrganismId == organismId) {
                    return ref records[i];
                }
            }

            MissingFactRecord record = new MissingFactRecord() {
                OrganismId = organismId,
                PropertyId = WaterPropertyId.NONE
            };
            records.PushBack(record);
            return ref records[records.Count - 1];
        }

        static private ref MissingFactRecord FindMissingRecord(RingBuffer<MissingFactRecord> records, WaterPropertyId propertyId) {
            for(int i = 0; i < records.Count; i++) {
                if (records[i].PropertyId == propertyId) {
                    return ref records[i];
                }
            }

            MissingFactRecord record = new MissingFactRecord() {
                OrganismId = default(StringHash32),
                PropertyId = propertyId
            };
            records.PushBack(record);
            return ref records[records.Count - 1];
        }
    }
}