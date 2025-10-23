/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafThreadState.cs
 * Purpose: Execution stacks for evaluating leaf nodes.
 */

#if CSHARP_7_3_OR_NEWER
#define EXPANDED_REFS
#endif // CSHARP_7_3_OR_NEWER

using BeauUtil;
using BeauUtil.Variants;
using BeauUtil.Debugger;
using BeauRoutine;
using System;
using System.Collections;
using BeauUtil.Tags;
using System.Runtime.InteropServices;

namespace Leaf.Runtime
{
    /// <summary>
    /// Representation of an executing leaf thread.
    /// </summary>
    public abstract class LeafThreadState : ILeafVariableAccess
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct RegisterState
        {
            // 0-7
            [FieldOffset(0)] public Variant B0_Variant;
            [FieldOffset(0)] public byte B0_Byte;
            [FieldOffset(2)] public ushort B0_Count;
            [FieldOffset(2)] public short B0_JumpShort;
            [FieldOffset(4)] public uint B0_Offset;
            [FieldOffset(4)] public int B0_JumpLong;

            // 8-15
            [FieldOffset(8)] public TableKeyPair B1_TableKey;
            [FieldOffset(12)] public StringHash32 B1_Identifier;

            // 16-23
            [FieldOffset(16)] public Variant B2_Variant;
            
            // 24-31
            [FieldOffset(24)] public Variant B3_Variant;
        }

        private readonly RingBuffer<Variant> m_ValueStack;
        private readonly RingBuffer<LeafThreadHandle> m_Children;
        private readonly LeafChoice m_ChoiceBuffer;
        private readonly VariantTable m_Locals;
        private readonly Action m_KillAction;
        private readonly ILeafPlugin m_BasePlugin;

        internal uint m_Id;
        internal bool m_Running;

        private string m_Name;
        private float m_QueuedDelay;
        private ILeafActor m_ActorThis;

        protected Routine m_Routine;

        /// <summary>
        /// Variant resolver. Overrides the default resolver in the plugin.
        /// </summary>
        public readonly CustomVariantResolver Resolver;

        /// <summary>
        /// Tagged string. Temporary state for the current string.
        /// </summary>
        public readonly TagString TagString;

        public LeafThreadState(ILeafPlugin inPlugin)
        {
            m_ValueStack = new RingBuffer<Variant>();
            m_ChoiceBuffer = new LeafChoice();
            m_Locals = new VariantTable(LeafUtils.LocalIdentifier);
            m_Children = new RingBuffer<LeafThreadHandle>();
            m_BasePlugin = inPlugin;

            Resolver = new CustomVariantResolver();
            TagString = new TagString();

            m_KillAction = Kill;
            Resolver.SetTable(LeafUtils.LocalIdentifier, m_Locals);
        }

        /// <summary>
        /// Name of the thread.
        /// </summary>
        public string Name { get { return m_Name; } }

        /// <summary>
        /// Local variable table.
        /// </summary>
        public VariantTable Locals { get { return m_Locals; } }

        /// <summary>
        /// Returns the default "this" object for this thread.
        /// </summary>
        public ILeafActor Actor { get { return m_ActorThis; } }

        IVariantResolver ILeafVariableAccess.Resolver { get { return Resolver; } }

        internal ILeafPlugin Plugin { get { return m_BasePlugin; }}

        #region Internal State

        #region Value Stack

        /// <summary>
        /// Pushes a value onto the value stack.
        /// </summary>
        internal void PushValue(Variant inValue)
        {
            m_ValueStack.PushBack(inValue);
        }

        /// <summary>
        /// Peeks the current value on the value stack.
        /// </summary>
        internal Variant PeekValue()
        {
            return m_ValueStack.PeekBack();
        }

        /// <summary>
        /// Pops a value from the value stack.
        /// </summary>
        internal Variant PopValue()
        {
            return m_ValueStack.PopBack();
        }

        #endregion // Stack

        internal abstract void InternalReadState(int inOffset, out LeafNode outNode, out uint outProgramCounter);
        internal abstract int InternalStackSize();

        #region Choices

        /// <summary>
        /// Adds an option to the choice buffer.
        /// </summary>
        public void AddOption(Variant inTargetId, StringHash32 inLineCode, LeafChoice.OptionFlags inFlags = LeafChoice.OptionFlags.IsAvailable)
        {
            m_ChoiceBuffer.AddOption(new LeafChoice.Option(inTargetId, inLineCode, inFlags));
        }

