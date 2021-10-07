using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using BeauUtil;
using System;

namespace Aqua.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(FactIdAttribute), true)]
    public class FactIdPropertyDrawer : PropertyDrawer
    {
        private double m_LastUpdated;
        private readonly NamedItemList<StringHash32> m_List = new NamedItemList<StringHash32>();

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            FactIdAttribute attr = (FactIdAttribute) attribute;
            Type fieldType = fieldInfo.FieldType;
            Type enumerableType = Reflect.GetEnumerableType(fieldType);
            if (enumerableType != null) {
                fieldType = enumerableType;
            }

            SerializedProperty hashProperty, stringProperty;

            if (fieldType == typeof(StringHash32)) {
                hashProperty = property.FindPropertyRelative("m_HashValue");
                stringProperty = null;
            } else if (fieldType == typeof(SerializedHash32)) {
                hashProperty = property.FindPropertyRelative("m_Hash");
                stringProperty = property.FindPropertyRelative("m_Source");
            } else {
                hashProperty = null;
                stringProperty = null;
                EditorGUI.LabelField(position, string.Format("Invalid type {0}", fieldType.Name));
                return;
            }

            if (Event.current.type == EventType.Layout && EditorApplication.timeSinceStartup - m_LastUpdated > 1) {
                m_List.Clear();
                m_List.Add(null, "[Null]", -1);
                foreach(var obj in AssetDBUtils.FindAssets(attr.FactType))
                {
                    BFBase fact = (BFBase) obj;
                    string name = fact.name;
                    string path = name;
                    int dot = path.IndexOf('.');
                    if (dot > 0) {
                        string critter = path.Substring(0, dot);
                        path = critter + "/" + path.Substring(dot + 1).Replace(".", "/");
                    }
                    m_List.Add(name, path);
                }
                m_LastUpdated = EditorApplication.timeSinceStartup;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = hashProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            StringHash32 newValue = ListGUI.Popup(position, label, new StringHash32((uint) hashProperty.longValue), m_List);
            if (EditorGUI.EndChangeCheck()) {
                hashProperty.longValue = newValue.HashValue;
                if (stringProperty != null) {
                    stringProperty.stringValue = newValue.ToDebugString();
                }
            }
            EditorGUI.EndProperty();
        }
    }
}