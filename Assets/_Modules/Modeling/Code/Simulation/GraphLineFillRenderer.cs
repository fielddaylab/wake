using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    [RequireComponent(typeof(CanvasRenderer))]
    public unsafe class GraphLineFillRenderer : MaskableGraphic {
        public const int MaxRegions = 16;
        private const int MaxRegionVertex = 40;

        static private readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
        static private readonly Vector3 s_DefaultNormal = Vector3.back;

        public struct Region {
            public sbyte Sign;
            public ushort StartIdx;
            public ushort EndIdx;
            public float StartIntersection;
            public float EndIntersection;
            public Vector2 Center;
        }

        #region Inspector

        [Header("Data")]
        public GraphLineRenderer LineA;
        public GraphLineRenderer LineB;
        public int PointCount = 0;

        [Header("Region")]
        public Color PositiveColor = Color.red;
        public Color NegativeColor = Color.red;

        #endregion // Inspector

        [NonSerialized] public RingBuffer<Region> Regions = new RingBuffer<Region>(2, RingBufferMode.Expand);

        public void SubmitChanges() {
            SetVerticesDirty();
        }

        [ContextMenu("Refresh")]
        public void LinesDirty() {
            AnalyzeRegions();
            SetVerticesDirty();
        }

        public override void SetAllDirty() {
            base.SetAllDirty();

            AnalyzeRegions();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            if (Regions.Count == 0) {
                return;
            }

            int renderedRegionCount = 0;
            int pointCutoff = PointCount;
            int quadCount = 0;
            for(int i = 0; i < Regions.Count; i++) {
                ref var region = ref Regions[i];
                if (region.StartIdx <= pointCutoff) {
                    renderedRegionCount++;
                    quadCount += Math.Max(Math.Min(pointCutoff, region.EndIdx) - region.StartIdx + 1, 0);
                    if (pointCutoff <= region.EndIdx) {
                        break;
                    }
                } else {
                    break;
                }
            }

            if (quadCount < 1) {
                return;
            }

            var r = GetPixelAdjustedRect();
            float left = r.xMin, bottom = r.yMin, width = r.width, height = r.height;

            UIVertex* vertBuffer = Frame.AllocArray<UIVertex>(4);
            
            for(int i = 0; i < 4; i++) {
                vertBuffer[i].normal = s_DefaultNormal;
                vertBuffer[i].tangent = s_DefaultTangent;
            }

            Vector2 a, b, c, d, e;
            int vertCount = 0;
            for(int i = 0; i < renderedRegionCount; i++) {
                ref var region = ref Regions[i];
                vertBuffer[0].color = vertBuffer[1].color
                    = vertBuffer[2].color = vertBuffer[3].color
                    = (region.Sign > 0 ? PositiveColor : NegativeColor) * this.color;

                int end = Mathf.Min(region.EndIdx, pointCutoff);

                int vertBase;

                for(int j = region.StartIdx; j < end; j++) {
                    a = LineA.Points[j];
                    b = LineB.Points[j];

                    vertBuffer[0].position = new Vector2(left + a.x * width, bottom + a.y * height);
                    vertBuffer[1].position = new Vector2(left + b.x * width, bottom + b.y * height);

                    vertBase = vertCount;
                    vh.AddVert(vertBuffer[0]);
                    vh.AddVert(vertBuffer[1]);
                    vertCount += 2;

                    if (j > 0 && j == region.StartIdx && region.StartIntersection > 0) {
                        e = Vector2.Lerp(LineA.Points[j - 1], a, region.StartIntersection);

                        vertBuffer[2].position = new Vector2(left + e.x * width, bottom + e.y * height);

                        vh.AddVert(vertBuffer[2]);

                        vh.AddTriangle(vertBase, vertBase + 1, vertCount);
                        vertCount++;
                    }

                    if (j + 1 < LineA.Points.Length && j + 1 < LineB.Points.Length) {
                        c = LineB.Points[j + 1];
                        d = LineA.Points[j + 1];

                        if (j + 1 == region.EndIdx) {
                            e = Vector2.Lerp(a, d, region.EndIntersection);

                            vertBuffer[2].position = new Vector2(left + e.x * width, bottom + e.y * height);

                            vh.AddVert(vertBuffer[2]);

                            vh.AddTriangle(vertBase, vertBase + 1, vertCount);
                            vertCount++;
                        } else if (j < end - 1) {
                            vertBuffer[2].position = new Vector2(left + c.x * width, bottom + c.y * height);
                            vertBuffer[3].position = new Vector2(left + d.x * width, bottom + d.y * height);

                            vh.AddVert(vertBuffer[2]);
                            vh.AddVert(vertBuffer[3]);

                            vh.AddTriangle(vertBase, vertBase + 1, vertCount);
                            vh.AddTriangle(vertCount, vertCount + 1, vertBase);
                            vertCount += 2;
                        }
                    }
                }
            }
        }

        #region Region Analysis

        public void AnalyzeRegions() {
            Regions.Clear();

            if (LineA == null || LineA.Points == null || LineB == null || LineB.Points == null) {
                return;
            }

            int pointCount = Mathf.Min(LineA.PointCount, LineA.Points.Length, LineB.PointCount, LineB.Points.Length);

            Region* allRegions = Frame.AllocArray<Region>(MaxRegions);
            Region* currentRegionPtr = allRegions;
            int regionCount = 0;
            int currentSign = 0;
            Vector2* regionCenterSum = Frame.AllocArray<Vector2>(MaxRegionVertex);
            int currentRegionPointCount = 0;

            // this is hardcoded for differences on the y axis.
            // if we want x-axis diff instead, this will require modification.
            Vector2 a = default, b = default;
            Vector2 prevA, prevB, polyA, polyB;
            for(int i = 0; i < pointCount; i++) {
                prevA = a;
                prevB = b;

                a = polyA = LineA.Points[i];
                b = polyB = LineB.Points[i];

                float diff = b.y - a.y;
                int sign = Mathf.Approximately(diff, 0) ? 0 : (int) Math.Sign(diff);
                if (sign == currentSign) {
                    if (sign != 0) {
                        Accumulate(regionCenterSum, ref currentRegionPointCount, polyA, polyB);
                    }
                    continue;
                }

                bool intersect = sign != 0 && sign == -currentSign;
                float intersectStart = 0;
                float intersectEnd = 1;
                if (intersect && i > 0) {
                    intersectStart = intersectEnd = IntersectionPoint(prevA, a, prevB, b);
                    polyA = Vector2.Lerp(prevA, a, intersectStart);
                    polyB = Vector2.Lerp(prevB, b, intersectStart);
                }

                bool wasFlat = currentSign == 0 && i > 0;

                if (currentSign != 0) {
                    currentRegionPtr->EndIdx = (ushort) i;
                    currentRegionPtr->EndIntersection = intersectEnd;
                    Accumulate(regionCenterSum, ref currentRegionPointCount, polyA, polyB);
                    currentRegionPtr->Center = Average(regionCenterSum, currentRegionPointCount);
                    currentRegionPointCount = 0;
                    currentRegionPtr++;
                }

                currentSign = sign;

                if (sign != 0) {
                    regionCount++;

                    Assert.True(regionCount <= MaxRegions);
                    currentRegionPtr->StartIdx = (ushort) (i + (wasFlat ? -1 : 0));
                    currentRegionPtr->StartIntersection = intersectStart;
                    currentRegionPtr->Sign = (sbyte) sign;
                    Accumulate(regionCenterSum, ref currentRegionPointCount, polyA, polyB);
                }
            }

            if (currentSign != 0) {
                currentRegionPtr->EndIdx = (ushort) pointCount;
                currentRegionPtr->EndIntersection = 0;
                currentRegionPtr->Center = Average(regionCenterSum, currentRegionPointCount);
                currentRegionPointCount = 0;
            }

            for(int i = 0; i < regionCount; i++) {
                Regions.PushBack(allRegions[i]);
            }
        }

        static private void Accumulate(Vector2* buffer, ref int count, Vector2 valA, Vector2 valB) {
            Assert.True(count < MaxRegionVertex);
            buffer[count] = valA;
            buffer[count + 1] = valB;
            count += 2;
        }

        static private Vector2 Average(Vector2* buffer, int count) {
            if (count == 0) {
                return Vector2.zero;
            }
            Vector2 accum = buffer[0];
            for(int i = 1; i < count; i++) {
                accum += buffer[i];
            }
            return accum / count;
        }

        static private float IntersectionPoint(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
            Vector2 a = new Vector2(a2.x - a1.x, a2.y - a1.y);
            Vector2 b = new Vector2(b2.x - b1.x, b2.y - b1.y);
            float perp = a.x * b.y - a.y * b.x;

            Assert.True(perp != 0);

            Vector2 c = new Vector2(b1.x - a1.x, b1.y - a1.y);
            return (c.x * b.y - c.y * b.x) / perp;
        }

        #endregion // Region Analysis

        #if UNITY_EDITOR

        protected override void OnValidate() {
            if (!Application.IsPlaying(this)) {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                    return;
                }
            }
            base.OnValidate();
        }

        #endif // UNITY_EDITOR
    }
}