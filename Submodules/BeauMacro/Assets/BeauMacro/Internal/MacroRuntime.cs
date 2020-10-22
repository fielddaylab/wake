/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroRuntime.cs
 * Purpose: Macro evaluation logic.
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BeauMacro
{
    /// <summary>
    /// Evaluates macros.
    /// </summary>
    internal class MacroRuntime
    {
        private const int MAX_DEPTH = 8;

        private const string ERROR_STACK = "[UNKNOWN]";
        private const string ERROR_LOG = "[Macro] Unknown macro '{0}'";
        private const string ERROR_STRING_LOG = "[Macro] Unknown string '{0}'";

        private struct State
        {
            public Stack<string> Stack;
            public MacroBankSet BankSet;
            public MacroBank Context;
        }

        private Stack<string> m_SharedStack;
        private StringBuilder[] m_SharedStringBuilders;
        private int m_StringBuilderStackIndex;

        public MacroRuntime()
        {
            m_SharedStack = new Stack<string>(MAX_DEPTH);
            m_SharedStringBuilders = new StringBuilder[MAX_DEPTH];
            for (int i = 0; i < m_SharedStringBuilders.Length; ++i)
                m_SharedStringBuilders[i] = new StringBuilder(1024);
            m_StringBuilderStackIndex = m_SharedStringBuilders.Length;
        }

        private StringBuilder GetStringBuilder()
        {
            if (m_StringBuilderStackIndex <= 0)
                throw new InvalidOperationException("Macro evaluation stack exceeded maximum depth of " + MAX_DEPTH.ToString());

            StringBuilder builder = m_SharedStringBuilders[--m_StringBuilderStackIndex];
            builder.Length = 0;
            return builder;
        }

        private void ReleaseStringBuilder()
        {
            ++m_StringBuilderStackIndex;
        }

        /// <summary>
        /// Evalutes the macro with the given name and returns the result.
        /// </summary>
        /// <param name="inMacroName">Name of the macro.</param>
        /// <param name="inBankSet">Set of compiled string banks.</param>
        /// <param name="inContext">Additional compiled string bank.</param>
        public string Evaluate(string inMacroName, MacroBankSet inBankSet, MacroBank inContext, out bool outbSuccess)
        {
            int argMacro = MacroUtil.Hash(inMacroName);

            State state = new State()
            {
                Stack = m_SharedStack,
                BankSet = inBankSet,
                Context = inContext
            };

            outbSuccess = true;
            if (!TryExecuteMacro(ref state, argMacro, ref outbSuccess))
            {
                Debug.LogWarningFormat(ERROR_LOG, inMacroName);
                return ERROR_STACK;
            }

            return m_SharedStack.Pop();
        }

        /// <summary>
        /// Evalutes the macro with the given name and returns the result.
        /// </summary>
        /// <param name="inMacroId">ID of the macro.</param>
        /// <param name="inBankSet">Set of compiled string banks.</param>
        /// <param name="inContext">Additional compiled string bank.</param>
        public string Evaluate(int inMacroId, MacroBankSet inBankSet, MacroBank inContext, out bool outbSuccess)
        {
            State state = new State()
            {
                Stack = m_SharedStack,
                BankSet = inBankSet,
                Context = inContext
            };

            outbSuccess = true;
            if (!TryExecuteMacro(ref state, inMacroId, ref outbSuccess))
            {
                Debug.LogWarningFormat(ERROR_LOG, inMacroId);
                return ERROR_STACK;
            }

            return m_SharedStack.Pop();
        }

        // Executes a macro
        private void ExecuteMacro(ref State ioState, Macro inMacro, MacroBank inMacroBank, ref bool iobSuccess)
        {
            // If no instructions, just return an empty string
            if (inMacro.Operations == null || inMacro.Operations.Length == 0)
            {
                ioState.Stack.Push(string.Empty);
                return;
            }

            // We can optimize for a single instruction of certain types.
            // This means we don't have to request a StringBuilder.
            if (inMacro.Operations.Length == 1)
            {
                OpCode operation = inMacro.Operations[0];
                switch (operation)
                {
                    case OpCode.AppendStringDirect:
                        string str;
                        if (!inMacroBank.TryGetString(inMacro.Args[0], out str))
                        {
                            Debug.LogWarningFormat(ERROR_STRING_LOG, inMacro.Args[0]);
                            ioState.Stack.Push(ERROR_STACK);
                            iobSuccess = false;
                        }
                        ioState.Stack.Push(str);
                        return;

                    case OpCode.CallMacroDirect:
                        if (!TryExecuteMacro(ref ioState, inMacro.Args[0], ref iobSuccess))
                        {
                            Debug.LogWarningFormat(ERROR_LOG, inMacro.Args[0]);
                            ioState.Stack.Push(ERROR_STACK);
                        }
                        return;
                }
            }

            StringBuilder stringBuilder = GetStringBuilder();

            for (int ptr = 0; ptr < inMacro.Operations.Length; ++ptr)
            {
                OpCode operation = inMacro.Operations[ptr];
                switch (operation)
                {
                    case OpCode.PushString:
                        {
                            string str;
                            if (!inMacroBank.TryGetString(inMacro.Args[ptr], out str))
                            {
                                Debug.LogWarningFormat(ERROR_STRING_LOG, inMacro.Args[ptr]);
                                ioState.Stack.Push(ERROR_STACK);
                                iobSuccess = false;
                            }
                            ioState.Stack.Push(str);
                        }
                        break;

                    case OpCode.AppendString:
                        {
                            stringBuilder.Append(ioState.Stack.Pop());
                        }
                        break;

                    case OpCode.AppendStringDirect:
                        {
                            string str;
                            if (!inMacroBank.TryGetString(inMacro.Args[ptr], out str))
                            {
                                Debug.LogWarningFormat(ERROR_STRING_LOG, inMacro.Args[ptr]);
                                ioState.Stack.Push(ERROR_STACK);
                                iobSuccess = false;
                            }
                            stringBuilder.Append(str);
                        }
                        break;

                    case OpCode.CallMacro:
                        {
                            string macro = ioState.Stack.Pop();
                            int hash = MacroUtil.Hash(ioState.Stack.Pop());
                            if (!TryExecuteMacro(ref ioState, hash, ref iobSuccess))
                            {
                                Debug.LogWarningFormat(ERROR_LOG, macro);
                                ioState.Stack.Push(ERROR_STACK);
                            }
                        }
                        break;

                    case OpCode.CallMacroDirect:
                        {
                            if (!TryExecuteMacro(ref ioState, inMacro.Args[ptr], ref iobSuccess))
                            {
                                Debug.LogWarningFormat(ERROR_LOG, inMacro.Args[ptr]);
                                ioState.Stack.Push(ERROR_STACK);
                            }
                        }
                        break;

                    case OpCode.AppendMacroDirect:
                        {
                            if (!TryExecuteMacro(ref ioState, inMacro.Args[ptr], ref iobSuccess))
                            {
                                Debug.LogWarningFormat(ERROR_LOG, inMacro.Args[ptr]);
                                stringBuilder.Append(ERROR_STACK);
                            }
                            else
                            {
                                stringBuilder.Append(ioState.Stack.Pop());
                            }
                        }
                        break;

                    case OpCode.JumpIfTrue:
                        {
                            if (!string.IsNullOrEmpty(ioState.Stack.Pop()))
                                ptr = inMacro.Args[ptr] - 1;
                        }
                        break;

                    case OpCode.JumpIfFalse:
                        {
                            if (string.IsNullOrEmpty(ioState.Stack.Pop()))
                                ptr = inMacro.Args[ptr] - 1;
                        }
                        break;

                    case OpCode.Jump:
                        {
                            ptr = inMacro.Args[ptr] - 1;
                        }
                        break;
                }
            }

            ioState.Stack.Push(stringBuilder.ToString());
            ReleaseStringBuilder();
        }

        // Attempts to execute a macro with the given id.
        private bool TryExecuteMacro(ref State ioState, int inMacroID, ref bool iobSuccess)
        {
            Macro macro = null;
            MacroBank bank = null;
            bool foundMacro = (ioState.Context != null && (bank = ioState.Context).TryGetMacro(inMacroID, out macro)) ||
                (ioState.BankSet.Variables != null && (bank = ioState.BankSet.Variables).TryGetMacro(inMacroID, out macro)) ||
                (ioState.BankSet.LocalizationLocal != null && (bank = ioState.BankSet.LocalizationLocal).TryGetMacro(inMacroID, out macro)) ||
                (ioState.BankSet.LocalizationGlobal != null && (bank = ioState.BankSet.LocalizationGlobal).TryGetMacro(inMacroID, out macro)) ||
                (ioState.BankSet.Global != null && (bank = ioState.BankSet.Global).TryGetMacro(inMacroID, out macro));

            if (foundMacro)
                ExecuteMacro(ref ioState, macro, bank, ref iobSuccess);
            else
                iobSuccess = false;

            return foundMacro;
        }
    }
}