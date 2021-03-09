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
    public class GraphPlot : MonoBehaviour
    {
        [Serializable] private class PointPool : SerializablePool<GraphPoint> { }

        #region Inspector

        [SerializeField] private Vector2 m_PointScale = Vector2.one;

        [Header("Pools")]
        [SerializeField] private PointPool m_PointPool = null;

        #endregion // Inspector

        private Dictionary<StringHash32, GraphPoint> m_PointMap = new Dictionary<StringHash32, GraphPoint>();
        private Rect m_LastRect;

        public void Clear()
        {
            m_PointMap.Clear();
            m_PointPool.Reset();
        }

        public Rect Range { get { return m_LastRect; } }

        public Rect LoadTargets(ModelingScenarioData inScenario)
        {
            Assert.NotNull(inScenario);

            float endX = inScenario.TotalTicks() * inScenario.TickScale();
            Rect varRange = new Rect(0, 0, endX, 0);

            using(PooledSet<StringHash32> unusedPoints = PooledSet<StringHash32>.Create(m_PointMap.Keys))
            {
                GraphPoint point;

                var targets = inScenario.PredictionTargets();
                int targetCount = targets.Length;
                
                for(int targetIdx = 0; targetIdx < targetCount; ++targetIdx)
                {
                    StringHash32 id = targets[targetIdx].Id;
                    if (!inScenario.ShouldGraph(id))
                        continue;

                    BestiaryDesc critterEntry = Services.Assets.Bestiary[id];
                    BFBody body = critterEntry.FactOfType<BFBody>();
                    float populationScale = body.StartingMass();

                    unusedPoints.Remove(id);
                    if (!m_PointMap.TryGetValue(id, out point))
                    {
                        point = m_PointPool.Alloc();
                        m_PointMap[id] = point;
                        point.Renderer.color = critterEntry.Color();
                        point.Renderer.transform.localScale = new Vector3(m_PointScale.x, m_PointScale.y, 1);
                    }

                    float mass = targets[targetIdx].Population * populationScale;
                    point.SetPoint(endX, mass);
                    if (mass > varRange.height)
                        varRange.height = mass;
                }

                foreach(var pointId in unusedPoints)
                {
                    point = m_PointMap[pointId];
                    m_PointPool.Free(point);
                    m_PointMap.Remove(pointId);
                }
            }

            return (m_LastRect = varRange);
        }

        public void RenderPoints(Rect inRange)
        {
            foreach(var point in m_PointPool.ActiveObjects)
            {
                point.Render(inRange);
            }
        }

        // TODO: Display environment vars?
    }
}