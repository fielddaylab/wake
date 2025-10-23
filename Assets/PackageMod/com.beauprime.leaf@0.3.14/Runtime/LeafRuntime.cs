/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafRuntime.cs
 * Purpose: Execution environment for LeafThreadState objects.
 */

using System;
using System.Collections;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace Leaf.Runtime
{
    /// <summary>
    /// Runtime environment for LeafThreadState.
    /// </summary>
    static public class LeafRuntime
    {
        /// <summary>
        /// Evaluates the given node, using the provided thread state.
        /// </summary>
        static public IEnumerator Execute<TNode>(LeafThreadState<TNode> ioThreadState, TNode inNode)
            where TNode : LeafNode
        {
            if (inNode == null)
                return null;
            
            ioThreadState.PushNode(inNode);
            return Execute(ioThreadState);
        }

        /// <summary>
        /// Executes the given thread.
        /// </summary>
        static public IEnumerator Execute<TNode>(LeafThreadState<TNode> ioThreadState)
            where TNode : LeafNode
        {
            return ioThreadState.GetExecutor();
        }

        internal sealed class Executor<TNode> : IEnumerator, IDisposable
            where TNode : LeafNode
        {
            public const int State_Default = 0;
            public const int State_Choose = 1;
            public const int State_Join = 2;
            public const int State_Interrupt = 3;
            public const int State_InterruptYielded = 4;
            public const int State_Done = -1; 

            public readonly ILeafPlugin<TNode> Plugin;
            public readonly LeafThreadState<TNode> Thread;
            public readonly LeafEvalContext EvalContext;
            public IEnumerator Wait;
            public IEnumerator InterruptWait;
            public int State;
            public LeafThreadState.RegisterState Registers;

            public Executor(ILeafPlugin<TNode> inPlugin, LeafThreadState<TNode> inThreadState)
            {
                Plugin = inPlugin;
                Thread = inThreadState;
                EvalContext = LeafEvalContext.FromPlugin(inPlugin, inThreadState);
                State = State_Done;
            }

            public object Current
            {
                get
                {
                    if (State == State_Interrupt)
                        State = State_InterruptYielded;
                    return InterruptWait ?? Wait;
                }
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (State == State_Done)
                {
                    return false;
                }

                TNode node;
                uint pc;
                LeafInstructionBlock block;
                LeafOpcode op;
                string line;
                MethodCall method;
                LeafChoice choice;

                if (State == State_Interrupt)
                {
                    return true;
                }

                if (State == State_InterruptYielded)
                {
                    InterruptWait = null;
                    State = State_Default;
                    if (Wait != null)
                    {
                        return true;
                    }
                }

                Wait = null;

                switch(State)
                {
                    case State_Choose:
                        {
                            Registers.B0_Variant = Thread.GetChosenOption();
                            Thread.ResetOptions();
                            Thread.PushValue(Registers.B0_Variant);
                            State = State_Default;
                            break;
                        }
                    case State_Join:
                        {
                            // TODO: Better check??
                            if (Thread.HasChildren())
                            {
                                return true;
                            }

                            State = State_Default;
                            break;
                        }
                }
                State = State_Default;

                while(Thread.HasNodes())
                {
                    // if an interrupt has been injected...
                    if (State == State_Interrupt)
                    {
                        return true;
                    }

                    Thread.ReadState(out node, out pc);
                    block = node.Package().m_Instructions;

                    if (pc >= node.m_InstructionOffset + node.m_InstructionCount)
                    {
                        Thread.PopNode();
                        continue;
                    }

                    op = LeafInstruction.ReadOpcode(block.InstructionStream, ref pc);

                    switch (op)
                    {
                        // TEXT

                        case LeafOpcode.RunLine:
                            {
                                Registers.B1_Identifier  = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                if (LeafUtils.TryLookupLine(Plugin, Registers.B1_Identifier, node, out line))
                                {
                                    Wait = Plugin.RunLine(Thread, new LeafLineInfo(Registers.B1_Identifier, line, node.Package().GetLineCustomName(Registers.B1_Identifier)));
                                    if (Wait != null)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    var lookupLineErrorHandler = Plugin.Configuration?.OnLineLookupError;
                                    if (lookupLineErrorHandler != null)
                                    {
                                        lookupLineErrorHandler(EvalContext, Registers.B1_Identifier, node.Id());
                                    }
                                    else
                                    {
                                        Log.Error("[LeafRuntime] Could not locate line '{0}' from node '{1}'", Registers.B1_Identifier, node.Id());
                                    }
                                }
                                break;
                            }

                        // EXPRESSIONS

                        case LeafOpcode.EvaluateSingleExpression:
                            {
                                Registers.B0_Offset = LeafInstruction.ReadUInt32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = EvaluateValueExpression(EvalContext, ref block.ExpressionTable[Registers.B0_Offset], block.StringTable);
                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.EvaluateExpressionsAnd:
                            {
                                Registers.B0_Offset = LeafInstruction.ReadUInt32(block.InstructionStream, ref pc);
                                Registers.B0_Count = LeafInstruction.ReadUInt16(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                bool bResult = true;
                                for(ushort i = 0; i < Registers.B0_Count; i++)
                                {
                                    if (!EvaluateLogicalExpression(EvalContext, ref block.ExpressionTable[Registers.B0_Offset + i], block.StringTable))
                                    {
                                        bResult = false;
                                        break;
                                    }
                                }

                                Thread.PushValue(bResult);
                                break;
                            }

                        case LeafOpcode.EvaluateExpressionsOr:
                            {
                                Registers.B0_Offset = LeafInstruction.ReadUInt32(block.InstructionStream, ref pc);
                                Registers.B0_Count = LeafInstruction.ReadUInt16(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                bool bResult = false;
                                for(ushort i = 0; i < Registers.B0_Count; i++)
                                {
                                    if (EvaluateLogicalExpression(EvalContext, ref block.ExpressionTable[Registers.B0_Offset + i], block.StringTable))
                                    {
                                        bResult = true;
                                        break;
                                    }
                                }

                                Thread.PushValue(bResult);
                                break;
                            }

                        case LeafOpcode.EvaluateExpressionsGroup:
                            {
                                Thread.WriteProgramCounter(pc);

                                // TODO: Implement
                                break;
                            }
                        
                        // INVOCATIONS

                        case LeafOpcode.Invoke_Unoptimized:
                            {
                                method.Id = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                method.Args = LeafInstruction.ReadStringTableString(block.InstructionStream, ref pc, block.StringTable);
                                Thread.WriteProgramCounter(pc);

                                Wait = Invoke(EvalContext, method, null);
                                if (Wait != null)
                                {
                                    return true;
                                }
                                break;
                            }

                        case LeafOpcode.InvokeWithReturn_Unoptimized:
                            {
                                method.Id = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                method.Args = LeafInstruction.ReadStringTableString(block.InstructionStream, ref pc, block.StringTable);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = InvokeWithReturn(EvalContext, method, null);
                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.InvokeWithTarget_Unoptimized:
                            {
                                method.Id = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                method.Args = LeafInstruction.ReadStringTableString(block.InstructionStream, ref pc, block.StringTable);
                                Thread.WriteProgramCounter(pc);

                                Registers.B1_Identifier = Thread.PopValue().AsStringHash();
                                object target;

                                if (!LeafUtils.TryLookupObject(Plugin, Registers.B1_Identifier, Thread, out target))
                                {
                                    var lookupObjectErrorHandler = Plugin.Configuration?.OnObjectLookupError;
                                    if (lookupObjectErrorHandler != null)
                                    {
                                        lookupObjectErrorHandler(EvalContext, Registers.B1_Identifier, node.Id());
                                    }
                                    else
                                    {
                                        Log.Warn("[LeafRuntime] Could not locate target {0} from node '{1}'", Registers.B1_Identifier, node.Id());
                                    }
                                    break;
                                }

                                Wait = Invoke(EvalContext, method, target);
                                if (Wait != null)
                                {
                                    return true;
                                }
                                break;
                            }
                        
                        // STACK

                        case LeafOpcode.PushValue:
                            {
                                Registers.B0_Variant = LeafInstruction.ReadVariant(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.PopValue:
                            {
                                Thread.WriteProgramCounter(pc);

                                Thread.PopValue();
                                break;
                            }

                        case LeafOpcode.DuplicateValue:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PeekValue();
                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        // MEMORY

                        case LeafOpcode.LoadTableValue:
                            {
                                Registers.B1_TableKey = LeafInstruction.ReadTableKeyPair(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.GetVariable(Registers.B1_TableKey, Thread); 
                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.StoreTableValue:
                            {
                                Registers.B1_TableKey = LeafInstruction.ReadTableKeyPair(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue(); 
                                Thread.SetVariable(Registers.B1_TableKey, Registers.B0_Variant, Thread);
                                break;
                            }

                        case LeafOpcode.IncrementTableValue:
                            {
                                Registers.B1_TableKey = LeafInstruction.ReadTableKeyPair(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Thread.IncrementVariable(Registers.B1_TableKey, 1, Thread);
                                break;
                            }

                        case LeafOpcode.DecrementTableValue:
                            {
                                Registers.B1_TableKey = LeafInstruction.ReadTableKeyPair(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Thread.IncrementVariable(Registers.B1_TableKey, -1, Thread);
                                break;
                            }

                        // ARITHMETIC

                        case LeafOpcode.Add:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B0_Variant + Registers.B2_Variant;
                                Thread.PushValue(Registers.B3_Variant);

                                break;
                            }

                        case LeafOpcode.Subtract:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant - Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);

                                break;
                            }

                        case LeafOpcode.Multiply:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B0_Variant * Registers.B2_Variant;
                                Thread.PushValue(Registers.B3_Variant);

                                break;
                            }

                        case LeafOpcode.Divide:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant / Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);

                                break;
                            }

                        // LOGICAL OPERATORS

                        case LeafOpcode.Not:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = !Registers.B0_Variant.AsBool();
                                Thread.PushValue(Registers.B2_Variant);
                                break;
                            }

                        case LeafOpcode.CastToBool:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue().AsBool();
                                Thread.PushValue(Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.LessThan:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant < Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }

                        case LeafOpcode.LessThanOrEqualTo:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant <= Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }

                        case LeafOpcode.EqualTo:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant == Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }

                        case LeafOpcode.NotEqualTo:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant != Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }

                        case LeafOpcode.GreaterThanOrEqualTo:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant >= Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }

                        case LeafOpcode.GreaterThan:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Registers.B2_Variant = Thread.PopValue();
                                Registers.B3_Variant = Registers.B2_Variant > Registers.B0_Variant;
                                Thread.PushValue(Registers.B3_Variant);
                                break;
                            }
                        
                        // JUMPS

                        case LeafOpcode.Jump:
                            {
                                Registers.B0_JumpShort = LeafInstruction.ReadInt16(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Thread.JumpRelative(Registers.B0_JumpShort);
                                break;
                            }

                        case LeafOpcode.JumpIfFalse:
                            {
                                Registers.B0_JumpShort = LeafInstruction.ReadInt16(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                if (!Thread.PopValue().AsBool())
                                {
                                    Thread.JumpRelative(Registers.B0_JumpShort);
                                }
                                break;
                            }

                        case LeafOpcode.JumpIndirect:
                            {
                                Thread.WriteProgramCounter(pc);
                                
                                Registers.B0_JumpLong = Thread.PopValue().AsInt();
                                Thread.JumpRelative(Registers.B0_JumpLong);
                                break;
                            }

                        // FLOW CONTROL

                        case LeafOpcode.GotoNode:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                TryGotoNode(Plugin, Thread, node, Registers.B1_Identifier);
                                break;
                            }

                        case LeafOpcode.GotoNodeIndirect:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B1_Identifier = Thread.PopValue().AsStringHash();
                                TryGotoNode(Plugin, Thread, node, Registers.B1_Identifier);
                                break;
                            }

                        case LeafOpcode.BranchNode:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                TryBranchNode(Plugin, Thread, node, Registers.B1_Identifier);
                                break;
                            }

                        case LeafOpcode.BranchNodeIndirect:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B1_Identifier = Thread.PopValue().AsStringHash();
                                TryBranchNode(Plugin, Thread, node, Registers.B1_Identifier);
                                break;
                            }

                        case LeafOpcode.ReturnFromNode:
                            {
                                Thread.PopNode();
                                break;
                            }

                        case LeafOpcode.Stop:
                            {
                                Thread.ClearNodes();
                                break;
                            }

                        case LeafOpcode.Loop:
                            {
                                Thread.ResetProgramCounter();
                                break;
                            }

                        case LeafOpcode.Yield:
                            {
                                Thread.WriteProgramCounter(pc);
                                return true;
                            }

                        // FORKING

                        case LeafOpcode.ForkNode:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                TryForkNode(Plugin, Thread, node, Registers.B1_Identifier, true);
                                break;
                            }

                        case LeafOpcode.ForkNodeIndirect:
                            {
                                Thread.WriteProgramCounter(pc);
                                
                                Registers.B1_Identifier = Thread.PopValue().AsStringHash();
                                TryForkNode(Plugin, Thread, node, Registers.B1_Identifier, true);
                                break;
                            }

                        case LeafOpcode.ForkNodeUntracked:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                TryForkNode(Plugin, Thread, node, Registers.B1_Identifier, false);
                                break;
                            }

                        case LeafOpcode.ForkNodeIndirectUntracked:
                            {
                                Thread.WriteProgramCounter(pc);

                                Registers.B1_Identifier = Thread.PopValue().AsStringHash();
                                TryForkNode(Plugin, Thread, node, Registers.B1_Identifier, false);
                                break;
                            }

                        case LeafOpcode.JoinForks:
                            {
                                Thread.WriteProgramCounter(pc);

                                if (Thread.HasChildren())
                                {
                                    State = State_Join;
                                    return true;
                                }
                                break;
                            }

                        // CHOICES

                        case LeafOpcode.AddChoiceOption:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Registers.B0_Byte = LeafInstruction.ReadByte(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                if (Thread.PopValue().AsBool())
                                {
                                    Registers.B0_Byte |= (byte) LeafChoice.OptionFlags.IsAvailable;
                                }
                                Registers.B2_Variant = Thread.PopValue();

                                Thread.AddOption(Registers.B2_Variant, Registers.B1_Identifier, (LeafChoice.OptionFlags) Registers.B0_Byte);
                                break;
                            }

                        case LeafOpcode.AddChoiceAnswer:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Thread.AddOptionAnswer(Registers.B1_Identifier, Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.AddChoiceData:
                            {
                                Registers.B1_Identifier = LeafInstruction.ReadStringHash32(block.InstructionStream, ref pc);
                                Thread.WriteProgramCounter(pc);

                                Registers.B0_Variant = Thread.PopValue();
                                Thread.AddOptionData(Registers.B1_Identifier, Registers.B0_Variant);
                                break;
                            }

                        case LeafOpcode.ShowChoices:
                            {
                                Thread.WriteProgramCounter(pc);

                                choice = Thread.OfferOptions();
                                if (choice.AvailableCount > 0)
                                {
                                    Wait = Plugin.ShowOptions(Thread, choice);
                                    if (Wait != null)
                                    {
                                        State = State_Choose;
                                        return true;
                                    }

                                    Registers.B0_Variant = choice.ChosenTarget();
                                    choice.Reset();
                                    Thread.PushValue(Registers.B0_Variant);
                                }
                                else
                                {
                                    choice.Reset();
                                    Thread.PushValue(Variant.Null);
                                }
                                break;
                            }
                    
                        case LeafOpcode.NoOp:
                            {
                                break;
                            }

                        default:
                            {
                                throw new InvalidOperationException("Unrecognized opcode " + op);
                            }
                    }
                }

                Plugin.OnEnd(Thread);
                return false;
            }

            public void Cleanup()
            {
                State = State_Done;

                (Wait as IDisposable)?.Dispose();
                Wait = null;

                (InterruptWait as IDisposable)?.Dispose();
                InterruptWait = null;
                
                Registers = default;
            }

            public void Reset()
            {
                State = State_Default;
                Wait = null;
                InterruptWait = null;
            }
        
            public void Interrupt()
            {
                State = State_Interrupt;
                InterruptWait = null;
            }

            public void Interrupt(IEnumerator inWait)
            {
                State = State_Interrupt;
                Assert.NotNull(inWait, "Cannot interrupt with null IEnumerator");
                InterruptWait = inWait;
            }
        }

        #region Small Operations
    
        /// <summary>
        /// Attempts to switch the current thread to the given node.
        /// </summary>
        static public void TryGotoNode<TNode>(ILeafPlugin<TNode> inPlugin, LeafThreadState<TNode> ioThreadState, TNode inLocalNode, StringHash32 inNodeId)
            where TNode : LeafNode
        {
            if (inNodeId.IsEmpty)
            {
                ioThreadState.GotoNode(null);
                return;
            }

            TNode targetNode;
            if (LeafUtils.TryLookupNode(inPlugin, inNodeId, inLocalNode, out targetNode))
            {
                ioThreadState.GotoNode(targetNode);
            }
            else
            {
                var lookupNodeErrorHandler = inPlugin.Configuration?.OnNodeLookupError;
                if (lookupNodeErrorHandler != null)
                {
                    lookupNodeErrorHandler(LeafEvalContext.FromThread(ioThreadState), inNodeId, inLocalNode?.Id() ?? StringHash32.Null);
                }
                else
                {
                    Log.Error("[LeafRuntime] Could not go to node '{0}' from '{1}' - node not found",
                        inNodeId, inLocalNode.Id());
                }
            }
        }

        /// <summary>
        /// Attempts to branch the current thread to the given node.
        /// Once the given node is finished, execution will resume at the previously loaded node.
        /// </summary>
        static public void TryBranchNode<TNode>(ILeafPlugin<TNode> inPlugin, LeafThreadState<TNode> ioThreadState, TNode inLocalNode, StringHash32 inNodeId)
            where TNode : LeafNode
        {
            if (inNodeId.IsEmpty)
            {
                return;
            }

            TNode targetNode;
            if (LeafUtils.TryLookupNode(inPlugin, inNodeId, inLocalNode, out targetNode))
            {
                ioThreadState.PushNode(targetNode);
            }
            else
            {
                var lookupNodeErrorHandler = inPlugin.Configuration?.OnNodeLookupError;
                if (lookupNodeErrorHandler != null)
                {
                    lookupNodeErrorHandler(LeafEvalContext.FromThread(ioThreadState), inNodeId, inLocalNode?.Id() ?? StringHash32.Null);
                }
                else
                {
                    Log.Error("[LeafRuntime] Could not branch to node '{0}' from '{1}' - node not found",
                        inNodeId, inLocalNode.Id());
                }
            }
        }

        /// <summary>
        /// Attempts to fork a thread for the given node.
        /// </summary>
        static public void TryForkNode<TNode>(ILeafPlugin<TNode> inPlugin, LeafThreadState<TNode> ioThreadState, TNode inLocalNode, StringHash32 inNodeId, bool inbTrack)
            where TNode : LeafNode
        {
            if (inNodeId.IsEmpty)
            {
                return;
            }

            TNode targetNode;
            if (LeafUtils.TryLookupNode(inPlugin, inNodeId, inLocalNode, out targetNode))
            {
                var newThread = inPlugin.Fork(ioThreadState, targetNode);
                if (inbTrack && newThread != null)
                {
                    ioThreadState.AddChild(newThread);
                }
            }
            else
            {
                var lookupNodeErrorHandler = inPlugin.Configuration?.OnNodeLookupError;
                if (lookupNodeErrorHandler != null)
                {
                    lookupNodeErrorHandler(LeafEvalContext.FromThread(ioThreadState), inNodeId, inLocalNode?.Id() ?? StringHash32.Null);
                }
                else
                {
                    Log.Error("[LeafRuntime] Could not branch to node '{0}' from '{1}' - node not found",
                        inNodeId, inLocalNode.Id());
                }
            }
        }

        #endregion // Small Operations

        #region Invocation

        /// <summary>
        /// Invokes the given method call from the given thread.
        /// </summary>
        static public IEnumerator Invoke(LeafEvalContext inContext, MethodCall inInvocation, object inTarget)
        {
            IMethodCache cache = inContext.MethodCache;
            if (cache == null)
                throw new InvalidOperationException("ILeafPlugin.MethodCache is not specified for plugin");
            
            bool bSuccess;
            NonBoxedValue result;
            if (inTarget == null)
            {
                bSuccess = cache.TryStaticInvoke(inInvocation.Id, inInvocation.Args, inContext, out result);
            }
            else
            {
                bSuccess = cache.TryInvoke(inTarget, inInvocation.Id, inInvocation.Args, inContext, out result);
            }

            if (!bSuccess)
            {
                var methodInvokeErrorHandler = inContext.Plugin.Configuration?.OnMethodCallError;
                if (methodInvokeErrorHandler != null)
                {
                    methodInvokeErrorHandler.Invoke(inContext, inInvocation, inTarget);
                }
                else
                {
                    Log.Error("[LeafRuntime] Unable to execute method '{0}'", inInvocation);
                }
            }

            return result.Type == NonBoxedValue.ValueType.Object ? result.AsObject() as IEnumerator : null;
        }

        /// <summary>
        /// Invokes the given method call from the given thread.
        /// </summary>
        static public Variant InvokeWithReturn(LeafEvalContext inContext, MethodCall inInvocation, object inTarget)
        {
            IMethodCache cache = inContext.MethodCache;
            if (cache == null)
                throw new InvalidOperationException("ILeafPlugin.MethodCache is not specified for plugin");
            
            bool bSuccess;
            NonBoxedValue result;
            if (inTarget == null)
            {
                bSuccess = cache.TryStaticInvoke(inInvocation.Id, inInvocation.Args, inContext, out result);
            }
            else
            {
                bSuccess = cache.TryInvoke(inTarget, inInvocation.Id, inInvocation.Args, inContext, out result);
            }

            if (!bSuccess)
            {
                var methodInvokeErrorHandler = inContext.Plugin.Configuration?.OnMethodCallError;
                if (methodInvokeErrorHandler != null)
                {
                    methodInvokeErrorHandler.Invoke(inContext, inInvocation, inTarget);
                }
                else
                {
                    Log.Error("[LeafRuntime] Unable to execute method '{0}'", inInvocation);
                }
                return default(Variant);
            }
            else
            {
                Variant variantResult;
                bSuccess = Variant.TryConvertFrom(result, out variantResult);
                if (!bSuccess)
                {
                    Log.Error("[LeafRuntime] Unable to convert result of method call '{0}' (type '{1}') to variant", inInvocation, result.GetType().Name);
                    return default(Variant);
                }

                return variantResult;
            }
        }

        #endregion // Invocation
    
        #region Expressions

        static internal Variant EvaluateValueExpression(LeafEvalContext inContext, ref LeafExpression inExpression, string[] inStringTable)
        {
            if ((inExpression.Flags & LeafExpression.TypeFlags.IsLogical) != 0)
            {
                return EvaluateLogicalExpression(inContext, ref inExpression, inStringTable);
            }

            Variant value;
            TryEvaluateOperand(inContext, inExpression.LeftType, ref inExpression.Left, inStringTable, out value);
            return value;
        }

        static internal bool TryEvaluateOperand(LeafEvalContext inContext, LeafExpression.OperandType inType, ref LeafExpression.OperandData inOperandData, string[] inStringTable, out Variant outValue)
        {
            switch(inType)
            {
                case LeafExpression.OperandType.Value:
                    {
                        outValue = inOperandData.Value;
                        return true;
                    }
                case LeafExpression.OperandType.Read:
                    {
                        return inContext.Resolver.TryResolve(inContext, inOperandData.TableKey, out outValue);
                    }
                case LeafExpression.OperandType.Method:
                    {
                        MethodCall call;
                        call.Id = inOperandData.MethodId;

                        uint stringIdx = inOperandData.MethodArgsIndex;
                        if (stringIdx == LeafInstruction.EmptyIndex)
                        {
                            call.Args = null;
                        }
                        else
                        {
                            call.Args = inStringTable[stringIdx];
                        }

                        outValue = InvokeWithReturn(inContext, call, null);
                        return true;
                    }
                default:
                    {
                        throw new InvalidOperationException("Unknown expression operand type " + inType);
                    }
            }
        }

        static internal bool EvaluateLogicalExpression(LeafEvalContext inContext, ref LeafExpression inExpression, string[] inStringTable)
        {
            bool leftExists;
            Variant left, right;
            leftExists = TryEvaluateOperand(inContext, inExpression.LeftType, ref inExpression.Left, inStringTable, out left);

            switch(inExpression.Operator)
            {
                case VariantCompareOperator.Exists:
                    return leftExists;
                case VariantCompareOperator.DoesNotExist:
                    return !leftExists;
                case VariantCompareOperator.True:
                    return left.AsBool();
                case VariantCompareOperator.False:
                    return !left.AsBool();
            }

            TryEvaluateOperand(inContext, inExpression.RightType, ref inExpression.Right, inStringTable, out right);

            switch(inExpression.Operator)
            {
                case VariantCompareOperator.LessThan:
                    return left < right;
                case VariantCompareOperator.LessThanOrEqualTo:
                    return left <= right;
                case VariantCompareOperator.EqualTo:
                    return left == right;
                case VariantCompareOperator.NotEqualTo:
                    return left != right;
                case VariantCompareOperator.GreaterThanOrEqualTo:
                    return left >= right;
                case VariantCompareOperator.GreaterThan:
                    return left > right;

                default:
                    throw new InvalidOperationException("Unknown expression comparison operator" + inExpression.Operator);
            }
        }

        #endregion // Expressions
    
        #region Scan

        /// <summary>
        /// Predicts if a choice is going to be the next blocking leaf operation for the given thread.
        /// </summary>
        static public bool PredictChoice(LeafThreadState inThread)
        {
            return PredictNextBlockingOperation(inThread, LeafOpcode.ShowChoices);
        }

        /// <summary>
        /// Predicts the next line code the given thread will display before the next blocking operation.
        /// </summary>
        static public StringHash32 PredictLine(LeafThreadState inThread)
        {
            return PredictNextLine(inThread);
        }

        /// <summary>
        /// Predicts if the given thread is going to transition to another node before any more blocking operations.
        /// </summary>
        static public bool PredictTransition(LeafThreadState inThread)
        {
            return PredictNodeTransition(inThread);
        }

        /// <summary>
        /// Predicts if the given thread is going to end before any more blocking operations.
        /// </summary>
        static public bool PredictEnd(LeafThreadState inThread)
        {
            return PredictNextEnd(inThread);
        }

        static private bool PredictNextBlockingOperation(LeafThreadState inThread, LeafOpcode inOperation)
        {
            LeafNode node;
            uint pc;
            uint next;
            int stackSize = inThread.InternalStackSize();
            int stackOffset = 0;
            uint end;
            byte[] stream;

            LeafOpcode op;
            while(stackOffset < stackSize)
            {
                inThread.InternalReadState(stackOffset, out node, out pc);
                end = node.m_InstructionOffset + node.m_InstructionCount;
                stream = node.Package().m_Instructions.InstructionStream;
                while(pc < end)
                {
                    op = LeafInstruction.ReadOpcode(stream, ref pc);
                    if (op == inOperation)
                        return true;

                    next = pc + OpSize(op) - 1;

                    if (ShouldInterruptScan(op))
                        return false;

                    switch(op)
                    {
                        case LeafOpcode.Jump:
                            {
                                short jmp = LeafInstruction.ReadInt16(stream, ref pc);
                                if (jmp < 0)
                                    return false;

                                next = pc + (uint) jmp;
                                break;
                            }

                        case LeafOpcode.ReturnFromNode:
                            {
                                next = end;
                                break;
                            }
                    }

                    pc = next;
                }

                stackOffset++;
            }

            return false;
        }

        static private StringHash32 PredictNextLine(LeafThreadState inThread)
        {
            LeafNode node;
            uint pc;
            uint next;
            int stackSize = inThread.InternalStackSize();
            int stackOffset = 0;
            uint end;
            byte[] stream;

            LeafOpcode op;
            while(stackOffset < stackSize)
            {
                inThread.InternalReadState(stackOffset, out node, out pc);
                end = node.m_InstructionOffset + node.m_InstructionCount;
                stream = node.Package().m_Instructions.InstructionStream;
                while(pc < end)
                {
                    op = LeafInstruction.ReadOpcode(stream, ref pc);
                    if (op == LeafOpcode.RunLine)
                    {
                        return LeafInstruction.ReadStringHash32(stream, ref pc);
                    }

                    next = pc + OpSize(op) - 1;

                    if (ShouldInterruptScan(op))
                    {
                        return null;
                    }

                    switch(op)
                    {
                        case LeafOpcode.Jump:
                            {
                                short jmp = LeafInstruction.ReadInt16(stream, ref pc);
                                if (jmp < 0)
                                {
                                    return null;
                                }

                                next = pc + (uint) jmp;
                                break;
                            }

                        case LeafOpcode.ReturnFromNode:
                            {
                                next = end;
                                break;
                            }
                    }

                    pc = next;
                }

                stackOffset++;
            }

            return null;
        }

        static private bool PredictNextEnd(LeafThreadState inThread)
        {
            LeafNode node;
            uint pc;
            uint next;
            int stackSize = inThread.InternalStackSize();
            int stackOffset = 0;
            uint end;
            byte[] stream;

            LeafOpcode op;
            while(stackOffset < stackSize)
            {
                inThread.InternalReadState(stackOffset, out node, out pc);
                end = node.m_InstructionOffset + node.m_InstructionCount;
                stream = node.Package().m_Instructions.InstructionStream;
                while(pc < end)
                {
                    op = LeafInstruction.ReadOpcode(stream, ref pc);
                    if (op == LeafOpcode.Stop)
                        return true;

                    next = pc + OpSize(op) - 1;

                    if (ShouldInterruptScan(op))
                        return false;

                    switch(op)
                    {
                        case LeafOpcode.Jump:
                            {
                                short jmp = LeafInstruction.ReadInt16(stream, ref pc);
                                if (jmp < 0)
                                    return false;

                                next = pc + (uint) jmp;
                                break;
                            }

                        case LeafOpcode.ReturnFromNode:
                            {
                                next = end;
                                break;
                            }
                    }

                    pc = next;
                }

                stackOffset++;
            }

            return true;
        }

        static private bool PredictNodeTransition(LeafThreadState inThread)
        {
            LeafNode node;
            uint pc;
            uint next;
            uint end;
            byte[] stream;

            LeafOpcode op;
            inThread.InternalReadState(0, out node, out pc);
            end = node.m_InstructionOffset + node.m_InstructionCount;
            stream = node.Package().m_Instructions.InstructionStream;
            while(pc < end)
            {
                op = LeafInstruction.ReadOpcode(stream, ref pc);
                next = pc + OpSize(op) - 1;

                switch(op)
                {
                    case LeafOpcode.Jump:
                        {
                            short jmp = LeafInstruction.ReadInt16(stream, ref pc);
                            if (jmp < 0)
                                return false;

                            next = pc + (uint) jmp;
                            break;
                        }

                    case LeafOpcode.ReturnFromNode:
                    case LeafOpcode.GotoNode:
                    case LeafOpcode.GotoNodeIndirect:
                    case LeafOpcode.BranchNode:
                    case LeafOpcode.BranchNodeIndirect:
                    case LeafOpcode.Stop:
                        {
                            return true;
                        }
                }

                if (ShouldInterruptScan(op))
                    return false;

                pc = next;
            }

            return true;
        }

        static internal bool ShouldInterruptScan(LeafOpcode inOpcode)
        {
            switch(inOpcode)
            {
                case LeafOpcode.RunLine:
                case LeafOpcode.ShowChoices:
                case LeafOpcode.Invoke:
                case LeafOpcode.Invoke_Unoptimized:
                case LeafOpcode.InvokeWithTarget:
                case LeafOpcode.InvokeWithTarget_Unoptimized:
                case LeafOpcode.GotoNode:
                case LeafOpcode.GotoNodeIndirect:
                case LeafOpcode.BranchNode:
                case LeafOpcode.BranchNodeIndirect:
                case LeafOpcode.JoinForks:
                case LeafOpcode.Stop:
                case LeafOpcode.Loop:
                case LeafOpcode.JumpIfFalse:
                case LeafOpcode.JumpIndirect:
                    return true;

                default:
                    return false;
            }
        }

        static internal uint OpSize(LeafOpcode inOpcode)
        {
            switch(inOpcode)
            {
                case LeafOpcode.RunLine: return 5;

                case LeafOpcode.EvaluateSingleExpression: return 5;
                case LeafOpcode.EvaluateExpressionsAnd: return 7;
                case LeafOpcode.EvaluateExpressionsOr: return 7;
                case LeafOpcode.EvaluateExpressionsGroup: return 7;

                case LeafOpcode.Invoke_Unoptimized: return 9;
                case LeafOpcode.InvokeWithTarget_Unoptimized: return 9;
                case LeafOpcode.InvokeWithReturn_Unoptimized: return 9;

                case LeafOpcode.Invoke: return 7;
                case LeafOpcode.InvokeWithTarget: return 7;
                case LeafOpcode.InvokeWithReturn: return 7;

                case LeafOpcode.PushValue: return 6;
                case LeafOpcode.PopValue: return 1;
                case LeafOpcode.DuplicateValue: return 1;
                
                case LeafOpcode.LoadTableValue: return 9;
                case LeafOpcode.StoreTableValue: return 9;
                case LeafOpcode.IncrementTableValue: return 9;
                case LeafOpcode.DecrementTableValue: return 9;

                case LeafOpcode.Add: return 1;
                case LeafOpcode.Subtract: return 1;
                case LeafOpcode.Multiply: return 1;
                case LeafOpcode.Divide: return 1;

                case LeafOpcode.Not: return 1;
                case LeafOpcode.CastToBool: return 1;
                case LeafOpcode.LessThan: return 1;
                case LeafOpcode.LessThanOrEqualTo: return 1;
                case LeafOpcode.EqualTo: return 1;
                case LeafOpcode.NotEqualTo: return 1;
                case LeafOpcode.GreaterThanOrEqualTo: return 1;
                case LeafOpcode.GreaterThan: return 1;

                case LeafOpcode.Jump: return 3;
                case LeafOpcode.JumpIfFalse: return 3;
                case LeafOpcode.JumpIndirect: return 1;

                case LeafOpcode.GotoNode: return 5;
                case LeafOpcode.GotoNodeIndirect: return 1;
                case LeafOpcode.BranchNode: return 5;
                case LeafOpcode.BranchNodeIndirect: return 1;

                case LeafOpcode.Stop: return 1;
                case LeafOpcode.Loop: return 1;
                case LeafOpcode.Yield: return 1;
                case LeafOpcode.NoOp: return 1;

                case LeafOpcode.ForkNode: return 5;
                case LeafOpcode.ForkNodeIndirect: return 1;
                case LeafOpcode.ForkNodeUntracked: return 5;
                case LeafOpcode.ForkNodeIndirectUntracked: return 1;

                case LeafOpcode.JoinForks: return 1;

                case LeafOpcode.AddChoiceOption: return 6;
                case LeafOpcode.AddChoiceAnswer: return 5;
                case LeafOpcode.AddChoiceData: return 5;
                case LeafOpcode.ShowChoices: return 1;

                default: throw new ArgumentOutOfRangeException("inOpcode");
            }
        }

        #endregion // Scan
    }

    /// <summary>
    /// Additional runtime configuration options and error handlers.
    /// </summary>
    public sealed class LeafRuntimeConfiguration
    {
        /// <summary>
        /// Invoked when a line fails to be located.
        /// </summary>
        public LeafLookupErrorHandler OnLineLookupError;

        /// <summary>
        /// Invoked when a node fails to be located.
        /// </summary>
        public LeafLookupErrorHandler OnNodeLookupError;

        /// <summary>
        /// Invoked when an object fails to be located.
        /// </summary>
        public LeafLookupErrorHandler OnObjectLookupError;

        /// <summary>
        /// Invoked when a method fails to be executed.
        /// </summary>
        public LeafMethodErrorHandler OnMethodCallError;
    }

    /// <summary>
    /// Error handler for when a lookup fails.
    /// </summary>
    public delegate void LeafLookupErrorHandler(LeafEvalContext inContext, StringHash32 inId, StringHash32 inLocalNodeId);
    
    /// <summary>
    /// Error handler for when a method call invocation fails.
    /// </summary>
    public delegate void LeafMethodErrorHandler(LeafEvalContext inContext, MethodCall inMethodCall, object inTarget);
}