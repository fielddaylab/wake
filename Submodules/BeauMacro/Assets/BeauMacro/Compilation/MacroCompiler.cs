/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroCompiler.cs
 * Purpose: Macro compiler. Compiles macros into optimized instructions.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BeauMacro
{
    /// <summary>
    /// Compiles macros into a CompiledMacroBank.
    /// </summary>
    public sealed class MacroCompiler
    {
        // Valid whole statements
        // ?<if MACRO>
        // ?<if not MACRO>
        // ?<else>
        // ?<elif MACRO>
        // ?<elif not MACRO>
        // ?<endif>
        // $(MACRO)
        // #(MACRO_ID)
        // LITERAL

        #region Types

        private struct RegexRule
        {
            public Regex Find;
            public string Replace;

            public RegexRule(string inFind, string inReplace)
            {
                Find = new Regex(inFind, RegexOptions.Compiled);
                Replace = inReplace;
            }
        }

        private enum TokenType
        {
            BEGIN_TAG, // ?(

            IF, // if
            ELSE, // else
            ELIF, // elseif
            ENDIF, // endif
            NOT, // not

            BEGIN_MACRO, // $(
            BEGIN_MACRO_ID, // #(
            END_MACRO_OR_TAG, // )

            LITERAL, // String
            MACRO, // String,
            ID, // String
        }

        private struct Token
        {
            public readonly TokenType Type;
            public readonly string Value;

            public Token(TokenType inType, string inValue = null)
            {
                Type = inType;
                Value = inValue;
            }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(Value))
                    return Type.ToString();

                return Type.ToString() + " " + Value;
            }
        }

        private class SequenceLinker
        {
            public int PrevPointer;
            public List<int> PointersToEnd;

            public SequenceLinker()
            {
                PrevPointer = -1;
                PointersToEnd = new List<int>();
            }

            public void Begin()
            {
                PrevPointer = -1;
                PointersToEnd.Clear();
            }

            public void Advance(int inCurrent, ref List<CompilerInstruction> ioInstructions)
            {
                CompilerInstruction instruction = ioInstructions[PrevPointer];
                instruction.IntArg = inCurrent;
                ioInstructions[PrevPointer] = instruction;
                PrevPointer = -1;
            }

            public void PointToNext(int inCurrent)
            {
                PrevPointer = inCurrent;
            }

            public void PointToEnd(int inCurrent)
            {
                PointersToEnd.Add(inCurrent);
            }

            public void End(int inEnd, ref List<CompilerInstruction> ioInstructions)
            {
                if (PrevPointer != -1)
                {
                    Advance(inEnd, ref ioInstructions);
                    PrevPointer = -1;
                }

                for (int i = 0; i < PointersToEnd.Count; ++i)
                {
                    CompilerInstruction instruction = ioInstructions[PointersToEnd[i]];
                    instruction.IntArg = inEnd;
                    ioInstructions[PointersToEnd[i]] = instruction;
                }
            }
        }

        #endregion // Types

        // Settings
        private readonly IMacroCompilerConfig m_Config;
        private readonly List<RegexRule> m_RegexRules;

        // Macro State
        private List<Token> m_Tokens = new List<Token>(16);
        private List<CompilerInstruction> m_Instructions = new List<CompilerInstruction>(16);
        private Stack<SequenceLinker> m_SequenceLinkers = new Stack<SequenceLinker>();
        private StringBuilder m_StringBuilder = new StringBuilder(256);

        // Output State
        private Dictionary<int, Macro> m_MacroDictionary = new Dictionary<int, Macro>(256);
        private List<string> m_StringTable = new List<string>(5000) { };
        private HashSet<string> m_KnownMacros = new HashSet<string>();
        private HashSet<string> m_UnknownMacros = new HashSet<string>();
        private HashSet<int> m_KnownMacroHashes = new HashSet<int>();
        private HashSet<int> m_UnknownMacroHashes = new HashSet<int>();
        private HashSet<string> m_CompiledMacroNames = new HashSet<string>();
        private int m_StringReuseCounter = 0;

        private ICollection<MacroBank> m_LinkedBanks;

        public MacroCompiler(IMacroCompilerConfig inConfig)
        {
            m_Config = inConfig;
            if (m_Config != null && inConfig.RegexReplace != null)
            {
                m_RegexRules = new List<RegexRule>();
                foreach (var rule in inConfig.RegexReplace)
                {
                    RegexRule compiledRule = new RegexRule(rule.Key, rule.Value);
                    m_RegexRules.Add(compiledRule);
                }
            }
        }

        #region Compilation

        private void CompileEntry(string inKey, string inString)
        {
            ClearCompileState();

            SetMacroKnown(inKey);

            m_StringBuilder.Append(inString);

            ReplaceRules(m_StringBuilder);
            ParseTokens(m_StringBuilder, ref m_Tokens);
            CompileInstructions(m_Tokens, ref m_Instructions, ref m_SequenceLinkers);

            int keyHash = MacroUtil.Hash(inKey);
            Macro macro = new Macro(m_Instructions.ToArray());
            m_MacroDictionary.Add(keyHash, macro);

            m_CompiledMacroNames.Add(inKey);

            ClearCompileState();
        }

        private void ClearCompileState()
        {
            m_StringBuilder.Length = 0;
            m_Tokens.Clear();
            m_Instructions.Clear();
            m_SequenceLinkers.Clear();
        }

        private void ReplaceRules(StringBuilder ioString)
        {
            if (m_Config != null)
            {
                m_Config.PreReplace(ioString);

                var replace = m_Config.SimpleReplace;
                if (replace != null)
                {
                    foreach (var keyValue in replace)
                    {
                        ioString.Replace(keyValue.Key, keyValue.Value);
                    }
                }

                if (m_RegexRules != null)
                {
                    string str = ioString.ToString();
                    foreach (var rule in m_RegexRules)
                    {
                        str = rule.Find.Replace(str, rule.Replace);
                    }
                    ioString.Length = 0;
                    ioString.Append(str);
                }

                m_Config.PostReplace(ioString);
            }
        }

        private void ParseTokens(StringBuilder inString, ref List<Token> ioTokens)
        {
            int length = inString.Length;
            int literalStartPos = 0;
            int tagDepth = 0;

            for (int i = 0; i < length; ++i)
            {
                char c = inString[i];

                // start token
                if (Match(inString, "$(", i))
                {
                    if (tagDepth == 0)
                    {
                        if (literalStartPos != i)
                            ioTokens.Add(new Token(TokenType.LITERAL, inString.ToString(literalStartPos, i - literalStartPos)));
                        literalStartPos = i + 2;

                        ioTokens.Add(new Token(TokenType.BEGIN_MACRO));
                        ++i;
                        ++tagDepth;
                    }
                }
                else if (Match(inString, "#(", i))
                {
                    if (tagDepth == 0)
                    {
                        if (literalStartPos != i)
                            ioTokens.Add(new Token(TokenType.LITERAL, inString.ToString(literalStartPos, i - literalStartPos)));
                        literalStartPos = i + 2;

                        ioTokens.Add(new Token(TokenType.BEGIN_MACRO_ID));
                        ++i;
                        ++tagDepth;
                    }
                }
                else if (Match(inString, ")", i))
                {
                    if (tagDepth > 0)
                    {
                        if (literalStartPos != i)
                        {
                            switch (ioTokens[ioTokens.Count - 1].Type)
                            {
                                case TokenType.BEGIN_MACRO_ID:
                                    ioTokens.Add(new Token(TokenType.ID, inString.ToString(literalStartPos, i - literalStartPos).Trim()));
                                    break;

                                default:
                                    ioTokens.Add(new Token(TokenType.MACRO, inString.ToString(literalStartPos, i - literalStartPos).Trim()));
                                    break;
                            }
                        }
                        literalStartPos = i + 1;

                        ioTokens.Add(new Token(TokenType.END_MACRO_OR_TAG));
                        --tagDepth;
                    }
                }
                else if (Match(inString, "?(", i))
                {
                    if (tagDepth == 0)
                    {
                        if (literalStartPos != i)
                            ioTokens.Add(new Token(TokenType.LITERAL, inString.ToString(literalStartPos, i - literalStartPos)));
                        literalStartPos = i + 2;

                        ioTokens.Add(new Token(TokenType.BEGIN_TAG));
                        ++i;
                        ++tagDepth;
                    }
                }
                else if (Match(inString, "if", i, false))
                {
                    if (tagDepth > 0 && ioTokens[ioTokens.Count - 1].Type == TokenType.BEGIN_TAG)
                    {
                        literalStartPos = i + 2;

                        ioTokens.Add(new Token(TokenType.IF));
                        ++i;
                    }
                }
                else if (Match(inString, "not", i, false))
                {
                    if (tagDepth > 0)
                    {
                        TokenType lastToken = ioTokens[ioTokens.Count - 1].Type;
                        if (lastToken == TokenType.IF || lastToken == TokenType.ELIF)
                        {
                            literalStartPos = i + 3;

                            ioTokens.Add(new Token(TokenType.NOT));
                            i += 2;
                        }
                    }
                }
                else if (Match(inString, "elif", i, false))
                {
                    if (tagDepth > 0 && ioTokens[ioTokens.Count - 1].Type == TokenType.BEGIN_TAG)
                    {
                        literalStartPos = i + 4;

                        ioTokens.Add(new Token(TokenType.ELIF));
                        i += 3;
                    }
                }
                else if (Match(inString, "else", i, false))
                {
                    if (tagDepth > 0 && ioTokens[ioTokens.Count - 1].Type == TokenType.BEGIN_TAG)
                    {
                        literalStartPos = i + 4;

                        ioTokens.Add(new Token(TokenType.ELSE));
                        i += 3;
                    }
                }
                else if (Match(inString, "endif", i, false))
                {
                    if (tagDepth > 0 && ioTokens[ioTokens.Count - 1].Type == TokenType.BEGIN_TAG)
                    {
                        literalStartPos = i + 5;

                        ioTokens.Add(new Token(TokenType.ENDIF));
                        i += 4;
                    }
                }
                else if (Match(inString, "\r", i, false))
                {
                    inString.Remove(i, 1);
                    --i;
                    --length;
                }
            }

            if (literalStartPos != length)
            {
                ioTokens.Add(new Token(TokenType.LITERAL, inString.ToString(literalStartPos, length - literalStartPos)));
            }

            if (tagDepth > 0)
                throw new FormatException("Syntax error - unclosed tags or macros");
        }

        private void CompileInstructions(List<Token> inTokens, ref List<CompilerInstruction> ioInstructions, ref Stack<SequenceLinker> ioSequences)
        {
            for (int i = 0; i < inTokens.Count; ++i)
            {
                Token token = inTokens[i];
                switch (token.Type)
                {
                    case TokenType.LITERAL:
                        {
                            int existingIndex = m_StringTable.IndexOf(token.Value);
                            if (existingIndex < 0)
                            {
                                existingIndex = m_StringTable.Count;
                                m_StringTable.Add(token.Value);
                            }
                            else
                            {
                                ++m_StringReuseCounter;
                            }
                            ioInstructions.Add(new CompilerInstruction(OpCode.AppendStringDirect, existingIndex));
                        }
                        break;

                    case TokenType.BEGIN_MACRO:
                    case TokenType.BEGIN_MACRO_ID:
                        {
                            ++i;
                            CompileMacro(inTokens, ref ioInstructions, ref i);
                        }
                        break;

                    case TokenType.BEGIN_TAG:
                        {
                            ++i;
                            CompileTag(inTokens, ref ioInstructions, ref ioSequences, ref i);
                        }
                        break;
                }
            }

            if (ioSequences.Count > 0)
                throw new FormatException("Syntax error - unclosed tag scopes");
        }

        private void CompileMacro(List<Token> inTokens, ref List<CompilerInstruction> ioInstructions, ref int ioPtr)
        {
            for (; ioPtr < inTokens.Count; ++ioPtr)
            {
                Token token = inTokens[ioPtr];
                switch (token.Type)
                {
                    case TokenType.MACRO:
                        {
                            // We can combine the CallMacroDirect and AppendString opcodes
                            // since in the context of a string macro they usually go together

                            int macroHash = MacroUtil.Hash(token.Value);
                            ioInstructions.Add(new CompilerInstruction(OpCode.AppendMacroDirect, macroHash));

                            SetMacroReferenced(token.Value);
                        }
                        break;

                    case TokenType.ID:
                        {
                            int macroHash = int.Parse(token.Value);
                            ioInstructions.Add(new CompilerInstruction(OpCode.AppendMacroDirect, macroHash));

                            SetMacroReferenced(macroHash);
                        }
                        break;

                    case TokenType.END_MACRO_OR_TAG:
                        {
                            // AppendString used to be here
                        }
                        return;
                }
            }
        }

        private void CompileTag(List<Token> inTokens, ref List<CompilerInstruction> ioInstructions, ref Stack<SequenceLinker> ioSequences, ref int ioPtr)
        {
            bool bTruthMode = true;

            for (; ioPtr < inTokens.Count; ++ioPtr)
            {
                Token token = inTokens[ioPtr];
                switch (token.Type)
                {
                    case TokenType.IF:
                        {
                            SequenceLinker linker = new SequenceLinker();
                            ioSequences.Push(linker);
                            linker.Begin();
                        }
                        break;

                    case TokenType.NOT:
                        {
                            bTruthMode = !bTruthMode;
                        }
                        break;

                    case TokenType.ELIF:
                    case TokenType.ELSE:
                        {
                            SequenceLinker linker = ioSequences.Peek();
                            ioInstructions.Add(new CompilerInstruction(OpCode.Jump, -1));
                            linker.PointToEnd(ioInstructions.Count - 1);

                            linker.Advance(ioInstructions.Count, ref ioInstructions);
                        }
                        break;

                    case TokenType.ENDIF:
                        {
                            SequenceLinker linker = ioSequences.Pop();
                            linker.End(ioInstructions.Count, ref ioInstructions);
                        }
                        break;

                    case TokenType.MACRO:
                        {
                            SetMacroReferenced(token.Value);

                            int macroHash = MacroUtil.Hash(token.Value);
                            ioInstructions.Add(new CompilerInstruction(OpCode.CallMacroDirect, macroHash));

                            if (bTruthMode)
                                ioInstructions.Add(new CompilerInstruction(OpCode.JumpIfFalse, -1));
                            else
                                ioInstructions.Add(new CompilerInstruction(OpCode.JumpIfTrue, -1));

                            SequenceLinker linker = ioSequences.Peek();
                            linker.PointToNext(ioInstructions.Count - 1);
                        }
                        break;

                    case TokenType.ID:
                        {
                            int macroHash = int.Parse(token.Value);
                            SetMacroReferenced(macroHash);

                            ioInstructions.Add(new CompilerInstruction(OpCode.CallMacroDirect, macroHash));

                            if (bTruthMode)
                                ioInstructions.Add(new CompilerInstruction(OpCode.JumpIfFalse, -1));
                            else
                                ioInstructions.Add(new CompilerInstruction(OpCode.JumpIfTrue, -1));

                            SequenceLinker linker = ioSequences.Peek();
                            linker.PointToNext(ioInstructions.Count - 1);
                        }
                        break;

                    case TokenType.END_MACRO_OR_TAG:
                        { }
                        return;
                }
            }
        }

        #endregion // Compilation

        #region Output

        /// <summary>
        /// Compiles the given source into a bank.
        /// </summary>
        /// <param name="inSource">Source for entries.</param>
        /// <param name="inLinks">Optional collection of banks to use when linking.</param>
        public MacroCompilerResult Compile(IEnumerable<KeyValuePair<string, string>> inSource, ICollection<MacroBank> inLinks = null)
        {
            return Compile(new IEnumerableMacroCompilerSource(inSource), inLinks);
        }

        /// <summary>
        /// Compiles the given source into a bank.
        /// </summary>
        /// <param name="inSource">Source for entries.</param>
        /// <param name="inLinks">Optional collection of banks to use when linking.</param>
        public MacroCompilerResult Compile(IMacroCompilerSource inSource, ICollection<MacroBank> inLinks = null)
        {
            ClearOutputState();

            m_LinkedBanks = inLinks;

            if (m_Config != null)
            {
                if (m_Config.RecognizedMacros != null)
                {
                    foreach (var macro in m_Config.RecognizedMacros)
                        SetMacroKnown(macro);
                }
            }

            MacroCompilerResult result = new MacroCompilerResult();

            foreach (var kv in inSource.Entries)
            {
                CompileEntry(kv.Key, kv.Value);
            }

            result.Bank = new CompiledMacroBank(m_MacroDictionary, m_StringTable.ToArray());

            result.UnrecognizedMacros = ToArray(m_UnknownMacros);
            result.UnrecognizedMacroIds = ToArray(m_UnknownMacroHashes);
            result.MacroNames = ToArray(m_CompiledMacroNames);

            result.MacroCount = m_MacroDictionary.Count;
            result.StringCount = m_StringTable.Count;
            result.ReusedStrings = m_StringReuseCounter;

            ClearOutputState();

            return result;
        }

        /// <summary>
        /// Asynchronously compiles the given source into a bank.
        /// Each call to MoveNext on the iterator will compile one entry.
        /// </summary>
        /// <param name="inSource">Source for entries.</param>
        /// <param name="outResult">Target for storing compilation results.</param>
        public IEnumerator CompileAsync(IMacroCompilerSource inSource, MacroCompilerResult outResult)
        {
            return CompileAsync(inSource, null, outResult);
        }

        /// <summary>
        /// Asynchronously compiles the given source into a bank.
        /// Each call to MoveNext on the iterator will compile one entry.
        /// </summary>
        /// <param name="inSource">Source for entries.</param>
        /// <param name="inLinks">Optional collection of banks to use when linking.</param>
        /// <param name="outResult">Target for storing compilation results.</param>
        public IEnumerator CompileAsync(IMacroCompilerSource inSource, ICollection<MacroBank> inLinks, MacroCompilerResult outResult)
        {
            ClearOutputState();

            m_LinkedBanks = inLinks;

            if (m_Config != null)
            {
                if (m_Config.RecognizedMacros != null)
                {
                    foreach (var macro in m_Config.RecognizedMacros)
                        SetMacroKnown(macro);
                }
            }

            foreach (var kv in inSource.Entries)
            {
                CompileEntry(kv.Key, kv.Value);
                yield return null;
            }

            outResult.Bank = new CompiledMacroBank(m_MacroDictionary, m_StringTable.ToArray());

            outResult.UnrecognizedMacros = ToArray(m_UnknownMacros);
            outResult.UnrecognizedMacroIds = ToArray(m_UnknownMacroHashes);
            outResult.MacroNames = ToArray(m_CompiledMacroNames);

            outResult.MacroCount = m_MacroDictionary.Count;
            outResult.StringCount = m_StringTable.Count;
            outResult.ReusedStrings = m_StringReuseCounter;

            ClearOutputState();
        }

        private void ClearOutputState()
        {
            m_MacroDictionary.Clear();
            m_StringTable.Clear();
            m_KnownMacros.Clear();
            m_UnknownMacros.Clear();
            m_KnownMacroHashes.Clear();
            m_UnknownMacroHashes.Clear();
            m_CompiledMacroNames.Clear();
            m_StringReuseCounter = 0;

            m_LinkedBanks = null;
        }

        #endregion // Output

        #region Known/Unknown Macros

        private void SetMacroKnown(string inMacro)
        {
            m_KnownMacros.Add(inMacro);
            m_UnknownMacros.Remove(inMacro);

            int hash = MacroUtil.Hash(inMacro);
            m_KnownMacroHashes.Add(hash);
            m_UnknownMacroHashes.Remove(hash);
        }

        private void SetMacroReferenced(string inMacro)
        {
            int hash = MacroUtil.Hash(inMacro);

            if (!m_KnownMacros.Contains(inMacro) && !MacroContainedInLinked(hash))
                m_UnknownMacros.Add(inMacro);
        }

        private void SetMacroReferenced(int inHash)
        {
            if (!m_KnownMacroHashes.Contains(inHash) && !MacroContainedInLinked(inHash))
                m_UnknownMacroHashes.Add(inHash);
        }

        private bool MacroContainedInLinked(int inHash)
        {
            if (m_LinkedBanks == null)
                return false;

            Macro macro;
            foreach (var link in m_LinkedBanks)
            {
                if (link.TryGetMacro(inHash, out macro))
                    return true;
            }

            return false;
        }

        #endregion // Known/Unknown Macros

        #region Utils

        static private bool Match(StringBuilder inString, string inMatch, int inStartIndex, bool inbMatchCase = true)
        {
            if (inStartIndex + inMatch.Length > inString.Length)
                return false;

            if (inbMatchCase)
            {
                for (int i = 0; i < inMatch.Length; ++i)
                    if (inString[inStartIndex + i] != inMatch[i])
                        return false;
            }
            else
            {
                for (int i = 0; i < inMatch.Length; ++i)
                    if (char.ToLower(inString[inStartIndex + i]) != char.ToLowerInvariant(inMatch[i]))
                        return false;
            }

            return true;
        }

        static private bool Match(String inString, string inMatch, int inStartIndex)
        {
            if (inStartIndex + inMatch.Length > inString.Length)
                return false;

            for (int i = 0; i < inMatch.Length; ++i)
                if (inString[inStartIndex + i] != inMatch[i])
                    return false;

            return true;
        }

        static private T[] ToArray<T>(ICollection<T> inSet)
        {
            T[] array = new T[inSet.Count];
            inSet.CopyTo(array, 0);
            return array;
        }

        #endregion // Utils
    }
}