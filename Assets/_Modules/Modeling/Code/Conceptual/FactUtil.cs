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
    }
}