using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using BeauUtil;
using System;
using System.Reflection;

namespace Aqua.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(FactIdAttribute), true)]
    public class FactIdPropertyDrawer : PropertyDrawer
    {
        private double m_LastUpdated;
        private readonly NamedItemList<StringHash32> m_List = new NamedItemList<StringHash32>();

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            Render(position, property, label, fieldInfo, (FactIdAttribute) attribute, ref m_LastUpdated, m_List);
        }
    
        static public void Render(Rect position, UnityEditor.SerializedProperty property, GUIContent label, FieldInfo fieldInfo, FactIdAttribute attr, ref double lastUpdated, NamedItemList<StringHash32> list) {
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

            if (Event.current.type == EventType.Layout && EditorApplication.timeSinceStartup - lastUpdated > 1) {
                list.Clear();
                list.Add(null, "[Null]", -1);
                foreach(var obj in AssetDBUtils.FindAssets(attr.FactType))
                {
                    BFBase fact = (BFBase) obj;
                    if (!fact.Parent) {
                        continue;
                    }

                    string rootPath;
                    StringHash32 stationId = fact.Parent.StationId();
                    if (stationId.IsEmpty) {
                        rootPath = string.Format("Shared/{0}/", fact.Parent.name);
                    } else {
                        rootPath = string.Format("{0}/{1}/", stationId.ToDebugString(), fact.Parent.name);
                    }

                    string name = fact.name;

                    string factPath = name;
                    int dot = factPath.IndexOf('.');
                    if (dot > 0) {
                        factPath = factPath.Substring(dot + 1);
                    }

                    string fullPath = rootPath + factPath;
                    list.Add(name, fullPath);
                }
                lastUpdated = EditorApplication.timeSinceStartup;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = hashProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            StringHash32 newValue = ListGUI.Popup(position, label, new StringHash32((uint) hashProperty.longValue), list);
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