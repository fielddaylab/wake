using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyAssetStreaming.Editor {
    [CustomPropertyDrawer(typeof(StreamingPathAttribute), true)]
    public sealed class StreamingPathAttributeDrawer : PropertyDrawer {
        private readonly string[] CachedFilters = new string[2];
        private readonly GUIContent CachedContent = new GUIContent();

        private const float ButtonDisplayWidth = 24;

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label) {
            StreamingPathAttribute attr = (StreamingPathAttribute)attribute;
            label = UnityEditor.EditorGUI.BeginProperty(position, label, property);

            Rect propRect = position;
            propRect.width -= ButtonDisplayWidth * 2 + 8;

            Rect selectButtonRect = new Rect(propRect.xMax + 4, propRect.y, ButtonDisplayWidth, propRect.height);
            Rect clearButtonRect = new Rect(selectButtonRect.xMax + 4, propRect.y, ButtonDisplayWidth, propRect.height);

            CachedContent.text = string.IsNullOrEmpty(property.stringValue) ? "(No Path Specified)" : property.stringValue;
            CachedContent.tooltip = property.stringValue;
            UnityEditor.EditorGUI.LabelField(propRect, label, CachedContent);

            if (GUI.Button(selectButtonRect, "...")) {
                string streamingAssetsPath = Path.GetFullPath(Application.streamingAssetsPath).Replace("\\", "/");
                string startFrom = string.IsNullOrEmpty(property.stringValue) ? streamingAssetsPath : streamingAssetsPath + property.stringValue;

                string nextPath;
                if (string.IsNullOrEmpty(attr.Filter)) {
                    nextPath = UnityEditor.EditorUtility.OpenFilePanel("Select File in StreamingAssets", startFrom, "*");
                } else {
                    CachedFilters[0] = attr.Filter;
                    CachedFilters[1] = attr.Filter;
                    nextPath = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select File in StreamingAssets", startFrom, CachedFilters);
                }

                nextPath = nextPath.Replace("\\", "/");

                if (!string.IsNullOrEmpty(nextPath)) {
                    if (!nextPath.StartsWith(streamingAssetsPath)) {
                        UnityEditor.EditorUtility.DisplayDialog("Invalid Path", "Path must be in the StreamingAsset directory or a subdirectory", "Okay");
                    } else {
                        string strippedPath = nextPath.Substring(streamingAssetsPath.Length);
                        if (strippedPath.StartsWith("/"))
                            strippedPath = strippedPath.Substring(1);
                        property.stringValue = strippedPath;
                    }
                }

                #if UNITY_2018_1_OR_NEWER

                property.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
                return;

                #endif // UNITY_2018_1_OR_NEWER
            }

            using (new UnityEditor.EditorGUI.DisabledScope(!property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue))) {
                if (GUI.Button(clearButtonRect, "X")) {
                    property.stringValue = string.Empty;
                }
            }

            UnityEditor.EditorGUI.EndProperty();
        }
    }
}