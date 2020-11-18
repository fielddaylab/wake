# Creating Dialogues

Creating Dialogues for the Argumentation is simple, and I will outline the key components. For some examples please check out "aqualab\Assets_Prototyping\Argumentation_Code\Graph\Dialogue3.txt"

## How the Dialogue works

The dialogue system is broken up into two main components: Nodes and Links. Nodes are what the NPC responds with and Links are what the player chooses to continue the conversation.
The basic format is that you create a node and then select optional links that will continue the dialogue. If the player selects one of these links then the next node will be responded with, otherwise an invalid node will appear and they will get to try again.
Each node and link have different settings that you must fill in and some that are optional. These are outlined below.

**NOTE**: This system is subject to change and just a prototype this is currently how the system works as of 11/10/2020

# Features of the Dialogue

## Node

There are different options you have when creating a node and these are definded below along with examples for each kind of node that you can create

### Requirements

    There are 3 nodes that must be definded at the top of the files and they are:

    # rootNodeId node.rootNodeId
    # endNodeId node.endNodeId
    # defaultInvalidNodeId node.invalid.DefaultNodeId

## Options

**Required Options**:

1. :: Id - for each node and is the id of the node. Our naming convention has been node.nodeId for nodes and node.invalid.nodeId for invalid nodes; however, the id can be anything that you decide
2. Text - This is the text that the node will display and is placed at the bottom of the node.

**Optional**:

3.  @responseIds - a list of linksIds seperated by commas that are 'correct' responses to this node and will continue the conversation. If one of these links is not chosen it will go to the default invalid node id, unless a invalid nodeid is listed.
    Note: This is not required because invalid nodes and end nodes do not need it, but it is required of your nodes that you want to have a conversation with.

4.  @invalidNodeId - an optional reference to an invalid node. If an invalid link is displayed it will display that invalid node rather than choosing the default invalid node.
5.  @nextNodeId - used for invalid nodes only, this will continue the conversation from an invalid node into another node. This allows the conversation to continue smoother and allows for better conversation.

### Node Examples

1. rootNodeId -This node is defined at the top of the file as #rootNodeId nodeId and will be the first node that appears to begin the conversation

```
:: node.nodeId
@responseIds node.firstResponse, node.secondResponse
Hello, this is the root of the conversation
```

2. endNodeId - This node is the end of your conversation and will automatically trigger the end scene.

```
:: node.endNodeId
Congrats, you have made it to the end of the conversation
```

3. defaultInvalidNode - this is a default invalid node that will be displayed if an incorrect link is chosen and there is no @invalidNodeId described for the node

```
:: node.invalid.defaultInvaidNode
Uh oh, that is not the right choice. Try again!
```

4. normalNode - This is a realatively normal node that will continue the conversation as expected, leveraging the use of all of the options

```
:: node.normalNode
@invalidNodeId node.invalid.invalidWithContinue
@responseId node.correctResponse1
Please answer correctResponse1 and the conversation will continue
```

5. invalidNodeWithNextNode - This is an invalid node that used the nextNodeId to continue the converstiaon

```
:: node.invalid.invalidWithContinue
@nextNodeId node.invaid.continueFromInvalid
That is not quite right.
```

6. continueFromInvalid - This is a continuation from node 5 and will node progress the conversation at all, and will simply display the text to keep the conversation flow going

```
:: node.invalid.continueFromInvalid
Do you have any other evidence?
```

## Link

Defining a link is in the early stages, and we are still uncertaion about the details of how these will be decided. For now I will just describe the options that currently exist and give a few examples.

**SUBJECT TO CHANGE**

## Options

**Required Options**:

1. :: Id - for each link and is the id of the link. Our naming convention has been link.linkId for link; however, the id can be anything that you decide
2. Text - This is the text that the link will display
3. @nextNodeId - Multiple of these can be described and it details the two nodes. The first node is the node that if it is chosen on it will be correct, and the second node is the node that it will continue to. (Technically not required if you want a link that will never continue the conversation)
   For example: If I am at node1 and I want link1 to take me from node1 to node2. I would describe **@nextNodeId node1, node2**
4. @tag - The tag that describes what section it is placed under. For now there are only four options and they are: **claim, behavior, model, ecosystem.**
5. @type - the type describes which center row divider it will belong to. These must be defined in the unity scene pool within the TypeManager script attached to the TypeButtons. You describe the type name and the DIsplay Label and attach a sprite that will be displayed.

**Optional Optons**:

6. @shortenedText - This is used to display a shortened version of the text when allowing the player to select the links. This will automatically switch to the text definded above when this link is selected.

### Link Examples

1. claimLink - This is a link that will be displayed before any others and will only be displayed then. Once a claim is selected currently there is no way to select a new one, but there likely will be eventually

```
:: link.claimLinkId
@tag claim
@type claim
@nextNodeId node.rootNode, node.firstNode
This is example claim link.
```

2. normalLink - This is a normal link using just the required features

```
:: link.normalLink
@tag behavior
@type otter
@nextNodeId node.firstNode, node.secondNode
@nextNodeId node.randomNode, node.nodeAfterRandomNode
Otters are cute, and behave this way.
```

3. shortenedLink - This is a link with a shortened text option

```
:: link.shortenedLink
@tag ecosystem
@type water
@nextNodeId node.thisNode, node.thatNode
@shortenedText: Water < 10* C
The water in this lake is less than 10 degrees celcius.
```
