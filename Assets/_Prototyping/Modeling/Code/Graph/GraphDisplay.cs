using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauPools;
using System;
using System.Collections.Generic;
using BeauUtil.Debugger;

namespace ProtoAqua.Modeling
{
    public class GraphDisplay : MonoBehaviour
    {
        [Serializable] private class LinePool : SerializablePool<GraphLine> { }

        #region Inspector

        [SerializeField] private float m_LineThickness = 1;

        [Header("Pools")]
        [SerializeField] private LinePool m_LinePool = null;

        #endregion // Inspector

        private Dictionary<StringHash32, GraphLine> m_LineMap = new Dictionary<StringHash32, GraphLine>();
        private Rect m_LastRect;

        public void Clear()
        {
            m_LineMap.Clear();
            m_LinePool.Reset();
        }

        public Rect Range { get { return m_LastRect; } }

        public Rect LoadTargets(ModelingScenarioData inScenario)
        {
            Assert.NotNull(inScenario);

            float endX = inScenario.TotalTicks() * inScenario.TickScale();
            Rect varRange = new Rect(0, 0, endX, 0);

            using(PooledSet<StringHash32> unusedLines = PooledSet<StringHash32>.Create(m_LineMap.Keys))
            {
                GraphLine line;

                var critterTargets = inScenario.PredictionTargets();
                int critterCount = critterTargets.Length;

                // Iterate over critters first, points second
                // this will reduce the number of lookups to the map considerably
                for(int critterIdx = 0; critterIdx < critterCount; ++critterIdx)
                {
                    ActorCountRange critterRange = critterTargets[critterIdx];

                    StringHash32 id = critterRange.Id;
                    if (!inScenario.ShouldGraph(id))
                        continue;

                    BestiaryDesc critterEntry = Services.Assets.Bestiary[id];
                    BFBody body = critterEntry.FactOfType<BFBody>();
                    float populationScale = body.MassPerPopulation() * body.MassDisplayScale();

                    unusedLines.Remove(id);
                    if (!m_LineMap.TryGetValue(id, out line))
                    {
                        line = m_LinePool.Alloc();
                        m_LineMap[id] = line;
                        line.Renderer.color = critterEntry.Color();
                        line.Renderer.LineThickness = m_LineThickness;
                    }

                    line.ClearPoints();

                    float minMass = ((float) critterRange.Population - critterRange.Range) * populationScale;
                    float maxMass = ((float) critterRange.Population + critterRange.Range) * populationScale;

                    line.AddPoint(endX, minMass);
                    line.AddPoint(endX, maxMass);

                    if (maxMass > varRange.height)
                        varRange.height = maxMass;
                }

                foreach(var lineId in unusedLines)
                {
                    line = m_LineMap[lineId];
                    m_LinePool.Free(line);
                    m_LineMap.Remove(lineId);
                }
            }

            return (m_LastRect = varRange);
        }

        public Rect LoadCritters(SimulationResult[] inResults, ModelingScenarioData inScenario)
        {
            Assert.NotNull(inResults);
            Assert.NotNull(inScenario);

            Rect varRange = new Rect(0, 0, inResults[inResults.Length - 1].Timestamp, 0);

            using(PooledSet<StringHash32> unusedLines = PooledSet<StringHash32>.Create(m_LineMap.Keys))
            {
                GraphLine line;

                int critterCount = inResults[0].Actors.Count;
                int tickCount = inResults.Length;

                // Iterate over critters first, points second
                // this will reduce the number of lookups to the map considerably
                for(int critterIdx = 0; critterIdx < critterCount; ++critterIdx)
                {
                    StringHash32 id = inResults[0].Actors[critterIdx].Id;
                    if (!inScenario.ShouldGraph(id))
                        continue;

                    int resultIdx = inResults[0].IndexOf(id); // this only works if this appears in the same place every time!
                    BestiaryDesc critterEntry = Services.Assets.Bestiary[id];
                    BFBody body = critterEntry.FactOfType<BFBody>();
                    float populationScale = body.MassPerPopulation() * body.MassDisplayScale();

                    // if population starts at 0, don't graph
                    if (inResults[0].Actors[resultIdx].Population == 0)
                        continue;

                    unusedLines.Remove(id);
                    if (!m_LineMap.TryGetValue(id, out line))
                    {
                        line = m_LinePool.Alloc();
                        m_LineMap[id] = line;
                        line.Renderer.color = critterEntry.Color();
                        line.Renderer.LineThickness = m_LineThickness;
                    }

                    line.ClearPoints();

                    CritterResult result;
                    float mass;
                    for(int tickIdx = 0; tickIdx < tickCount; ++tickIdx)
                    {
                        result = inResults[tickIdx].Actors[resultIdx];
                        Assert.True(result.Id == id, "Result index desync - expected {0}, got {1}", id.ToDebugString(), result.Id.ToDebugString());

                        mass = result.Population * populationScale;
                        line.AddPoint(inResults[tickIdx].Timestamp, mass);

                        if (mass > varRange.height)
                            varRange.height = mass;
                    }
                }

                foreach(var lineId in unusedLines)
                {
                    line = m_LineMap[lineId];
                    m_LinePool.Free(line);
                    m_LineMap.Remove(lineId);
                }
            }

            return (m_LastRect = varRange);
        }

        public void RenderLines(Rect inRange)
        {
            foreach(var line in m_LinePool.ActiveObjects)
            {
                line.Render(inRange);
            }
        }

        // TODO: Display environment vars?
    }
}