using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace EasyAssetStreaming {
    static internal class MeshGeneration {
        #region Quad

        static private readonly Vector3[] s_QuadGeneratorVertices = new Vector3[4];
        static private readonly Color32[] s_QuadGeneratorColors = new Color32[4];

        // constants
        static private readonly Vector2[] s_QuadGeneratorUVs = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
        static private readonly ushort[] s_QuadGeneratorIndices = new ushort[] { 0, 1, 2, 3, 2, 1 };

        /// <summary>
        /// Generates a quad mesh.
        /// </summary>
        static public Mesh CreateQuad(Vector2 inSize, Vector2 inNormalizedPivot, Color32 inColor, Rect inUV, uint inTessellation, Mesh ioOverwrite, ref ulong ioHash) {
            inTessellation = Math.Min(inTessellation, 5);

            QuadMeshMeta newMeta;
            newMeta.Size = inSize;
            newMeta.Pivot = inNormalizedPivot;
            newMeta.Color = inColor;
            newMeta.UV = inUV;
            newMeta.Tessellation = inTessellation;

            ulong newHash = StreamingHelper.Hash(newMeta);
            if (ioOverwrite != null && ioHash == newHash) {
                return ioOverwrite;
            }

            ioHash = newHash;

            Mesh mesh = ioOverwrite;
            if (mesh == null) {
                mesh = new Mesh();
                #if UNITY_EDITOR
                mesh.name = "Quad" + inTessellation.ToString();
                #endif // UNITY_EDITOR
            }

            if (inTessellation == 0) {
                GenerateSimpleQuad(inSize, inNormalizedPivot, inColor, inUV, mesh);
            } else {
                GenerateTessellatedQuad(inSize, inNormalizedPivot, inColor, inUV, inTessellation, mesh);
            }
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            return mesh;
        }

        static private void GenerateSimpleQuad(Vector2 inSize, Vector2 inNormalizedPivot, Color32 inColor, Rect inUV, Mesh ioMesh) {
            float left = inSize.x * -inNormalizedPivot.x;
            float bottom = inSize.y * -inNormalizedPivot.y;
            float right = left + inSize.x;
            float top = bottom + inSize.y;

            s_QuadGeneratorVertices[0] = new Vector3(left, bottom, 0);
            s_QuadGeneratorVertices[1] = new Vector3(left, top, 0);
            s_QuadGeneratorVertices[2] = new Vector3(right, bottom, 0);
            s_QuadGeneratorVertices[3] = new Vector3(right, top, 0);

            s_QuadGeneratorColors[0] = inColor;
            s_QuadGeneratorColors[1] = inColor;
            s_QuadGeneratorColors[2] = inColor;
            s_QuadGeneratorColors[3] = inColor;

            float u0 = inUV.xMin, u1 = inUV.xMax,
                v0 = inUV.yMin, v1 = inUV.yMax;

            s_QuadGeneratorUVs[0] = new Vector2(u0, v0);
            s_QuadGeneratorUVs[1] = new Vector2(u0, v1);
            s_QuadGeneratorUVs[2] = new Vector2(u1, v0);
            s_QuadGeneratorUVs[3] = new Vector2(u1, v1);

            ioMesh.Clear(true);
            ioMesh.SetVertices(s_QuadGeneratorVertices);
            ioMesh.SetColors(s_QuadGeneratorColors);
            ioMesh.SetUVs(0, s_QuadGeneratorUVs);
            ioMesh.SetIndices(s_QuadGeneratorIndices, MeshTopology.Triangles, 0, false, 0);
        }

        static private unsafe void GenerateTessellatedQuad(Vector2 inSize, Vector2 inNormalizedPivot, Color32 inColor, Rect inUV, uint inTessellation, Mesh ioMesh) {
            int quadsPerRow = 1 << (int) inTessellation;
            int verticesPerRow = quadsPerRow + 1;
            int totalVertices = verticesPerRow * verticesPerRow;
            int totalQuads = quadsPerRow * quadsPerRow;
            int totalIndices = totalQuads * 6;

            Vector3* vertices = stackalloc Vector3[totalVertices];
            Color32* colors = stackalloc Color32[totalVertices];
            Vector2* uvs = stackalloc Vector2[totalVertices];
            ushort* indices = stackalloc ushort[totalIndices];

            float xMin = inSize.x * -inNormalizedPivot.x;
            float yMin = inSize.y * -inNormalizedPivot.y;
            float xInc = inSize.x / quadsPerRow;
            float yInc = inSize.y / quadsPerRow;

            float uMin = inUV.xMin;
            float vMin = inUV.yMin;
            float uInc = inUV.width / quadsPerRow;
            float vInc = inUV.height / quadsPerRow;

            int idx = 0;
            for(int y = 0; y < verticesPerRow; y++) {
                for(int x = 0; x < verticesPerRow; x++) {
                    vertices[idx] = new Vector3(xMin + xInc * x, yMin + yInc * y, 0);
                    colors[idx] = inColor;
                    uvs[idx] = new Vector2(uMin + uInc * x, vMin + vInc * y);
                    idx++;
                }
            }

            ushort* indicesWritePtr = indices;
            for(int i = 0; i < totalQuads; i++) {
                WriteTessellatedIndices(i, quadsPerRow, verticesPerRow, &indicesWritePtr);
            }

            ioMesh.Clear(true);
            using(var context = StreamingHelper.NewArrayContext()) {
                ioMesh.SetVertices<Vector3>(context.GetNativeArray(vertices, totalVertices), 0, totalVertices);
                ioMesh.SetColors<Color32>(context.GetNativeArray(colors, totalVertices));
                ioMesh.SetUVs<Vector2>(0, context.GetNativeArray(uvs, totalVertices));
                ioMesh.SetIndices<ushort>(context.GetNativeArray(indices, totalIndices), MeshTopology.Triangles, 0, false);
            }
        }

        static private unsafe void WriteTessellatedIndices(int inQuadIndex, int inQuadsPerRow, int inVertsPerRow, ushort** outVerts) {
            ushort v0 = (ushort) ((inQuadIndex / inQuadsPerRow) * inVertsPerRow + (inQuadIndex % inQuadsPerRow));
            ushort v2 = (ushort) (v0 + 1);
            ushort v1 = (ushort) (v0 + inVertsPerRow);
            ushort v3 = (ushort) (v1 + 1);
            ushort* arr = *outVerts;

            arr[0] = v0;
            arr[1] = v1;
            arr[2] = v2;
            arr[3] = v3;
            arr[4] = v2;
            arr[5] = v1;

            *outVerts += 6;
        }

        private struct QuadMeshMeta {
            public Vector2 Size;
            public Vector2 Pivot;
            public Color32 Color;
            public Rect UV;
            public uint Tessellation;
        }

        #endregion // Quad
    }
}