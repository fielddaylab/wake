/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    29 Jan 2022
 * 
 * File:    LeafEvaluationContext.cs
 * Purpose: Evaluation context for certain Leaf methods.
 */

using BeauUtil;
using BeauUtil.Variants;

namespace Leaf.Runtime {
    
    /// <summary>
    /// Evaluation context for leaf utilities.
    /// </summary>
    public struct LeafEvalContext
    {
        public readonly ILeafPlugin Plugin;
        public readonly LeafThreadState Thread;
        public readonly IVariantResolver Resolver;
        public readonly object ContextObject;

        private LeafEvalContext(ILeafPlugin inPlugin, IVariantResolver inResolver, LeafThreadState inThread, object inContext)
        {
            Plugin = inPlugin;
            Resolver = inResolver;
            Thread = inThread;
            ContextObject = inContext;
        }

        public IMethodCache MethodCache { get { return Plugin?.MethodCache; } }
        public IVariantTable Table { get { return (ContextObject as IVariantTable) ?? Thread?.Locals; } }

        static public LeafEvalContext FromPlugin(ILeafPlugin inPlugin)
        {
            return new LeafEvalContext(
                inPlugin,
                inPlugin.Resolver,
                null,
                inPlugin
            );
        }

        static public LeafEvalContext FromPlugin(ILeafPlugin inPlugin, LeafThreadState inThread)
        {
            return new LeafEvalContext(
                inPlugin,
                inThread.Resolver,
                inThread,
                (object) inThread.Actor ?? inThread
            );
        }

        static public LeafEvalContext FromThreadHandle(LeafThreadHandle inThreadHandle)
        {
            return FromThread(inThreadHandle.GetThread());
        }

        static public LeafEvalContext FromThread(LeafThreadState inThread)
        {
            return new LeafEvalContext(
                inThread.Plugin,
                inThread.Resolver,
                inThread,
                (object) inThread.Actor ?? inThread
            );
        }

        static public LeafEvalContext FromResolver(ILeafPlugin inPlugin, IVariantResolver inResolver, object inContext = null)
        {
            return new LeafEvalContext(
                inPlugin,
                inResolver,
                inContext as LeafThreadState,
                inContext ?? inResolver
            );
        }

        static public LeafEvalContext FromObject(object inObject, ILeafPlugin inDefaultPlugin = null)
        {
            if (inObject is LeafEvalContext)
                return (LeafEvalContext) inObject;

            if (inObject is LeafThreadHandle) {
                return FromThreadHandle((LeafThreadHandle) inObject);
            }

            LeafThreadState thread = inObject as LeafThreadState;
            if (thread != null)
                return FromThread(thread);
            
            ILeafPlugin plugin = inObject as ILeafPlugin;
            if (plugin != null)
                return FromPlugin(plugin);

            return new LeafEvalContext(inDefaultPlugin, inDefaultPlugin?.Resolver, null, inObject);
        }
    }
}