# Scripting Reference

## Flow Control

### `$branch`
```
$branch nodeId
$branch [nodeId_expression]
$branch nodeId, condition_expression
$branch [nodeId_expression], condition_expression
```

Starts executing the given node. The current node will be resumed once the given node is completed.
If provided an additional expression argument, the instruction will only take effect if the given expression evaluates to `true`.

### `$choice`
```
$choice nodeId; content
$choice [nodeId_expression]; content
$choice nodeId, condition_expression; content
$choice [nodeId_expression], condition_expression; 
```

### `$else`

### `$elseif`

### `$endif`

### `$goto`
```
$goto nodeId
$goto [nodeId_expression]
$goto nodeId, condition_expression
$goto [nodeId_expression], condition_expression
```

Switches execution over to the given node. Progress in the current node is not recorded.
If provided an additional expression argument, the instruction will only take effect if the given expression evaluates to `true`.

### `$if`

### `$loop`
```
$loop
$loop condition_expression
```

Returns to the beginning of the current node.
If provided an expression argument, the instruction will only take effect if the given expression evaluates to `true`.

### `$return`
```
$return
$return condition_expression
```

Exits the current node, and returns to the calling node, if this was called via `$branch` by another node.
If provided an expression argument, the instruction will only take effect if the given expression evaluates to `true`.

### `$stop`
```
$stop
$stop condition_expression
```

Stops execution of the script.
If provided an expression argument, the instruction will only take effect if the given expression evaluates to `true`.

### `$yield`
Waits one frame before continuing execution.

## Script Metadata

### `@boostScore`
Boosts the score for trigger evaluation.

### `@chatter`

### `@cutscene`

### `@entrypoint`

### `@important`

### `@once`

### `@repeat`

### `@tags`

### `@trigger`

### `@triggerPriority`

### `@when`

### `@who`

## Script Events

### `{@actorId}`

### `{auto}`

### `{bgm-pitch}`

### `{bgm-stop}`

### `{bgm}`

### `{broadcast-event}`, `{broadcast}`

### `{cash}`, `{/cash}`

### `{clear}`

### `{disable-object}`

### `{enable-object}`

### `{fade-in}`

### `{fade-out}`

### `{gears}`, `{/gears}`

### `{hide-dialog}`

### `{highlight}`, `{/highlight}`

### `{input-continue}`, `{continue}`

### `{letterbox}`, `{/letterbox}`

### `{load-scene}`

### `{loc}`

### `{n}`, `{newline}`

### `{pg}`

### `{player-name}`

### `{reallySlow}`, `{/reallySlow}`

### `{screen-flash}`

### `{set}`

### `{show-dialog}`

### `{slow}`, `{/slow}`

### `{speaker}`

### `{speed}`

### `{style}`

### `{switch-var}`

### `{trigger-response}`, `{trigger}`

### `{type}`

### `{var}`, `{var-i}`, `{var-f}`, `{var-s}`

### `{wait-abs}`

### `{wait}`

### `{wipe-in}`

### `{wipe-out}`
