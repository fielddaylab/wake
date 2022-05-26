using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {
    public unsafe class SimLineGraph : MonoBehaviour {
        public struct StressedRegion {
            public ushort StartIdx;
            public ushort EndIdx;
        }

        #region Inspector

        [SerializeField] private Color m_StressColor = Color.red;
        [SerializeField] private SimGraphBlock.Pool m_Blocks = null;
        [SerializeField] private GraphTargetRegion.Pool m_Targets = null;
        [SerializeField] private Sprite m_MissingOrganisms = null;

        #endregion // Inspector

        private readonly List<SimGraphBlock> m_AllocatedBlocks = new List<SimGraphBlock>();
        private readonly Dictionary<StringHash32, SimGraphBlock> m_AllocatedBlockMap = new Dictionary<StringHash32, SimGraphBlock>();
        private readonly SimGraphBlock[] m_AllocatedWaterMap = new SimGraphBlock[(int) WaterProperties.TrackedMax];

        [NonSerialized] private SimGraphBlock m_InterventionBlock;

        public void AllocateBlocks(ModelState state) {
            ClearBlocks();
            foreach(var organismId in state.Simulation.RelevantCritterIds()) {
                GetBlock(organismId, state);
            }
            foreach(var waterProp in state.Simulation.RelevantWaterProperties()) {
                GetBlock(waterProp, state);
            }
        }

        public void Intervene(SimulationDataCtrl.InterventionData data, ModelState state) {
            StringHash32 id = data.Target?.Id() ?? StringHash32.Null;
            
            if (m_InterventionBlock != null) {
                if (m_InterventionBlock.ActorId == id) {
                    return;
                }

                m_AllocatedBlockMap.Remove(m_InterventionBlock.ActorId);
                m_AllocatedBlocks.FastRemove(m_InterventionBlock);
                m_Blocks.Free(m_InterventionBlock);
                m_InterventionBlock = null;
            }

            if (!id.IsEmpty && !m_AllocatedBlockMap.ContainsKey(id)) {
                m_InterventionBlock = GetBlock(id, state);
                m_InterventionBlock.transform.SetAsFirstSibling();
            }
        }

        public void PopulateData(ModelState state, ModelProgressInfo info, SimRenderMask mask) {
            Populate(state.Simulation, m_AllocatedBlocks, mask, m_StressColor);
            if ((mask & SimRenderMask.Intervene) != 0) {
                PopulateTargets(state, info);
            } else {
                m_Targets.Reset();
            }
        }

        public void RenderData(SimRenderMask mask, bool onlyFirstPoint = false) {
            Render(onlyFirstPoint ? 1 : 99, m_AllocatedBlocks, mask);
        }

        private void PopulateTargets(ModelState state, ModelProgressInfo info) {
            if (info.Scope) {
                m_Targets.Reset();
                foreach(var target in info.Scope.InterventionTargets) {
                    var block = GetBlock(target.Id, state);
                    var targetObj = m_Targets.TempAlloc();
                    block.Intervention = targetObj;
                    targetObj.Object.Layout.SetParent(block.PredictGroup.transform, false);
                    targetObj.Object.MinValue = target.Population - target.Range;
                    targetObj.Object.MaxValue = target.Population + target.Range;
                    targetObj.Object.Background.color = block.PrimaryColor;
                }
            } else {
                m_Targets.Reset();
            }
        }

        public void ClearBlocks() {
            m_Blocks.Reset();
            m_AllocatedBlockMap.Clear();
            m_AllocatedBlocks.Clear();
            m_InterventionBlock = null;
            Array.Clear(m_AllocatedWaterMap, 0, m_AllocatedWaterMap.Length);
        }

        #region Retrieving Blocks

        private SimGraphBlock GetBlock(StringHash32 organismId, ModelState state) {
            SimGraphBlock block;
            if (!m_AllocatedBlockMap.TryGetValue(organismId, out block)) {
                block = m_Blocks.Alloc();
                block.ActorId = organismId;
                block.PropertyId = WaterPropertyId.NONE;
                BestiaryDesc info = Assets.Bestiary(organismId);
                block.PrimaryColor = info.Color();
                block.IconBG.SetColor(block.PrimaryColor * 0.5f);
                block.Icon.sprite = state.Simulation.Intervention.Target == info || state.Conceptual.GraphedEntities.Contains(info) ? info.Icon() : m_MissingOrganisms;
                m_AllocatedBlockMap.Add(organismId, block);
                m_AllocatedBlocks.Add(block);
            }
            return block;
        }

        private SimGraphBlock GetBlock(WaterPropertyId propertyId, ModelState state) {
            SimGraphBlock block = m_AllocatedWaterMap[(int) propertyId];
            if (block == null) {
                block = m_Blocks.Alloc();
                block.ActorId = null;
                block.PropertyId = propertyId;
                WaterPropertyDesc info = Assets.Property(propertyId);
                block.PrimaryColor = info.Color();
                block.IconBG.SetColor(block.PrimaryColor * 0.5f);
                block.Icon.sprite = info.Icon();
                m_AllocatedWaterMap[(int) propertyId] = block;
                m_AllocatedBlocks.Add(block);
            }
            return block;
        }

        #endregion // Retrieving Blocks

        #region Populating Blocks

        static public void Populate(SimulationDataCtrl data, ListSlice<SimGraphBlock> blocks, SimRenderMask mask, Color stressColor) {
            Vector2* pointBuffer = Frame.AllocArray<Vector2>(Simulation.MaxTicks + 1);
            StressedRegion* stressBuffer = Frame.AllocArray<StressedRegion>(Simulation.MaxTicks / 2);
            
            if ((mask & SimRenderMask.Historical) != 0) {
                SimSnapshot* historical = data.RetrieveHistoricalData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(historical, count, data.HistoricalProfile, blocks[i], SimRenderMask.Historical, pointBuffer, stressBuffer, stressColor);
                }
            }

            if ((mask & SimRenderMask.Player) != 0) {
                SimSnapshot* player = data.RetrievePlayerData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(player, count, data.PlayerProfile, blocks[i], SimRenderMask.Player, pointBuffer, stressBuffer, stressColor);
                }
            }

            if ((mask & SimRenderMask.Predict) != 0) {
                SimSnapshot* predict = data.RetrievePredictData(out uint countU);
                int count = (int) countU;
                for(int i = 0; i < blocks.Length; i++) {
                    PopulateBlock(predict, count, data.PredictProfile, blocks[i], SimRenderMask.Predict, pointBuffer, stressBuffer, stressColor);
                }
            }
        }

        static private void PopulateBlock(SimSnapshot* results, int count, SimProfile profile, SimGraphBlock block, SimRenderMask phase, Vector2* pointBuffer, StressedRegion* stressBuffer, Color stressColor) {
            ref Rect dataRange = ref GetDataRegion(block, phase);
            GraphLineRenderer line = GetLine(block, phase);
            if (block.PropertyId != WaterPropertyId.NONE) {
                dataRange = GenerateWater(results, count, block.PropertyId, block.PrimaryColor, pointBuffer, line);
            } else {
                int idx = profile.IndexOfActorType(block.ActorId);
                switch(phase) {
                    case SimRenderMask.Historical: {
                        dataRange = GenerateHistoricalPopulation(results, count, idx, block.PrimaryColor, pointBuffer, line);
                        break;
                    }
                    case SimRenderMask.Player:
                    case SimRenderMask.Predict: {
                        dataRange = GeneratePlayerPopulation(results, count, idx, block.PrimaryColor, stressColor, pointBuffer, stressBuffer, line);
                        break;
                    }
                }
            }
            line.InvalidateScale();
        }

        static private GraphLineRenderer GetLine(SimGraphBlock block, SimRenderMask phase) {
            switch(phase) {
                case SimRenderMask.Historical: {
                    return block.Historical;
                }
                case SimRenderMask.Player: {
                    return block.Player;
                }
                case SimRenderMask.Predict: {
                    return block.Predict;
                }
                default: {
                    throw new ArgumentOutOfRangeException("phase");
                }
            }
        }

        static private ref Rect GetDataRegion(SimGraphBlock block, SimRenderMask phase) {
            switch(phase) {
                case SimRenderMask.Historical: {
                    return ref block.LastRectHistorical;
                }
                case SimRenderMask.Player: {
                    return ref block.LastRectPlayer;
                }
                case SimRenderMask.Predict: {
                    return ref block.LastRectPredict;
                }
                default: {
                    throw new ArgumentOutOfRangeException("phase");
                }
            }
        }

        #endregion // Populating Blocks

        #region Rendering Blocks

        static public void Render(int pointCount, ListSlice<SimGraphBlock> blocks, SimRenderMask mask) {
            bool showHistorical = pointCount > 0 && (mask & SimRenderMask.Historical) != 0;
            bool showPlayer = pointCount > 0 && (mask & SimRenderMask.Player) != 0;
            bool showFill = showHistorical && showPlayer && (mask & SimRenderMask.Fill) != 0;
            bool showPredict = !showHistorical && !showPlayer && pointCount > 0 && (mask & SimRenderMask.Predict) != 0;
            bool showIntervene = showPredict && (mask & SimRenderMask.Intervene) != 0;

            for(int i = 0; i < blocks.Length; i++) {
                SimGraphBlock block = blocks[i];

                bool changed = false;
                changed |= Ref.Replace(ref block.Historical.PointRenderCount, pointCount);
                changed |= Ref.Replace(ref block.Player.PointRenderCount, pointCount);
                changed |= Ref.Replace(ref block.Predict.PointRenderCount, pointCount);

                if (showHistorical && showPlayer) {
                    Rect bounds = block.LastRectHistorical;
                    SimMath.CombineBounds(ref bounds, block.LastRectPlayer);
                    SimMath.FinalizeBounds(ref bounds);
                    block.LastRect = bounds;

                    changed |= block.Historical.ApplyScale(bounds);
                    changed |= block.Player.ApplyScale(bounds);
                    if (changed && showFill) {
                        block.Fill.LinesDirty();
                    }

                    block.IconPin.SetAnchorY(block.Historical.Points[0].y);
                } else if (showHistorical) {
                    Rect bounds = block.LastRectHistorical;
                    SimMath.FinalizeBounds(ref bounds);
                    block.LastRect = bounds;

                    changed |= block.Historical.ApplyScale(bounds);

                    block.IconPin.SetAnchorY(block.Historical.Points[0].y);
                } else if (showPlayer) {
                    Rect bounds = block.LastRectPlayer;
                    SimMath.FinalizeBounds(ref bounds);
                    block.LastRect = bounds;

                    changed |= block.Player.ApplyScale(bounds);
                } else if (showPredict) {
                    Rect bounds = block.LastRectPredict;
                    if (showIntervene && block.Intervention != null) {
                        bounds.yMax = Math.Max(bounds.yMax, block.Intervention.Object.MaxValue);
                    }
                    SimMath.FinalizeBounds(ref bounds);
                    block.LastRect = bounds;

                    changed |= block.Predict.ApplyScale(bounds);

                    if (showIntervene && pointCount < 2) {
                        block.IconPin.SetAnchorY(0.5f);
                    } else {
                        block.IconPin.SetAnchorY(block.Predict.Points[0].y);
                    }
                }

                if (showIntervene && block.Intervention != null) {
                    GraphTargetRegion interveneObj = block.Intervention;
                    float min = MathUtil.Remap(interveneObj.MinValue, block.LastRect.yMin, block.LastRect.yMax, 0, 1);
                    float max = MathUtil.Remap(interveneObj.MaxValue, block.LastRect.yMin, block.LastRect.yMax, 0, 1);
                    interveneObj.Layout.SetAnchorsY(min, max);
                }

                block.Historical.enabled = showHistorical;
                block.Player.enabled = showPlayer;
                block.Predict.enabled = showPredict;
                block.Fill.enabled = showFill;
                if (block.Intervention != null) {
                    block.Intervention.Object.Layout.gameObject.SetActive(showIntervene);
                }

                if (changed) {
                    block.Historical.SubmitChanges();
                    block.Player.SubmitChanges();
                    block.Predict.SubmitChanges();
                    block.Fill.LinesDirty();
                }
            }
        }

        #endregion // Populating Blocks

        #region Generating Lines

        static public Rect GenerateHistoricalPopulation(SimSnapshot* results, int count, int actorIdx, Color actorColor, Vector2* pointBuffer, GraphLineRenderer renderer) {
            Rect range = FillPopulationBuffer(results, count, actorIdx, pointBuffer, null, null, false);
            renderer.Colors = Array.Empty<Color>();
            renderer.SetColor(actorColor);
            renderer.EnsurePointBuffer(count);
            Unsafe.Copy(pointBuffer, count, renderer.Points);
            renderer.PointCount = count;
            renderer.PointRenderCount = count;
            return range;
        }

        static public Rect GeneratePlayerPopulation(SimSnapshot* results, int count, int actorIdx, Color actorColor, Color stressColor, Vector2* pointBuffer, StressedRegion* stressBuffer, GraphLineRenderer renderer) {
            int stressCount;
            Rect range = FillPopulationBuffer(results, count, actorIdx, pointBuffer, stressBuffer, &stressCount, false);
            renderer.EnsureColorBuffer(count);
            renderer.EnsurePointBuffer(count);
            Unsafe.Copy(pointBuffer, count, renderer.Points);
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
            renderer.PointRenderCount = count;
            return range;
        }

        static public Rect GenerateWater(SimSnapshot* results, int count, WaterPropertyId waterProperty, Color propertyColor, Vector2* pointBuffer, GraphLineRenderer renderer) {
            Rect range = FillWaterBuffer(results, count, waterProperty, pointBuffer);
            renderer.Colors = Array.Empty<Color>();
            renderer.SetColor(propertyColor);
            renderer.EnsurePointBuffer(count);
            Unsafe.Copy(pointBuffer, count, renderer.Points);
            renderer.PointCount = count;
            renderer.PointRenderCount = count;
            return range;
        }

        static private Rect FillPopulationBuffer(SimSnapshot* results, int resultCount, int organismIdx, Vector2* pointBuffer, StressedRegion* stressBuffer, int* stressRegionCount, bool trackStressed) {
            if (stressRegionCount != null) {
                *stressRegionCount = 0;
            }
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

        #endregion // Generating Lines
    }
}