using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;

namespace UnityEditor.U2D.Animation
{
    internal class SpriteBoneInfluenceListWidget : VisualElement
    {
        public class CustomUxmlFactory : UxmlFactory<SpriteBoneInfluenceListWidget, CustomUxmlTraits> {}
        public class CustomUxmlTraits : UxmlTraits {}

        static class Contents
        {
            public static readonly GUIContent PlusSign = new GUIContent("+");
            public static readonly GUIContent MinusSign = new GUIContent("-");
        }

        const float k_AddRemoveButtonWidth = 30f;

        public Action onAddBone = () => {};
        public Action onRemoveBone = () => {};
        public Action<IEnumerable<BoneCache>> onReordered = _ => {};
        public Action<IEnumerable<BoneCache>> onSelectionChanged = (s) => {};
        public Func<SpriteBoneInflueceToolController> GetController = () => null;

        IMGUIContainer m_IMGUIContainer;
        ReorderableList m_ReorderableList;
        
        List<BoneCache> m_BoneInfluences = new List<BoneCache>();
        bool m_IgnoreSelectionChange = false;
        Vector2 m_ScrollPosition = Vector2.zero;

        public SpriteBoneInfluenceListWidget()
        {
            var visualTree = ResourceLoader.Load<VisualTreeAsset>("SkinningModule/SpriteBoneInfluenceListWidget.uxml");
            var ve = visualTree.CloneTree().Q("Container");
            ve.styleSheets.Add(ResourceLoader.Load<StyleSheet>("SkinningModule/SpriteBoneInfluenceListWidgetStyle.uss"));
            if (EditorGUIUtility.isProSkin)
                AddToClassList("Dark");
            this.Add(ve);
            BindElements();
        }
        
        void BindElements()
        {
            m_ReorderableList = new ReorderableList(m_BoneInfluences, typeof(List<BoneCache>), true, false, false, false);
            m_ReorderableList.headerHeight = 0;
            m_ReorderableList.footerHeight = 0;
            m_ReorderableList.drawElementCallback = OnDrawElement;
            m_ReorderableList.onSelectCallback = OnListViewSelectionChanged;
            m_ReorderableList.onReorderCallback = _ => { onReordered(m_BoneInfluences); };

            m_IMGUIContainer = this.Q<IMGUIContainer>();
            m_IMGUIContainer.onGUIHandler = OnDrawGUIContainer;
        }

        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_BoneInfluences != null && m_BoneInfluences.Count > index)
                EditorGUI.LabelField(rect, m_BoneInfluences[index].name);
        }

        void OnAddCallback()
        {
            onAddBone();
            OnListViewSelectionChanged(m_ReorderableList);
        }

        void OnRemoveCallback()
        {
            onRemoveBone();
            OnListViewSelectionChanged(m_ReorderableList);
        }

        void OnListViewSelectionChanged(ReorderableList list)
        {
            if (m_IgnoreSelectionChange)
                return;

            var selectedBones = new List<BoneCache>()
            {
                m_BoneInfluences.Count > 0 ? m_BoneInfluences[list.index] : null
            };
            onSelectionChanged(selectedBones);
        }

        void OnDrawGUIContainer()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition,
                false,
                false,
                GUILayout.Height(GetScrollViewHeight()));
            if(m_BoneInfluences != null)
                m_ReorderableList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = IsAddButtonEnabled();
            if(GUILayout.Button(Contents.PlusSign, GUILayout.Width(k_AddRemoveButtonWidth)))
                OnAddCallback();
            
            GUI.enabled = IsRemoveButtonEnabled();
            if(GUILayout.Button(Contents.MinusSign, GUILayout.Width(k_AddRemoveButtonWidth)))
                OnRemoveCallback();
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        bool IsAddButtonEnabled() => GetController().ShouldEnableAddButton(m_BoneInfluences);
        bool IsRemoveButtonEnabled() => GetController().InCharacterMode() && m_BoneInfluences.Count > 0 && m_ReorderableList.index >= 0;
        float GetScrollViewHeight() => Mathf.Min(m_ReorderableList.GetHeight(), 130f);

        public void Update()
        {
            m_BoneInfluences = GetController().GetSelectedSpriteBoneInfluence().ToList();
            m_ReorderableList.list = m_BoneInfluences;
        }

        internal void OnBoneSelectionChanged()
        {
            var selectedBones = GetController().GetSelectedBoneForList(m_BoneInfluences);
            m_IgnoreSelectionChange = true;
            m_ReorderableList.index = selectedBones.Length > 0 ? selectedBones[0] : 0;
            m_IgnoreSelectionChange = false;
        }
    }
}
