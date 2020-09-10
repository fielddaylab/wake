using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        // thread management
        private Dictionary<string, ScriptThreadHandle> m_ThreadMap = new Dictionary<string, ScriptThreadHandle>(64, StringComparer.Ordinal);
        private List<ScriptThreadHandle> m_ThreadList = new List<ScriptThreadHandle>(64);
        
        // event parsing
        private TagStringEventHandler m_TagEventHandler;
        private CustomTagParserConfig m_TagEventParser;
        private TagStringParser m_TagStringParser;
        private StringUtils.ArgsList.Splitter m_ArgListSplitter;

        // script nodes
        private HashSet<ScriptNodePackage> m_LoadedPackages;
        private Dictionary<string, ScriptNode> m_LoadedEntrypoints;

        // objects
        private List<ScriptObject> m_ScriptObjects = new List<ScriptObject>();

        // pool
        private IPool<TagString> m_TagStrings;

        #region Operations

        #region Starting Threads with IEnumerator

        /// <summary>
        /// Returns a new scripting thread running the given IEnumerator.
        /// </summary>
        public ScriptThreadHandle StartThread(IEnumerator inEnumerator)
        {
            return StartThreadInternal(null, null, inEnumerator);
        }

        /// <summary>
        /// Returns a new scripting thread with the given id running the given IEnumerator.
        /// </summary>
        public ScriptThreadHandle StartThread(string inId, IEnumerator inEnumerator)
        {
            return StartThreadInternal(inId, null, inEnumerator);
        }

        /// <summary>
        /// Returns a new scripting thread running the given IEnumerator and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartThread(IScriptContext inContext, IEnumerator inEnumerator)
        {
            return StartThreadInternal(null, inContext, inEnumerator);
        }

        /// <summary>
        /// Returns a new scripting thread running the given IEnumerator and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartThread(string inId, IScriptContext inContext, IEnumerator inEnumerator)
        {
            return StartThreadInternal(inId, inContext, inEnumerator);
        }

        #endregion // Starting Threads with IEnumerator

        #region Starting Threads with ScriptNode

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode.
        /// </summary>
        public ScriptThreadHandle StartNode(ScriptNode inNode)
        {
            return StartThreadInternal(null, null, ProcessNode(inNode, null));
        }

        /// <summary>
        /// Returns a new scripting thread with the given id running the given ScriptNode.
        /// </summary>
        public ScriptThreadHandle StartNode(string inId, ScriptNode inNode)
        {
            return StartThreadInternal(inId, null, ProcessNode(inNode, null));
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(IScriptContext inContext, ScriptNode inNode)
        {
            return StartThreadInternal(null, inContext, ProcessNode(inNode, inContext));
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(string inId, IScriptContext inContext, ScriptNode inNode)
        {
            return StartThreadInternal(inId, inContext, ProcessNode(inNode, inContext));
        }

        private IEnumerator ProcessNode(ScriptNode inNode, IScriptContext inContext)
        {
            if (inNode == null)
                return null;

            return PerformNodeInternal(inNode, inContext);
        }

        #endregion // Starting Threads with ScriptNode

        #region Starting Threads with Entrypoint

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint.
        /// </summary>
        public ScriptThreadHandle StartNode(string inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Debug.LogWarningFormat("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternal(null, null, ProcessNode(node, null));
        }

        /// <summary>
        /// Returns a new scripting thread with the given id running the given ScriptNode entrypoint.
        /// </summary>
        public ScriptThreadHandle StartNode(string inId, string inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Debug.LogWarningFormat("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternal(inId, null, ProcessNode(node, null));
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(IScriptContext inContext, string inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Debug.LogWarningFormat("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternal(null, inContext, ProcessNode(node, inContext));
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(string inId, IScriptContext inContext, string inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Debug.LogWarningFormat("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternal(inId, inContext, ProcessNode(node, inContext));
        }

        #endregion // Starting Threads with Entrypoint

        #region Killing Threads

        /// <summary>
        /// Kills a currently running scripting thread.
        /// </summary>
        public bool KillThread(string inThreadId)
        {
            ScriptThreadHandle handle;
            
            // wildcard id match
            if (inThreadId.IndexOf('*') >= 0)
            {
                bool bKilled = false;
                for(int i = m_ThreadList.Count - 1; i >= 0; --i)
                {
                    handle = m_ThreadList[i];
                    string id = handle.Id();
                    if (StringUtils.WildcardMatch(id, inThreadId))
                    {
                        handle.Routine().Stop();
                        m_ThreadList.FastRemoveAt(i);
                        m_ThreadMap.Remove(id);
                        bKilled = true;
                    }
                }

                return bKilled;
            }
            else
            {
                if (m_ThreadMap.TryGetValue(inThreadId, out handle))
                {
                    handle.Routine().Stop();
                    m_ThreadMap.Remove(inThreadId);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kills all currently running scripting threads for the given context.
        /// </summary>
        public bool KillThreads(IScriptContext inContext)
        {
            bool bKilled = false;
            ScriptThreadHandle handle;
            for(int i = m_ThreadList.Count - 1; i >= 0; --i)
            {
                handle = m_ThreadList[i];
                if (handle.Context() == inContext)
                {
                    string id = handle.Id();
                    handle.Routine().Stop();
                    m_ThreadList.FastRemoveAt(i);
                    m_ThreadMap.Remove(id);
                    bKilled = true;
                }
            }
            return bKilled;
        }

        /// <summary>
        /// Kills a currently running scripting thread.
        /// </summary>
        public void KillThread(ScriptThreadHandle inThreadHandle)
        {
            string id = inThreadHandle.Id();
            inThreadHandle.Routine().Stop();
            if (!string.IsNullOrEmpty(id))
            {
                m_ThreadMap.Remove(id);
            }
        }

        /// <summary>
        /// Kills all currently running threads.
        /// </summary>
        public void KillAllThreads()
        {
            foreach(var thread in m_ThreadList)
            {
                thread.Routine().Stop();
            }

            m_ThreadList.Clear();
            m_ThreadMap.Clear();
        }

        #endregion // Killing Threads

        #endregion // Operations

        #region Utils

        /// <summary>
        /// Parses a string into a TagString.
        /// </summary>
        public TagString ParseToTag(string inLine, object inContext = null)
        {
            return m_TagStringParser.Parse(inLine, inContext);
        }

        /// <summary>
        /// Parses a string into a TagString.
        /// </summary>
        public void ParseToTag(ref TagString ioTag, string inLine, object inContext = null)
        {
            m_TagStringParser.Parse(ref ioTag, inLine, inContext);
        }

        #endregion // Utils

        #region Internal

        // Performs a node
        private IEnumerator PerformNodeInternal(ScriptNode inStartingNode, IScriptContext inContext)
        {
            ScriptNode currentNode = inStartingNode;
            while(currentNode != null)
            {
                ScriptNode processingNode = currentNode;
                currentNode = null;

                bool bNodeIsCutscene = (processingNode.Flags() & ScriptNodeFlags.Cutscene) != 0;
                if (bNodeIsCutscene)
                    Services.UI.ShowLetterbox();

                try
                {
                    foreach(var line in processingNode.Lines())
                    {
                        yield return Routine.Inline(PerformEventLine(line, inContext));
                    }
                }
                finally
                {
                    if (bNodeIsCutscene)
                        Services.UI.HideLetterbox();
                }
            }

            // TODO: make this work for non-main dialog?
            DialogPanel dialogPanel = Services.UI.Dialog();
            dialogPanel.CompleteSequence();
        }

        // Performs a block of event lines
        private IEnumerator PerformEventLines(IEnumerable<StringSlice> inLines, IScriptContext inContext)
        {
            foreach(var line in inLines)
            {
                yield return Routine.Inline(PerformEventLine(line, inContext));
            }

            // TODO: make this work for non-main dialog?
            DialogPanel dialogPanel = Services.UI.Dialog();
            dialogPanel.CompleteSequence();
        }

        // Reads a line of scripting
        private IEnumerator PerformEventLine(StringSlice inLine, IScriptContext inContext)
        {
            if (inLine.IsEmpty || inLine.IsWhitespace)
                yield break;
            
            TagString lineEvents = m_TagStrings.Alloc();
            TagStringEventHandler eventHandler = m_TagEventHandler;
            DialogPanel dialogPanel = Services.UI.Dialog();
            
            try
            {
                m_TagStringParser.Parse(ref lineEvents, inLine, inContext);
                eventHandler = dialogPanel.PrepLine(lineEvents, m_TagEventHandler);

                for(int i = 0; i < lineEvents.Nodes.Length; ++i)
                {
                    TagNodeData node = lineEvents.Nodes[i];
                    switch(node.Type)
                    {
                        case TagNodeType.Event:
                            {
                                IEnumerator coroutine;
                                if (eventHandler.TryEvaluate(node.Event, inContext, out coroutine))
                                {
                                    if (coroutine != null)
                                        yield return coroutine;
                                    
                                    dialogPanel.UpdateInput();
                                }
                                break;
                            }
                        case TagNodeType.Text:
                            {
                                yield return Routine.Inline(dialogPanel.TypeLine(node.Text));
                                break;
                            }
                    }
                }

                yield return dialogPanel.CompleteLine();
            }
            finally
            {
                m_TagStrings.Free(lineEvents);
            }
        }

        // Starts a scripting thread
        private ScriptThreadHandle StartThreadInternal(string inId, IScriptContext inContext, IEnumerator inEnumerator)
        {
            if (inEnumerator == null)
            {
                return default(ScriptThreadHandle);
            }

            bool bHasId = !string.IsNullOrEmpty(inId);
            if (bHasId)
            {
                if (inId.IndexOf('*') >= 0)
                {
                    Debug.LogErrorFormat("[ScriptingService] Thread id of '{0}' is invalid - contains wildchar", inId);
                    return default(ScriptThreadHandle);
                }

                ScriptThreadHandle current;
                if (m_ThreadMap.TryGetValue(inId, out current))
                {
                    current.Routine().Stop();
                }
            }

            Routine routine = Routine.Start(this, inEnumerator);
            ScriptThreadHandle handle = new ScriptThreadHandle(inId, inContext, routine);
            m_ThreadList.Add(handle);
            if (bHasId)
                m_ThreadMap[inId] = handle;

            return handle;
        }

        #endregion // Internal

        #region Unity Events

        private void LateUpdate()
        {
            // remove all invalid threads
            ScriptThreadHandle handle;
            for(int i = m_ThreadList.Count - 1; i >= 0; --i)
            {
                handle = m_ThreadList[i];
                if (!handle.IsRunning())
                {
                    m_ThreadList.FastRemoveAt(i);

                    string id = handle.Id();
                    if (!string.IsNullOrEmpty(id))
                    {
                        m_ThreadMap.Remove(id);
                    }
                }
            }
        }

        #endregion // Unity Events

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Scripting;
        }

        protected override void OnRegisterService()
        {
            m_TagStrings = new DynamicPool<TagString>(16, Pool.DefaultConstructor<TagString>());

            InitParser();
            InitHandlers();

            m_TagStringParser = new TagStringParser();
            m_TagStringParser.Delimiters = TagStringParser.CurlyBraceDelimiters;
            m_TagStringParser.EventProcessor = m_TagEventParser;
            m_TagStringParser.ReplaceProcessor = m_TagEventParser;

            m_LoadedPackages = new HashSet<ScriptNodePackage>();
            m_LoadedEntrypoints = new Dictionary<string, ScriptNode>(64, StringComparer.Ordinal);
        }

        protected override void OnDeregisterService()
        {
            m_TagEventParser = null;
            m_TagEventHandler = null;

            m_ScriptObjects.Clear();
        }

        #endregion // IService
    }
}