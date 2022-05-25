using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {
    public unsafe class SimLineGraph : MonoBehaviour {
        public struct StressedRegion {
            public ushort StartIdx;
            public ushort EndIdx;
        }

        #region Populating Blocks

        static public void Populate(SimulationDataCtrl data, ListSlice<SimGraphBlock> blocks, SimGraphBlock.RenderMask mask, Color stressColor) {
            Vector2* pointBuffer = Frame.AllocArray<Vector2>(Simulation.MaxTicks + 1);
            StressedRegion* stressBuffer = Frame.AllocArray<StressedRegion>(Simulation.MaxTicks / 2);
            
            if ((mask & SimGraphBlock.RenderMask.Historical) != 0) {
                SimSnapshot* historical = data.RetrieveHistoricalData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(historical, count, data.HistoricalProfile, blocks[i], SimGraphBlock.RenderMask.Historical, pointBuffer, stressBuffer, stressColor);
                }
            }

            if ((mask & SimGraphBlock.RenderMask.Player) != 0) {
                SimSnapshot* player = data.RetrievePlayerData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(player, count, data.PlayerProfile, blocks[i], SimGraphBlock.RenderMask.Player, pointBuffer, stressBuffer, stressColor);
                }
            }

            if ((mask & SimGraphBlock.RenderMask.Predict) != 0) {
                SimSnapshot* predict = data.RetrievePredictData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(predict, count, data.PredictProfile, blocks[i], SimGraphBlock.RenderMask.Predict, pointBuffer, stressBuffer, stressColor);
                }
            }
        }

        static private void PopulateBlock(SimSnapshot* results, int count, SimProfile profile, SimGraphBlock block, SimGraphBlock.RenderMask phase, Vector2* pointBuffer, StressedRegion* stressBuffer, Color stressColor) {
            switch(phase) {
                case SimGraphBlock.RenderMask.Historical: {
                    if (block.PropertyId != WaterPropertyId.NONE) {
                        block.LastRectHistorical = GenerateWater(results, count, block.PropertyId, block.PrimaryColor, pointBuffer, block.Historical);
                    } else {
                        int idx = profile.IndexOfActorType(block.ActorId);
                        block.LastRectHistorical = GenerateHistoricalPopulation(results, count, idx, block.PrimaryColor, pointBuffer, block.Historical);
                    }
                    break;
                }

                case SimGraphBlock.RenderMask.Player: {
                    if (block.PropertyId != WaterPropertyId.NONE) {
                        block.LastRectPlayer = GenerateWater(results, count, block.PropertyId, block.PrimaryColor, pointBuffer, block.Player);
                    } else {
                        int idx = profile.IndexOfActorType(block.ActorId);
                        block.LastRectPlayer = GeneratePlayerPopulation(results, count, idx, block.PrimaryColor, stressColor, pointBuffer, stressBuffer, block.Player);
                    }
                    break;
                }

                case SimGraphBlock.RenderMask.Predict: {
                    if (block.PropertyId != WaterPropertyId.NONE) {
                        block.LastRectPredict = GenerateWater(results, count, block.PropertyId, block.PrimaryColor, pointBuffer, block.Predict);
                    } else {
                        int idx = profile.IndexOfActorType(block.ActorId);
                        block.LastRectPredict = GeneratePlayerPopulation(results, count, idx, block.PrimaryColor, stressColor, pointBuffer, stressBuffer, block.Predict);
                    }
                    break;
                }
            }

            block.AppliedScaleMask &= ~phase;
        }

        #endregion // Populating Blocks

        #region Rendering Blocks

        static public void Render(int pointCount, ListSlice<SimGraphBlock> blocks, SimGraphBlock.RenderMask mask) {
            bool showHistorical = pointCount > 1 && (mask & SimGraphBlock.RenderMask.Historical) != 0;
            bool showPlayer = pointCount > 1 && (mask & SimGraphBlock.RenderMask.Player) != 0;
            bool showPredict = !showHistorical && !showPlayer && pointCount > 1 && (mask & SimGraphBlock.RenderMask.Predict) != 0;

            for(int i = 0; i < blocks.Length; i++) {
                SimGraphBlock block = blocks[i];

                bool changed = false;
                changed |= Ref.Replace(ref block.Historical.PointCount, pointCount);
                changed |= Ref.Replace(ref block.Player.PointCount, pointCount);
                changed |= Ref.Replace(ref block.Predict.PointCount, pointCount);

                if (showHistorical && showPlayer) {
                    Rect bounds = block.LastRectHistorical;
                    SimMath.CombineBounds(ref bounds, block.LastRectPlayer);
                    if (bounds != block.LastRect) {
                        if ((block.AppliedScaleMask & SimGraphBlock.RenderMask.Historical) != 0) {
                            InvScaleLine(block.Historical, pointCount, block.LastRect);
                        }
                        ScaleLine(block.Historical, pointCount, bounds);

                        if ((block.AppliedScaleMask & SimGraphBlock.RenderMask.Player) != 0) {
                            InvScaleLine(block.Player, pointCount, block.LastRect);
                        }
                        ScaleLine(block.Player, pointCount, bounds);

                        block.AppliedScaleMask |= SimGraphBlock.RenderMask.Historical | SimGraphBlock.RenderMask.Player;
                        block.LastRect = bounds;
                        changed = true;
                    }
                } else if (showHistorical) {
                    Rect bounds = block.LastRectHistorical;
                    if (bounds != block.LastRect) {
                        if ((block.AppliedScaleMask & SimGraphBlock.RenderMask.Historical) != 0) {
                            InvScaleLine(block.Historical, pointCount, block.LastRect);
                        }
                        ScaleLine(block.Historical, pointCount, bounds);

                        block.AppliedScaleMask |= SimGraphBlock.RenderMask.Historical;
                        block.LastRect = bounds;
                        changed = true;
                    }
                } else if (showPlayer) {
                    Rect bounds = block.LastRectPlayer;
                    if (bounds != block.LastRect) {
                        if ((block.AppliedScaleMask & SimGraphBlock.RenderMask.Player) != 0) {
                            InvScaleLine(block.Player, pointCount, block.LastRect);
                        }
                        ScaleLine(block.Player, pointCount, bounds);

                        block.AppliedScaleMask |= SimGraphBlock.RenderMask.Player;
                        block.LastRect = bounds;
                        changed = true;
                    }
                } else if (showPredict) {
                    Rect bounds = block.LastRectPredict;
                    if (bounds != block.LastRect) {
                        if ((block.AppliedScaleMask & SimGraphBlock.RenderMask.Predict) != 0) {
                            InvScaleLine(block.Predict, pointCount, block.LastRect);
                        }
                        ScaleLine(block.Predict, pointCount, bounds);

                        block.AppliedScaleMask |= SimGraphBlock.RenderMask.Predict;
                        block.LastRect = bounds;
                        changed = true;
                    }
                }

                block.Historical.enabled = showHistorical;
                block.Player.enabled = showPlayer;
                block.Predict.enabled = showPredict;

                if (changed) {
                    block.Historical.SubmitChanges();
                    block.Player.SubmitChanges();
                    block.Predict.SubmitChanges();
                }
            }
        }

        #endregion // Populating Blocks

        #region Generating Lines

        static public Rect GenerateHistoricalPopulation(SimSnapshot* results, int count, int actorIdx, Color actorColor, Vector2* pointBuffer, GraphLineRenderer renderer) {
            Rect range = FillPopulationBuffer(results, count, actorIdx, pointBuffer, null, null, false);
            renderer.Colors = Array.Empty<Color>();
            renderer.color = actorColor;
            renderer.EnsurePointBuffer(count);
            UnsafeExt.MemCpy(pointBuffer, count, renderer.Points);
            renderer.PointCount = count;
            return range;
        }

        static public Rect GeneratePlayerPopulation(SimSnapshot* results, int count, int actorIdx, Color actorColor, Color stressColor, Vector2* pointBuffer, StressedRegion* stressBuffer, GraphLineRenderer renderer) {
            int stressCount;
            Rect range = FillPopulationBuffer(results, count, actorIdx, pointBuffer, stressBuffer, &stressCount, false);
            renderer.EnsureColorBuffer(count);
            renderer.EnsurePointBuffer(count);
            UnsafeExt.MemCpy(pointBuffer, count, renderer.Points);
            Color[] colors = renderer.Colors;
            for(int i = 0; i < count; i++) {
                colors[i] = actorColor;
            }
            for(int i = 0; i < stressCount; i++) {
                int start = stressBuffer[i].StartIdx;
                int end = stressBuffer[i].EndIdx;
                for(int j = start; j < end; j++) {
                    colors[j] = stressColor;
                }
            }
            renderer.PointCount = count;
            return range;
        }

        static public Rect GenerateWater(SimSnapshot* results, int count, WaterPropertyId waterProperty, Color propertyColor, Vector2* pointBuffer, GraphLineRenderer renderer) {
            Rect range = FillWaterBuffer(results, count, waterProperty, pointBuffer);
            renderer.Colors = Array.Empty<Color>();
            renderer.color = propertyColor;
            renderer.EnsurePointBuffer(count);
            UnsafeExt.MemCpy(pointBuffer, count, renderer.Points);
            renderer.PointCount = count;
            return range;
        }

        static private Rect FillPopulationBuffer(SimSnapshot* results, int resultCount, int organismIdx, Vector2* pointBuffer, StressedRegion* stressBuffer, int* stressRegionCount, bool trackStressed) {
            *stressRegionCount = 0;
            bool currentStress = false, stress = false;

            Vector2* pointHead = pointBuffer;
            StressedRegion* stressHead = stressBuffer;
            
            for(int i = 0; i < resultCount; i++) {
                pointHead->x = i;
                pointHead->y = results->Populations[organismIdx];

                if (trackStressed) {
                    stress = results->StressedRatio[organismIdx] >= 64; // stressed ratio values are 0 - 128; half is 64
                    if (stress != currentStress) {
                        ushort idx = (ushort) Math.Max(0, i - 1);
                        if (stress) {
                            stressHead->StartIdx = idx;
                        } else {
                            stressHead->EndIdx = idx;
                            (*stressRegionCount)++;
                            stressHead++;
                        }
                        currentStress = stress;
                    }
                }

                results++;
                pointHead++;
            }

            if (currentStress) {
                (*stressRegionCount)++;
                stressHead->EndIdx = (ushort) (resultCount - 1);
            }

            return SimMath.CalculateBounds(pointBuffer, resultCount);
        }

        static private Rect FillWaterBuffer(SimSnapshot* results, int resultCount, WaterPropertyId propertyId, Vector2* pointBuffer) {
            Vector2* pointHead = pointBuffer;
            
            for(int i = 0; i < resultCount; i++) {
                pointHead->x = i;
                pointHead->y = results->Water[propertyId];

                results++;
                pointHead++;
            }

            return SimMath.CalculateBounds(pointBuffer, resultCount);
        }
    
        static public void ScaleLine(GraphLineRenderer renderer, int count, Rect range) {
            SimMath.Scale(renderer.Points, count, range);
        }

        static public void InvScaleLine(GraphLineRenderer renderer, int count, Rect range) {
            SimMath.InvScale(renderer.Points, count, range);
        }

        #endregion // Generating Lines
    }
}