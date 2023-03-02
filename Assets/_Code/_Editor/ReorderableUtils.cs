using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Aqua.Editor {
    static public class ReorderableUtils {

        static public ReorderableList CreateList(SerializedObject root, SerializedProperty property, string name) {
            ReorderableList list = new ReorderableList(root, property);
            list.drawElementCallback = DefaultElementDelegate(list);
            list.elementHeightCallback = DefaultHeightDelegate(list);

            if (!string.IsNullOrEmpty(name)) {
                list.drawHeaderCallback = (r) => EditorGUI.LabelField(r, name);
            } else {
                list.drawHeaderCallback = (r) => { };
                list.headerHeight = 0;
            }

            return list;
        }

        static private ReorderableList.ElementCallbackDelegate DefaultElementDelegate(ReorderableList list) {
            return (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };
        }

        static private ReorderableList.ElementHeightCallbackDelegate DefaultHeightDelegate(ReorderableList list) {
            return (int index) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, null, true);
            };
        }
    }
}