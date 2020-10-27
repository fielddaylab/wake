using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using ProtoAqua.Scripting;
using Leaf;
using Leaf.Runtime;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour, ILeafPlugin<ScriptNode>
    {
        #region ILeafPlugin

        void ILeafPlugin<ScriptNode>.OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState)
        {
            Services.Data.Profile.Script.RecordNodeVisit(inNode.Id(), inNode.TrackingLevel());
            if ((inNode.Flags() & ScriptNodeFlags.Cutscene) != 0)
            {
                ScriptThread thread = ((ScriptThread) inThreadState);
                thread.PushCutscene();
            }
            else if ((inNode.Flags() & ScriptNodeFlags.CornerChatter) != 0)
            {
                ScriptThread thread = ((ScriptThread) inThreadState);
                thread.Dialog = Services.UI.GetDialog("cornerKevin");
            }
        }

        void ILeafPlugin<ScriptNode>.OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState)
        {
            if ((inNode.Flags() & ScriptNodeFlags.Cutscene) != 0)
            {
                ScriptThread thread = ((ScriptThread) inThreadState);
                thread.PopCutscene();
            }
        }

        bool ILeafPlugin<ScriptNode>.TryLookupLine(StringHash32 inLineCode, ScriptNode inLocalNode, out string outLine)
        {
            return inLocalNode.Package().TryGetLine(inLineCode, out outLine);
        }

        bool ILeafPlugin<ScriptNode>.TryLookupNode(StringHash32 inNodeId, ScriptNode inLocalNode, out ScriptNode outNode)
        {
            return TryGetScriptNode(inLocalNode, inNodeId, out outNode);
        }

        IEnumerator ILeafPlugin<ScriptNode>.RunLine(LeafThreadState<ScriptNode> inThreadState, StringSlice inLine, ILeafContentResolver inContentResolver)
        {
            return PerformEventLine((ScriptThread) inThreadState, inLine);
        }

        IEnumerator ILeafPlugin<ScriptNode>.ShowOptions(LeafThreadState<ScriptNode> inThreadState, LeafChoice inChoice, ILeafContentResolver inContentResolver)
        {
            throw new NotImplementedException();
        }

        #endregion // ILeafPlugin

        // Performs a node
        private IEnumerator ProcessNodeInstructions(ScriptThread inThread, ScriptNode inStartingNode)
        {
            yield return m_ThreadRuntime.Execute(inThread, inStartingNode);
            inThread.Kill();
        }

        // Performs a block of event lines
        private IEnumerator PerformEventLines(ScriptThread inThread, IEnumerable<StringSlice> inLines)
        {
            foreach (var line in inLines)
            {
                yield return Routine.Inline(PerformEventLine(inThread, line));
            }
        }

        // Reads a line of scripting
        private IEnumerator PerformEventLine(ScriptThread inThread, StringSlice inLine)
        {
            if (inLine.IsEmpty || inLine.IsWhitespace)
                yield break;

            TagString lineEvents = inThread.TagString;
            TagStringEventHandler eventHandler = m_TagEventHandler;
            DialogPanel dialogPanel = inThread.Dialog ?? (inThread.Dialog = Services.UI.Dialog);

            ParseToTag(ref lineEvents, inLine, inThread);
            eventHandler = dialogPanel.PrepLine(lineEvents, m_TagEventHandler);

            for (int i = 0; i < lineEvents.Nodes.Length; ++i)
            {
                TagNodeData node = lineEvents.Nodes[i];
                switch (node.Type)
                {
                    case TagNodeType.Event:
                        {
                            IEnumerator coroutine;
                            if (eventHandler.TryEvaluate(node.Event, inThread, out coroutine))
                            {
                                if (coroutine != null)
                                    yield return coroutine;

                                if (inThread.Dialog != dialogPanel)
                                {
                                    dialogPanel = inThread.Dialog;
                                    eventHandler = dialogPanel.PrepLine(lineEvents, m_TagEventHandler);
                                }

                                inThread.Dialog?.UpdateInput();
                            }
                            break;
                        }
                    case TagNodeType.Text:
                        {
                            yield return Routine.Inline(inThread.Dialog.TypeLine(node.Text));
                            break;
                        }
                }
            }

            if (lineEvents.RichText.Length > 0)
            {
                yield return inThread.Dialog?.CompleteLine();
            }
        }
    }
}