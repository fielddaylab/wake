/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafOpcode.cs
 * Purpose: Opcodes for leaf execution.
 */

using BeauUtil;

namespace Leaf.Runtime
{
    /// <summary>
    /// Enumeration for small operations.
    /// </summary>
    internal enum LeafOpcode : byte
    {
        // TEXT

        // Runs a text line
        // Args: StringHash32 textId
        // Size: 5
        RunLine,

        // EXPRESSIONS

        // Evaluates a single expression and pushes the value to the stack
        // Args: uint expressionIndex
        // Stack: push Variant value
        // Size: 5
        EvaluateSingleExpression,

        // Evaluates a set of expressions and pushes the logical AND of their results onto the stack
        // Args: uint expressionOffset, ushort expressionCount
        // Stack: push bool logicalResult
        // Size: 7
        EvaluateExpressionsAnd,

        // Evaluates a set of expressions and pushes the logical OR of their results onto the stack
        // Args: uint expressionOffset, ushort expressionCount
        // Stack: push bool logicalResult
        // Size: 7
        EvaluateExpressionsOr,

        // Evaluates a group of expressions and pushes the logical combination of their results onto the stack
        // Args: uint expressionOffset, ushort expressionCount
        // Stack: push bool logicalResult
        // Size: 7
        EvaluateExpressionsGroup,

        // INVOCATIONS

        // Invokes a method call dynamically parsing args from a string
        // Args: StringHash32 callId, uint stringIndex
        // Size: 9
        Invoke_Unoptimized,

        // Invokes a method call for a specific target dynamically parsing args from a string
        // Args: StringHash32 callId, uint argsIndex
        // Stack: pop StringHash32 targetId
        // Size: 9
        InvokeWithTarget_Unoptimized,

        // Invokes a method call dynamically parsing args from a string and pushes the return value to the stack 
        // Args: StringHash32 callId, uint argsIndex
        // Stack: push Variant returnValue
        // Size: 9
        InvokeWithReturn_Unoptimized,

        // Invokes a method call popping args from the stack
        // Args: StringHash32 callId, ushort argsCount
        // Stack: pop [argsCount]
        // Size: 7
        Invoke,

        // Invokes a method call for a specific target popping args from the stack
        // Args: StringHash32 callId, ushort argsCount
        // Stack: pop [argsCount], pop StringHash32 targetId
        // Size: 7
        InvokeWithTarget,

        // Invokes a method call popping args from the stack and pushing the return value to the stack
        // Args: StringHash32 callId, ushort argsCount
        // Stack: pop [argsCount], push Variant returnValue
        // Size: 7
        InvokeWithReturn,

        // STACK

        // Pushes a value onto the stack
        // Args: Variant value
        // Stack: push Variant value
        // Size: 6
        PushValue,

        // Pops a value from the stack and discards it
        // Stack: pop Variant value
        // Size: 1
        PopValue,

        // Duplicates the value at the top of the stack
        // Stack: read Variant value, push Variant value
        // Size: 1
        DuplicateValue,

        // MEMORY

        // Loads a value from the table to the stack
        // Args: TableKeyPair tableKey
        // Stack: push Variant value
        // Size: 9
        LoadTableValue,

        // Stores a value to the table to the stack
        // Args: TableKeyPair tableKey
        // Stack: pop Variant value
        // Size: 9
        StoreTableValue,

        // Increments a value in the table by 1
        // Args: TableKeyPair tableKey
        // Size: 9
        IncrementTableValue,

        // Decrements a value in the table by 1
        // Args: TableKeyPair tableKey
        // Size: 9
        DecrementTableValue,

        // ARITHMETIC

        // Adds the top two values from the stack and pushes the result
        // Stack: pop Variant value (x2), push Variant value
        // Size: 1
        Add,

        // Subtracts the top two values from the stack and pushes the result
        // Stack: pop Variant value (x2), push Variant value
        // Size: 1
        Subtract,

