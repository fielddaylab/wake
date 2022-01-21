using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using BeauUtil;
using System;
using System.Reflection;

namespace Aqua.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(DBObjectIdAttribute), true)]
    public class DBObjectIdPropertyDrawer : PropertyDrawer
    {
        private double m_LastUpdated;
        private readonly NamedItemList<StringHash32> m_List = new NamedItemList<StringHash32>();

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            Render(position, property, label, fieldInfo, (DBObjectIdAttribute) attribute, ref m_LastUpdated, m_List);
        }

        static public void Render(Rect position, UnityEditor.SerializedProperty property, GUIContent label, FieldInfo fieldInfo, DBObjectIdAttribute attr, ref double lastUpdated, NamedItemList<StringHash32> list) {
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
                hashProperty = property.FindPropertyRelative("m_HashValue");
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
                foreach(var obj in AssetDBUtils.FindAssets(attr.AssetType))
                {
                    DBObject dbObj = (DBObject) obj;
                    if (char.IsLetterOrDigit(obj.name[0]) && attr.Filter(dbObj))
                        list.Add(dbObj.Id(), attr.Name(dbObj));
                }
                lastUpdated = EditorApplication.timeSinceStartup;
            }

            label = EditorGUI.BeginProperty(position, label, property);

            Rect fieldPosition = position;
            fieldPosition.width -= 16;
            EditorGUI.showMixedValue = hashProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            StringHash32 newValue = ListGUI.Popup(fieldPosition, label, new StringHash32((uint) hashProperty.longValue), list);
            if (EditorGUI.EndChangeCheck()) {
                hashProperty.longValue = newValue.HashValue;
                if (stringProperty != null) {
                    stringProperty.stringValue = newValue.ToDebugString();
                }
            }
            EditorGUI.EndProperty();

            Rect pingPosition = position;
            pingPosition.x = pingPosition.x + pingPosition.width - 16;
            pingPosition.width = 16;

            using(GUIScopes.IndentLevelScope.SetIndent(0)) {
                long hashProp = hashProperty.longValue;
                string reversed = new StringHash32((uint) hashProp).ToDebugString();
                if (hashProperty.hasMultipleDifferentValues || string.IsNullOrEmpty(reversed)) {
                    EditorGUI.LabelField(pingPosition, EditorGUIUtility.IconContent("DotFrame"));
                } else {
                    EditorGUI.LabelField(pingPosition, EditorGUIUtility.IconContent("DotFill"));
                    if (Event.current.type == EventType.MouseDown && pingPosition.Contains(Event.current.mousePosition)) {
                        DBObject asset = (DBObject) AssetDBUtils.FindAsset(attr.AssetType, reversed);
                        if (Event.current.clickCount >= 2) {
                            Selection.activeObject = asset;
                        } else {
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                }
            }
        }
    }
}