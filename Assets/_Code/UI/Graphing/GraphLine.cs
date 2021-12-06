using UnityEngine;
using BeauUtil;
using UnityEngine.UI.Extensions;
using System;
using BeauPools;

namespace Aqua
{
    public class GraphLine : MonoBehaviour
    {
        [Serializable] public class Pool : SerializablePool<GraphLine> { }

        #region Inspector

        [SerializeField] private UILineRenderer m_Renderer = null;
        [SerializeField] private GraphPoint m_InitialPointRenderer = null;

        #endregion // Inspector

        [NonSerialized] private RingBuffer<Vector2> m_RawPoints = new RingBuffer<Vector2>(32, RingBufferMode.Expand);
        [NonSerialized] private Vector2[] m_RelativePoints;

        public UILineRenderer Renderer { get { return m_Renderer; } }
        public GraphPoint PointRenderer { get { return m_InitialPointRenderer; } }

        public void ClearPoints()
        {
            m_RawPoints.Clear();
        }

        public void AddPoint(Vector2 inPoint)
        {
            m_RawPoints.PushBack(inPoint);
        }

        public void AddPoint(float inX, float inY)
        {
            m_RawPoints.PushBack(new Vector2(inX, inY));
        }

        public float MaximumHeight(int inPointCount = -1)
        {
            if (inPointCount < 0 || inPointCount > m_RawPoints.Count)
                inPointCount = m_RawPoints.Count;

            float maxHeight = 0;
            for(int i = 0; i < inPointCount; i++)
            {
                maxHeight = Math.Max(m_RawPoints[i].y, maxHeight);
            }
            
            return maxHeight;
        }

        public void Render(Rect inBounds, int inPointCount = -1, bool inRenderInitial = false)
        {
            if (inPointCount < 0 || inPointCount > m_RawPoints.Count)
                inPointCount = m_RawPoints.Count;
            
            Array.Resize(ref m_RelativePoints, inPointCount);

            float xMin = inBounds.xMin, xMax = inBounds.xMax, yMin = inBounds.yMin, yMax = inBounds.yMax;

            for(int i = 0, len = m_RelativePoints.Length; i < len; i++)
            {
                ref Vector2 rawPoint = ref m_RawPoints[i];
                m_RelativePoints[i] = new Vector2(
                    MathUtil.Remap(rawPoint.x, xMin, xMax, 0, 1),
                    MathUtil.Remap(rawPoint.y, yMin, yMax, 0, 1)
                );
            }

            m_Renderer.RelativeSize = true;
            m_Renderer.Points = m_RelativePoints;

            if (m_InitialPointRenderer)
            {
                if (inPointCount > 0 && inRenderInitial)
                {
                    m_InitialPointRenderer.gameObject.SetActive(true);
                    m_InitialPointRenderer.SetPoint(m_RawPoints[0]);
                    m_InitialPointRenderer.Render(inBounds);
                }
                else
                {
                    m_InitialPointRenderer.gameObject.SetActive(false);
                }
            }
        }
    }
}