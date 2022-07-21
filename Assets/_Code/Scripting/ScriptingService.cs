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
using UnityEngine.Scripting;
using BeauUtil.Blocks;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(UIMgr), typeof(LocService), typeof(AssetsService), typeof(TweakMgr))]
    public partial class ScriptingService : ServiceBehaviour, IPauseable, IDebuggable, ILoadable
    {
        public delegate void ScriptThreadHandler(ScriptThreadHandle inHandle);
        public delegate void ScriptTargetHandler(StringHash32 inTarget);
        public delegate Future<StringHash32> ChoiceSelectorHandler();

        private struct QueuedEvent {
            public int Priority;
            public uint Id;

            public Action OnStart;
            public Action OnComplete;

            public StringHash32 TriggerId;
            public TempVarTable Vars;
            public Future<ScriptThreadHandle> Return;

            static public readonly Comparison<QueuedEvent> SortByPriority = (x, y) => {
                int priorityCompare = Math.Sign(y.Priority - x.Priority);
                if (priorityCompare == 0) {
                    return Math.Sign((int) x.Id - (int) y.Id);
                } else {
                    return priorityCompare;
                }
            };

            static private uint s_CurrentId = 0;
            static internal uint NextId() {
                return s_CurrentId++;
            }

            static internal void ResetIds() {
                s_CurrentId = 0;
            }
        }

        // thread management
        private Dictionary<StringHash32, ScriptThread> m_ThreadTargetMap = new Dictionary<StringHash32, ScriptThread>(8);
        private List<ScriptThread> m_ThreadList = new List<ScriptThread>(64);
        private ScriptThread m_CutsceneThread = null;
        
        // event parsing
        private TagStringEventHandler m_TagEventHandler;
        private CustomTagParserConfig m_TagEventParser;
        private HashSet<StringHash32> m_SkippedEvents;
        private HashSet<StringHash32> m_DialogOnlyEvents;
        private MethodCache<LeafMember> m_LeafCache;
        private Dictionary<StringHash32, ChoiceSelectorHandler> m_ChoiceSelectors;

        // trigger eval
        private CustomVariantResolver m_CustomResolver;
        private RingBuffer<QueuedEvent> m_QueuedTriggers;

        // script nodes
        private HashSet<ScriptNodePackage> m_LoadedPackages;
        private Dictionary<LeafAsset, ScriptNodePackage> m_LoadedPackageSourcesAssets;

        private RingBuffer<LeafAsset> m_PackageLoadQueue = new RingBuffer<LeafAsset>();
        private RingBuffer<LeafAsset> m_PackageUnloadQueue = new RingBuffer<LeafAsset>();
        private Routine m_PackageLoadWorker;
        private Routine m_PackageUnloadWorker;
        private LeafAsset m_CurrentPackageBeingLoaded;
        private AsyncHandle m_CurrentPackageLoadHandle;

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

        #region Triggering Responses

        /// <summary>
        /// Attempts to trigger a response.
        /// </summary>
        public ScriptThreadHandle TriggerResponse(StringHash32 inTriggerId, VariantTable inContextTable = null)
        {
            return TriggerResponse(inTriggerId, null, null, inContextTable);
        }

        /// <summary>
        /// Attempts to trigger a response.
        /// </summary>
        public ScriptThreadHandle TriggerResponse(StringHash32 inTriggerId, StringHash32 inTarget, ScriptObject inContext = null, VariantTable inContextTable = null, Action inOnComplete = null)
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
                    LeafEvalContext context = LeafEvalContext.FromResolver(this, resolver, inContext);
                    int responseCount = responseSet.GetHighestScoringNodes(context, Save.Current?.Script, inTarget, m_ThreadTargetMap, nodes, ref minScore);
                    if (responseCount > 0)
                    {
                        ScriptNode node = RNG.Instance.Choose(nodes);
                        DebugService.Log(LogMask.Scripting, "[ScriptingService] Trigger '{0}' -> Running node '{1}'", inTriggerId, node.Id());
                        Services.Events.Dispatch(GameEvents.ScriptFired, node.FullName());
                        handle = StartThreadInternalNode(inContext, node, inContextTable, inOnComplete);
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
                            StartThreadInternalNode(inContext, nodes[i], inContextTable, null);
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
            m_ThreadTargetMap.Clear();
            m_CutsceneThread = null;

            foreach(var trigger in m_QueuedTriggers)
            {
                trigger.Vars.Dispose();
            }
            m_QueuedTriggers.Clear();
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

        #region Queued Triggers

        /// <summary>
        /// Queues up a trigger response.
        /// </summary>
        public void QueueTriggerResponse(StringHash32 inTriggerId, int inPriority = 0, TempVarTable inContextTable = default, Action inOnCompleted = null)
        {
            m_QueuedTriggers.PushBack(new QueuedEvent()
            {
                Id = QueuedEvent.NextId(),
                TriggerId = inTriggerId,
                Priority = inPriority,
                Vars = inContextTable,
                OnComplete = inOnCompleted
            });
        }

        /// <summary>
        /// Queues up an invocation.
        /// </summary>
        public void QueueInvoke(Action inInvoke, int inPriority = 0)
        {
            m_QueuedTriggers.PushBack(new QueuedEvent()
            {
                Id = QueuedEvent.NextId(),
                OnStart = inInvoke,
                Priority = inPriority,
            });
        }

        private void ProcessQueuedTriggers()
        {
            if (m_QueuedTriggers.Count == 0)
            {
                QueuedEvent.ResetIds();
                return;
            }

            if (Script.ShouldBlock())
            {
                return;
            }
            
            ScriptThreadHandle handle;
            QueuedEvent trigger;
            m_QueuedTriggers.Sort(QueuedEvent.SortByPriority);
            while(m_QueuedTriggers.TryPopFront(out trigger))
            {
                int expectedSize = m_QueuedTriggers.Count;
                trigger.OnStart?.Invoke();

                if (!trigger.TriggerId.IsEmpty)
                {
                    handle = TriggerResponse(trigger.TriggerId, null, null, trigger.Vars, trigger.OnComplete);
                    trigger.Vars.Dispose();
                    if (handle.IsRunning())
                    {
                        trigger.Return?.Complete(handle);
                        break;
                    }
                }
            
                trigger.OnComplete?.Invoke();

                if (Script.ShouldBlock())
                    break;

                if (m_QueuedTriggers.Count != expectedSize)
                {
                    m_QueuedTriggers.Sort(QueuedEvent.SortByPriority);
                }
            }
        }

        #endregion // Queued Triggers

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

        #region Choice Handlers

        public void RegisterChoiceSelector(StringHash32 inId, ChoiceSelectorHandler inHandler)
        {
            m_ChoiceSelectors[inId] = inHandler;
        }

        public bool TryHandleChoiceSelector(StringHash32 inId, out Future<StringHash32> outResponse)
        {
            ChoiceSelectorHandler handler;
            if (m_ChoiceSelectors.TryGetValue(inId, out handler))
            {
                outResponse = handler();
                Assert.NotNull(outResponse, "Cannot return null future from a choice handler");
                return true;
            }

            outResponse = null;
            return false;
        }

        #endregion // Choice Handlers

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

            StringHash32 who = inThread.Target();
            if (!who.IsEmpty)
            {
                m_ThreadTargetMap.Remove(who);
                OnTargetedThreadKilled?.Invoke(who);
            }

            if (m_CutsceneThread == inThread)
                m_CutsceneThread = null;
        }

        private ScriptThreadHandle StartThreadInternalNode(ScriptObject inContext, ScriptNode inNode, VariantTable inVars, Action inOnComplete)
        {
            if (inNode == null || !CheckPriority(inNode))
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
            ScriptThreadHandle handle = thread.Prep(inContext, tempVars);
            thread.SyncPriority(inNode);
            Routine routine = Routine.Start(this, ProcessNodeInstructions(thread, inNode)).SetPhase(RoutinePhase.Manual);
            if (inOnComplete != null)
            {
                routine.OnComplete(inOnComplete);
                routine.OnStop(inOnComplete);
            }
            thread.AttachRoutine(routine);

            m_ThreadList.Add(thread);

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

        private bool CheckPriority(ScriptNode inNode)
        {
            StringHash32 target = inNode.TargetId();
            if (target.IsEmpty)
                return true;

            ScriptThread thread;
            if (m_ThreadTargetMap.TryGetValue(target, out thread))
            {
                bool higherPriority = (inNode.Flags() & ScriptNodeFlags.Interrupt) != 0
                    ? thread.Priority() > inNode.Priority()
                    : thread.Priority() >= inNode.Priority();
                if (higherPriority)
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

        #region Loading

        public bool IsLoading()
        {
            return m_PackageLoadQueue.Count > 0 || m_CurrentPackageBeingLoaded != null || m_PackageUnloadQueue.Count > 0;
        }

        #endregion // Loading

        #region Unity Events

        private void LateUpdate()
        {
            if (m_PauseCount == 0)
            {
                Routine.ManualUpdate(Time.deltaTime);
                ProcessQueuedTriggers();
            }
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

            ScriptNodePackage.Generator.Instance.MethodCache = m_LeafCache;

            BlockMetaCache.Default.Cache(typeof(ScriptNode));
            BlockMetaCache.Default.Cache(typeof(ScriptNodePackage));
            LookupTables.ToStringLookup(1);

            m_ParserPool = new DynamicPool<TagStringParser>(4, (p) => {
                var parser = new TagStringParser();
                parser.Delimiters = Parsing.InlineEvent;
                parser.EventProcessor = m_TagEventParser;
                parser.ReplaceProcessor = m_TagEventParser;
                return parser;
            });

            m_ChoiceSelectors = new Dictionary<StringHash32, ChoiceSelectorHandler>();
            m_LoadedPackages = new HashSet<ScriptNodePackage>();
            m_LoadedEntrypoints = new Dictionary<StringHash32, ScriptNode>(256);
            m_LoadedResponses = new Dictionary<StringHash32, TriggerResponseSet>();
            m_LoadedFunctions = new Dictionary<StringHash32, FunctionSet>();
            m_LoadedPackageSourcesAssets = new Dictionary<LeafAsset, ScriptNodePackage>();

            m_TablePool = new DynamicPool<VariantTable>(8, Pool.DefaultConstructor<VariantTable>());
            m_TablePool.Config.RegisterOnFree((p, obj) => { obj.Reset(); });
            m_TablePool.Prewarm();

            m_ThreadPool = new DynamicPool<ScriptThread>(16, (p) => new ScriptThread(this));
            m_QueuedTriggers = new RingBuffer<QueuedEvent>(16, RingBufferMode.Expand);
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

            scriptingMenu.AddDivider();
            scriptingMenu.AddButton("Dump Loaded Responses", DumpTriggerResponses);

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
                psb.Builder.Append("[ScriptingService] Dumping Script State");
                foreach(var table in resolver.AllTables())
                {
                    psb.Builder.Append('\n').Append(table.ToDebugString());
                }

                psb.Builder.Append("\nAll Visited Nodes");
                foreach(var node in Save.Script.ProfileNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                psb.Builder.Append("\nAll Visited in Current Session");
                foreach(var node in Save.Script.SessionNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                psb.Builder.Append("\nRecent Node History");
                foreach(var node in Save.Script.RecentNodeHistory)
                {
                    psb.Builder.Append("\n  ").Append(node.ToDebugString());
                }

                Debug.Log(psb.Builder.Flush());
            }
        }

        static private void DumpTriggerResponses()
        {
            var responses = Services.Script.m_LoadedResponses;
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append("[ScriptingService] Dumping loaded trigger responses");
                foreach(var table in responses)
                {
                    table.Value.Dump(psb.Builder, table.Key);
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
            Save.Script.Reset();
            Log.Warn("[DebugService] Cleared all scripting state");
        }

        #endif // DEVELOPMENT

        #endregion // IDebuggable

        #region Leaf Methods

        /// <summary>
        /// Stops skipping the current cutscene.
        /// </summary>
        [LeafMember("StopSkippingCutscene"), Preserve]
        static public void LeafThreadStopSkippingCutscene()
        {
            Services.Script.GetCutscene().GetThread().StopSkipping();
        }

        [LeafMember("StopSkipping"), Preserve]
        static private void LeafThreadStopSkipping([BindThread] ScriptThread inThread)
        {
            inThread.StopSkipping();
        }

        #endregion // Leaf Methods

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
                    else if (node.Event.Type == LeafUtils.Events.Character)
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
                                outName = Save.Name;
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