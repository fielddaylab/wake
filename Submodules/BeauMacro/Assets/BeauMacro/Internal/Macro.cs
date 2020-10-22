/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    Macro.cs
 * Purpose: Macro implementation.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeauMacro
{
    /// <summary>
    /// Chain of instructions for assembling a string.
    /// </summary>
    [Serializable]
    internal class Macro
    {
        [SerializeField]
        public OpCode[] Operations;
        [SerializeField]
        public int[] Args;

        public Macro() { }

        public Macro(CompilerInstruction inInstruction)
        {
            Operations = new OpCode[] { inInstruction.Operation };
            if (inInstruction.IntArg.HasValue)
                Args = new int[] { inInstruction.IntArg.Value };
        }

        public Macro(CompilerInstruction[] inInstructions)
        {
            Operations = new OpCode[inInstructions.Length];
            for (int i = inInstructions.Length - 1; i >= 0; --i)
            {
                Operations[i] = inInstructions[i].Operation;
                if (inInstructions[i].IntArg.HasValue)
                {
                    if (Args == null)
                        Args = new int[i + 1];
                    Args[i] = inInstructions[i].IntArg.Value;
                }
            }
        }

        public void GetReferencedStringIds(ref List<int> ioStringIDs)
        {
            for (int i = 0; i < Operations.Length; ++i)
            {
                switch (Operations[i])
                {
                    case OpCode.AppendString:
                    case OpCode.AppendStringDirect:
                        ioStringIDs.Add(Args[i]);
                        break;
                }
            }
        }
    }
}