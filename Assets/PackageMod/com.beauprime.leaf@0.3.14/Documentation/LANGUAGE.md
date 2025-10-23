## Leaf File

## Basics

### Choice

```
$choice node_identifier; content
$choice node_identifier, conditional_expression; content
```

A `$choice` statement establishes an option for a future `$choose` statement. The option will display with the given content. If a conditional expression is provided, the choice will only be enabled if the expression evaluates to true.

```
$choice .someNodeA; Go to Node A
$choice .someNodeB; Go to Node B
$choice .someNodeC, candlePower > 50; Go to Node C
```

### Choose

```
$choose
$choose goto
$choose branch
```

A `$choose` statement asks the user to select a choice from the options established with previously executed `$choice` statements.

By default (or if `goto` is specified), a `$choose` will go to the node specified by the selected option, stopping execution of the current node.

```
$choice .nodeA; Node A
$choice .nodeB; Node B

// ask the user to select
$choose
```

If `branch` is specified, execution of the current node will pause and the node specified by the current option will be executed. Once execution of that node is completed, execution of the current node will resume.

```
$choice .nodeA; Node A
$choice .nodeB; Node B

// ask the user to select
$choose branch

// more script to be executed
```

If no `$choose` is specified and there are unhandled `$choice` statements, this will be executed implicitly.

```
:: nodeZ

$choice .nodeA; Node A
$choice .nodeB; Node B

// this will implicitly call $choose

:: nodeY
```

### Set

```
$set assignment_expression
```

A `$set` expression will assign values to external variable storage. These variables can be later checked with conditional expressions.

### If Blocks

An `$if` block is a conditional block of script executed only if its conditional expression evaluates to true. It is closed with an `$endif` statement

```
$if someVariable > 3
    // some script if someVariable is greater than 3
$endif
```

You can also specify a separate branch to execute if the conditional expression was not true with the `$else` statement. 

```
$if anotherVariable == "foo"
    // some script if anotherVariable equals "foo"
$else
    // some script if anotherVariable is not equal to "foo"
$endif
```

You can chain these branches together with `$elseif` statements, specifying fallthrough logic and conditions.

```
$if variableThree == 3
    // some script if variableThree is equal to 3
$elseif variableThree == 2
    // some script if variableThree is equal to 2
$else
    // some script if variableThree is not equal to 2 or 3
$endif
```

## Additional Control Flow

### While Loops

A `$while` block will continue to execute as long as the expression at the beginning of the while loop evaluates to true.

```
$while someVariable < 3
    // some script to execute while someVariable < 3
$endwhile
```

Break
Continue

### Goto

```
$goto node_identifier
$goto node_identifier, conditional_expression
```

A `$goto` statement will stop execution of the current node and begin executing the given node.

If a conditional expression is specified, this will only execute if the expression evaluates to true.

### Branch

```
$branch node_identifier
$branch node_identifier, conditional_expression
```

A `$branch` statement will pause execution of the current node and begin executing the given node. Once execution of the given node is completed, execution of the current node will resume.

If a conditional expression is specified, this will only execute if the expression evaluates to true.

### Loop

```
$loop
$loop conditional_expression
```

A `$loop` statement will restart the current node from its first line.

```
:: someNode

// do something
$loop
```

If a conditional expression is specified, this will only execute if the expression evaluates to true.

```
:: someNode

// do something
$loop someCounter < 5

// something if someCounter is >= 5
```

### Stop

```
$stop
$stop conditional_expression
```

A `$stop` statement will stop executing all nodes in the current thread. Nodes paused by `$branch` or `$choose branch` statements will not resume.

If a conditional expression is specified, this will only execute if the expression evaluates to true.

### Return

```
$return
$return conditional_expression
```

A `$return` statement will stop executing the current node. Unlike `$stop`, if this node was started from a `$branch` statement, or a `$choose branch` statement, execution of that node will resume.

If a conditional expression is specified, this will only execute if the expression evaluates to true.

### Yield

```
$yield
```

A `$yield` statement will wait one frame before continuing. Useful for debugging purposes, or, in the case of loops, to control how many loops are executed in a single frame.

## Identifiers and Expressions

### Node Identifiers

Node identifiers point to another node in the leaf file, or within another leaf file. They can be specified in three ways.

| Type | Description | Example |
| ----- | -------- | ------ |
| Full | Full path of the node | `aBasePath.someSubpath.nodeId` |
| Local | Local path of the node, relative to the last `basePath` command. Must start with the `.` character | `.aSubpath.nodeId` |
| Indirect | Variable identifier. The variable must contain a full node path. Must be surrounded by the `[` and `]` characters | `[someVariableContainingANodeId]` |

### Object Identifiers

Object identifiers point to an object within the scene. They can be specified in two ways.

| Type | Description | Example |
| ----- | -------- | ------ |
| Full | Full identifier of the object | `FocusHighlight`, `GreenBlock3` |
| Indirect | Variable identifier. The variable must contain a full object identifier. Must be surrounded by the `[` and `]` characters | `[currentPlayerTarget]` |

### Conditional Expressions

### Assignment Expressions

## Advanced Features

### Call

```
$call method_name()
$call method_name(arg0, arg1, arg2, ...)
$call object_name->method_name()
$call object_name->method_name(arg0, arg1, arg2, ...)
```

The `$call` statement executes a developer-defined method in the code and waits for it to finish invoking before continuing execution of the thread. An arbitrary number of arguments can be passed into these methods, corresponding to the number of arguments supported by the corresponding code. These arguments must be separated by the `,` character.

Optionally, an object identifier can be specified along with the `->` operator, to call a developer-defined method on a specific object.

### Start

```
$start node_identifier
$start node_identifier, conditional_expression
```

The `$start` statement begins running a separate thread, spun off from the current one, to execute another node.

If a conditional expression is provided, the thread will only be started if the conditional expression evaluates to true.

```
$start .someNodeA
$start .someNodeB, bigImportantVariable == "unimportant"
```

### Fork

```
$fork node_identifier
$fork node_identifier, conditional_expression
```

The `$fork` statement behaves similarly to the `$start` statement, but the resulting thread is parented to the current thread. When the current thread is stopped, all forked threads will also be stopped.

If a conditional expression is provided, the thread will only be started if the conditional expression evaluates to true.

```
$fork .someNode1
$fork .someNode2, someVariable > 3
```

### Join

```
$join
```

The `$join` statement will wait for all threads previously set to execute with a `$fork` statement.

```
// spin off some threads
$fork .someNode1
$fork .someNode2
$fork .someNode3

// wait until the previous forked threads are completed
$join
```

Note that if there are previously un-joined `$fork` statements at the end of your node, this will implicitly be executed.

```
:: someNode

// spin off some threads
$fork .someNode1
$fork .someNode2
$fork .someNode3

// these will be joined automatically

:: someOtherNode
...
```