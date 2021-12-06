using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Modeling {
    public unsafe class SimLineGraph : MonoBehaviour {
        #region Inspector

        [SerializeField] private float m_LineThickness = 1;
        [SerializeField] private GraphLine.Pool m_LinePool = null;

        #endregion // Inspector

        private Dictionary<StringHash32, GraphLine> m_LineMap = new Dictionary<StringHash32, GraphLine>();
        private Rect m_LastRect;

        public void Clear() {
            m_LineMap.Clear();
            m_LinePool.Reset();
        }

        public Rect Range { get { return m_LastRect; } }
        public Rect BoundedRange(int pointCount) {
            float maxHeight = 0;
            foreach(var line in m_LinePool.ActiveObjects) {
                maxHeight = Math.Max(line.MaximumHeight(pointCount), maxHeight);
            }
            Rect r = m_LastRect;
            r.height = maxHeight;
            return r;
        }

        public unsafe Rect LoadOrganisms(SimSnapshot* results, uint resultCount, uint timestampOffset, SimProfile profile, Predicate<StringHash32> predicate) {
            Assert.True(results != null);
            Assert.NotNull(profile);

            Rect varRange = new Rect(0, 0, timestampOffset + resultCount - 1, 0);

            using(PooledSet<StringHash32> unusedLines = PooledSet<StringHash32>.Create(m_LineMap.Keys)) {
                GraphLine line;

                int critterCount = profile.ActorCount;
                uint tickCount = resultCount;
                SimProfile.ActorInfo* actorInfo;

                // Iterate over critters first, points second
                // this will reduce the number of lookups to the map considerably
                for (int critterIdx = 0; critterIdx < critterCount; critterIdx++) {
                    actorInfo = &profile.Actors[critterIdx];
                    StringHash32 id = actorInfo->Id;
                    if (!predicate(id))
                        continue;

                    BestiaryDesc critterEntry = Assets.Bestiary(id);
                    BFBody body = critterEntry.FactOfType<BFBody>();
                    float populationScale = body.MassPerPopulation * body.MassDisplayScale;

                    unusedLines.Remove(id);
                    if (!m_LineMap.TryGetValue(id, out line)) {
                        line = m_LinePool.Alloc();
                        m_LineMap[id] = line;
                        line.Renderer.color = critterEntry.Color();
                        line.Renderer.LineThickness = m_LineThickness;
                    }

                    line.ClearPoints();

                    float mass;
                    for (int tickIdx = 0; tickIdx < tickCount; ++tickIdx) {
                        mass = results[tickIdx].Populations[critterIdx] * populationScale;
                        float stressedRatio = results[tickIdx].StressedRatio[critterIdx] / 128f;
                        line.AddPoint(timestampOffset + tickIdx, mass);

                        if (mass > varRange.height)
                            varRange.height = mass;
                    }
                }

                foreach (var lineId in unusedLines) {
                    line = m_LineMap[lineId];
                    m_LinePool.Free(line);
                    m_LineMap.Remove(lineId);
                }
            }

            return (m_LastRect = varRange);
        }

        public unsafe Rect LoadProperties(SimSnapshot* results, uint resultCount, uint timestampOffset, Predicate<WaterPropertyId> predicate) {
            Assert.True(results != null);

            Rect varRange = new Rect(0, 0, timestampOffset + resultCount - 1, 0);

            using(PooledSet<StringHash32> unusedLines = PooledSet<StringHash32>.Create(m_LineMap.Keys)) {
                GraphLine line;

                int propertyCount = (int) WaterPropertyId.TRACKED_COUNT;
                uint tickCount = resultCount;

                for (int propertyIdx = 0; propertyIdx < propertyCount; propertyIdx++) {
                    WaterPropertyId propId = (WaterPropertyId) propertyIdx;
                    if (!predicate(propId))
                        continue;
                    
                    WaterPropertyDesc propertyEntry = Assets.Property(propId);
                    StringHash32 id = propertyEntry.Id();

                    unusedLines.Remove(id);
                    if (!m_LineMap.TryGetValue(id, out line)) {
                        line = m_LinePool.Alloc();
                        m_LineMap[id] = line;
                        line.Renderer.color = propertyEntry.Color();
                        line.Renderer.LineThickness = m_LineThickness;
                    }

                    line.ClearPoints();

                    float value;
                    for (int tickIdx = 0; tickIdx < tickCount; ++tickIdx) {
                        value = results[tickIdx].Water[propId];
                        line.AddPoint(timestampOffset + tickIdx, value);

                        if (value > varRange.height)
                            varRange.height = value;
                    }
                }

                foreach (var lineId in unusedLines) {
                    line = m_LineMap[lineId];
                    m_LinePool.Free(line);
                    m_LineMap.Remove(lineId);
                }
            }

            return (m_LastRect = varRange);
        }

        public void RenderLines(Rect inRange, int inPointCount = -1, bool inbRenderInitialPoint = false) {
            foreach (var line in m_LinePool.ActiveObjects) {
                line.Render(inRange, inPointCount, inbRenderInitialPoint);
            }
        }
    }
}