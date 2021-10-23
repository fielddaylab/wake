using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Portable;
using Aqua.Scripting;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using Leaf.Runtime;
using BeauUtil.Debugger;

namespace ProtoAqua.Argumentation
{
    public class ArgueActivity : MonoBehaviour
    {
        static public readonly StringHash32 Event_SelectClaim = "argue:select-claim";
        static public readonly StringHash32 Event_OpenFactSelect = "argue:open-fact-select";

        [Serializable]
        public class ChatPool : SerializablePool<ArgueChatLine> { }

        static private ArgueActivity s_Instance;

        #region Inspector

        [Header("Data")]
        [SerializeField] private Graph m_Graph = null;
        
        [Header("Chat")]
        [SerializeField] private LocText m_CharacterName = null;
        [SerializeField] private ScrollRect m_ScrollRect = null;
        [SerializeField] private LayoutGroup m_ChatLayout = null;
        [SerializeField] private ChatPool m_NodePool = null;
        [SerializeField] private Transform m_ConvoFinished = null;

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
        [NonSerialized] private ScriptCharacterDef m_CharacterDef;
        [NonSerialized] private ScriptCharacterDef m_PlayerDef;
        [NonSerialized] private List<ArgueLink> m_Claims = new List<ArgueLink>();
        [NonSerialized] private HashSet<StringHash32> m_VisitedNodes = new HashSet<StringHash32>();
        [NonSerialized] private HashSet<StringHash32> m_SetFlags = new HashSet<StringHash32>();
        [NonSerialized] private ArgueNode m_CurrentNode;

        private void Start()
        {
            m_NodePool.Initialize();

            Services.Events.Register<StringHash32>(Event_SelectClaim, OnOptionSelected, this);
            Services.Events.Register(Event_OpenFactSelect, OnOpenBestiary, this);

            m_Graph.OnGraphLoaded += OnGraphLoaded;
            m_Graph.OnGraphNotAvailable += NotAvailable;

            m_ConvoFinished.gameObject.SetActive(false);

            s_Instance = this;
        }

        private void OnGraphLoaded()
        {
            m_Group.gameObject.SetActive(true);
            m_NotAvailableGroup.gameObject.SetActive(false);

            foreach(var link in m_Graph.Links)
            {
                if (link.Tag == "claim")
                    m_Claims.Add(link);
            }

            m_CharacterDef = Assets.Character(m_Graph.CharacterId);
            m_PlayerDef = Assets.Character(GameConsts.Target_Player);

            m_CharacterName.SetText(m_CharacterDef.NameId());

            m_ChatRoutine = Routine.Start(this. OnStartChat());
        }

        private void NotAvailable()
        {
            m_Group.gameObject.SetActive(false);
            m_NotAvailableGroup.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            s_Instance = null;
            Services.Events?.DeregisterAll(this);
        }

        #region Callbacks

        private void OnOptionSelected(StringHash32 inOptionId)
        {
            ArgueLink link = m_Graph.FindLink(inOptionId);
            m_ChatRoutine.Replace(this, DisplayLink(link)).TryManuallyUpdate(0);
        }

        private void OnFactSelected(StringHash32 inId)
        {
            ArgueLink link = m_Graph.FindLink(inId);
            if (link == null)
            {
                BFBase fact = Assets.Fact(inId);
                BFDiscoveredFlags flags = Services.Data.Profile.Bestiary.GetDiscoveredFlags(inId);
                link = new ArgueLink(fact, flags);
            }

            m_ChatRoutine.Replace(this, DisplayLink(link)).TryManuallyUpdate(0);
        }

        private void OnOpenBestiary()
        {
            PortableMenu.RequestFact()
                .OnComplete(OnFactSelected);
        }

        #endregion // Callbacks

        #region Display

        private IEnumerator DisplayLink(ArgueLink inLink)
        {
            m_InputRaycasterLayer.Override = false;
            m_LinkManager.Hide();

            ArgueChatLine chat = m_NodePool.Alloc();
            chat.Populate(inLink.DisplayText, m_PlayerDef);
            m_ChatLayout.ForceRebuild();
            Services.Audio.PostEvent("argue.chat.player");
            
            yield return ScrollDown();
            yield return 1;

            ArgueNode node = m_Graph.NextNode(m_CurrentNode, inLink.Id);
            yield return DisplayNode(node);
        }