        /// <summary>
        /// Adds an answer to the latest option to the choice buffer.
        /// </summary>
        public void AddOptionAnswer(Variant inAnswerId, Variant inTargetId)
        {
            m_ChoiceBuffer.AddAnswer(new LeafChoice.Answer(inAnswerId, inTargetId));
        }

        /// <summary>
        /// Adds data to the latest option to the choice buffer.
        /// </summary>
        public void AddOptionData(StringHash32 inDataId, Variant inDataValue)
        {
            m_ChoiceBuffer.AddData(new LeafChoice.Datum(inDataId, inDataValue));
        }

        /// <summary>
        /// Locks and returns the choice buffer.
        /// </summary>
        public LeafChoice OfferOptions()
        {
            m_ChoiceBuffer.Offer();
            return m_ChoiceBuffer;
        }

        /// <summary>
        /// Returns the choice buffer in its current state.
        /// </summary>
        public LeafChoice PeekOptions()
        {
            return m_ChoiceBuffer;
        }

        /// <summary>
        /// Number of available options.
        /// </summary>
        public int AvailableOptionCount()
        {
            return m_ChoiceBuffer.AvailableCount;
        }

        /// <summary>
        /// Number of available options.
        /// </summary>
        public int AvailableOptionCount(LeafChoice.OptionPredicate inPredicate)
        {
            return m_ChoiceBuffer.AvailableOptionCount(inPredicate);
        }

        /// <summary>
        /// Returns the identifier of the chosen option.
        /// </summary>
        public Variant GetChosenOption()
        {
            return m_ChoiceBuffer.ChosenTarget();
        }

        /// <summary>
        /// Resets the choice buffer.
        /// </summary>
        public void ResetOptions()
        {
            m_ChoiceBuffer.Reset();
        }

        #endregion // Choices

        #region Forks

        /// <summary>
        /// Returns if this thread has any children still running.
        /// </summary>
        public bool HasChildren()
        {
            while(m_Children.Count > 0)
            {
                if (m_Children.PeekBack().IsRunning())
                    return true;
                m_Children.PopBack();
            }

            return false;
        }

        /// <summary>
        /// Adds the given thread as a child of this thread.
        /// </summary>
        public void AddChild(LeafThreadState inThreadState)
        {
            if (inThreadState != null && inThreadState.m_Running)
            {
                m_Children.PushBack(inThreadState.GetHandle());
            }
        }

        /// <summary>
        /// Kills all children.
        /// </summary>
        public void KillChildren()
        {
            while(m_Children.Count > 0)
            {
                var thread = m_Children.PopBack().GetThread();
                if (thread != null)
                    thread.Kill();
            }
        }

        #endregion // Forks

        #region Handles

        public LeafThreadHandle GetHandle()
        {
            return m_Running ? new LeafThreadHandle(this, m_Id) : default(LeafThreadHandle);
        }

        /// <summary>
        /// Returns if this thread is operating with the given id.
        /// </summary>
        public bool HasId(uint inId)
        {
            return m_Running && m_Id == inId;
        }

        #endregion // Handles

        #endregion // Internal State

        #region Lifecycle

        /// <summary>
        /// Sets up the state.
        /// </summary>
        public virtual LeafThreadHandle Setup(string inName, ILeafActor inActor, VariantTable inLocals)
        {
            if (inLocals != null)
            {
                inLocals.CopyTo(m_Locals);
            }
            else
            {
                m_Locals.Clear();
            }

            if (inActor != null && inActor.Locals != null)
            {
                Resolver.SetTable(LeafUtils.ThisIdentifier, inActor.Locals);
            }

            m_Name = inName;
            m_ActorThis = inActor;
            m_Running = true;
            m_Id = (m_Id == uint.MaxValue) ? 1 : m_Id + 1;
            
            return GetHandle();
        }

        /// <summary>
        /// Attaches a routine to this thread.
        /// </summary>
        public virtual void AttachRoutine(Routine inRoutine)
        {
            m_Routine = inRoutine;
            inRoutine.OnComplete(m_KillAction);
            if (m_QueuedDelay > 0)
            {
                m_Routine.DelayBy(m_QueuedDelay);
                m_QueuedDelay = 0;
            }
        }

        #endregion // Lifecycle

        #region Updates

