using BeauRoutine.Extensions;
using UnityEngine;
using BeauUtil;
using BeauPools;
using UnityEngine.UI;
using System;
using BeauRoutine;
using System.Collections;
using TMPro;

namespace Aqua
{
    public class DialogHistoryPanel : SharedPanel
    {
        [Serializable] private class NodePool : SerializablePool<DialogHistoryNode> { }

        #region Inspector

        [Header("History Panel")]
        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private NodePool m_NodePool = null;
        [SerializeField] private LayoutGroup m_ListLayout = null;
        [SerializeField] private ContentSizeFitter m_ListFitter = null;
        [SerializeField] private ScrollRect m_Scroller = null;

        [Header("Default Settings")]
        [SerializeField] private Color m_DefaultTextColor = Color.white;
        [SerializeField] private float m_DefaultTextIndent = 20;
        [SerializeField] private float m_ChoiceTextIndent = 60;

        #endregion // Inspector

        [NonSerialized] private BaseInputLayer m_InputLayer = null;

        protected override void Awake()
        {
            base.Awake();

            m_InputLayer = BaseInputLayer.Find(this);
            m_CloseButton.onClick.AddListener(ForceHide);
            Services.Events.Register(GameEvents.SceneWillUnload, InstantHide);
            m_Scroller.verticalScrollbar.gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);

            base.OnDestroy();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            if (m_InputLayer.PushPriority())
            {
                Services.Pause.Pause();
            }
            PopulateHistory();
        }

        protected override void OnHide(bool inbInstant)
        {
            if (m_InputLayer.PopPriority())
            {
                Services.Pause?.Resume();
            }
            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_NodePool.Reset();
            base.OnHideComplete(inbInstant);
        }

        private void ForceHide()
        {
            Hide();
        }

        private void PopulateHistory()
        {
            m_NodePool.Reset();

            StringHash32 currentCharacter = StringHash32.Null;
            string currentName = null;
            Color textColor = m_DefaultTextColor;

            bool bShowName = true;
            var history = Services.Data.DialogHistory;
            int historyCount = history.Count;
            for(int i = 0; i < historyCount; ++i)
            {
                ref DialogRecord record = ref history[i];

                if (record.CharacterId != currentCharacter)
                {
                    currentCharacter = record.CharacterId;
                    ScriptCharacterDef actorDef = Assets.Character(record.CharacterId);
                    textColor = actorDef.HistoryColorOverride() ?? m_DefaultTextColor;
                }

                if (record.IsChoice)
                {
                    bShowName = false;
                    currentName = null;
                }
                else
                {
                    if (record.Name != currentName)
                    {
                        bShowName = true;
                        currentName = record.Name;
                    }
                    else
                    {
                        bShowName = record.IsBoundary;
                    }
                }

                DialogHistoryNode node = m_NodePool.Alloc();
                node.Text.color = textColor;
                node.Text.SetText(record.Text);
                node.ChoiceBackground.enabled = record.IsChoice;

                Vector4 margins = node.Text.margin;
                if (record.IsChoice)
                {
                    margins.x = 0;
                    margins.z = m_ChoiceTextIndent;
                    node.Text.horizontalAlignment = HorizontalAlignmentOptions.Right;
                }
                else
                {
                    margins.x = m_DefaultTextIndent;
                    margins.z = 0;
                    node.Text.horizontalAlignment = HorizontalAlignmentOptions.Left;
                }

                node.Text.margin = margins;

                if (bShowName)
                {
                    node.Name.color = textColor;
                    node.Name.SetText(currentName);
                    node.Name.gameObject.SetActive(true);
                }
                else
                {
                    node.Name.gameObject.SetActive(false);
                }

                node.Layout.enabled = true;
                node.Fitter.enabled = true;
            }

            m_ListLayout.enabled = true;
            m_ListFitter.enabled = true;

            m_Scroller.verticalNormalizedPosition = 0;

            RebuildLayout();
        }

        private void RebuildLayout()
        {
            Routine.Start(this, RebuildDelayed());
        }

        private IEnumerator RebuildDelayed()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_ListLayout.transform);
            m_ListLayout.enabled = false;
            m_ListFitter.enabled = false;

            foreach(var node in m_NodePool.ActiveObjects)
            {
                node.Layout.enabled = false;
                node.Fitter.enabled = false;
            }

            m_Scroller.verticalNormalizedPosition = 0;
        }
    }
}