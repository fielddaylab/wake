/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafCompiler.cs
 * Purpose: Compilation environment for LeafNodes.
 */

// define LEAF_USE_OLD_LINECODE_GENERATION to use the old linecode generation method
// #define LEAF_USE_OLD_LINECODE_GENERATION

#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD || DEVELOPMENT
#define PRESERVE_DEBUG_SYMBOLS
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace Leaf.Compiler
{
    /// <summary>
    /// Compiles leaf nodes into instructions.
    /// </summary>
    public sealed class LeafCompiler
    {
        #region Types

        public struct Report
        {
            public LeafCompilerErrorMask WarningMask;
            public LeafCompilerErrorMask ErrorMask;
        }

        private class ConditionalBlockLinker
        {
            private enum BlockType
            {
                Unassigned,
                If,
                While
            }

            private enum Phase
            {
                Unstarted,
                Started,
                Continued,
                FinalElse
            }

            private Phase m_Phase;
            private BlockType m_Type;
            private int m_NextPointer = -1;
            private int m_StartPointer = -1;
            private int m_ConditionalEndPointer = -1;
            private readonly List<int> m_EndPointers = new List<int>(4);

            #region If

            /// <summary>
            /// Handles an if statement.
            /// </summary>
            public void If(BlockFilePosition inPosition, StringSlice inExpression, LeafCompiler ioCompiler)
            {
                if (m_Phase != Phase.Unstarted)
                    throw new SyntaxException(inPosition, "If statement in an unexpected location");

                m_Phase = Phase.Started;
                m_Type = BlockType.If;

                EmitExpressionCheck(inPosition, inExpression, ioCompiler);
            }

            /// <summary>
            /// Handles an elseif statement
            /// </summary>
            public void ElseIf(BlockFilePosition inPosition, StringSlice inExpression, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.If)
                    throw new SyntaxException(inPosition, "elseIf while not in an if block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "ElseIf without corresponding initial if statement");
                    case Phase.FinalElse:
                        throw new SyntaxException(inPosition, "ElseIf cannot come after a final Else statement");
                    case Phase.Started:
                        m_Phase = Phase.Continued;
                        break;
                }

                PointToEnd(ioCompiler);
                Advance(ioCompiler);
                EmitExpressionCheck(inPosition, inExpression, ioCompiler);
            }

            /// <summary>
            /// Handles an else statement
            /// </summary>
            public void Else(BlockFilePosition inPosition, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.If)
                    throw new SyntaxException(inPosition, "else while not in an if block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "Else without corresponding initial if statement");
                    case Phase.FinalElse:
                        throw new SyntaxException(inPosition, "Else cannot come after a final Else statement");
                    case Phase.Started:
                    case Phase.Continued:
                        m_Phase = Phase.FinalElse;
                        break;
                }

                PointToEnd(ioCompiler);
                Advance(ioCompiler);
            }

            /// <summary>
            /// Handles an endif statement
            /// </summary>
            public void EndIf(BlockFilePosition inPosition, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.If)
                    throw new SyntaxException(inPosition, "endif while not in an if block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "EndIf without corresponding initial if statement");
                }

                m_Phase = Phase.Unstarted;
                Advance(ioCompiler);

                LinkEndPointers(ioCompiler);
            }

            #endregion // If

            #region While

            /// <summary>
            /// Handles a while statement.
            /// </summary>
            public void While(BlockFilePosition inPosition, StringSlice inExpression, LeafCompiler ioCompiler)
            {
                if (m_Phase != Phase.Unstarted)
                    throw new SyntaxException(inPosition, "while statement in an unexpected location");

                m_Phase = Phase.Started;
                m_Type = BlockType.While;
                m_StartPointer = ioCompiler.StreamLength;

                EmitExpressionCheckBlock(inPosition, inExpression, ioCompiler);
            }

            /// <summary>
            /// Handles a break statement
            /// </summary>
            public void Break(BlockFilePosition inPosition, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.While)
                    throw new SyntaxException(inPosition, "break while not in a while block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "Break without corresponding initial while statement");
                }

                PointToEnd(ioCompiler);
            }

            /// <summary>
            /// Handles a continue statement
            /// </summary>
            public void Continue(BlockFilePosition inPosition, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.While)
                    throw new SyntaxException(inPosition, "continue while not in a while block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "Continue without corresponding initial while statement");
                }

                PointToStart(ioCompiler);
            }

            /// <summary>
            /// Handles an endwhile statement
            /// </summary>
            public void EndWhile(BlockFilePosition inPosition, LeafCompiler ioCompiler)
            {
                if (m_Type != BlockType.While)
                    throw new SyntaxException(inPosition, "endwhile while not in a while block");

                switch (m_Phase)
                {
                    case Phase.Unstarted:
                        throw new SyntaxException(inPosition, "EndWhile without corresponding initial while statement");
                }

                m_Phase = Phase.Unstarted;
                PointToStart(ioCompiler);

                LinkEndPointers(ioCompiler);
            }

            #endregion // While

            private void LinkEndPointers(LeafCompiler ioCompiler)
            {
                if (m_ConditionalEndPointer >= 0)
                {
                    short jump = (short) (ioCompiler.StreamLength - (m_ConditionalEndPointer + 2));
                    LeafInstruction.OverwriteInt16(ioCompiler.m_InstructionStream, m_ConditionalEndPointer, jump);

                    m_ConditionalEndPointer = -1;
                }

                for (int i = m_EndPointers.Count - 1; i >= 0; --i)
                {
                    int idx = m_EndPointers[i];
                    short jump = (short) (ioCompiler.StreamLength - (idx + 2));
                    LeafInstruction.OverwriteInt16(ioCompiler.m_InstructionStream, idx, jump);
                }

                m_EndPointers.Clear();
            }

            private void EmitExpressionCheck(BlockFilePosition inPosition, StringSlice inExpression, LeafCompiler ioCompiler)
            {
                ioCompiler.WriteExpressionLogical(inPosition, inExpression);
                ioCompiler.WriteOp(LeafOpcode.JumpIfFalse);
                m_NextPointer = ioCompiler.StreamLength;
                ioCompiler.WriteInt16((short) 0);
            }

            private void EmitExpressionCheckBlock(BlockFilePosition inPosition, StringSlice inExpression, LeafCompiler ioCompiler)
            {
                ioCompiler.WriteExpressionLogical(inPosition, inExpression);
                ioCompiler.WriteOp(LeafOpcode.JumpIfFalse);
                m_ConditionalEndPointer = ioCompiler.StreamLength;
                ioCompiler.WriteInt16((short) 0);
            }

            private void Advance(LeafCompiler ioCompiler)
            {
                if (m_NextPointer >= 0)
                {
                    short jump = (short) (ioCompiler.StreamLength - (m_NextPointer + 2));
                    LeafInstruction.OverwriteInt16(ioCompiler.m_InstructionStream, m_NextPointer, jump);
                    m_NextPointer = -1;
                }
            }

            private void PointToEnd(LeafCompiler ioCompiler)
            {
                ioCompiler.WriteOp(LeafOpcode.Jump);
                m_EndPointers.Add(ioCompiler.StreamLength);
                ioCompiler.WriteInt16((short) 0);
            }

            private void PointToStart(LeafCompiler ioCompiler)
            {
                ioCompiler.WriteOp(LeafOpcode.Jump);
                short jump = (short) (m_StartPointer - (ioCompiler.StreamLength + 2));
                ioCompiler.WriteInt16(jump);
            }

            /// <summary>
            /// Clears the block.
            /// </summary>
            public void Clear()
            {
                m_NextPointer = -1;
                m_StartPointer = -1;
                m_ConditionalEndPointer = -1;
                m_EndPointers.Clear();
                m_Phase = Phase.Unstarted;
                m_Type = BlockType.Unassigned;
            }
        }

        private struct JumpHelper
        {
            public int Offset;

            public JumpHelper(int inOffset)
            {
                Offset = inOffset;
            }

            public void OverwriteJumpRelative(LeafCompiler ioCompiler, int inTarget)
            {
                short jump = (short) (inTarget - (Offset + 2));
                LeafInstruction.OverwriteInt16(ioCompiler.m_InstructionStream, Offset, jump);
            }
        }

        private struct MacroDefinition
        {
            public int RequiredArgumentCount;
            public int TotalArgumentCount;
            public bool HasVaradicArgs;
            public string Replace;
        }

        private delegate void CommandHandler(BlockFilePosition inPosition, TagData inData);

        #endregion // Types

        #region Consts

        static private readonly char[] ContentTrimChars = new char[] { '\n', ' ', '\t', '\r' };
        static private readonly char[] MacroStartTrimChars = new char[] { '#', '$' };
        private const int MaxMacroArgs = 16;

        #endregion // Consts

        // setup

        private readonly ILeafCompilerPlugin m_Plugin;
        private bool m_Verbose;
        private LeafCompilerFlags m_Flags;
        private Func<string> m_RetrieveRoot;
        private readonly Dictionary<StringHash32, CommandHandler> m_Handlers = new Dictionary<StringHash32, CommandHandler>(23, CompareUtils.DefaultEquals<StringHash32>());
        private readonly StringSlice.ISplitter m_ArgsListSplitter = new StringUtils.ArgsList.Splitter(false);
        private readonly StringSlice.ISplitter m_MacroArgsListSplitter = new StringUtils.ArgsList.Splitter(',', false, false);
        private IMethodCache m_MethodCache;
        private IBlockParserUtil m_BlockParserState;

        // temp resources

        private readonly object[] m_MacroReplaceArgs = new object[MaxMacroArgs];
        private readonly Dictionary<StringHash32, string> m_MacroFormatReplacements = new Dictionary<StringHash32, string>(16, CompareUtils.DefaultEquals<StringHash32>());
        private readonly char[] m_TempNoAllocCharBuffer = new char[1024];

        private readonly StringBuilder m_ContentBuilder;

        private ConditionalBlockLinker[] m_LinkerStack = new ConditionalBlockLinker[4];
        private int m_LinkerCount = 0;

        // package emission

        private LeafNodePackage m_CurrentPackage;
        private readonly Dictionary<StringHash32, string> m_PackageLines = new Dictionary<StringHash32, string>(256, CompareUtils.DefaultEquals<StringHash32>());
        private readonly Dictionary<StringHash32, string> m_PackageLineNames = new Dictionary<StringHash32, string>(256, CompareUtils.DefaultEquals<StringHash32>());
        private readonly Dictionary<StringHash32, MacroDefinition> m_Macros = new Dictionary<StringHash32, MacroDefinition>(7, CompareUtils.DefaultEquals<StringHash32>());
        private readonly Dictionary<StringHash32, string> m_Consts = new Dictionary<StringHash32, string>(7, CompareUtils.DefaultEquals<StringHash32>());

        private readonly RingBuffer<byte> m_InstructionStream = new RingBuffer<byte>(2048, RingBufferMode.Expand);
        private readonly List<string> m_StringTable = new List<string>(128);
        private readonly List<LeafExpression> m_ExpressionTable = new List<LeafExpression>(128);
        private readonly Dictionary<StringHash32, uint> m_StringTableReuseMap = new Dictionary<StringHash32, uint>(128, CompareUtils.DefaultEquals<StringHash32>());

        // validation

        private readonly HashSet<TableKeyPair> m_ReadVariables = new HashSet<TableKeyPair>(CompareUtils.DefaultEquals<TableKeyPair>());
        private readonly HashSet<TableKeyPair> m_WrittenVariables = new HashSet<TableKeyPair>(CompareUtils.DefaultEquals<TableKeyPair>());
        private readonly HashSet<StringHash32> m_UnrecognizedMethods = new HashSet<StringHash32>(CompareUtils.DefaultEquals<StringHash32>());
        private readonly HashSet<StringHash32> m_UnrecognizedInstanceMethods = new HashSet<StringHash32>(CompareUtils.DefaultEquals<StringHash32>());
        private readonly HashSet<StringHash32> m_ParsedNodeIds = new HashSet<StringHash32>(CompareUtils.DefaultEquals<StringHash32>());
        private readonly HashSet<StringHash32> m_ReferencedNodeIds = new HashSet<StringHash32>(CompareUtils.DefaultEquals<StringHash32>());
        private readonly HashSet<StringHash32> m_ReferencedLocalNodeIds = new HashSet<StringHash32>(CompareUtils.DefaultEquals<StringHash32>());

        // current node

        private BlockFilePosition m_ContentStartPosition;
        private ConditionalBlockLinker m_CurrentLinker;

        private BlockFilePosition m_LineRetryLastRetry;
        private int m_LineRetryCounter;
        private string m_CurrentNodeId;
        private bool m_HasChoices;
        private bool m_HasForks;
        private int m_CurrentNodeLineOffset;
        private uint m_CurrentNodeInstructionOffset;
        private uint m_CurrentNodeInstructionLength;
        private StringHash32 m_CurrentNodeLineCodePrefix;
        private string m_CurrentNodeLineCodePrefixString;

        public LeafCompiler(ILeafCompilerPlugin inPlugin)
        {
            if (inPlugin == null)
                throw new ArgumentNullException("inPlugin");

            m_Plugin = inPlugin;

            if (HasFlag(m_Plugin.CompilerFlags, LeafCompilerFlags.Parse_CollapseContent))
                m_ContentBuilder = new StringBuilder(1024);
            else
                m_ContentBuilder = new StringBuilder(256);

            InitHandlers();
        }

        #region Lifecycle

        /// <summary>p
        /// Prepares to start compiling a module.
        /// </summary>
        public void StartModule(LeafNodePackage inPackage, IMethodCache inMethodCache, IBlockParserUtil inStateUtil, LeafCompilerFlags inFlags)
        {
            Reset();
            m_Flags = inFlags;
            m_Verbose = HasFlag(inFlags, LeafCompilerFlags.Debug);
            m_RetrieveRoot = inPackage.RootPath;
            m_MethodCache = inMethodCache;
            m_BlockParserState = inStateUtil;
            m_CurrentPackage = inPackage;

            if (m_Verbose)
            {
                UnityEngine.Debug.LogFormat("[LeafCompiler] Starting compilation for module '{0}'", inPackage.Name());
            }
        }

        /// <summary>
        /// Prepares to start compiling a node with the given id and starting position.
        /// </summary>
        public void StartNode(string inNodeId, BlockFilePosition inStartPosition)
        {
            if (!LeafUtils.IsValidIdentifier(inNodeId))
                throw new SyntaxException(inStartPosition, "Invalid node id '{0}'", inNodeId);

            m_CurrentNodeId = inNodeId;
            m_HasChoices = false;
            m_HasForks = false;
            m_CurrentNodeLineOffset = -(int) inStartPosition.LineNumber;
            m_CurrentNodeInstructionOffset = (uint) m_InstructionStream.Count;
            m_CurrentNodeInstructionLength = 0;
#if LEAF_USE_OLD_LINECODE_GENERATION
            m_CurrentNodeLineCodePrefixString = string.Concat(inStartPosition.FileName, "|", inNodeId, ":");
#else
            m_CurrentNodeLineCodePrefixString = string.Concat(inStartPosition.FileName, "_", inNodeId, "_");
#endif // LEAF_USE_OLD_LINECODE_GENERATION

            m_CurrentNodeLineCodePrefix = m_CurrentNodeLineCodePrefixString;

            m_ParsedNodeIds.Add(inNodeId);
        }

        /// <summary>
        /// Prepares to start compiling node content.
        /// </summary>
        public void StartNodeContent(BlockFilePosition inStartPosition)
        {
            m_CurrentNodeLineOffset = -(int) inStartPosition.LineNumber;
        }

        /// <summary>
        /// Flushes the currently compiled instructions to the given node.
        /// </summary>
        public void FinishNode(LeafNode ioNode, BlockFilePosition inPosition)
        {
            if (m_LinkerCount > 0)
                throw new SyntaxException(inPosition, "Unclosed linker block (if/endif or while/endwhile)");

            FlushContent();

            if (m_HasForks)
            {
                WriteOp(LeafOpcode.JoinForks);
            }

            if (m_HasChoices)
            {
                WriteOp(LeafOpcode.ShowChoices);
                WriteOp(LeafOpcode.GotoNodeIndirect);
            }

            ioNode.SetInstructionOffsets(m_CurrentNodeInstructionOffset, m_CurrentNodeInstructionLength);

            if (HasFlag(m_Flags, LeafCompilerFlags.Generate_NoOpBoundary))
            {
                WriteOp(LeafOpcode.NoOp);
            }

            m_CurrentNodeId = null;
            m_HasChoices = false;
            m_HasForks = false;

            m_CurrentNodeInstructionLength = 0;
            m_CurrentNodeInstructionOffset = 0;

            m_LineRetryCounter = 0;
            m_LineRetryLastRetry = default;
        }

        /// <summary>
        /// Flushes the accumulated content and expressions to the given package.
        /// </summary>
        public Report FinishModule(LeafNodePackage ioPackage)
        {
            ioPackage.SetLines(m_PackageLines, m_PackageLineNames);
            ioPackage.m_Instructions.InstructionStream = m_InstructionStream.ToArray();
            ioPackage.m_Instructions.StringTable = m_StringTable.ToArray();
            ioPackage.m_Instructions.ExpressionTable = m_ExpressionTable.ToArray();

            Report report = default;

            if (m_Verbose)
            {
                m_BlockParserState.TempBuilder.Length = 0;
                m_BlockParserState.TempBuilder.EnsureCapacity(2048);

                m_BlockParserState.TempBuilder.Append("[LeafCompiler] Finished compiling module '")
                    .Append(ioPackage.Name()).Append('\'');

                if (HasFlag(m_Flags, LeafCompilerFlags.Dump_Stats))
                {
                    m_BlockParserState.TempBuilder.Append("\nEmitted ").Append(m_InstructionStream.Count).Append(" bytes of instructions");
                    m_BlockParserState.TempBuilder.Append("\nEmitted ").Append(m_PackageLines.Count).Append(" text lines");
                    m_BlockParserState.TempBuilder.Append("\nEmitted ").Append(m_PackageLineNames.Count).Append(" custom line names");
                    m_BlockParserState.TempBuilder.Append("\nEmitted ").Append(m_ExpressionTable.Count).Append(" expressions");
                    m_BlockParserState.TempBuilder.Append("\nEmitted ").Append(m_StringTable.Count).Append(" strings");
                    m_BlockParserState.TempBuilder.Append("\nMemory Usage: ").Append(LeafInstructionBlock.CalculateMemoryUsage(ioPackage.m_Instructions)).Append(" bytes leaf / ")
                        .Append(CalculateLineMemoryUsage(m_PackageLines)).Append(" bytes text lines / ").Append(CalculateLineMemoryUsage(m_PackageLineNames)).Append(" bytes custom line names");
                }

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_LoadStore))
                {
                    HashSet<TableKeyPair> unused = new HashSet<TableKeyPair>(m_ReadVariables);
                    unused.ExceptWith(m_WrittenVariables);
                    foreach (var key in unused)
                    {
                        m_BlockParserState.TempBuilder.Append("\nWARN: Variable ").Append(key.ToDebugString()).Append(" is read but not written to");
                    }

                    if (unused.Count > 0)
                    {
                        report.WarningMask |= LeafCompilerErrorMask.ReadOnlyVariable;
                    }

                    unused.Clear();
                    unused.UnionWith(m_WrittenVariables);
                    unused.ExceptWith(m_ReadVariables);
                    foreach (var key in unused)
                    {
                        m_BlockParserState.TempBuilder.Append("\nWARN: Variable ").Append(key.ToDebugString()).Append(" is written to but not read");
                    }

                    if (unused.Count > 0)
                    {
                        report.WarningMask |= LeafCompilerErrorMask.WriteOnlyVariable;
                    }
                }

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_MethodInvocation))
                {
                    if (m_UnrecognizedMethods.Count > 0)
                    {
                        foreach (var methodId in m_UnrecognizedMethods)
                        {
                            m_BlockParserState.TempBuilder.Append("\nERROR: Method ").Append(methodId.ToDebugString()).Append(" is unrecognized");
                        }

                        report.ErrorMask |= LeafCompilerErrorMask.StaticMethodUnrecognized;
                    }

                    if (m_UnrecognizedInstanceMethods.Count > 0)
                    {
                        if (HasFlag(m_Flags, LeafCompilerFlags.Validate_InstanceMethodStrict))
                        {
                            foreach (var methodId in m_UnrecognizedInstanceMethods)
                            {
                                m_BlockParserState.TempBuilder.Append("\nERROR: Instance Method ").Append(methodId.ToDebugString()).Append(" is unrecognized");
                            }

                            report.ErrorMask |= LeafCompilerErrorMask.InstanceMethodUnrecognized;
                        }
                        else
                        {
                            foreach (var methodId in m_UnrecognizedInstanceMethods)
                            {
                                m_BlockParserState.TempBuilder.Append("\nWARN: Instance Method ").Append(methodId.ToDebugString()).Append(" is unrecognized");
                            }

                            report.WarningMask |= LeafCompilerErrorMask.InstanceMethodUnrecognized;
                        }
                    }
                }

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_NodeRef))
                {
                    HashSet<StringHash32> unrecognizedNodeIds = new HashSet<StringHash32>(m_ReferencedNodeIds);
                    unrecognizedNodeIds.ExceptWith(m_ParsedNodeIds);

                    if (unrecognizedNodeIds.Count > 0)
                    {
                        foreach (var nodeId in unrecognizedNodeIds)
                        {
                            m_BlockParserState.TempBuilder.Append("\nWARN: Node Id '").Append(nodeId.ToDebugString()).Append("' is unrecognized");
                        }

                        report.WarningMask |= LeafCompilerErrorMask.NodeUnrecognized;
                    }

                    unrecognizedNodeIds.Clear();
                    unrecognizedNodeIds.UnionWith(m_ReferencedLocalNodeIds);
                    unrecognizedNodeIds.ExceptWith(m_ParsedNodeIds);

                    if (unrecognizedNodeIds.Count > 0)
                    {
                        foreach (var nodeId in unrecognizedNodeIds)
                        {
                            m_BlockParserState.TempBuilder.Append("\nERROR: Local Node Id '").Append(nodeId.ToDebugString()).Append("' is unrecognized");
                        }

                        report.ErrorMask |= LeafCompilerErrorMask.LocalNodeUnrecognized;
                    }
                }

                if (HasFlag(m_Flags, LeafCompilerFlags.Dump_Disassembly))
                {
                    m_BlockParserState.TempBuilder.Append("\nDisassembly:\n");
                    LeafInstruction.Disassemble(ioPackage.m_Instructions, m_BlockParserState.TempBuilder);
                }

                if (report.ErrorMask != 0)
                {
                    UnityEngine.Debug.LogErrorFormat(m_BlockParserState.TempBuilder.Flush());
                }
                else if (report.WarningMask != 0)
                {
                    UnityEngine.Debug.LogWarningFormat(m_BlockParserState.TempBuilder.Flush());
                }
                else
                {
                    UnityEngine.Debug.LogFormat(m_BlockParserState.TempBuilder.Flush());
                }
            }

            m_PackageLines.Clear();
            m_PackageLineNames.Clear();
            m_Macros.Clear();
            m_Consts.Clear();
            m_ExpressionTable.Clear();
            m_InstructionStream.Clear();
            m_StringTable.Clear();
            m_StringTableReuseMap.Clear();
            m_BlockParserState = null;
            m_RetrieveRoot = null;
            m_MethodCache = null;
            m_CurrentPackage = null;

            return report;
        }

        /// <summary>
        /// Resets compiler state.
        /// </summary>
        public void Reset()
        {
            m_InstructionStream.Clear();
            m_PackageLines.Clear();
            m_PackageLineNames.Clear();
            m_ExpressionTable.Clear();
            m_StringTable.Clear();
            m_StringTableReuseMap.Clear();
            m_Macros.Clear();
            m_Consts.Clear();
            m_ReadVariables.Clear();
            m_WrittenVariables.Clear();
            m_UnrecognizedMethods.Clear();
            m_UnrecognizedInstanceMethods.Clear();
            m_ParsedNodeIds.Clear();
            m_ReferencedNodeIds.Clear();
            m_ReferencedLocalNodeIds.Clear();
            m_MethodCache = null;
            m_CurrentPackage = null;

            m_CurrentNodeId = null;
            m_HasChoices = false;
            m_HasForks = false;
            m_CurrentNodeLineOffset = 0;
            m_CurrentNodeInstructionOffset = 0;
            m_CurrentNodeInstructionLength = 0;

            m_BlockParserState = null;
            m_RetrieveRoot = null;
            m_MethodCache = null;
            m_LinkerCount = 0;

            m_LineRetryCounter = 0;
            m_LineRetryLastRetry = default;
        }

        #endregion // Lifecycle

        #region Preprocessor

        /// <summary>
        /// Processes replace rules.
        /// </summary>
        public void PreprocessLine(StringBuilder ioStringBuilder)
        {
            ReplaceConsts(ioStringBuilder, m_Consts, m_TempNoAllocCharBuffer, true);
        }

        private void ExpandMacro(BlockFilePosition inPosition, StringHash32 inMacroId, MacroDefinition inDefinition, StringSlice inArgs)
        {
            string text = inDefinition.Replace;

            if (inDefinition.TotalArgumentCount == 0)
            {
                m_BlockParserState.InsertText(text);
                return;
            }

            Array.Clear(m_MacroReplaceArgs, 0, MaxMacroArgs);
            int argCount = 0;
            StringSliceOptions options;
            if (inDefinition.HasVaradicArgs)
            {
                options = new StringSliceOptions(StringSplitOptions.None, inDefinition.TotalArgumentCount);
            }
            else
            {
                options = new StringSliceOptions(StringSplitOptions.None);
            }
            foreach (var slice in inArgs.EnumeratedSplit(m_MacroArgsListSplitter, options))
            {
                m_MacroReplaceArgs[argCount++] = slice.ToString();
            }
            for (int i = argCount; i < inDefinition.TotalArgumentCount; i++)
            {
                m_MacroReplaceArgs[i] = string.Empty;
            }
            if (argCount < inDefinition.RequiredArgumentCount || argCount > inDefinition.TotalArgumentCount)
            {
                throw new SyntaxException(inPosition, "Macro '{0}' was expecting between {1} and {2} arguments but {2} provided ('{3}')", inMacroId, inDefinition.RequiredArgumentCount, inDefinition.TotalArgumentCount, argCount, inArgs);
            }
            text = string.Format(text, m_MacroReplaceArgs);
            m_BlockParserState.InsertText(text);
            Array.Clear(m_MacroReplaceArgs, 0, argCount);
        }

        /// <summary>
        /// Defines a constant.
        /// </summary>
        public void DefineConst(StringSlice inConst, StringSlice inValue)
        {
            if (!IsValidSimpleToken(inConst))
            {
                throw new SyntaxException(m_BlockParserState.Position, "'{0}' is an invalid const identifier", inConst);
            }

            StringHash32 id = inConst;

            if (m_Handlers.ContainsKey(id) || id == LeafTokens.Macro || id == LeafTokens.Const)
            {
                throw new SyntaxException(m_BlockParserState.Position, "'{0}' is a reserved keyword and cannot be used for consts", id);
            }

            m_Consts[inConst] = inValue.ToString();
        }

        /// <summary>
        /// Defines a macro.
        /// </summary>
        public void DefineMacro(StringSlice inMacroId, StringSlice inDefinition, StringSlice inReplace)
        {
            if (!IsValidSimpleToken(inMacroId))
            {
                throw new SyntaxException(m_BlockParserState.Position, "'{0}' is an invalid macro identifier", inMacroId);
            }

            StringHash32 id = inMacroId;

            if (m_Handlers.ContainsKey(id) || id == LeafTokens.Macro || id == LeafTokens.Const)
            {
                throw new SyntaxException(m_BlockParserState.Position, "'{0}' is a reserved keyword and cannot be used for macros", id);
            }

            if (inDefinition.Contains('(') || inDefinition.Contains(')'))
            {
                throw new SyntaxException(m_BlockParserState.Position, "Argument definitions for macro '{0}' ({1}) must be a comma-separated list only", id, inDefinition);
            }

            MacroDefinition definition = new MacroDefinition();

            if (inDefinition.IsEmpty)
            {
                definition.RequiredArgumentCount = 0;
                definition.TotalArgumentCount = 0;
                definition.Replace = EscapeCurlyBraces(inReplace.ToString());
            }
            else
            {
                bool startOptional = false;
                bool hasVaradic = false;
                m_MacroFormatReplacements.Clear();
                foreach (var varIdString in inDefinition.EnumeratedSplit(m_ArgsListSplitter, StringSplitOptions.None))
                {
                    StringSlice varId = varIdString;
                    if (hasVaradic)
                    {
                        throw new SyntaxException(m_BlockParserState.Position, "Macro definition '{0}' has arguments after varadic arg '{1}'", id, inDefinition);
                    }

                    bool isOptional = varId.EndsWith("?");
                    if (isOptional)
                    {
                        varId = varId.Substring(0, varId.Length - 1);
                        startOptional = true;
                    }
                    else if (varId.StartsWith("..."))
                    {
                        isOptional = true;
                        startOptional = true;
                        hasVaradic = true;
                        varId = varId.Substring(3).TrimStart();
                    }

                    if (!isOptional && startOptional)
                    {
                        throw new SyntaxException(m_BlockParserState.Position, "Argument in macro definition '{0}' has non-optional argument '{1}' located after optional arguments ({2})", id, varIdString, inDefinition);
                    }

                    if (varId.IsEmpty)
                    {
                        throw new SyntaxException(m_BlockParserState.Position, "Argument in macro definition '{0}' ({1}) is empty", id, inDefinition);
                    }

                    StringHash32 varHash = varId;

                    if (m_MacroFormatReplacements.ContainsKey(varHash))
                    {
                        throw new SyntaxException(m_BlockParserState.Position, "Argument id {0} in macro definition '{1}' ({2}) is repeated more than once", varIdString, id, inDefinition);
                    }

                    m_MacroFormatReplacements.Add(varHash, string.Concat("{", m_MacroFormatReplacements.Count.ToStringLookup(), "}"));
                    if (!startOptional)
                    {
                        definition.RequiredArgumentCount++;
                    }
                    definition.TotalArgumentCount++;
                    definition.HasVaradicArgs = hasVaradic;
                }

                StringBuilder tempBuilder = m_BlockParserState.TempBuilder;
                tempBuilder.Length = 0;
                tempBuilder.AppendSlice(inReplace);
                EscapeCurlyBraces(tempBuilder);
                ReplaceConsts(tempBuilder, m_MacroFormatReplacements, m_TempNoAllocCharBuffer, false);
                definition.Replace = tempBuilder.Flush();

                definition.TotalArgumentCount = m_MacroFormatReplacements.Count;
                m_MacroFormatReplacements.Clear();
            }

            m_Macros[id] = definition;
        }

        /// <summary>
        /// Undefines a macro.
        /// </summary>
        public void UndefineMacro(StringHash32 inMacroId)
        {
            if (m_Handlers.ContainsKey(inMacroId) || inMacroId == LeafTokens.Macro || inMacroId == LeafTokens.Const)
            {
                throw new SyntaxException(m_BlockParserState.Position, "'{0}' is a reserved keyword and cannot be used for macros", inMacroId);
            }

            m_Macros.Remove(inMacroId);
        }

        static private string EscapeCurlyBraces(string inMacroReplacement)
        {
            return inMacroReplacement.Replace("{", "{{").Replace("}", "}}");
        }

        static private void EscapeCurlyBraces(StringBuilder inMacroReplacement)
        {
            inMacroReplacement.Replace("{", "{{").Replace("}", "}}");
        }

        #endregion // Preprocessor

        #region Process

        private void InitHandlers()
        {
            m_Handlers.Add(LeafTokens.Stop, (p, d) => {
                FlushContent();
                ProcessSingleOpOptionalCondition(p, d, LeafOpcode.Stop);
            });

            m_Handlers.Add(LeafTokens.Yield, (p, d) => {
                FlushContent();
                WriteOp(LeafOpcode.Yield);
            });

            m_Handlers.Add(LeafTokens.Return, (p, d) => {
                FlushContent();
                ProcessSingleOpOptionalCondition(p, d, LeafOpcode.ReturnFromNode);
            });

            m_Handlers.Add(LeafTokens.Loop, (p, d) => {
                FlushContent();
                ProcessSingleOpOptionalCondition(p, d, LeafOpcode.Loop);
            });

            m_Handlers.Add(LeafTokens.Goto, (p, d) => {
                FlushContent();
                ProcessGotoBranch(p, d, LeafOpcode.GotoNode, LeafOpcode.GotoNodeIndirect);
            });

            m_Handlers.Add(LeafTokens.Branch, (p, d) => {
                FlushContent();
                ProcessGotoBranch(p, d, LeafOpcode.BranchNode, LeafOpcode.BranchNodeIndirect);
            });

            m_Handlers.Add(LeafTokens.Fork, (p, d) => {
                FlushContent();
                ProcessGotoBranch(p, d, LeafOpcode.ForkNode, LeafOpcode.ForkNodeIndirect);
                m_HasForks = true;
            });

            m_Handlers.Add(LeafTokens.Start, (p, d) => {
                FlushContent();
                ProcessGotoBranch(p, d, LeafOpcode.ForkNodeUntracked, LeafOpcode.ForkNodeIndirectUntracked);
                m_HasForks = true;
            });

            m_Handlers.Add(LeafTokens.Join, (p, d) => {
                if (!m_HasForks)
                    throw new SyntaxException(p, "join must come after at least one fork statement");

                FlushContent();
                WriteOp(LeafOpcode.JoinForks);
                m_HasForks = false;
            });

            m_Handlers.Add(LeafTokens.Set, (p, d) => {
                if (d.Data.IsEmpty)
                    throw new SyntaxException(p, "set must be provided an expression");

                FlushContent();
                WriteExpressionAssignment(p, d.Data);
            });

            m_Handlers.Add(LeafTokens.Call, (p, d) => {
                if (d.Data.IsEmpty)
                    throw new SyntaxException(p, "call must be provided a method");

                FlushContent();
                ProcessInvocation(p, d);
            });

            m_Handlers.Add(LeafTokens.Choose, (p, d) => {
                if (!m_HasChoices)
                    throw new SyntaxException(p, "choose must come after at least one choice statement");

                FlushContent();

                WriteOp(LeafOpcode.ShowChoices);
                if (d.Data.IsEmpty || d.Data == LeafTokens.Goto)
                    WriteOp(LeafOpcode.GotoNodeIndirect);
                else if (d.Data == LeafTokens.Branch)
                    WriteOp(LeafOpcode.BranchNodeIndirect);
                else if (d.Data == LeafTokens.Continue)
                    WriteOp(LeafOpcode.PopValue);
                else
                    throw new SyntaxException(p, "unrecognized argument to choose statement '{0}' - must be either goto or branch", d.Data);

                m_HasChoices = false;
            });

            m_Handlers.Add(LeafTokens.Choice, (p, d) => {
                FlushContent();
                ProcessChoice(p, d);
            });

            m_Handlers.Add(LeafTokens.Answer, (p, d) => {
                FlushContent();
                ProcessAnswer(p, d);
            });

            m_Handlers.Add(LeafTokens.Data, (p, d) => {
                FlushContent();
                ProcessData(p, d);
            });

            m_Handlers.Add(LeafTokens.If, (p, d) => {
                FlushContent();
                NewLinker().If(p, d.Data, this);
            });

            m_Handlers.Add(LeafTokens.ElseIf, (p, d) => {
                FlushContent();
                CurrentLinker(p).ElseIf(p, d.Data, this);
            });

            m_Handlers.Add(LeafTokens.Else, (p, d) => {
                FlushContent();
                CurrentLinker(p).Else(p, this);
            });

            m_Handlers.Add(LeafTokens.EndIf, (p, d) => {
                FlushContent();
                CurrentLinker(p).EndIf(p, this);
                PopLinker();
            });

            m_Handlers.Add(LeafTokens.While, (p, d) => {
                FlushContent();
                NewLinker().While(p, d.Data, this);
            });

            m_Handlers.Add(LeafTokens.Break, (p, d) => {
                FlushContent();
                CurrentLinker(p).Break(p, this);
            });

            m_Handlers.Add(LeafTokens.Continue, (p, d) => {
                FlushContent();
                CurrentLinker(p).Continue(p, this);
            });

            m_Handlers.Add(LeafTokens.EndWhile, (p, d) => {
                FlushContent();
                CurrentLinker(p).EndWhile(p, this);
                PopLinker();
            });
        }

        /// <summary>
        /// Processes the given line into instructions.
        /// </summary>
        public void Process(BlockFilePosition inFilePosition, StringSlice inLine)
        {
            StringSlice beginningTrimmed = inLine.TrimStart(TagData.MinimalWhitespaceChars);

            if (m_CurrentNodeInstructionLength == 0)
                inLine = beginningTrimmed;

            if (TryProcessCommand(inFilePosition, beginningTrimmed))
                return;

            ProcessContent(inFilePosition, inLine);
        }

        private bool TryProcessCommand(BlockFilePosition inPosition, StringSlice inLine)
        {
            if (inLine.IsEmpty || !inLine.StartsWith('$'))
                return false;

            inLine = inLine.Substring(1).Trim(TagData.MinimalWhitespaceChars);

            TagData data = default;

            int spaceIdx = inLine.IndexOf(' ');
            int tabIdx = inLine.IndexOf('\t');
            int seperatorIdx = spaceIdx == -1 ? tabIdx : (tabIdx == -1 ? spaceIdx : Math.Min(spaceIdx, tabIdx));
            if (seperatorIdx >= 0)
            {
                data.Id = inLine.Substring(0, seperatorIdx).TrimEnd(TagData.MinimalWhitespaceChars);
                data.Data = inLine.Substring(seperatorIdx + 1).TrimStart(TagData.MinimalWhitespaceChars);
            }
            else
            {
                data.Id = inLine;
                data.Data = default(StringSlice);
            }

            StringHash32 commandType = data.Id;

            CommandHandler handler;
            if (m_Handlers.TryGetValue(commandType, out handler))
            {
                handler(inPosition, data);
                return true;
            }

            return TryProcessMacro(inPosition, inLine);
        }

        /// <summary>
        /// Returns if any macros are defined.
        /// </summary>
        public bool HasMacros()
        {
            return m_Macros.Count > 0;
        }

        /// <summary>
        /// Attempts to process a macro.
        /// </summary>
        public bool TryProcessMacro(BlockFilePosition inPosition, StringSlice inLine)
        {
            if (m_Macros.Count == 0)
                return false;

            StringSlice macroId, macroArgs;
            if (TrySplitMethodArgs(inPosition, inLine, out macroId, out macroArgs))
            {
                MacroDefinition macroDef;
                if (m_Macros.TryGetValue(macroId, out macroDef))
                {
                    ExpandMacro(inPosition, macroId, macroDef, macroArgs);
                    return true;
                }
            }

            return false;
        }

        #endregion // Process

        #region Commands

        private void ProcessGotoBranch(BlockFilePosition inPosition, TagData inData, LeafOpcode inDirect, LeafOpcode inIndirect)
        {
            // Syntax
            // goto node
            // goto $node expression
            // goto node, expression
            // goto $node expression, expression
            // branch node
            // branch $node expression
            // branch node, expression
            // branch $node expression, expression
            // fork node
            // fork $node expression
            // fork node, expression
            // fork $node expression, expression

            StringSlice nodeId, expression;
            SplitNodeExpression(inData.Data, out nodeId, out expression);

            JumpHelper skip = default;

            if (!expression.IsEmpty)
            {
                WriteExpressionLogical(inPosition, expression);
                WriteOp(LeafOpcode.JumpIfFalse);
                skip = new JumpHelper(StreamLength);
                WriteUInt16(0);
            }

            if (nodeId.IsEmpty)
                throw new SyntaxException(inPosition, "goto or branch commands cannot have empty target");

            StringSlice nodeExp;
            if (IsIndirect(nodeId, out nodeExp))
            {
                WriteExpressionEvaluate(inPosition, nodeExp);
                WriteOp(inIndirect);
            }
            else
            {
                bool isLocal;
                nodeId = ProcessNodeId(nodeId, out isLocal);

                if (!LeafUtils.IsValidIdentifier(nodeId))
                    throw new SyntaxException(inPosition, "node identifier '{0}' is not a valid identifier", nodeId);

                WriteOp(inDirect);
                WriteStringHash32(nodeId);

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_NodeRef))
                {
                    if (isLocal)
                        m_ReferencedLocalNodeIds.Add(nodeId);
                    else
                        m_ReferencedNodeIds.Add(nodeId);
                }
            }

            if (!expression.IsEmpty)
            {
                skip.OverwriteJumpRelative(this, StreamLength);
            }
        }

        private void ProcessSingleOpOptionalCondition(BlockFilePosition inPosition, TagData inData, LeafOpcode inOpcode)
        {
            if (!inData.Data.IsEmpty)
            {
                WriteExpressionLogical(inPosition, inData.Data);
                WriteOp(LeafOpcode.JumpIfFalse);
                WriteInt16((short) 1);
            }

            WriteOp(inOpcode);
        }

        private void ProcessChoice(BlockFilePosition inPosition, TagData inData)
        {
            // Syntax
            // choice node; text
            // choice $node expression; text
            // choice node, expression; text
            // choice $node expression, expression; text

            StringSlice args, content;
            SplitArgsContent(inData.Data, out args, out content);

            if (content.IsEmpty)
                throw new SyntaxException(inPosition, "choice commands must have ; followed by at least one non-whitespace character");

            StringSlice nodeId, expression;
            SplitNodeExpression(args, out nodeId, out expression);

            // push node id
            if (nodeId.IsEmpty)
                throw new SyntaxException(inPosition, "choice commands cannot have empty target");

            StringSlice nodeExp;
            LeafChoice.OptionFlags choiceFlags = 0;
            if (IsIndirect(nodeId, out nodeExp))
            {
                WriteExpressionEvaluate(inPosition, nodeExp);
            }
            else if (nodeId.StartsWith('#'))
            {
                choiceFlags |= LeafChoice.OptionFlags.IsSelector;
                if (nodeId.Length < 2)
                    throw new SyntaxException(inPosition, "node answer selector '{0}' is not a valid identifier", nodeId);

                WritePushValue(nodeId.Substring(1).Hash32());
            }
            else
            {
                bool isLocal;
                nodeId = ProcessNodeId(nodeId, out isLocal);

                if (!LeafUtils.IsValidIdentifier(nodeId))
                    throw new SyntaxException(inPosition, "node identifier '{0}' is not a valid identifier", nodeId);

                WritePushValue(nodeId.Hash32());

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_NodeRef))
                {
                    if (isLocal)
                        m_ReferencedLocalNodeIds.Add(nodeId);
                    else
                        m_ReferencedNodeIds.Add(nodeId);
                }
            }

            // push line code
            StringHash32 lineCode = EmitLine(inPosition, content);

            // push bool
            if (!expression.IsEmpty)
            {
                WriteExpressionLogical(inPosition, expression);
            }
            else
            {
                WritePushValue(true);
            }

            WriteOp(LeafOpcode.AddChoiceOption);
            WriteStringHash32(lineCode);
            WriteByte((byte) choiceFlags);

            m_HasChoices = true;
        }

        private void ProcessAnswer(BlockFilePosition inPosition, TagData inData)
        {
            // Syntax
            // answer answerId, node
            // answer answerId, $node expression
            // answer answerId, conditions, node
            // answer answerId, conditions, $node expression

            // TODO: handle method call node expressions correctly

            int firstComma, lastComma;
            firstComma = inData.Data.IndexOf(',');
            lastComma = inData.Data.LastIndexOf(',');

            StringSlice answerSlice, conditionsSlice, nodeSlice;

            answerSlice = inData.Data.Substring(0, firstComma).TrimEnd(TagData.MinimalWhitespaceChars);
            nodeSlice = inData.Data.Substring(lastComma + 1).TrimStart(TagData.MinimalWhitespaceChars);
            if (firstComma != lastComma)
            {
                conditionsSlice = inData.Data.Substring(firstComma + 1, lastComma - firstComma - 1).Trim(TagData.MinimalWhitespaceChars);
            }
            else
            {
                conditionsSlice = default(StringSlice);
            }

            if (answerSlice.Length == 0)
            {
                throw new SyntaxException(inPosition, "Answer id cannot be empty. For a default selector, use *");
            }
            else if (answerSlice.Length == 1 && answerSlice[0] == '*')
            {
                answerSlice = StringSlice.Empty;
            }

            JumpHelper skip = default(JumpHelper);

            if (!conditionsSlice.IsEmpty)
            {
                WriteExpressionLogical(inPosition, conditionsSlice);
                WriteOp(LeafOpcode.JumpIfFalse);
                skip = new JumpHelper(StreamLength);
                WriteInt16(0);
            }

            StringSlice indirectNode;
            if (IsIndirect(nodeSlice, out indirectNode))
            {
                WriteExpressionEvaluate(inPosition, indirectNode);
            }
            else
            {
                bool isLocal;
                nodeSlice = ProcessNodeId(nodeSlice, out isLocal);

                if (!LeafUtils.IsValidIdentifier(nodeSlice))
                    throw new SyntaxException(inPosition, "node identifier '{0}' is not a valid identifier", nodeSlice);

                StringHash32 nodeHash = nodeSlice.Hash32();
                WritePushValue(nodeHash);

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_NodeRef))
                {
                    if (isLocal)
                        m_ReferencedLocalNodeIds.Add(nodeHash);
                    else
                        m_ReferencedNodeIds.Add(nodeHash);
                }
            }

            WriteOp(LeafOpcode.AddChoiceAnswer);
            WriteStringHash32(answerSlice);

            if (!conditionsSlice.IsEmpty)
            {
                skip.OverwriteJumpRelative(this, StreamLength);
            }
        }

        private void ProcessData(BlockFilePosition inPosition, TagData inData)
        {
            // Syntax
            // data dataId
            // data dataId = value
            // data dataId = $dataExpression

            int equals;
            equals = inData.Data.IndexOf('=');

            StringSlice idSlice, valueSlice;

            if (equals >= 0)
            {
                idSlice = inData.Data.Substring(0, equals).TrimEnd(TagData.MinimalWhitespaceChars);
                valueSlice = inData.Data.Substring(equals + 1).TrimStart(TagData.MinimalWhitespaceChars);
            }
            else
            {
                idSlice = inData.Data;
                valueSlice = StringSlice.Empty;
            }

            if (idSlice.Length == 0)
            {
                throw new SyntaxException(inPosition, "Data id cannot be empty");
            }

            StringSlice indirectNode;
            if (valueSlice.IsEmpty)
            {
                if (equals >= 0)
                {
                    WritePushValue(Variant.Null);
                }
                else
                {
                    WritePushValue(true);
                }
            }
            else if (IsIndirect(valueSlice, out indirectNode))
            {
                WriteExpressionEvaluate(inPosition, indirectNode);
            }
            else
            {
                Variant dataValue;
                if (!Variant.TryParse(valueSlice, out dataValue))
                    throw new SyntaxException(inPosition, "data value '{0}' cannot be parsed to a Variant", valueSlice);

                WritePushValue(dataValue);
            }

            WriteOp(LeafOpcode.AddChoiceData);
            WriteStringHash32(idSlice);
        }

        private void ProcessInvocation(BlockFilePosition inPosition, TagData inData)
        {
            StringHash32 targetDirect;
            StringHash32 methodId;
            uint argsIndex = LeafInstruction.EmptyIndex;

            StringSlice method, args, targetSlice;
            SplitMethodArgs(inPosition, inData.Data, out method, out args);
            SplitTargetMethod(method, out targetSlice, out method);

            StringSlice indirectTarget;
            if (IsIndirect(targetSlice, out indirectTarget))
            {
                targetDirect = null;
            }
            else
            {
                targetDirect = targetSlice;
            }

            methodId = method;
            argsIndex = EmitStringTableEntry(args);

            if (targetDirect.IsEmpty)
            {
                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_MethodInvocation) && m_MethodCache != null && !m_MethodCache.HasStatic(methodId))
                {
                    m_UnrecognizedMethods.Add(methodId);
                }

                WriteOp(LeafOpcode.Invoke_Unoptimized);
            }
            else
            {
                if (!indirectTarget.IsEmpty)
                {
                    WriteExpressionEvaluate(inPosition, indirectTarget);
                }
                else
                {
                    WritePushValue(targetDirect);
                }

                if (HasFlag(m_Flags, LeafCompilerFlags.Validate_MethodInvocation) && m_MethodCache != null && !m_MethodCache.HasInstance(methodId))
                {
                    m_UnrecognizedInstanceMethods.Add(methodId);
                }

                WriteOp(LeafOpcode.InvokeWithTarget_Unoptimized);
            }

            WriteStringHash32(methodId);
            WriteUInt32(argsIndex);
        }

        #endregion // Commands

        #region Instructions

        private int StreamLength
        {
            get { return m_InstructionStream.Count; }
        }

        private void WriteOp(LeafOpcode inOpcode)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteOpcode(m_InstructionStream, inOpcode);
        }

        private void WriteByte(byte inByte)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteByte(m_InstructionStream, inByte);
        }

        private void WriteStringHash32(StringHash32 inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteStringHash32(m_InstructionStream, inArgument);
        }

        private void WriteUInt32(uint inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteUInt32(m_InstructionStream, inArgument);
        }

        private void WriteUInt16(ushort inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteUInt16(m_InstructionStream, inArgument);
        }

        private void WriteInt16(short inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteInt16(m_InstructionStream, inArgument);
        }

        private void WriteVariant(Variant inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteVariant(m_InstructionStream, inArgument);
        }

        private void WriteTableKeyPair(TableKeyPair inArgument)
        {
            m_CurrentNodeInstructionLength += LeafInstruction.WriteTableKeyPair(m_InstructionStream, inArgument);
        }

        private void WritePushValue(Variant inValue)
        {
            WriteOp(LeafOpcode.PushValue);
            WriteVariant(inValue);
        }

        #endregion // Instructions

        #region Strings

        private uint EmitStringTableEntry(StringSlice inString)
        {
            if (inString.IsEmpty)
            {
                return LeafInstruction.EmptyIndex;
            }

            StringHash32 hash = StringHash32.Fast(inString);
            uint index;
            if (!m_StringTableReuseMap.TryGetValue(hash, out index))
            {
                index = (uint) m_StringTable.Count;
                m_StringTable.Add(inString.ToString());
                m_StringTableReuseMap.Add(hash, index);
            }

            return index;
        }

        #endregion // Strings

        #region Expressions

        private void WriteExpressionEvaluate(BlockFilePosition inPosition, StringSlice inExpression)
        {
            VariantOperand operand;
            if (VariantOperand.TryParse(inExpression, out operand))
            {
                WriteVariantOperand(operand);
                return;
            }

            throw new SyntaxException(inPosition, "Expression '{0}' is not a single, non-logical expression", inExpression);
        }

        private void WriteExpressionLogical(BlockFilePosition inPosition, StringSlice inExpression)
        {
            VariantOperand operand;
            VariantComparison comparison;
            if (StringUtils.ArgsList.IsList(inExpression))
            {
                LeafExpressionGroup group = CompileExpressionGroup(inPosition, inExpression);
                WriteOp(LeafOpcode.EvaluateExpressionsAnd);
                WriteUInt32(group.m_Offset);
                WriteUInt16(group.m_Count);
            }
            else if (VariantOperand.TryParse(inExpression, out operand))
            {
                WriteVariantOperand(operand);
                // WriteOp(LeafOpcode.CastToBool); // don't need to do this
            }
            else if (VariantComparison.TryParse(inExpression, out comparison))
            {
                // TODO: Possible optimization if comparing constants?

                switch (comparison.Operator)
                {
                    case VariantCompareOperator.True:
                    {
                        WriteVariantOperand(comparison.Left);
                        break;
                    }

                    case VariantCompareOperator.False:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteOp(LeafOpcode.Not);
                        break;
                    }

                    case VariantCompareOperator.LessThan:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.LessThan);
                        break;
                    }

                    case VariantCompareOperator.LessThanOrEqualTo:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.LessThanOrEqualTo);
                        break;
                    }

                    case VariantCompareOperator.EqualTo:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.EqualTo);
                        break;
                    }

                    case VariantCompareOperator.NotEqualTo:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.NotEqualTo);
                        break;
                    }

                    case VariantCompareOperator.GreaterThanOrEqualTo:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.GreaterThanOrEqualTo);
                        break;
                    }

                    case VariantCompareOperator.GreaterThan:
                    {
                        WriteVariantOperand(comparison.Left);
                        WriteVariantOperand(comparison.Right);
                        WriteOp(LeafOpcode.GreaterThan);
                        break;
                    }

                    default:
                    {
                        throw new SyntaxException(inPosition, "Comparison '{0}' is not supported by Leaf for expression '{1}'", comparison.Operator, inExpression);
                    }
                }
            }
            else
            {
                throw new SyntaxException(inPosition, "Expression '{0}' was unable to be evaluated", inExpression);
            }
        }

        private void WriteExpressionAssignment(BlockFilePosition inPosition, StringSlice inExpression)
        {
            VariantModification modification;
            if (!VariantModification.TryParse(inExpression, out modification))
                throw new SyntaxException(inPosition, "string '{0}' cannot be parsed to a set expression", inExpression);

            switch (modification.Operator)
            {
                case VariantModifyOperator.Set:
                {
                    WriteVariantOperand(modification.Operand);

                    WriteOp(LeafOpcode.StoreTableValue);
                    WriteTableKeyPair(modification.VariableKey);
                    break;
                }

                case VariantModifyOperator.Add:
                {
                    // special case for single increment
                    if (modification.Operand.Type == VariantOperand.Mode.Variant && modification.Operand.Value.AsInt() == 1)
                    {
                        WriteOp(LeafOpcode.IncrementTableValue);
                        WriteTableKeyPair(modification.VariableKey);
                    }
                    else
                    {
                        WriteOp(LeafOpcode.LoadTableValue);
                        WriteTableKeyPair(modification.VariableKey);

                        WriteVariantOperand(modification.Operand);

                        WriteOp(LeafOpcode.Add);

                        WriteOp(LeafOpcode.StoreTableValue);
                        WriteTableKeyPair(modification.VariableKey);
                    }
                    break;
                }

                case VariantModifyOperator.Subtract:
                {
                    if (modification.Operand.Type == VariantOperand.Mode.Variant && modification.Operand.Value.AsInt() == 1)
                    {
                        WriteOp(LeafOpcode.DecrementTableValue);
                        WriteTableKeyPair(modification.VariableKey);
                    }
                    else
                    {
                        WriteOp(LeafOpcode.LoadTableValue);
                        WriteTableKeyPair(modification.VariableKey);

                        WriteVariantOperand(modification.Operand);

                        WriteOp(LeafOpcode.Subtract);

                        WriteOp(LeafOpcode.StoreTableValue);
                        WriteTableKeyPair(modification.VariableKey);
                    }
                    break;
                }

                case VariantModifyOperator.Multiply:
                {
                    WriteOp(LeafOpcode.LoadTableValue);
                    WriteTableKeyPair(modification.VariableKey);

                    WriteVariantOperand(modification.Operand);

                    WriteOp(LeafOpcode.Multiply);

                    WriteOp(LeafOpcode.StoreTableValue);
                    WriteTableKeyPair(modification.VariableKey);
                    break;
                }

                case VariantModifyOperator.Divide:
                {
                    WriteOp(LeafOpcode.LoadTableValue);
                    WriteTableKeyPair(modification.VariableKey);

                    WriteVariantOperand(modification.Operand);

                    WriteOp(LeafOpcode.Divide);

                    WriteOp(LeafOpcode.StoreTableValue);
                    WriteTableKeyPair(modification.VariableKey);
                    break;
                }

                default:
                {
                    throw new InvalidOperationException("Unknown modification operator " + modification.Operator);
                }
            }

            if (HasFlag(m_Flags, LeafCompilerFlags.Validate_LoadStore))
            {
                m_WrittenVariables.Add(modification.VariableKey);
            }
        }

        private void WriteVariantOperand(VariantOperand inOperand)
        {
            switch (inOperand.Type)
            {
                case VariantOperand.Mode.Variant:
                {
                    WritePushValue(inOperand.Value);
                    break;
                }

                case VariantOperand.Mode.TableKey:
                {
                    if (HasFlag(m_Flags, LeafCompilerFlags.Validate_LoadStore))
                    {
                        m_ReadVariables.Add(inOperand.TableKey);
                    }

                    WriteOp(LeafOpcode.LoadTableValue);
                    WriteTableKeyPair(inOperand.TableKey);
                    break;
                }

                case VariantOperand.Mode.Method:
                {
                    MethodCall method = inOperand.MethodCall;
                    uint argsIndex = EmitStringTableEntry(method.Args);

                    if (HasFlag(m_Flags, LeafCompilerFlags.Validate_MethodInvocation) && m_MethodCache != null && !m_MethodCache.HasStatic(method.Id))
                    {
                        m_UnrecognizedMethods.Add(method.Id);
                    }

                    WriteOp(LeafOpcode.InvokeWithReturn_Unoptimized);
                    WriteStringHash32(method.Id);
                    WriteUInt32(argsIndex);
                    break;
                }
            }
        }

        private LeafExpression CompileLogicalExpressionChunk(BlockFilePosition inPosition, StringSlice inExpression)
        {
            LeafExpression expression;
            VariantComparison comparison;
            if (!VariantComparison.TryParse(inExpression, out comparison))
            {
                throw new SyntaxException(inPosition, "Expression chunk '{0}' unable to be evaluated as a comparison", inExpression);
            }

            expression.Flags = LeafExpression.TypeFlags.IsLogical;
            expression.Operator = comparison.Operator;
            CompileLogicalExpressionOperand(comparison.Left, out expression.LeftType, out expression.Left);
            CompileLogicalExpressionOperand(comparison.Right, out expression.RightType, out expression.Right);

            return expression;
        }

        private void CompileLogicalExpressionOperand(VariantOperand inOperand, out LeafExpression.OperandType outType, out LeafExpression.OperandData outData)
        {
            switch (inOperand.Type)
            {
                case VariantOperand.Mode.Variant:
                {
                    outType = LeafExpression.OperandType.Value;
                    outData = new LeafExpression.OperandData(inOperand.Value);
                    break;
                }
                case VariantOperand.Mode.TableKey:
                {
                    if (HasFlag(m_Flags, LeafCompilerFlags.Validate_LoadStore))
                    {
                        m_ReadVariables.Add(inOperand.TableKey);
                    }
                    outType = LeafExpression.OperandType.Read;
                    outData = new LeafExpression.OperandData(inOperand.TableKey);
                    break;
                }
                case VariantOperand.Mode.Method:
                {
                    MethodCall method = inOperand.MethodCall;
                    uint argsIndex = EmitStringTableEntry(method.Args);

                    if (HasFlag(m_Flags, LeafCompilerFlags.Validate_MethodInvocation) && m_MethodCache != null && !m_MethodCache.HasStatic(method.Id))
                    {
                        m_UnrecognizedMethods.Add(method.Id);
                    }

                    outType = LeafExpression.OperandType.Method;
                    outData = new LeafExpression.OperandData(method.Id, argsIndex);
                    break;
                }
                default:
                {
                    throw new InvalidOperationException("Unknown operand type " + inOperand.Type);
                }
            }
        }

        public LeafExpressionGroup CompileExpressionGroup(StringSlice inExpression)
        {
            return CompileExpressionGroup(m_BlockParserState.Position, inExpression);
        }

        public LeafExpressionGroup CompileExpressionGroup(BlockFilePosition inPosition, StringSlice inExpression)
        {
            uint expressionOffset = (uint) m_ExpressionTable.Count;
            ushort expressionCount = 0;

            LeafExpression expression;
            foreach (var group in inExpression.EnumeratedSplit(m_ArgsListSplitter, StringSplitOptions.RemoveEmptyEntries))
            {
                expression = CompileLogicalExpressionChunk(inPosition, group);
                m_ExpressionTable.Add(expression);
                expressionCount++;
            }

            LeafExpressionGroup expGroup;
            expGroup.m_Offset = expressionOffset;
            expGroup.m_Count = expressionCount;
            expGroup.m_Package = m_CurrentPackage;
            if (expressionCount >= 1)
            {
                expGroup.m_Type = LeafExpression.TypeFlags.IsLogical | LeafExpression.TypeFlags.IsAnd;
            }
            else if (expressionCount == 1)
            {
                expGroup.m_Type = m_ExpressionTable[(int) expressionOffset].Flags;
            }
            else
            {
                expGroup.m_Type = default;
            }

            return expGroup;
        }

        #endregion // Expressions

        #region Content

        private void ProcessContent(BlockFilePosition inPosition, StringSlice inLine)
        {
            if (HasFlag(m_Flags, LeafCompilerFlags.Parse_CollapseContent))
            {
                AccumulateContent(inPosition, inLine);
                return;
            }

            if (m_ContentBuilder.Length > 0 || inLine.Length > 0)
            {
                AccumulateContent(inPosition, inLine);
                FlushContent();
            }
        }

        private void AccumulateContent(BlockFilePosition inPosition, StringSlice inLine)
        {
            if (m_ContentBuilder.Length == 0)
            {
                m_ContentStartPosition = inPosition;
            }

            inLine.Unescape(m_ContentBuilder);
            m_ContentBuilder.Append('\n');
        }

        private void FlushContent()
        {
            if (m_ContentBuilder.Length > 0)
            {
                m_ContentBuilder.TrimEnd(ContentTrimChars);
                string text = m_ContentBuilder.Flush();

                StringHash32 lineCode = EmitLine(m_ContentStartPosition, text);
                WriteOp(LeafOpcode.RunLine);
                WriteStringHash32(lineCode);

                m_ContentStartPosition = default(BlockFilePosition);
            }
        }

        private StringHash32 EmitLine(BlockFilePosition inPosition, StringSlice inLine)
        {
            StringHash32 key = default(StringHash32);
            int lineCodeIdx = inLine.IndexOf("$[");
            string staticLineCode = null;
            if (lineCodeIdx >= 0)
            {
                int end = inLine.IndexOf(']', lineCodeIdx);
                if (end >= 0)
                {
                    staticLineCode = inLine.Substring(lineCodeIdx + 2, end - lineCodeIdx - 2).Trim().ToString();
                    inLine = inLine.Substring(0, lineCodeIdx).TrimEnd();

                    uint hexKey;
                    if (uint.TryParse(staticLineCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexKey))
                    {
                        key = new StringHash32(hexKey);
                    }
                    else
                    {
                        key = staticLineCode;
                    }
                }
            }
            if (key.IsEmpty)
            {
                key = GenerateLocalLineCode(inPosition);
            }
            else if (HasFlag(m_Flags, LeafCompilerFlags.Preserve_CustomLineNameStrings))
            {
                m_PackageLineNames.Add(key, staticLineCode);
            }
            m_PackageLines.Add(key, inLine.ToString());
            return key;
        }

        #endregion // Content

        #region Sequence Linker

        private ConditionalBlockLinker NewLinker()
        {
            if (m_LinkerCount >= m_LinkerStack.Length)
            {
                Array.Resize(ref m_LinkerStack, m_LinkerStack.Length + 2);
            }

            int idx = m_LinkerCount++;
            ConditionalBlockLinker linker = m_LinkerStack[idx];
            if (linker == null)
            {
                linker = m_LinkerStack[idx] = new ConditionalBlockLinker();
            }
            linker.Clear();

            m_CurrentLinker = linker;
            return linker;
        }

        private ConditionalBlockLinker CurrentLinker(BlockFilePosition inPosition)
        {
            if (m_CurrentLinker == null)
                throw new SyntaxException(inPosition, "elseif, else, or endif statement without a corresponding if");

            return m_CurrentLinker;
        }

        private void PopLinker()
        {
            if (m_CurrentLinker == null)
                throw new InvalidOperationException("Attempting to pop null linker");

            m_CurrentLinker.Clear();
            if (--m_LinkerCount > 0)
                m_CurrentLinker = m_LinkerStack[m_LinkerCount - 1];
            else
                m_CurrentLinker = null;
        }

        #endregion // Sequence Linker

        #region Utilities

        static private long CalculateLineMemoryUsage(Dictionary<StringHash32, string> inLines)
        {
            long size = 0;
            size += (Unsafe.SizeOf<StringHash32>() + 4 + Unsafe.PointerSize) * inLines.Count;

            long sizeOfChar = sizeof(char);
            foreach (var line in inLines.Values)
            {
                size += sizeOfChar * line.Length;
            }

            return size;
        }

        private StringSlice ProcessNodeId(StringSlice inNodeId, out bool outbIsLocal)
        {
            string root = m_RetrieveRoot();

            if (inNodeId.StartsWith(m_Plugin.PathSeparator))
            {
                outbIsLocal = true;
                return LeafUtils.AssembleFullId(m_BlockParserState.TempBuilder, root, inNodeId.Substring(1), m_Plugin.PathSeparator);
            }

            outbIsLocal = string.IsNullOrEmpty(root);
            return inNodeId;
        }

        static private void SplitNodeExpression(StringSlice inData, out StringSlice outNodeId, out StringSlice outExpression)
        {
            int commaIdx = inData.IndexOf(',');
            if (commaIdx >= 0)
            {
                outNodeId = inData.Substring(0, commaIdx).TrimEnd(TagData.MinimalWhitespaceChars);
                outExpression = inData.Substring(commaIdx + 1).TrimStart(TagData.MinimalWhitespaceChars);
            }
            else
            {
                outNodeId = inData;
                outExpression = StringSlice.Empty;
            }
        }

        static private void SplitTargetMethod(StringSlice inData, out StringSlice outTarget, out StringSlice outMethod)
        {
            int indirectIndex = inData.IndexOf("->");
            if (indirectIndex >= 0)
            {
                outTarget = inData.Substring(0, indirectIndex).TrimEnd(TagData.MinimalWhitespaceChars);
                outMethod = inData.Substring(indirectIndex + 2).TrimStart(TagData.MinimalWhitespaceChars);
            }
            else
            {
                outTarget = StringSlice.Empty;
                outMethod = inData;
            }
        }

        static private void SplitMethodArgs(BlockFilePosition inPosition, StringSlice inData, out StringSlice outMethod, out StringSlice outArgs)
        {
            int openParenIdx = inData.IndexOf('(');
            int closeParenIdx = inData.LastIndexOf(')');

            if (openParenIdx < 0 || closeParenIdx < 0)
            {
                throw new SyntaxException(inPosition, "Method call {0} does not have property formatted () operators", inData);
            }

            StringSlice methodSlice = inData.Substring(0, openParenIdx).TrimEnd();
            int argsLength = closeParenIdx - 1 - openParenIdx;

            outMethod = methodSlice;
            outArgs = inData.Substring(openParenIdx + 1, argsLength);
        }

        static private bool TrySplitMethodArgs(BlockFilePosition inPosition, StringSlice inData, out StringSlice outMethod, out StringSlice outArgs)
        {
            int openParenIdx = inData.IndexOf('(');
            int closeParenIdx = inData.LastIndexOf(')');

            if (openParenIdx < 0 || closeParenIdx < 0)
            {
                outMethod = null;
                outArgs = null;
                return false;
            }

            StringSlice methodSlice = inData.Substring(0, openParenIdx).TrimEnd();
            int argsLength = closeParenIdx - 1 - openParenIdx;

            outMethod = methodSlice;
            outArgs = inData.Substring(openParenIdx + 1, argsLength);
            return true;
        }

        static private void SplitArgsContent(StringSlice inData, out StringSlice outArgs, out StringSlice outContent)
        {
            int semiIdx = inData.IndexOf(';');
            if (semiIdx >= 0)
            {
                outArgs = inData.Substring(0, semiIdx).TrimEnd(TagData.MinimalWhitespaceChars);
                outContent = inData.Substring(semiIdx + 1).TrimStart(TagData.MinimalWhitespaceChars);
            }
            else
            {
                outArgs = inData;
                outContent = StringSlice.Empty;
            }
        }

        static private bool IsIndirect(StringSlice inValue, out StringSlice outValue)
        {
            if (inValue.StartsWith('$'))
            {
                outValue = inValue.Substring(1).Trim(TagData.MinimalWhitespaceChars);
                return !outValue.IsEmpty;
            }

            outValue = StringSlice.Empty;
            return false;
        }

        private StringHash32 GenerateLocalLineCode(BlockFilePosition inFilePosition)
        {
            int lineNumber = (int) (inFilePosition.LineNumber + m_CurrentNodeLineOffset);
            StringHash32 firstAttempt = m_CurrentNodeLineCodePrefix.Concat(lineNumber.ToStringLookup());
            if (!m_PackageLines.ContainsKey(firstAttempt))
            {
                m_LineRetryLastRetry = default;
                m_LineRetryCounter = 0;
                return firstAttempt;
            }

            if (m_LineRetryLastRetry.FileName != inFilePosition.FileName || m_LineRetryLastRetry.LineNumber != inFilePosition.LineNumber)
            {
                m_LineRetryLastRetry = inFilePosition;
                m_LineRetryCounter = 0;
            }

            StringHash32 prefix = firstAttempt.FastConcat("-");
            while (++m_LineRetryCounter < 999)
            {
                StringHash32 withSuffix = prefix.FastConcat(m_LineRetryCounter.ToStringLookup());
                if (!m_PackageLines.ContainsKey(withSuffix))
                {
                    CommitLineCodeToReverseLookup(m_CurrentNodeLineCodePrefixString, lineNumber, m_LineRetryCounter);
                    return withSuffix;
                }
            }

            throw new SyntaxException(inFilePosition, "Cannot generate a unique line code for this line");
        }

        [Conditional("PRESERVE_DEBUG_SYMBOLS")]
        static private void CommitLineCodeToReverseLookup(string inPrefix, int inLineNumber, int inRetryCount)
        {
#if PRESERVE_DEBUG_SYMBOLS
            new StringHash32(string.Concat(inPrefix, inLineNumber.ToStringLookup(), "-", inRetryCount.ToStringLookup()));
#endif // PRESERVE_DEBUG_SYMBOLS
        }

        /// <summary>
        /// Generates a line code for the given position, node, and line type.
        /// </summary>
        static public StringHash32 GenerateLineCode(BlockFilePosition inFilePosition, string inNodeId, int inLineOffset = 0)
        {
#if LEAF_USE_OLD_LINECODE_GENERATION
            return string.Format("{0}|{1}:{2}", inFilePosition.FileName, inNodeId, inFilePosition.LineNumber + inLineOffset);
#else
            return string.Format("{0}_{1}_{2}", inFilePosition.FileName, inNodeId, inFilePosition.LineNumber + inLineOffset);
#endif // LEAF_USE_OLD_LINECODE_GENERATION
        }

        static public void ReplaceConsts(StringBuilder ioLine, Dictionary<StringHash32, string> inConsts, char[] inNoAllocHelper, bool inbSkipFirst)
        {
            if (inConsts == null || inConsts.Count == 0 || ioLine.Length == 0)
            {
                return;
            }

            int offset = 0;
            if (inbSkipFirst && ioLine[0] == '$')
            {
                offset = 1;
            }

            char c;
            for (; offset < ioLine.Length; offset++)
            {
                c = ioLine[offset];
                if (c == '$')
                {
                    int start = offset + 1;
                    int end = start;
                    while (end < ioLine.Length)
                    {
                        if (IsTokenEndCharacter(ioLine[end]))
                            break;

                        end++;
                    }

                    StringBuilderSlice constId = new StringBuilderSlice(ioLine, start, end - start);
                    StringHash32 constHash = StringHash32.Fast(constId);

                    string constValue;
                    if (inConsts.TryGetValue(constHash, out constValue))
                    {
                        int remainingIdx = offset + constId.Length + 1;
                        int remainingLength = ioLine.Length - remainingIdx;
                        ioLine.CopyTo(remainingIdx, inNoAllocHelper, 0, remainingLength);
                        ioLine.Length = offset;
                        ioLine.Append(constValue);
                        ioLine.Append(inNoAllocHelper, 0, remainingLength);
                        //ioLine.Remove(offset, constId.Length + 1);
                        //ioLine.Insert(offset, constValue);
                        offset--;
                    }
                    else
                    {
                        offset = end - 1;
                    }
                }
            }
        }

        static private bool IsValidSimpleToken(StringSlice inToken)
        {
            for (int i = 0; i < inToken.Length; i++)
            {
                if (IsTokenEndCharacter(inToken[i]))
                    return false;
            }

            return true;
        }

        static private bool IsTokenEndCharacter(char inChar)
        {
            if (char.IsWhiteSpace(inChar))
                return true;
            if (char.IsLetterOrDigit(inChar))
                return false;
            switch (inChar)
            {
                case '_':
                case '-':
                case '.':
                    return false;

                default:
                    return true;
            }
        }

        [MethodImpl(256)]
        static private bool HasFlag(LeafCompilerFlags inFlags, LeafCompilerFlags inMask)
        {
            return (inFlags & inMask) == inMask;
        }

        #endregion // Utilities
    }

    /// <summary>
    /// Compiler flags.
    /// </summary>
    [Flags]
    public enum LeafCompilerFlags : uint
    {
        // Enables debug output
        Debug = 0x01,

        // Validates read/write of variables
        Validate_LoadStore = 0x02,

        // Validates method invocation
        Validate_MethodInvocation = 0x04,

        // Validates references to other nodes 
        Validate_NodeRef = 0x08,

        // Generates a NoOp at node boundaries
        Generate_NoOpBoundary = 0x10,

        // Collapses sequential content lines automatically
        Parse_CollapseContent = 0x20,

        // Dumps module memory stats when module compilation is completed
        Dump_Stats = 0x40,

        // Dumps module disassembly when module compilation is completed
        Dump_Disassembly = 0x80,

        // Treats missing instance methods as errors
        Validate_InstanceMethodStrict = 0x100,

        // Preserves all custom line names as strings
        Preserve_CustomLineNameStrings = 0x200,

        Default_Development = Debug | Validate_NodeRef | Validate_MethodInvocation | Generate_NoOpBoundary,
        Default_Strict = Debug | Validate_NodeRef | Validate_MethodInvocation | Validate_InstanceMethodStrict | Validate_LoadStore | Generate_NoOpBoundary,
        Default_Release = 0
    }

    /// <summary>
    /// Mask of all error/warning types.
    /// </summary>
    [Flags]
    public enum LeafCompilerErrorMask : ushort
    {
        // Variable is read but not written
        ReadOnlyVariable = 0x01,

        // Variable is written but not read
        WriteOnlyVariable = 0x02,

        // Cannot recognize static method
        StaticMethodUnrecognized = 0x04,

        // Cannot recognize instance method
        InstanceMethodUnrecognized = 0x08,

        // Cannot recognize node id
        NodeUnrecognized = 0x10,

        // Cannot recognize local node id
        LocalNodeUnrecognized = 0x20,

        // Block parser error
        BlockParserError = 0x40,
    }
}