        // Multiplies the top two values from the stack and pushes the result
        // Stack: pop Variant value (x2), push Variant value
        // Size: 1
        Multiply,

        // Divides the top two values from the stack and pushes the result
        // Stack: pop Variant value (x2), push Variant value
        // Size: 1
        Divide,

        // LOGICAL OPERATORS

        // Inverts the boolean value of the top value from the stack and pushes the result
        // Stack: pop Variant value, push bool value
        // Size: 1
        Not,

        // Casts the top value on the stack to a bool and pushes the result
        // Stack: pop Variant value, push bool value
        // Size: 1
        CastToBool,

        // Determines if the value beneath the top stack value is less than the top stack value, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        LessThan,

        // Determines if the value beneath the top stack value is less than or equal to the top stack value, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        LessThanOrEqualTo,

        // Determines if the top two values in the stack are equal, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        EqualTo,

        // Determines if the top two values in the stack are not equal, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        NotEqualTo,

        // Determines if the value beneath the top stack value is greater than or equal to the top stack value, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        GreaterThanOrEqualTo,

        // Determines if the value beneath the top stack value is greater than the top stack value, and pushes the result.
        // Stack: pop Variant value (x2), push bool value
        // Size: 1
        GreaterThan,

        // JUMPS
        
        // Unconditionally moves the program counter
        // Args: short programCounterDisplacement
        // Size: 3
        Jump,

        // Pops the top value and moves program counter if the value is false
        // Args: short programCounterDisplacement
        // Stack: pop bool value
        // Size: 3
        JumpIfFalse,

        // Unconditionally moves the program counter by a value on the stack
        // Stack: pop short programCounterDisplacement
        // Size: 1
        JumpIndirect,

        // FLOW CONTROL

        // Exits the current node frame and enters the given node
        // Args: StringHash32 nodeId
        // Size: 5
        GotoNode,

        // Exits the current node frame and enters the node with the id on the stack
        // Stack: pop StringHash32 nodeId
        // Size: 1
        GotoNodeIndirect,

        // Enters the given node
        // Args: StringHash32 nodeId
        // Size: 5
        BranchNode,

        // Enters the node with the id on the stack
        // Stack: pop StringHash32 nodeId
        // Size: 1
        BranchNodeIndirect,

        // Exits the current node frame
        // Size: 1
        ReturnFromNode,

        // Stops executing the thread
        // Size: 1
        Stop,

        // Restarts the current node frame
        // Size: 1
        Loop,

        // Yields processing to another thread or code
        // Size: 1
        Yield,

        // No operation
        // Size: 1
        NoOp,

        // FORKING

        // Forks a child thread that runs the given node id
        // Args: StringHash32 nodeId
        // Size: 5
        ForkNode,

        // Forks a child thread that runs the node id on the stack
        // Stack: pop StringHash32 nodeId
        // Size: 1
        ForkNodeIndirect,

        // Forks an untracked thread that runs the given node id
        // Args: StringHash32 nodeId
        // Size: 5
        ForkNodeUntracked,

        // Forks an untracked thread that runs the node id on the stack
        // Stack: pop StringHash32 nodeId
        // Size: 1
        ForkNodeIndirectUntracked,

        // Waits for all tracked forked threads to complete
        // Size: 1
        JoinForks,

        // CHOICES
        
        // Adds an option to an upcoming choice
        // Args: StringHash32 textId, LeafChoice.OptionFlags flags
        // Stack: pop bool available, pop StringHash32 nodeId
        // Size: 6
        AddChoiceOption,

        // Adds a suboption selector to the last provided choice
        // Args: StringHash32 answerId
        // Stack: pop StringHash32 nodeId
        // Size: 5
        AddChoiceAnswer,

        // Adds choice data
        // Args: StringHash32 dataId
        // Stack: pop Variant dataValue
        AddChoiceData,

        // Shows any options set for the last known choice and flushes the choice
        // Stack: push Variant selectedTarget
        // Size: 1
        ShowChoices
    }
}