        /// <summary>
        /// If attached to a routine, forces this thread to tick forward.
        /// </summary>
        public void ForceTick()
        {
            m_Routine.TryManuallyUpdate(0);
        }

        /// <summary>
        /// Returns if the thread is running or paused.
        /// </summary>
        public bool IsRunning() { return m_Running; }

        /// <summary>
        /// Pauses the thread.
        /// </summary>
        public void Pause() { m_Routine.Pause(); }

        /// <summary>
        /// Resumes the thread.
        /// </summary>
        public void Resume() { m_Routine.Resume(); }

        /// <summary>
        /// Returns if the thread is paused.
        /// </summary>
        public bool IsPaused() { return m_Routine.GetPaused(); }

        /// <summary>
        /// Waits for the thread to be completed.
        /// </summary>
        public IEnumerator Wait() { return m_Routine.Wait(); }

        /// <summary>
        /// Delays execution of the thread.
        /// </summary>
        public void DelayBy(float inDelaySeconds)
        {
            if (m_Routine)
            {
                m_Routine.DelayBy(inDelaySeconds);
            }
            else
            {
                m_QueuedDelay += inDelaySeconds;
            }
        }

        /// <summary>
        /// Interrupts the current thread for a frame.
        /// </summary>
        public abstract void Interrupt();

        /// <summary>
        /// Interrupts the current thread to process the given IEnumerator.
        /// </summary>
        public abstract void Interrupt(IEnumerator inInterrupt);

        #endregion // Updates

        #region Lookups

        /// <summary>
        /// Attempts to look up an object from the given id.
        /// </summary>
        internal bool TryLookupObject(StringHash32 inId, out object outObject)
        {
            if (inId.IsEmpty)
            {
                outObject = null;
                return true;
            }

            if (inId == LeafUtils.ThisIdentifier)
            {
                outObject = Actor;
                return true;
            }

            if (inId == LeafUtils.ThreadIdentifier)
            {
                outObject = this;
                return true;
            }

            return m_BasePlugin.TryLookupObject(inId, this, out outObject);
        }

        /// <summary>
        /// Attempts to resolve the given operand.
        /// </summary>
        internal bool TryResolveOperand(VariantOperand inOperand, out Variant outValue)
        { 
            return inOperand.TryResolve(Resolver, this, out outValue, m_BasePlugin.MethodCache);
        }

        #endregion // Lookups

        #region Cleanup

        /// <summary>
        /// Kills this thread.
        /// </summary>
        public void Kill()
        {
            if (m_Running)
            {
                Reset();
            }
        }

        /// <summary>
        /// Resets all thread state.
        /// </summary>
        protected virtual void Reset()
        {
            m_Running = false;

            KillChildren();
            
            m_ValueStack.Clear();
            m_ChoiceBuffer.Reset();
            m_Children.Clear();
            m_ChoiceBuffer.Reset();
            m_Locals.Clear();
            m_Name = null;
            m_QueuedDelay = 0;
            m_ActorThis = null;
            Resolver.ClearTable(LeafUtils.ThisIdentifier);

            m_Routine.Stop();
        }

