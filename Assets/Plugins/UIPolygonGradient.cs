/// Credit CiaccoDavide
/// Sourced from - http://ciaccodavi.de/unity/UIPolygon

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Primitives/UI Polygon Gradient")]
    public class UIPolygonGradient : UIPrimitiveBase
    {
        public bool fill = true;
        public float thickness = 5;
        [Range(3, 360)]
        public int sides = 3;
        [Range(0, 360)]
        public float rotation = 0;
        [Range(0, 1)]
        public float[] VerticesDistances = new float[3];
        public Color[] VerticesColors = new Color[3];
        private float size = 0;

        public void DrawPolygon(int _sides)
        {
            sides = _sides;
            VerticesDistances = new float[_sides + 1];
            for (int i = 0; i < _sides; i++) VerticesDistances[i] = 1; ;
            rotation = 0;
            SetAllDirty();
        }
        public void DrawPolygon(int _sides, float[] _VerticesDistances)
        {
            sides = _sides;
            VerticesDistances = _VerticesDistances;
            rotation = 0;
            SetAllDirty();
        }
        public void DrawPolygon(int _sides, float[] _VerticesDistances, float _rotation)
        {
            sides = _sides;
            VerticesDistances = _VerticesDistances;
            rotation = _rotation;
            SetAllDirty();
        }
        void Update()
        {
            size = rectTransform.rect.width;
            if (rectTransform.rect.width > rectTransform.rect.height)
                size = rectTransform.rect.height;
            else
                size = rectTransform.rect.width;
            thickness = (float)Mathf.Clamp(thickness, 0, size / 2);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector2 prevX = Vector2.zero;
            Vector2 prevY = Vector2.zero;
            Vector2 uv0 = new Vector2(0, 0);
            Vector2 uv1 = new Vector2(0, 1);
            Vector2 uv2 = new Vector2(1, 1);
            Vector2 uv3 = new Vector2(1, 0);
            Vector2 pos0;
            Vector2 pos1;
            Vector2 pos2;
            Vector2 pos3;
            Color32 c0, c1, c2, c3;
            float degrees = 360f / sides;
            int vertices = sides + 1;
            if (VerticesDistances.Length != vertices)
            {
                VerticesDistances = new float[vertices];
                for (int i = 0; i < vertices - 1; i++) VerticesDistances[i] = 1;
            }
            if (VerticesColors.Length != vertices)
            {
                VerticesColors = new Color[vertices];
                for (int i = 0; i < vertices - 1; i++) VerticesColors[i] = Color.white;
            }
            // last vertex is also the first!
            VerticesDistances[vertices - 1] = VerticesDistances[0];
            VerticesColors[vertices - 1] = VerticesColors[0];
            float pivotX = -rectTransform.pivot.x;
            for (int i = 0; i < vertices; i++)
            {
                float outer = pivotX * size * VerticesDistances[i];
                float inner = pivotX * size * VerticesDistances[i] + thickness;
                float rad = Mathf.Deg2Rad * (i * degrees + rotation);
                float c = Mathf.Cos(rad);
                float s = Mathf.Sin(rad);
                uv0 = new Vector2(0, 1);
                uv1 = new Vector2(1, 1);
                uv2 = new Vector2(1, 0);
                uv3 = new Vector2(0, 0);
                pos0 = prevX;
                pos1 = new Vector2(outer * c, outer * s);
                if (fill)
                {
                    pos2 = Vector2.zero;
                    pos3 = Vector2.zero;
                }
                else
                {
                    pos2 = new Vector2(inner * c, inner * s);
                    pos3 = prevY;
                }
                prevX = pos1;
                prevY = pos2;
                c0 = c3 = VerticesColors[(i + vertices - 1) % vertices];
                c1 = c2 = VerticesColors[i];
                vh.AddUIVertexQuad(SetVbo(new[] { pos0, pos1, pos2, pos3 }, new[] { uv0, uv1, uv2, uv3 }, new Color32[] { c0, c1, c2, c3 }));
            }
        }

        protected UIVertex[] SetVbo(Vector2[] vertices, Vector2[] uvs, Color32[] colors)
        {
            UIVertex[] vbo = new UIVertex[4];
            for (int i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = colors[i] * this.color;
                vert.position = vertices[i];
                vert.uv0 = uvs[i];
                vbo[i] = vert;
            }
            return vbo;
        }
    }
}