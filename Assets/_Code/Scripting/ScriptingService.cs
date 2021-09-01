#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using Aqua.Scripting;
using BeauUtil.Variants;
using Leaf.Runtime;
using Leaf;
using BeauUtil.Services;
using Aqua.Debugging;
using BeauUtil.Debugger;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(UIMgr), typeof(LocService), typeof(AssetsService), typeof(TweakMgr))]
    public partial class ScriptingService : ServiceBehaviour, IPauseable, IDebuggable
    {
        public delegate void ScriptThreadHandler(ScriptThreadHandle inHandle);
        public delegate void ScriptTargetHandler(StringHash32 inTarget);

        // thread management
        private Dictionary<string, ScriptThread> m_ThreadMap = new Dictionary<string, ScriptThread>(64, StringComparer.Ordinal);
        private Dictionary<StringHash32, ScriptThread> m_ThreadTargetMap = new Dictionary<StringHash32, ScriptThread>(8);
        private List<ScriptThread> m_ThreadList = new List<ScriptThread>(64);
        private ScriptThread m_CutsceneThread = null;
        
        // event parsing
        private TagStringEventHandler m_TagEventHandler;
        private CustomTagParserConfig m_TagEventParser;
        private LeafRuntime<ScriptNode> m_ThreadRuntime;
        private HashSet<StringHash32> m_SkippedEvents;
        private HashSet<StringHash32> m_DialogOnlyEvents;
        private MethodCache<LeafMember> m_LeafCache;

        // trigger eval
        private CustomVariantResolver m_CustomResolver;

        // script nodes
        private HashSet<ScriptNodePackage> m_LoadedPackages;
        private Dictionary<LeafAsset, ScriptNodePackage> m_LoadedPackageSourcesAssets;

        private Dictionary<StringHash32, ScriptNode> m_LoadedEntrypoints;
        private Dictionary<StringHash32, TriggerResponseSet> m_LoadedResponses;
        private Dictionary<StringHash32, FunctionSet> m_LoadedFunctions;

        // objects
        [NonSerialized] private List<ScriptObject> m_ScriptObjects = new List<ScriptObject>();
        [NonSerialized] private bool m_ScriptObjectListDirty = false;

        // pools
        private IPool<VariantTable> m_TablePool;
        private IPool<ScriptThread> m_ThreadPool;
        private IPool<TagStringParser> m_ParserPool;

        // pausing
        private int m_PauseCount = 0;

        #region Checks

        /// <summary>
        /// Returns if a thread is executing on the given target.
        /// </summary>
        public bool IsTargetExecuting(StringHash32 inTarget)
        {
            return m_ThreadTargetMap.ContainsKey(inTarget);
        }

        /// <summary>
        /// Returns the thread executing for the given target.
        /// </summary>
        public ScriptThreadHandle GetTargetThread(StringHash32 inTarget)
        {
            ScriptThread thread;
            if (m_ThreadTargetMap.TryGetValue(inTarget, out thread))
            {
                return thread.GetHandle();
            }

            return default(ScriptThreadHandle);
        }

        /// <summary>
        /// Returns if a cutscene thread is executing.
        /// </summary>
        public bool IsCutscene()
        {
            return m_CutsceneThread != null;
        }

        /// <summary>
        /// Returns the current cutscene thread.
        /// </summary>
        public ScriptThreadHandle GetCutscene()
        {
            return m_CutsceneThread?.GetHandle() ?? default(ScriptThreadHandle);
        }

        #endregion // Checks

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
        public ScriptThreadHandle StartThread(IEnumerator inEnumerator, string inThreadId)
        {
            return StartThreadInternal(inThreadId, null, inEnumerator);
        }

        /// <summary>
        /// Returns a new scripting thread running the given IEnumerator and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartThread(ScriptObject inContext, IEnumerator inEnumerator)
        {
            return StartThreadInternal(null, inContext, inEnumerator);
        }

        /// <summary>
        /// Returns a new scripting thread running the given IEnumerator and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartThread(ScriptObject inContext, IEnumerator inEnumerator, string inThreadId)
        {
            return StartThreadInternal(inThreadId, inContext, inEnumerator);
        }

        #endregion // Starting Threads with IEnumerator

        #region Starting Threads with Entrypoint

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint.
        /// </summary>
        public ScriptThreadHandle StartNode(StringHash32 inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Log.Warn("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternalNode(null, null, node, null);
        }

        /// <summary>
        /// Returns a new scripting thread with the given id running the given ScriptNode entrypoint.
        /// </summary>
        public ScriptThreadHandle StartNode(StringHash32 inEntrypointId, string inThreadId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Log.Warn("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternalNode(inThreadId, null, node, null);
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(ScriptObject inContext, StringHash32 inEntrypointId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Log.Warn("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternalNode(null, inContext, node, null);
        }

        /// <summary>
        /// Returns a new scripting thread running the given ScriptNode entrypoint and attached to the given context.
        /// </summary>
        public ScriptThreadHandle StartNode(ScriptObject inContext, StringHash32 inEntrypointId, string inThreadId)
        {
            ScriptNode node;
            if (!TryGetEntrypoint(inEntrypointId, out node))
            {
                Log.Warn("[ScriptingService] No entrypoint '{0}' is currently loaded", inEntrypointId);
                return default(ScriptThreadHandle);
            }

            return StartThreadInternalNode(inThreadId, inContext, node, null);
        }

        #endregion // Starting Threads with Entrypoint

        #region Triggering Responses

        /// <summary>
        /// Attempts to trigger a response.
        /// </summary>
        public ScriptThreadHandle TriggerResponse(StringHash32 inTriggerId, StringHash32 inTarget = default(StringHash32), ScriptObject inContext = null, VariantTable inContextTable = null, string inThreadId = null)
        {
            TryCallFunctions(inTriggerId, inTarget, inContext, inContextTable);

            ScriptThreadHandle handle = default(ScriptThreadHandle);
            IVariantResolver resolver = GetResolver(inContextTable);
            TriggerResponseSet responseSet;
            if (m_LoadedResponses.TryGetValue(inTriggerId, out responseSet))
            {
                using(PooledList<ScriptNode> nodes = PooledList<ScriptNode>.Create())
                {
                    DebugService.Log(LogMask.Scripting, "[ScriptingService] Evaluating trigger {0}...", inTriggerId.ToDebugString());
                    
                    int minScore = int.MinValue;
                    int responseCount = responseSet.GetHighestScoringNodes(resolver, m_LeafCache, inContext, Services.Data.Profile?.Script, inTarget, m_ThreadTargetMap, nodes, ref minScore);
                    if (responseCount > 0)
                    {
                        ScriptNode node = RNG.Instance.Choose(nodes);
                        DebugService.Log(LogMask.Scripting, "[ScriptingService] Trigger '{0}' -> Running node '{1}'", inTriggerId, node.Id());
                        handle = StartThreadInternalNode(inThreadId, inContext, node, inContextTable);
                    }
                }
            }
            if (!handle.IsRunning())
            {
                DebugService.Log(LogMask.Scripting, "[ScriptingService] Trigger '{0}' had no valid responses", inTriggerId);
            }
            ResetCustomResolver();
            return handle;
        }

        private IVariantResolver GetResolver(VariantTable inContext)
        {
            if (inContext == null || (inContext.Count == 0 && inContext.Base == null))
                return Services.Data.VariableResolver;
            
            if (m_CustomResolver == null)
            {
                m_CustomResolver = new CustomVariantResolver();
                m_CustomResolver.Base = Services.Data.VariableResolver;
            }

            m_CustomResolver.SetDefaultTable(inContext);
            return m_CustomResolver;
        }

        private void ResetCustomResolver()
        {
            if (m_CustomResolver != null)
            {
                m_CustomResolver.ClearDefaultTable();
            }
        }

        #endregion // Triggering Responses
        
        #region Functions

        /// <summary>
        /// Attempts to trigger a response.
        /// </summary>
        public void TryCallFunctions(StringHash32 inFunctionId, StringHash32 inTarget = default(StringHash32), ScriptObject inContext = null, VariantTable inContextTable = null)
        {
            IVariantResolver resolver = GetResolver(inContextTable);
            FunctionSet functionSet;
            if (m_LoadedFunctions.TryGetValue(inFunctionId, out functionSet))
            {
                using(PooledList<ScriptNode> nodes = PooledList<ScriptNode>.Create())
                {
                    int responseCount = functionSet.GetNodes(inTarget, nodes);
                    if (responseCount > 0)
                    {
                        for(int i = responseCount - 1; i >= 0; --i)
                        {
                            DebugService.Log(LogMask.Scripting,  "[ScriptingService] Executing function {0} with function id '{1}'", nodes[i].Id(), inFunctionId);
                            StartThreadInternalNode(null, inContext, nodes[i], inContextTable);
                        }
                    }
                    else
                    {
                        DebugService.Log(LogMask.Scripting,  "[ScriptingService] No functions available with id '{0}'", inFunctionId);
                    }
                }
            }
            else
            {
                DebugService.Log(LogMask.Scripting,  "[ScriptingService] No functions with id '{0}'", inFunctionId);
            }
            ResetCustomResolver();
        }

        #endregion // Functions

        #region Killing Threads

        /// <summary>
        /// Kills a currently running scripting thread.
        /// </summary>
        public bool KillThread(string inThreadId)
        {
            ScriptThread thread;
            
            // wildcard id match
            if (inThreadId.IndexOf('*') >= 0)
            {
                bool bKilled = false;
                for(int i = m_ThreadList.Count - 1; i >= 0; --i)
                {
                    thread = m_ThreadList[i];
                    string id = thread.Name;
                    if (StringUtils.WildcardMatch(id, inThreadId))
                    {
                        thread.Kill();
                        bKilled = true;
                    }
                }

                return bKilled;
            }
            else
            {
                if (m_ThreadMap.TryGetValue(inThreadId, out thread))
                {
                    thread.Kill();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kills all currently running scripting threads for the given context.
        /// </summary>
        public bool KillThreads(ScriptObject inContext)
        {
            bool bKilled = false;
            ScriptThread thread;
            for(int i = m_ThreadList.Count - 1; i >= 0; --i)
            {
                thread = m_ThreadList[i];
                if (thread.Actor == inContext)
                {
                    string id = thread.Name;
                    thread.Kill();
                    bKilled = true;
                }
            }
            return bKilled;
        }

        /// <summary>
        /// Kills all currently running threads.
        /// </summary>
        public void KillAllThreads()
        {
            DebugService.Log(LogMask.Scripting,  "[ScriptingService] Killing all threads");

            for(int i = m_ThreadList.Count - 1; i >= 0; --i)
            {
                m_ThreadList[i].Kill();
            }

            m_ThreadList.Clear();
            m_ThreadMap.Clear();
            m_ThreadTargetMap.Clear();
            m_CutsceneThread = null;
        }

        /// <summary>
        /// Kills all threads with a priority less than the given priority
        /// </summary>
        public void KillLowPriorityThreads(TriggerPriority inThreshold = TriggerPriority.Cutscene)
        {
            DebugService.Log(LogMask.Scripting,  "[ScriptingService] Killing all with priority less than {0}", inThreshold);

            for(int i = m_ThreadList.Count - 1; i >= 0; --i)
            {
                var thread = m_ThreadList[i];
                if (thread.Priority() < inThreshold)
                    thread.Kill();
            }
        }

        /// <summary>
        /// Kills all threads for the given target.
        /// </summary>
        public bool KillTargetThread(StringHash32 inTargetId)
        {
            ScriptThread thread;
            if (m_ThreadTargetMap.TryGetValue(inTargetId, out thread))
            {
                thread.Kill();
                return true;
            }

            return false;
        }

        #endregion // Killing Threads

        #region Calling Methods

        /// <summary>
        /// Executes a script command.
        /// Format should be "method arg0, arg1, arg2, ..." or "targetId->method arg0, arg1, arg2, ..."
        /// </summary>
        public object Execute(StringSlice inCommand)
        {
            StringSlice target = StringSlice.Empty, method, args;
            var methodArgs = TagData.Parse(inCommand, Parsing.InlineEvent);
            method = methodArgs.Id;
            args = methodArgs.Data;

            int indirectIndex = method.IndexOf("->");
            if (indirectIndex >= 0)
            {
                target = method.Substring(0, indirectIndex);
                method = method.Substring(indirectIndex + 2);
            }

            object result;
            if (target.IsEmpty)
            {
                m_LeafCache.TryStaticInvoke(method, args, null, out result);
            }
            else
            {
                ScriptObject targetObj;
                if (!TryGetScriptObjectById(target, out targetObj))
                {
                    Log.Warn("[ScriptingService] No ScriptObject with id '{0}' exists");
                    result = null;
                }
                else
                {
                    m_LeafCache.TryInvoke(targetObj, method, args, null, out result);
                }
            }

            return result;
        }

        #endregion // Calling Methods

        #endregion // Operations

        #region Contexts

        public TempVarTable GetTempTable()
        {
            var table = m_TablePool.TempAlloc();
            table.Object.Name = "temp";
            return new TempVarTable(table);
        }

        public TempVarTable GetTempTable(VariantTable inBase)
        {
            var table = m_TablePool.TempAlloc();
            table.Object.Name = "temp";
            table.Object.Base = inBase;
            return new TempVarTable(table);
        }

        #endregion // Contexts

        #region Utils

        /// <summary>
        /// Parses a string into a TagString.
        /// </summary>
        public TagString ParseToTag(StringSlice inLine, object inContext = null)
        {
            TagString str = new TagString();
            ParseToTag(ref str, inLine, inContext);
            return str;
        }

        /// <summary>
        /// Parses a string into a TagString.
        /// </summary>
        public void ParseToTag(ref TagString ioTag, StringSlice inLine, object inContext = null)
        {
            TagStringParser parser = m_ParserPool.Alloc();
            parser.Parse(ref ioTag, inLine, inContext);
            m_ParserPool.Free(parser);
        }

        #endregion // Utils

        #region Internal

        internal IMethodCache LeafInvoker { get { return m_LeafCache; } }

        internal void UntrackThread(ScriptThread inThread)
        {
            m_ThreadList.FastRemove(inThread);

            string name = inThread.Name;
            if (!string.IsNullOrEmpty(name))
                m_ThreadMap.Remove(name);

            StringHash32 who = inThread.Target();
            if (!who.IsEmpty)
            {
                m_ThreadTargetMap.Remove(who);
                OnTargetedThreadKilled?.Invoke(who);
            }

            if (m_CutsceneThread == inThread)
                m_CutsceneThread = null;
        }

        // Starts a scripting thread
        private ScriptThreadHandle StartThreadInternal(string inThreadName, ScriptObject inContext, IEnumerator inEnumerator)
        {
            if (inEnumerator == null || !FreeName(inThreadName))
            {
                return default(ScriptThreadHandle);
            }

            ScriptThread thread = m_ThreadPool.Alloc();
            ScriptThreadHandle handle = thread.Prep(inThreadName, inContext, null);
            thread.AttachRoutine(Routine.Start(this, inEnumerator).SetPhase(RoutinePhase.Manual));

            m_ThreadList.Add(thread);
            if (!string.IsNullOrEmpty(inThreadName))
                m_ThreadMap.Add(inThreadName, thread);
            return handle;
        }

        private ScriptThreadHandle StartThreadInternalNode(string inThreadName, ScriptObject inContext, ScriptNode inNode, VariantTable inVars)
        {
            if (inNode == null || !FreeName(inThreadName) || !CheckPriority(inNode))
            {
                return default(ScriptThreadHandle);
            }

            if (inNode.IsCutscene())
            {
                m_CutsceneThread?.Kill();
                KillLowPriorityThreads(TriggerPriority.High);
            }

            TempAlloc<VariantTable> tempVars = m_TablePool.TempAlloc();
            if (inVars != null && inVars.Count > 0)
            {
                inVars.CopyTo(tempVars.Object);
                tempVars.Object.Base = inVars.Base;
            }

            ScriptThread thread = m_ThreadPool.Alloc();
            ScriptThreadHandle handle = thread.Prep(inThreadName, inContext, tempVars);
            thread.SyncPriority(inNode);
            thread.AttachRoutine(Routine.Start(this, ProcessNodeInstructions(thread, inNode)).SetPhase(RoutinePhase.Manual));

            m_ThreadList.Add(thread);
            if (!string.IsNullOrEmpty(inThreadName))
                m_ThreadMap.Add(inThreadName, thread);

            if (inNode.IsCutscene())
            {
                m_CutsceneThread = thread;
            }

            StringHash32 who = thread.Target();
            if (!who.IsEmpty)
            {
                m_ThreadTargetMap.Add(who, thread);
                OnTargetedThreadStarted?.Invoke(handle);
            }

            if (!IsPaused())
                thread.ForceTick();
            
            return handle;
        }

        private bool FreeName(string inThreadName)
        {
            bool bHasId = !string.IsNullOrEmpty(inThreadName);
            if (bHasId)
            {
                if (inThreadName.IndexOf('*') >= 0)
                {
                    Log.Error("[ScriptingService] Thread id of '{0}' is invalid - contains wildchar", inThreadName);
                    return false;
                }

                ScriptThread current;
                if (m_ThreadMap.TryGetValue(inThreadName, out current))
                {
                    current.Kill();
                }
            }

            return true;
        }

        private bool CheckPriority(ScriptNode inNode)
        {
            StringHash32 target = inNode.TargetId();
            if (target.IsEmpty)
                return true;

            ScriptThread thread;
            if (m_ThreadTargetMap.TryGetValue(target, out thread))
            {
                if (thread.Priority() >= inNode.Priority())
                {
                    DebugService.Log(LogMask.Scripting,  "[ScriptingService] Could not trigger node '{0}' on target '{1}' - higher priority thread already running for given target",
                        inNode.Id(), target);
                    return false;
                }

                DebugService.Log(LogMask.Scripting,  "[ScriptingService] Killed thread with priority '{0}' running on target '{1}' - higher priority node '{2}' was requested",
                    thread.Priority(), target, inNode.Id());

                thread.Kill();
                m_ThreadTargetMap.Remove(target);
            }

            return true;
        }

        #endregion // Internal

        #region Pausing

        /// <summary>
        /// Pauses all script execution.
        /// </summary>
        public void Pause()
        {
            ++m_PauseCount;
        }

        /// <summary>
        /// Resumes all script execution.
        /// </summary>
        public void Resume()
        {
            Assert.True(m_PauseCount > 0, "Unbalanced pause/resume calls");
            --m_PauseCount;
        }

        /// <summary>
        /// Returns if the ScriptingService is paused.
        /// </summary>
        public bool IsPaused()
        {
            return m_PauseCount > 0;
        }

        #endregion // Pausing

        #region Unity Events

        private void LateUpdate()
        {
            if (m_PauseCount == 0)
                Routine.ManualUpdate(Time.deltaTime);
        }

        #endregion // Unity Events

        #region Events

        /// <summary>
        /// Dispatched when a thread is started.
        /// </summary>
        public event ScriptThreadHandler OnTargetedThreadStarted;

        /// <summary>
        /// Dispatched when a thread is killed.
        /// </summary>
        public event ScriptTargetHandler OnTargetedThreadKilled;

        #endregion // Events

        #region IService

        protected override void Initialize()
        {
            InitParsers();
            InitHandlers();

            m_LeafCache = LeafUtils.CreateMethodCache(typeof(IScriptComponent));
            m_LeafCache.LoadStatic();

            m_ParserPool = new DynamicPool<TagStringParser>(4, (p) => {
                var parser = new TagStringParser();
                parser.Delimiters = Parsing.InlineEvent;
                parser.EventProcessor = m_TagEventParser;
                parser.ReplaceProcessor = m_TagEventParser;
                return parser;
            });

            m_ThreadRuntime = new LeafRuntime<ScriptNode>(this);

            m_LoadedPackages = new HashSet<ScriptNodePackage>();
            m_LoadedEntrypoints = new Dictionary<StringHash32, ScriptNode>(256);
            m_LoadedResponses = new Dictionary<StringHash32, TriggerResponseSet>();
            m_LoadedFunctions = new Dictionary<StringHash32, FunctionSet>();
            m_LoadedPackageSourcesAssets = new Dictionary<LeafAsset, ScriptNodePackage>();

            m_TablePool = new DynamicPool<VariantTable>(8, Pool.DefaultConstructor<VariantTable>());
            m_TablePool.Config.RegisterOnFree((p, obj) => { obj.Reset(); });

            m_ThreadPool = new DynamicPool<ScriptThread>(16, (p) => new ScriptThread(this));
        }

        protected override void Shutdown()
        {
            m_TagEventParser = null;
            m_TagEventHandler = null;

            m_TablePool.Dispose();
            m_TablePool = null;

            m_ScriptObjects.Clear();
        }

        #endregion // IService

        #region IDebuggable

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            DMInfo scriptingMenu = new DMInfo("Scripting");

            DMInfo triggerMenu = new DMInfo("Trigger Response");
            RegisterTriggerResponse(triggerMenu, GameTriggers.SceneStart);
            RegisterTriggerResponse(triggerMenu, GameTriggers.RequestPartnerHelp);
            RegisterTriggerResponse(triggerMenu, GameTriggers.PartnerTalk);
            RegisterTriggerResponse(triggerMenu, GameTriggers.JobSwitched);
            RegisterTriggerResponse(triggerMenu, GameTriggers.JobStarted);
            RegisterTriggerResponse(triggerMenu, GameTriggers.JobCompleted);
            RegisterTriggerResponse(triggerMenu, GameTriggers.InspectObject);

            scriptingMenu.AddSubmenu(triggerMenu);
            scriptingMenu.AddDivider();

            RegisterLogging(scriptingMenu);
            scriptingMenu.AddDivider();
            
            scriptingMenu.AddButton("Dump Scripting State", DumpScriptingState);
            scriptingMenu.AddButton("Clear Scripting State", ClearScriptingState);

            yield return scriptingMenu;
        }

        static private void RegisterLogging(DMInfo inMenu)
        {
            inMenu.AddToggle("Enable Logging", () => DebugService.IsLogging(LogMask.Scripting),
                (b) => {
                    if (b)
                        DebugService.AllowLogs(LogMask.Scripting);
                    else
                        DebugService.DisallowLogs(LogMask.Scripting);
                });
        }

        static private void RegisterTriggerResponse(DMInfo inMenu, StringHash32 inResponse)
        {
            inMenu.AddButton(inResponse.ToDebugString(), () => Services.Script.TriggerResponse(inResponse));
        }

        static private void DumpScriptingState()
        {
            var resolver = (CustomVariantResolver) Services.Data.VariableResolver;
            using (PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append("[DebugService] Dumping Script State");
                foreach(var table in resolver.AllTables())
                {
                    psb.Builder.Append('\n').Append(table.ToDebugString());
                }

                psb.Builder.Append("\nAll Visited Nodes");
                foreach(var node in Services.Data.Profile.Script.ProfileNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                psb.Builder.Append("\nAll Visited in Current Session");
                foreach(var node in Services.Data.Profile.Script.SessionNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                psb.Builder.Append("\nRecent Node History");
                foreach(var node in Services.Data.Profile.Script.RecentNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                Debug.Log(psb.Builder.Flush());
            }
        }

        static private void ClearScriptingState()
        {
            var resolver = (CustomVariantResolver) Services.Data.VariableResolver;
            foreach(var table in resolver.AllTables())
            {
                table.Clear();
            }
            Services.Data.Profile.Script.Reset();
            Log.Warn("[DebugService] Cleared all scripting state");
        }

        #endif // DEVELOPMENT

        #endregion // IDebuggable

        #region Text Utils

        /// <summary>
        /// Attempts to locate the character and name associtaed 
        /// </summary>
        static public bool TryFindCharacter(TagString inTagString, out StringHash32 outCharacterId, out string outName)
        {
            outName = null;

            var nodes = inTagString.Nodes;
            TagNodeData node;
            for(int i = nodes.Length - 1; i >= 0; --i)
            {
                node = nodes[i];
                if (node.Type == TagNodeType.Event)
                {
                    if (node.Event.Type == ScriptEvents.Dialog.Speaker)
                    {
                        outName = node.Event.StringArgument.ToString();
                    }
                    else if (node.Event.Type == ScriptEvents.Dialog.Target)
                    {
                        StringHash32 target = node.Event.Argument0.AsStringHash();
                        outCharacterId = target;

                        if (target.IsEmpty)
                        {
                            outName = null;
                            return true;
                        }

                        if (outName == null)
                        {
                            var character = Assets.Character(target);
                            if (character.HasFlags(ScriptActorTypeFlags.IsPlayer))
                            {
                                outName = Services.Data.CurrentCharacterName();
                            }
                            else
                            {
                                outName = Loc.Find(character.NameId());
                            }
                        }

                        return true;
                    }
                }
            }

            outCharacterId = StringHash32.Null;
            return outName != null;
        }

        #endregion // Text Utils
    }
}