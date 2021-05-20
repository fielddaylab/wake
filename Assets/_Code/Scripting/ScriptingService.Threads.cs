using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using Aqua.Scripting;
using Leaf;
using Leaf.Runtime;

namespace Aqua
{
    public partial class ScriptingService : ServiceBehaviour, ILeafPlugin<ScriptNode>
    {
        #region ILeafPlugin

        IMethodCache ILeafPlugin<ScriptNode>.MethodCache { get { return m_LeafCache; } }

        void ILeafPlugin<ScriptNode>.OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState)
        {
            var thread = ScriptThread(inThreadState);
            inNode.Package().IncrementUseCount();

            Services.Data.Profile.Script.RecordNodeVisit(inNode.Id(), inNode.TrackingLevel());
            if (inNode.IsCutscene())
            {
                thread.PushCutscene();
            }
            else if ((inNode.Flags() & ScriptNodeFlags.CornerChatter) != 0)
            {
                thread.Dialog = Services.UI.GetDialog("cornerKevin");
            }

            thread.Delay(inNode.InitialDelay());
        }

        void ILeafPlugin<ScriptNode>.OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState)
        {
            inNode.Package().DecrementUseCount();

            if (inNode.IsCutscene())
            {
                ScriptThread thread = ((ScriptThread) inThreadState);
                thread.PopCutscene();
            }

            if ((inNode.Flags() & ScriptNodeFlags.Autosave) != 0)
            {
                AutoSave.Hint();
            }
        }

        void ILeafPlugin<ScriptNode>.OnEnd(LeafThreadState<ScriptNode> inThreadState)
        {
            var thread = ScriptThread(inThreadState);
            if (thread.IsSkipping())
            {
                if (thread.Dialog != null)
                {
                    thread.Dialog.InstantHide();
                    thread.Dialog = null;
                }
            }
            thread.Kill();
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
            var thread = ScriptThread(inThreadState);
            if (thread.IsSkipping())
            {
                SkipEventLine(thread, inLine);
                return null;
            }

            return PerformEventLine(thread, inLine);
        }

        IEnumerator ILeafPlugin<ScriptNode>.ShowOptions(LeafThreadState<ScriptNode> inThreadState, LeafChoice inChoice, ILeafContentResolver inContentResolver)
        {
            var thread = ScriptThread(inThreadState);
            return PerformEventChoice(thread, inChoice, inContentResolver);
        }

        bool ILeafPlugin<ScriptNode>.TryLookupObject(StringHash32 inObjectId, LeafThreadState<ScriptNode> inThreadState, out object outObject)
        {
            var thread = ScriptThread(inThreadState);
            if (inObjectId == "this")
            {
                outObject = thread.Context.Object;
                return outObject != null;
            }

            ScriptObject obj;
            bool bFound = TryGetScriptObjectById(inObjectId, out obj);
            outObject = obj;
            return bFound;
        }

        LeafThreadState<ScriptNode> ILeafPlugin<ScriptNode>.Fork(LeafThreadState<ScriptNode> inThreadState, ScriptNode inForkNode)
        {
            var thread = ScriptThread(inThreadState);
            var handle = StartThreadInternalNode(null, thread.Context, inForkNode, thread.Locals);
            return handle.GetThread();
        }

        void ILeafPlugin<ScriptNode>.Kill(LeafThreadState<ScriptNode> inThreadState)
        {
            var thread = ScriptThread(inThreadState);
            thread.Kill();
        }

        #endregion // ILeafPlugin

        static private ScriptThread ScriptThread(LeafThreadState<ScriptNode> inThreadState)
        {
            return (ScriptThread) inThreadState;
        }

        // Performs a node
        private IEnumerator ProcessNodeInstructions(ScriptThread inThread, ScriptNode inStartingNode)
        {
            return m_ThreadRuntime.Execute(inThread, inStartingNode);
        }

        // Reads a line of scripting
        private IEnumerator PerformEventLine(ScriptThread inThread, StringSlice inLine)
        {
            if (inLine.IsEmpty || inLine.IsWhitespace)
                yield break;

            ScriptThreadHandle handle = inThread.GetHandle();
            TagString lineEvents = inThread.TagString;
            TagStringEventHandler eventHandler = m_TagEventHandler;
            DialogPanel dialogPanel = inThread.Dialog ?? (inThread.Dialog = Services.UI.Dialog);

            ParseToTag(ref lineEvents, inLine, inThread);
            bool bHasDialogEvents = false;
            for(int i = 0; !bHasDialogEvents && i < lineEvents.Nodes.Length; i++)
            {
                bHasDialogEvents = lineEvents.Nodes[i].Type == TagNodeType.Event && m_DialogOnlyEvents.Contains(lineEvents.Nodes[i].Event.Type);
            }

            eventHandler = dialogPanel.PrepLine(lineEvents, m_TagEventHandler, bHasDialogEvents);

            inThread.RecordDialog(lineEvents);

            for (int i = 0; i < lineEvents.Nodes.Length; i++)
            {
                TagNodeData node = lineEvents.Nodes[i];
                switch (node.Type)
                {
                    case TagNodeType.Event:
                        {
                            if (inThread.IsSkipping() && m_SkippedEvents.Contains(node.Event.Type))
                                continue;

                            IEnumerator coroutine;
                            if (eventHandler.TryEvaluate(node.Event, inThread, out coroutine))
                            {
                                if (!inThread.GetHandle().Equals(handle))
                                    yield break;
                                
                                if (coroutine != null)
                                    yield return coroutine;

                                if (inThread.Dialog != dialogPanel)
                                {
                                    dialogPanel = inThread.Dialog;
                                    eventHandler = dialogPanel.PrepLine(lineEvents, m_TagEventHandler, bHasDialogEvents);
                                }

                                inThread.Dialog?.UpdateInput();
                            }
                            break;
                        }
                    case TagNodeType.Text:
                        {
                            if (inThread.IsSkipping())
                                continue;

                            yield return Routine.Inline(inThread.Dialog.TypeLine(node.Text));
                            break;
                        }
                }
            }

            if (!inThread.IsSkipping() && lineEvents.RichText.Length > 0)
            {
                yield return inThread.Dialog?.CompleteLine();
            }

            yield return Routine.Command.BreakAndResume;
        }

        private void SkipEventLine(ScriptThread inThread, StringSlice inLine)
        {
            if (inLine.IsEmpty || inLine.IsWhitespace)
                return;

            TagString lineEvents = inThread.TagString;
            TagStringEventHandler eventHandler = m_TagEventHandler;
            ParseToTag(ref lineEvents, inLine, inThread);

            inThread.RecordDialog(lineEvents);

            for (int i = 0; i < lineEvents.Nodes.Length; ++i)
            {
                TagNodeData node = lineEvents.Nodes[i];
                switch (node.Type)
                {
                    case TagNodeType.Event:
                        {
                            if (m_SkippedEvents.Contains(node.Event.Type))
                                continue;

                            eventHandler.TryEvaluate(node.Event, inThread, out IEnumerator coroutine);
                            break;
                        }
                }
            }
        }
    
        private IEnumerator PerformEventChoice(ScriptThread inThread, LeafChoice inChoice, ILeafContentResolver inContentResolver)
        {
            inThread.StopSkipping();
            
            DialogPanel dialogPanel = inThread.Dialog ?? (inThread.Dialog = Services.UI.GetDialog("center"));
            inThread.MarkChoice();
            yield return dialogPanel.ShowOptions(inThread.PeekNode(), inChoice, inContentResolver, inThread);
            inThread.EndChoice();

            LeafChoice.Option chosenOption = inChoice[inChoice.ChosenIndex()];

            string chosenLine;
            inContentResolver.TryGetLine(chosenOption.LineCode, inThread.PeekNode(), out chosenLine);
            inThread.RecordChoice(chosenLine);
        }
    }
}