        #endregion // Cleanup
    }

    /// <summary>
    /// Leaf thread
    /// </summary>
    public class LeafThreadState<TNode> : LeafThreadState
        where TNode : LeafNode
    {
        #region Types

        private struct Frame
        {
            public TNode Node;
            public uint ProgramCounter;

            public Frame(TNode inNode)
            {
                Node = inNode;
                ProgramCounter = inNode.m_InstructionOffset;
            }
        }

        #endregion // Types
        
        private readonly RingBuffer<Frame> m_FrameStack;
        private ILeafPlugin<TNode> m_Plugin;
        private readonly LeafRuntime.Executor<TNode> m_Executor;

        private bool m_QueuedInterrupt;
        private IEnumerator m_QueuedInterruptWait;

        public LeafThreadState(ILeafPlugin<TNode> inPlugin)
            : base(inPlugin)
        {
            if (inPlugin == null)
                throw new ArgumentNullException("inPlugin");

            m_FrameStack = new RingBuffer<Frame>();
            m_Plugin = inPlugin;
            Resolver.Base = inPlugin.Resolver;

            m_Executor = new LeafRuntime.Executor<TNode>(m_Plugin, this);
        }

        #region Internal State

        internal LeafRuntime.Executor<TNode> GetExecutor()
        {
            m_Executor.Reset();
            if (m_QueuedInterrupt)
            {
                if (m_QueuedInterruptWait != null)
                {
                    m_Executor.Interrupt(m_QueuedInterruptWait);
                    m_QueuedInterruptWait = null;
                }
                else
                {
                    m_Executor.Interrupt();
                }
                m_QueuedInterrupt = false;
            }
            return m_Executor;
        }

        #region Program Counters

        internal void ReadState(out TNode outNode, out uint outProgramCounter)
        {
            Frame currentFrame = m_FrameStack[0];
            outNode = currentFrame.Node;
            outProgramCounter = currentFrame.ProgramCounter;
        }

        internal override void InternalReadState(int inOffset, out LeafNode outNode, out uint outProgramCounter)
        {
            Frame currentFrame = m_FrameStack[inOffset];
            outNode = currentFrame.Node;
            outProgramCounter = currentFrame.ProgramCounter;
        }

        internal override int InternalStackSize()
        {
            return m_FrameStack.Count;
        }

        internal void WriteProgramCounter(uint inProgramCounter)
        {
            #if EXPANDED_REFS
            ref Frame currentFrame = ref m_FrameStack[0];
            #else
            Frame currentFrame = m_FrameStack[0];
            #endif // EXPANDED_REFS

            currentFrame.ProgramCounter = inProgramCounter;

            #if !EXPANDED_REFS
            m_FrameStack[0] = currentFrame;
            #endif // !EXPANDED_REFS
        }

        internal void JumpRelative(int inJump)
        {
            m_FrameStack[0].ProgramCounter = (uint) (m_FrameStack[0].ProgramCounter + inJump);
        }

        internal void JumpAbsolute(uint inIndex)
        {
            m_FrameStack[0].ProgramCounter = inIndex;
        }

        internal void ResetProgramCounter()
        {
            m_FrameStack[0].ProgramCounter = m_FrameStack[0].Node.m_InstructionOffset;
        }

        #endregion // Program Counters

        #region Node Stack

        /// <summary>
        /// Pushes the given node onto the frame stack.
        /// </summary>
        public void PushNode(TNode inNode)
        {
            m_FrameStack.PushFront(new Frame(inNode));
            m_Plugin?.OnNodeEnter(inNode, this);
        }

        /// <summary>
        /// Peeks the node at the top of the frame stack.
        /// </summary>
        public TNode PeekNode()
        {
            return m_FrameStack.PeekFront().Node;
        }

        /// <summary>
        /// Pops the current node from the frame stack.
        /// </summary>
        public void PopNode()
        {
            TNode node = m_FrameStack.PopFront().Node;
            m_Plugin?.OnNodeExit(node, this);
        }

        /// <summary>
        /// Pops the current node from the frame stack
        /// and inserts the given node.
        /// </summary>
        public void GotoNode(TNode inNode)
        {
            if (m_FrameStack.Count > 0)
            {
                PopNode();
            }

            if (inNode != null)
            {
                PushNode(inNode);
            }
        }

        /// <summary>
        /// Returns if there are any nodes in the frame stack.
        /// </summary>
        public bool HasNodes()
        {
            return m_FrameStack.Count > 0;
        }

        /// <summary>
        /// Flushes all nodes from the frame stack.
        /// </summary>
        public void ClearNodes()
        {
            while(m_FrameStack.Count > 0)
            {
                PopNode();
            }
        }

        #endregion // Node Stack

        #endregion // Internal State

        /// <summary>
        /// Interrupts execution of the thread for the given frame.
        /// </summary>
        public override void Interrupt()
        {
            if (m_Executor.State == LeafRuntime.Executor<TNode>.State_Done)
            {
                m_QueuedInterrupt = true;
            }
            else
            {
                m_Executor.Interrupt();
            }
        }

        /// <summary>
        /// Interrupts execution of the thread to process the given wait.
        /// </summary>
        public override void Interrupt(IEnumerator inWait)
        {
            if (m_Executor.State == LeafRuntime.Executor<TNode>.State_Done)
            {
                m_QueuedInterrupt = true;
                m_QueuedInterruptWait = inWait;
            }
            else
            {
                m_Executor.Interrupt(inWait);
            }
        }

        #region Cleanup

        /// <summary>
        /// Resets thread state.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            m_Executor.Cleanup();
            ClearNodes();
        }

        #endregion // Cleanup
    }
}