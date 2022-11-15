using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif // UNITY_EDITOR

namespace Aqua
{
    [RequireComponent(typeof(EdgeCollider2D))]
    public class TerrainEdgeCollider2D : MonoBehaviour
    {
        #region Inspector

        [SerializeField, HideInInspector] private EdgeCollider2D m_Collider = null;
        [SerializeField] private float m_EdgeOffset = 0;
        [SerializeField] private float m_ZRadius = 1;
        [SerializeField, Range(4, 12)] private int m_Resolution = 8;
        [SerializeField, Range(0, 1)] private float m_Tolerance = 0.2f;
        [SerializeField, Range(0, 1)] private float m_ClampedMin = 0;
        [SerializeField, Range(0, 1)] private float m_ClampedMax = 1;

        #endregion // Inspector

        #if UNITY_EDITOR

        private void Reset() {
            m_Collider = GetComponent<EdgeCollider2D>();
        }
        
        private void Rebuild()
        {
            Terrain terrain = Terrain.activeTerrain;
            Vector3 terrainPos = terrain.GetPosition();
            float terrainY = terrainPos.y;

            Transform selfTransform = transform;
            Vector3 selfPos = selfTransform.position;
            
            Bounds b = terrain.terrainData.bounds;
            b.center += terrainPos;
            float terrainWidth = b.size.x;

            selfPos.x = terrainPos.x;
            selfPos.y = terrainPos.y;

            Undo.RecordObject(selfTransform, "Wrapping collider edge to terrain");
            Undo.RecordObject(m_Collider, "Wrapping collider edge to terrain");

            selfTransform.SetPositionAndRotation(selfPos, Quaternion.identity);

            int numSamples = 1 + 2 << m_Resolution;
            float xMin = b.min.x + m_ClampedMin * terrainWidth;
            float xStep = terrainWidth * (m_ClampedMax - m_ClampedMin) / (numSamples - 1);

            List<Vector2> points = new List<Vector2>(numSamples);
            Vector3 checkPoint = selfPos;
            float height;

            for(int i = 0; i < numSamples; i++) {
                checkPoint.x = xMin + xStep * i;
                height = terrain.SampleHeight(checkPoint);
                if (m_ZRadius > 0) {
                    checkPoint.z += m_ZRadius;
                    height = Math.Max(height, terrain.SampleHeight(checkPoint));
                    checkPoint.z -= m_ZRadius * 2;
                    height = Math.Max(height, terrain.SampleHeight(checkPoint));
                    checkPoint.z = selfPos.z;
                }

                checkPoint.y  = selfPos.y + height + m_EdgeOffset;
                points.Add(selfTransform.InverseTransformPoint(checkPoint));
            }

            List<Vector2> simplified = new List<Vector2>(points.Count);
            LineUtility.Simplify(points, m_Tolerance, simplified);

            m_Collider.points = simplified.ToArray();
            EditorUtility.SetDirty(selfTransform);
            EditorUtility.SetDirty(m_Collider);
        }

        [UnityEditor.CustomEditor(typeof(TerrainEdgeCollider2D))]
        private class Inspector : UnityEditor.Editor
        {
            private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                EditorGUILayout.Space();
                using(new EditorGUI.DisabledScope(!Terrain.activeTerrain)) {
                    if (GUILayout.Button("Refresh")) {
                        (target as TerrainEdgeCollider2D).Rebuild();
                    }
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}