        private IEnumerator DisplayNode(ArgueNode inNode)
        {
            m_InputRaycasterLayer.Override = false;

            ArgueNode toDisplay = inNode;
            ArgueNode currentNode = inNode;
            while(toDisplay != null) {
                currentNode = toDisplay;
                m_CurrentNode = toDisplay;
                m_VisitedNodes.Add(currentNode.Id);

                foreach(var flag in m_CurrentNode.FlagsToUnset) {
                    m_SetFlags.Remove(flag);
                }
                foreach(var flag in m_CurrentNode.FlagsToSet) {
                    m_SetFlags.Add(flag);
                }

                var lines = toDisplay.DisplayTexts;
                for(int i = 0; i < lines.Length; i++) {
                    ArgueChatLine chat = m_NodePool.Alloc();
                    chat.Populate(lines[i], m_CharacterDef);

                    if (toDisplay.IsInvalid) {
                        chat.OverrideColors(null, m_InvalidColor);
                        if (i == 0) {
                            Routine.Start(this, ShakeNode(chat));
                            Services.Audio.PostEvent("argue.chat.incorrect");
                        } else {
                            Services.Audio.PostEvent("argue.chat.new");
                        }
                    } else if (toDisplay.Id == m_Graph.EndNodeId) {
                        chat.OverrideColors(null, m_EndColor);
                        if (i == 0) {
                            Services.Audio.PostEvent("argue.chat.end");
                        } else {
                            Services.Audio.PostEvent("argue.chat.new");
                        }
                    } else {
                        Services.Audio.PostEvent("argue.chat.new");
                    }

                    m_ChatLayout.ForceRebuild();
                    yield return ScrollDown();
                    yield return 1.2f;
                }
                
                toDisplay = m_Graph.NextNode(toDisplay);
            }

            yield return 0.5f;

            if (currentNode.CancelFlow) {
                m_ConvoFinished.gameObject.SetActive(true);
                m_ConvoFinished.SetAsLastSibling();
                m_ChatLayout.ForceRebuild();
                yield return ScrollDown();
                yield return 3;
                StateUtil.LoadPreviousSceneWithWipe();
            } else if (currentNode.Id == m_Graph.EndNodeId) {
                m_ConvoFinished.gameObject.SetActive(true);
                m_ConvoFinished.SetAsLastSibling();
                m_ChatLayout.ForceRebuild();
                yield return ScrollDown();
                yield return 3;
                EndConversationPopup();
            } else {
                m_InputRaycasterLayer.Override = null;
                if (currentNode.ShowClaims) {
                    m_LinkManager.DisplayClaims(m_Claims);
                } else {
                    m_LinkManager.DisplayBestiary();
                }
            }
        }

        private IEnumerator ScrollDown()
        {
            yield return m_ScrollRect.NormalizedPosTo(0, m_ScrollTime, Axis.Y).Ease(Curve.CubeOut);
        }

        private IEnumerator ShakeNode(ArgueChatLine inLine)
        {
            yield return inLine.Shake();
        }

        #endregion // Display

        private IEnumerator OnStartChat()
        {
            yield return 1;
            ArgueNode rootNode = m_Graph.FindNode(m_Graph.RootNodeId);
            yield return Routine.Inline(DisplayNode(rootNode));
        }

        private void EndConversationPopup()
        {
            m_ChatRoutine.Replace(this, CompleteConversation()).TryManuallyUpdate(0);
        }

        private IEnumerator CompleteConversation()
        {
            m_InputRaycasterLayer.Override = null;

            using(var table = TempVarTable.Alloc())
            {
                table.Set("jobId", Services.Data.CurrentJobId());
                yield return Services.Script.TriggerResponse("ArgumentationComplete", table).Wait();
            }

            Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob());

            if (Services.Script.IsCutscene())
            {
                yield return Services.Script.GetCutscene().Wait();
            }
            else
            {
                yield return Services.UI.Popup.Display("Congratulations!", "Job complete!");
            }

            StateUtil.LoadPreviousSceneWithWipe();
        }

        [LeafMember("VisitedArgueNode")]
        static private bool LeafVisitedNode(StringHash32 inId)
        {
            Assert.NotNull(s_Instance, "Argue Activity not started");
            return s_Instance.m_VisitedNodes.Contains(inId);
        }

        [LeafMember("ArgueFlag")]
        static private bool LeafArgueFlag(StringHash32 inId)
        {
            Assert.NotNull(s_Instance, "Argue Activity not started");
            return s_Instance.m_SetFlags.Contains(inId);
        }
    }
}
