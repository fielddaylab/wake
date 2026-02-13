/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    ILeafPlugin.cs
 * Purpose: Plugin for operating leaf nodes.
 */

using System.Collections;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace Leaf
{
    public interface ILeafPlugin : ILeafVariableAccess
    {
        IMethodCache MethodCache { get; }
        bool TryLookupLine(StringHash32 inLineCode, LeafNode inLocalNode, out string outLine);
        bool TryLookupObject(StringHash32 inObjectId, LeafThreadState inThreadState, out object outObject);

        LeafRuntimeConfiguration Configuration { get; }

        int RandomInt(int inMin, int inMaxExclusive);
        float RandomFloat(float inMin, float inMax);
    }

    public interface ILeafPlugin<TNode> : ILeafPlugin
        where TNode : LeafNode
    {
        void OnNodeEnter(TNode inNode, LeafThreadState<TNode> inThreadState);
        void OnNodeExit(TNode inNode, LeafThreadState<TNode> inThreadState);

        void OnEnd(LeafThreadState<TNode> inThreadState);

        bool TryLookupNode(StringHash32 inNodeId, TNode inLocalNode, out TNode outNode);

        IEnumerator RunLine(LeafThreadState<TNode> inThreadState, LeafLineInfo inLine);
        IEnumerator ShowOptions(LeafThreadState<TNode> inThreadState, LeafChoice inChoice);
        
        LeafThreadState<TNode> Fork(LeafThreadState<TNode> inParentThreadState, TNode inForkNode);
    }
}