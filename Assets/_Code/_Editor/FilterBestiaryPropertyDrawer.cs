using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;

namespace Aqua.Editor
{
    [UnityEditor.CustomPropertyDrawer(typeof(FilterBestiaryAttribute))]
    public class FilterBestiaryPropertyDrawer : PropertyDrawer
    {
        private readonly NamedItemList<BestiaryDesc> m_List = new NamedItemList<BestiaryDesc>();

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            FilterBestiaryAttribute attr = (FilterBestiaryAttribute) attribute;
            BestiaryDescCategory category = attr.Category;

            if (Event.current.type == EventType.Layout)
            {
                m_List.Clear();
                foreach(var obj in AssetDBUtils.FindAssets<BestiaryDesc>())
                {
                    if (obj.HasCategory(category))
                        m_List.Add(obj, obj.name);
                }
            }
            
            label = EditorGUI.BeginProperty(position, label, property);
            property.objectReferenceValue = ListGUI.Popup(position, label, (BestiaryDesc) property.objectReferenceValue, m_List);
            EditorGUI.EndProperty();
        }
    }
}