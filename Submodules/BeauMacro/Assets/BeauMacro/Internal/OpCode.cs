/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    OpCode.cs
 * Purpose: Instruction codes.
 */

namespace BeauMacro
{
    internal enum OpCode : byte
    {
        // String management
        PushString, // Pushes a string on top of the stack (arg: string id) (push)
        AppendString, // Appends the string from the stack (pop)
        AppendStringDirect, // Appends the string (arg: string id)

        // Decoding macros
        CallMacro, // Calls the macro from the stack (pop, push)
        CallMacroDirect, // Calls the macro from the given argument (arg: macro id) (push)
        AppendMacroDirect, // Calls the macro from the given argument and appends it (arg: macro id)
        
        // Branching
        JumpIfTrue, // Jumps to the given label if the string on top of the stack is empty (arg: index) (pop)
        JumpIfFalse, // Jumps to the given label if the string on top of the stack is not empty (arg: index) (pop)
        Jump, // Jumps to the given index (arg: index)
    }
}
