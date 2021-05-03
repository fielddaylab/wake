using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class ChatManager : MonoBehaviour
    {
        static public readonly StringHash32 Event_SelectClaim = "argue:select-claim";
        static public readonly StringHash32 Event_OpenFactSelect = "argue:open-fact-select";

        [Serializable]
        public class ChatPool : SerializablePool<ArgueChatLine> { }

        #region Inspector

        [Header("Data")]
        [SerializeField] private Graph m_Graph = null;
        
        [Header("Chat")]
        [SerializeField] private LocText m_CharacterName = null;
        [SerializeField] private ScrollRect m_ScrollRect = null;
        [SerializeField] private LayoutGroup m_ChatLayout = null;
        [SerializeField] private ChatPool m_NodePool = null;

        [Header("Input")]
        [SerializeField] private LinkManager m_LinkManager = null;
        [SerializeField] private InputRaycasterLayer m_InputRaycasterLayer = null;

        [Header("Animation")]
        [SerializeField] private float m_ScrollTime = 0.25f;
        [SerializeField] private Color m_InvalidColor = Color.red;
        [SerializeField] private Color m_EndColor = Color.green;

        [Header("Layers")]
        [SerializeField] private Transform m_Group = null;
        [SerializeField] private Transform m_NotAvailableGroup = null;

        #endregion // Inspector

        private Routine m_ChatRoutine;
        [NonSerialized] private ScriptActorDef m_CharacterDef;
        [NonSerialized] private ScriptActorDef m_PlayerDef;
        [NonSerialized] private List<Link> m_Claims = new List<Link>();

        private void Start()
        {
            m_NodePool.Initialize();

            Services.Events.Register<StringHash32>(Event_SelectClaim, OnOptionSelected, this);
            Services.Events.Register<BestiaryDescCategory>(Event_OpenFactSelect, OnOpenBestiary, this);

            m_Graph.OnGraphLoaded += OnGraphLoaded;
            m_Graph.OnGraphNotAvailable += NotAvailable;
        }

        private void OnGraphLoaded()
        {
            foreach(var link in m_Graph.LinkDictionary.Values)
            {
                if (link.Tag == "claim")
                    m_Claims.Add(link);
            }

            m_CharacterDef = Services.Assets.Characters.Get(m_Graph.CharacterId);
            m_PlayerDef = Services.Assets.Characters.Get(GameConsts.Target_Player);

            m_CharacterName.SetText(m_CharacterDef.NameId());

            Routine.StartCall(this, OnStartChat);
        }

        private void NotAvailable()
        {
            m_Group.gameObject.SetActive(false);
            m_NotAvailableGroup.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #region Callbacks

        private void OnOptionSelected(StringHash32 inOptionId)
        {
            Link link = m_Graph.FindLink(inOptionId);
            m_ChatRoutine.Replace(this, DisplayLink(link)).TryManuallyUpdate(0);
        }

        private void OnFactSelected(PlayerFactParams inFact)
        {
            Link link = m_Graph.FindLink(inFact.FactId);
            if (link == null)
            {
                link = new Link(inFact);
            }

            m_ChatRoutine.Replace(this, DisplayLink(link)).TryManuallyUpdate(0);
        }

        private void OnOpenBestiary(BestiaryDescCategory inCategory)
        {
            var future = BestiaryApp.RequestFact(inCategory);
            future.OnComplete(OnFactSelected);
        }

        #endregion // Callbacks

        #region Display

        private IEnumerator DisplayLink(Link inLink)
        {
            m_InputRaycasterLayer.Override = false;
            m_LinkManager.Hide();

            ArgueChatLine chat = m_NodePool.Alloc();
            chat.Populate(inLink.DisplayText, m_PlayerDef);
            m_ChatLayout.ForceRebuild();
            yield return ScrollDown();

            Node node = m_Graph.NextNode(inLink.Id);
            yield return DisplayNode(node);
        }

        private IEnumerator DisplayNode(Node inNode)
        {
            m_InputRaycasterLayer.Override = false;

            Node toDisplay = inNode;
            Node currentNode = inNode;
            while(toDisplay != null)
            {
                currentNode = toDisplay;
                m_Graph.SetCurrentNode(currentNode);

                ArgueChatLine chat = m_NodePool.Alloc();
                chat.Populate(toDisplay.DisplayText, m_CharacterDef);
                
                if (toDisplay.IsInvalid)
                {
                    chat.OverrideColors(null, m_InvalidColor);
                    Routine.Start(this, ShakeNode(chat));
                }
                else if (toDisplay.Id == m_Graph.EndNodeId)
                {
                    chat.OverrideColors(null, m_EndColor);
                }
                
                m_ChatLayout.ForceRebuild();
                yield return ScrollDown();

                toDisplay = m_Graph.FindNode(toDisplay.NextNodeId);
                if (toDisplay != null)
                    yield return m_ScrollTime * 2;
            }

            if (currentNode.Id == m_Graph.EndNodeId)
            {
                EndConversationPopup();
            }
            else
            {
                m_InputRaycasterLayer.Override = null;
                if (currentNode.ShowClaims)
                {
                    m_LinkManager.DisplayClaims(m_Claims);
                }
                else
                {
                    m_LinkManager.DisplayBestiary();
                }
            }
        }

        private IEnumerator ScrollDown()
        {
            yield return m_ScrollRect.NormalizedPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
        }

        private IEnumerator ShakeNode(ArgueChatLine inLine)
        {
            yield return inLine.Shake();
        }

        #endregion // Display

        private void OnStartChat()
        {
            m_ChatRoutine.Replace(this, DisplayNode(m_Graph.RootNode)).TryManuallyUpdate(0);
        }

        private void EndConversationPopup()
        {
            m_ChatRoutine.Replace(this, CompleteConversation()).TryManuallyUpdate(0);
        }

        private IEnumerator CompleteConversation()
        {
            m_InputRaycasterLayer.Override = false;

            using(var table = Services.Script.GetTempTable())
            {
                table.Set("jobId", Services.Data.CurrentJobId());
                yield return Services.Script.TriggerResponse("ArgumentationComplete");
            }

            Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob());

            if (!Services.Script.IsCutscene())
            {
                Services.UI.Popup.Display("Congratulations!", "Job complete!")
                    .OnComplete((s) => {
                        StateUtil.LoadPreviousSceneWithWipe();
                    });
            }
        }
    }
}
