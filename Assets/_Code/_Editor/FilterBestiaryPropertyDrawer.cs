using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;

namespace Aqua.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(FilterBestiaryAttribute))]
    public class FilterBestiaryPropertyDrawer : PropertyDrawer
    {
        private double m_LastUpdated;
        private readonly NamedItemList<BestiaryDesc> m_List = new NamedItemList<BestiaryDesc>();

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            FilterBestiaryAttribute attr = (FilterBestiaryAttribute) attribute;
            BestiaryDescCategory category = attr.Category;

            if (Event.current.type == EventType.Layout && EditorApplication.timeSinceStartup - m_LastUpdated > 1)
            {
                m_List.Clear();
                m_List.Add(null, "[Null]", -1);
                foreach(var obj in AssetDBUtils.FindAssets<BestiaryDesc>())
                {
                    if (obj.HasCategory(category))
                        m_List.Add(obj, obj.name);
                }
                m_LastUpdated = EditorApplication.timeSinceStartup;
            }
            
            label = EditorGUI.BeginProperty(position, label, property);
            property.objectReferenceValue = ListGUI.Popup(position, label, (BestiaryDesc) property.objectReferenceValue, m_List);
            EditorGUI.EndProperty();
        }
    }
}