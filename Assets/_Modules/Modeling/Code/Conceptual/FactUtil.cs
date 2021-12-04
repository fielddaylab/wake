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
                        case BFTypeId.Eat: {
                            if (critters.Contains(((BFEat) fact).Critter)) {
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
                    switch(fact.Type) {
                        case BFTypeId.Eat: {
                            BestiaryDesc target = ((BFEat) fact).Critter;
                            if (!currentEntities.Contains(target) && !pendingEntities.Contains(target)) {
                                add = false;
                            }
                            break;
                        }
                    }
                    if (add) {
                        pending.Add(fact);
                    }
                }
            }
        }
    }
}