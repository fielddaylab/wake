---
pinned: true
---
# Leaf - Writer's Guide

Welcome to the Leaf Writer's Guide! This document is intended for writers working with the Leaf scripting language to implement content.

### What is Leaf?

**Leaf** is a narrative scripting language for Unity3D. Its focus is on clear and concise scripting of narrative content, with powerful features for flow control and project-specific integration.

Similar to contemporary narrative languages like Twine or Yarn, it's built on the concept of **Nodes**, which are chunks of content connected by **Transitions**. Nodes are bundled together into **Packages**, which are generated from `.leaf` files.

**Node**
: The fundamental organizational unit of content in Leaf. It contains content, which can be a mix of dialog, choices, commands, comments, and control flow statements.

**Transition**
: A connection from one Node to another Node. Transitions are the result of choices, as well as `$goto` and `$branch` commands.

**Package**
: A bundle of Nodes. This is the file-level organizational unit of content in Leaf.

### Basics

#### Creating your first Node

### Flow Control

### Variables



### Advanced Features

#### Line Continuations

Leaf is generally processed line-by-line. However, there may be points where you need to insert line breaks within your content. While you have the option of inserting a line break with a `\n` sequence, that neither scales well nor is it easily readable. A **Line Continuation** allows your line to extend to multiple lines in your document, effectively joining several visual lines with line breaks into one processed line.

You can specify a Line Continuation by adding a `\` character to the end of your visual line. You can chain this together on multiple successive lines to join sequences of visual lines together.

```
This is a regular line.
This is one line...\n
And this is another line.
This is a several-part line. This is the first line... \
This is the second line... \
And this is the third line.
This line is not part of the three-part line preceding it.
```

#### Substitutions

#### Constants

#### Macros

### Project Integration

As Leaf is designed to run within a Unity3D project, developers can enhance Leaf for writers with custom attributes, variable tables, text tags, and methods. This can allow you to implement project-specific features.

#### Custom Attributes

Nodes can be annotated with custom **Attributes** (sometimes referred to as **meta tags**). These are specified between the node id and the node content. Together, the node id and the attributes are known as the **Node Header**.

You can specify an attributes by beginning a line with the `@` symbol, followed by the name of the attribute. Depending on your project, these attributes may have values, which you can specify by putting a list of arguments after the attribute name.

```
:: SomeNodeId
@ThisIsAnAttributeName
@ YouCanHaveWhitespaceBeforeTheAttributeName
@ ThisIsAnotherAttribute WithAnArgument
@ MultiArgumentAttribute ArgumentA, ArgumentB, ArgumentC
And now some regular content
```

These attributes can be used for multiple purposes depending on your project. They may specify conditions under which the node can execute, some visuals to invoke when running the node, whether or not the node can run multiple times, who the default speaker is for the node, and more.

For a list of attributes for your project, refer to your project documentation or talk to a developer for your project.

### Reference...

#### Choices

| **Name**  | **Arguments**     | **Description**                                                                                                                                                                                        | **Argument Description**                                                                                                                                                                   |
|-----------|-------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `$choice` |                   |                                                                                                                                                                                                        |                                                                                                                                                                                            |
| `$data`   | `id [= valueExp]` | Adds additional metadata to the previous choice.                                                                                                                                                       | `id`: Metadata id. <br/> `valueExp`: Optional expression for the metadata value. If not specified, `true` will be used.                                                                    |
| `$answer` |                   |                                                                                                                                                                                                        |                                                                                                                                                                                            |
| `$choose` | `[mode]`          | Presents all choices (specified by `$choice`) to the player and asks them to select. This will be implicitly executed if there are previously unhandleed `$choice` statements at the end of your node. | `mode`: How to handle the player's choice. `goto` behaves like a `$goto` statement, `branch` behaves like a `$branch` statement, and `continue` will discard the player's choice entirely. |

#### Control Flow

| **Name**  | **Arguments**               | **Description**                                                                                                                                                                                                                                      | **Argument Description**                                                                       |
|-----------|-----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `$goto`   | `nodeExp, [conditionalExp]` | Stops execution of the current node and starts executing the given node. If a conditional expression is provided, this will only execute if those conditions are met.                                                                                | `nodeExp`: Node identifier expression <br/> `conditionalExp`: Optional conditional requirement |
| `$branch` | `nodeExp, [conditionalExp]` | Pauses execution of the current node and starts executing the given node. Execution will return to the current node once the branched node is complete. If a conditional expression is provided, this will only execute if those conditions are met. | `nodeExp`: Node identifier expression <br/> `conditionalExp`: Optional conditional requirement |

#### Multithreading

| **Name** | **Arguments**               | **Description**                                                                                                                                                                                                                                        | **Argument Descriptions**                                                                      |
|----------|-----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `$start` | `nodeExp, [conditionalExp]` | Starts executing a separate thread with the given node id. This thread will inherit the local parameters for the current thread. If a conditional expression is provided, this will only execute if those conditions are met.                          | `nodeExp`: Node identifier expression <br/> `conditionalExp`: Optional conditional requirement |
| `$fork`  | `nodeExp, [conditionalExp]` | Starts executing a child thread with the given node id. This thread inherits local parameters from its parent, but will stop when the parent thread ends. If a conditional expression is provided, this will only execute if those conditions are met. | `nodeExp`: Node identifier expression <br/> `conditionalExp`: Optional conditional requirement |
| `$join`  | N/a                         | Waits for all child threads (spawned by `$fork`) to finish before continuing. This will be implicitly executed if there are previously un-joined `$fork` statements at the end of your node.                                                           | N/a                                                                                            |