/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    12 May 2021
 * 
 * File:    DefaultLeafManager.cs
 * Purpose: Default leaf runtime manager.
 */

using System.Collections;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine;
using BeauUtil.Tags;
using BeauRoutine;
using System;
using BeauUtil.Debugger;

namespace Leaf.Defaults
{
    /// <summary>
    /// Default Leaf manager implementation.
    /// </summary>
    public class DefaultLeafManager<TNode> : ILeafPlugin<TNode>
        where TNode : LeafNode
    {
        /// <summary>
        /// Variable resolver.
        /// </summary>
        public readonly CustomVariantResolver Resolver;

        /// <summary>
        /// Runtime configuration.
        /// </summary>
        public readonly LeafRuntimeConfiguration RuntimeConfig;

        protected readonly MonoBehaviour m_RoutineHost;
        protected readonly IMethodCache m_MethodCache;
        protected readonly TagStringParser m_TagParser;
        
        protected CustomTagParserConfig m_TagParseConfig;
        protected TagStringEventHandler m_TagHandler;

        protected ITextDisplayer m_TextDisplayer;
        protected IChoiceDisplayer m_ChoiceDisplayer;

        public DefaultLeafManager(MonoBehaviour inHost, CustomVariantResolver inResolver, IMethodCache inCache = null, LeafRuntimeConfiguration inConfiguration = null)
        {
            if (inHost == null)
                throw new ArgumentNullException("inHost");
                
            m_RoutineHost = inHost;

            if (inResolver != null)
            {
                Resolver = inResolver;
            }
            else
            {
                CustomVariantResolver defaultResolver = new CustomVariantResolver();
                defaultResolver.SetDefaultTable(new VariantTable());
                Resolver = defaultResolver;
            }

            m_MethodCache = inCache ?? LeafUtils.CreateMethodCache();
            RuntimeConfig = inConfiguration ?? new LeafRuntimeConfiguration();
            m_TagParser = new TagStringParser();
            m_TagParser.Delimiters = TagStringParser.CurlyBraceDelimiters;
        }

        /// <summary>
        /// Sets up tag string handling.
        /// </summary>
        public void ConfigureTagStringHandling(CustomTagParserConfig inConfig, TagStringEventHandler inHandler)
        {
            m_TagParseConfig = inConfig;
            m_TagHandler = inHandler;
            m_TagParser.EventProcessor = inConfig;
            m_TagParser.ReplaceProcessor = inConfig;
        }

        /// <summary>
        /// Configures text and choice display.
        /// </summary>
        public void ConfigureDisplay(ITextDisplayer inTextDisplay, IChoiceDisplayer inChoiceDisplay)
        {
            m_TextDisplayer = inTextDisplay;
            m_ChoiceDisplayer = inChoiceDisplay;
        }

        #region Caches

        public IMethodCache MethodCache { get { return m_MethodCache; } }
        IVariantResolver ILeafVariableAccess.Resolver { get { return Resolver; } }

        #endregion // Caches

        #region Runtime Configuration

        LeafRuntimeConfiguration ILeafPlugin.Configuration { get { return RuntimeConfig; } }

        #endregion // Runtime Configuration

        #region Routines

        public virtual LeafThreadHandle Run(TNode inNode, ILeafActor inActor = null, VariantTable inLocals = null, string inName = null, bool inbImmediateTick = true)
        {
            if (inNode == null)
            {
                return default(LeafThreadHandle);
            }

            LeafThreadState<TNode> threadState = new LeafThreadState<TNode>(this);
            LeafThreadHandle handle = threadState.Setup(inName, inActor, inLocals);
            threadState.AttachRoutine(Routine.Start(m_RoutineHost, LeafRuntime.Execute(threadState, inNode)));

            if (inbImmediateTick && m_RoutineHost.isActiveAndEnabled)
                threadState.ForceTick();

            return handle;
        }

        public virtual LeafThreadState<TNode> Fork(LeafThreadState<TNode> inThreadState, TNode inForkNode)
        {
            LeafThreadHandle handle = Run(inForkNode, inThreadState.Actor, inThreadState.Locals, null);
            return handle.GetThread<LeafThreadState<TNode>>();
        }

        #endregion // Routines

        #region Handlers

        public virtual void OnNodeEnter(TNode inNode, LeafThreadState<TNode> inThreadState)
        {
        }

        public virtual void OnNodeExit(TNode inNode, LeafThreadState<TNode> inThreadState)
        {
        }

        public virtual void OnEnd(LeafThreadState<TNode> inThreadState)
        {
            inThreadState.Kill();
        }

        #endregion // Handlers

        #region Random

        public int RandomInt(int inMin, int inMaxExclusive)
        {
            return UnityEngine.Random.Range(inMin, inMaxExclusive);
        }

        public float RandomFloat(float inMin, float inMax)
        {
            return UnityEngine.Random.Range(inMin, inMax);
        }

        #endregion // Random

        #region Dialog

        public virtual IEnumerator RunLine(LeafThreadState<TNode> inThreadState, LeafLineInfo inLine)
        {
            if (inLine.IsEmptyOrWhitespace)
                yield break;

            LeafThreadHandle handle = inThreadState.GetHandle();
            TagString eventString = inThreadState.TagString;
            TagStringEventHandler eventHandler = m_TagHandler;

            m_TagParser.Parse(ref eventString, inLine.Text, inThreadState);

            TagStringEventHandler overrideHandler = m_TextDisplayer.PrepareLine(eventString, eventHandler);
            if (overrideHandler != null)
            {
                overrideHandler.Base = eventHandler;
                eventHandler = overrideHandler;
            }

            for(int i = 0; i < eventString.Nodes.Length; i++)
            {
                TagNodeData node = eventString.Nodes[i];
                switch(node.Type)
                {
                    case TagNodeType.Event:
                        {
                            IEnumerator coroutine;
                            if (eventHandler.TryEvaluate(node.Event, inThreadState, out coroutine))
                            {
                                // if executing this event somehow killed this thread, stop here
                                if (!handle.IsRunning())
                                    yield break;

                                if (coroutine != null)
                                    yield return coroutine;
                            }
                            break;
                        }

                    case TagNodeType.Text:
                        {
                            yield return Routine.Inline(m_TextDisplayer.TypeLine(eventString, node.Text));
                            break;
                        }
                }
            }

            if (eventString.RichText.Length > 0)
            {
                yield return m_TextDisplayer.CompleteLine();
            }

            yield return Routine.Command.BreakAndResume;
        }

        public virtual IEnumerator ShowOptions(LeafThreadState<TNode> inThreadState, LeafChoice inChoice)
        {
            yield return m_ChoiceDisplayer.ShowChoice(inChoice, inThreadState, this);
        }

        #endregion // Dialog

        #region Lookups

        public virtual bool TryLookupLine(StringHash32 inLineCode, LeafNode inLocalNode, out string outLine)
        {
            // use default implementation in LeafRuntime
            outLine = null;
            return false;
        }

        public virtual bool TryLookupNode(StringHash32 inNodeId, TNode inLocalNode, out TNode ouLeafNode)
        {
            // use default implementation in LeafRuntime
            ouLeafNode = null;
            return false;
        }

        public virtual bool TryLookupObject(StringHash32 inObjectId, LeafThreadState inThreadState, out object outObject)
        {
            // use default implementation in LeafRuntime
            outObject = null;
            return false;
        }
    
        #endregion // Lookups

        #region Tables

        /// <summary>
        /// Binds a table to the leaf manager.
        /// </summary>
        public void BindTable(StringHash32 inTableId, VariantTable inTable)
        {
            Resolver.SetTable(inTableId, inTable);
        }

        /// <summary>
        /// Removes a table from the leaf manager.
        /// </summary>
        public void UnbindTable(StringHash32 inTableId)
        {
            Resolver.ClearTable(inTableId);
        }

        #endregion // Tables
    }
}