using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    [RequireComponent(typeof(CanvasRenderer))]
    public unsafe class GraphLineRenderer : MaskableGraphic {
        static private readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
        static private readonly Vector3 s_DefaultNormal = Vector3.back;

        #region Inspector

        [Header("Data")]
        public Vector2[] Points = Array.Empty<Vector2>();
        public Color[] Colors = Array.Empty<Color>();
        public int PointCount = 0;

        [Header("Line Properties")]
        [SerializeField] private float m_LineThickness = 1;
        [SerializeField] private Texture2D m_Texture = null;
        [SerializeField] private float m_TextureWrapWidth = 100;

        #endregion // Inspector

        public Texture2D Texture {
            get { return m_Texture; }
            set { if (Ref.Replace(ref m_Texture, value)) SetMaterialDirty(); }
        }

        public float LineThickness {
            get { return m_LineThickness; }
            set { if (Ref.Replace(ref m_LineThickness, value)) SetVerticesDirty(); }
        }

        public float TextureWrapWidth {
            get { return m_TextureWrapWidth; }
            set { if (Ref.Replace(ref m_TextureWrapWidth, value)) SetVerticesDirty(); }
        }

        public override Texture mainTexture { 
            get { return m_Texture != null ? m_Texture : base.mainTexture; }
        }

        protected GraphLineRenderer() {
            useLegacyMeshGeneration = false;
        }

        public void EnsurePointBuffer(int count) {
            if (Points == null || Points.Length < count) {
                count = (int) Unsafe.AlignUp8((uint) count);
                Array.Resize(ref Points, count);
            }
        }

        public void EnsureColorBuffer(int count) {
            if (Colors == null || Colors.Length < count) {
                count = (int) Unsafe.AlignUp8((uint) count);
                Array.Resize(ref Colors, count);
            }
        }

        public void SubmitChanges() {
            if (!IsActive()) {
                return;
            }
            
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            if (Points == null || m_LineThickness <= 0) {
                return;
            }

            int pointCount = Mathf.Clamp(PointCount, 0, Points.Length);
            int segmentCount = pointCount - 1;
            if (segmentCount <= 0) {
                return;
            }

            int colorCount = Colors == null ? 0 : Colors.Length;

            var r = GetPixelAdjustedRect();
            float left = r.xMin, bottom = r.yMin, width = r.width, height = r.height;

            Vector2* segmentVerts = Frame.AllocArray<Vector2>(segmentCount * 4);
            float* uvDist = Frame.AllocArray<float>(segmentCount);
            UIVertex* vertBuffer = Frame.AllocArray<UIVertex>(4);

            // generate segment vertices
            for(int i = 0; i < segmentCount; i++) {
                Vector2* segStart = segmentVerts + (i * 4);
                Vector2 a = Points[i];
                Vector2 b = Points[i + 1];
                a.x = left + a.x * width;
                a.y = bottom + a.y * height;
                b.x = left + b.x * width;
                b.y = bottom + b.y * height;

                Vector2 hypotenuse = b - a;
                float dist = hypotenuse.magnitude;
                Vector2 normal = hypotenuse / dist * (m_LineThickness * 0.5f);
                normal.Set(-normal.y, normal.x);

                segStart[0] = a - normal;
                segStart[1] = a + normal;
                segStart[2] = b + normal;
                segStart[3] = b - normal;

                uvDist[i] = dist / m_TextureWrapWidth;
            }

            // TODO: line joins?
            // otherwise, can save on allocations significantly

            for(int i = 0; i < 4; i++) {
                vertBuffer[i].normal = s_DefaultNormal;
                vertBuffer[i].tangent = s_DefaultTangent;
            }

            // output verts
            float uvStart = 0;
            for(int i = 0; i < segmentCount; i++) {
                Vector2* segStart = segmentVerts + (i * 4);
                float uvEnd = uvStart + uvDist[i];
                
                vertBuffer[0].color = vertBuffer[1].color
                    = vertBuffer[2].color = vertBuffer[3].color = colorCount == 0 ? this.color : Colors[Mathf.Min(i, colorCount - 1)] * this.color;

                vertBuffer[0].position = segStart[0];
                vertBuffer[1].position = segStart[1];
                vertBuffer[2].position = segStart[2];
                vertBuffer[3].position = segStart[3];

                vertBuffer[0].uv0 = new Vector2(uvStart, 0);
                vertBuffer[1].uv0 = new Vector2(uvStart, 1);
                vertBuffer[2].uv0 = new Vector2(uvEnd, 1);
                vertBuffer[3].uv0 = new Vector2(uvEnd, 0);

                vh.AddVert(vertBuffer[0]);
                vh.AddVert(vertBuffer[1]);
                vh.AddVert(vertBuffer[2]);
                vh.AddVert(vertBuffer[3]);

                vh.AddTriangle(i * 4 , i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2 , i * 4 + 3, i * 4);

                uvStart = uvEnd;
            }
        }
    
        #if UNITY_EDITOR

        protected override void OnValidate() {
            if (!Frame.IsActive(this)) {
                return;
            }
            base.OnValidate();
        }

        #endif // UNITY_EDITOR
    }
}