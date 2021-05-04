# Scripting Reference

## Reference

### Script Metadata

| Name | Arguments | Description | Example |
| ---- | --------- | ----------- | ------- |
| **Scripting** |
| `@entrypoint` | N/a | Exposes this node as an entrypoint to the script file. Accessing nodes from other script files requires those nodes be set as entrypoints. |
| `@tags [tag1], [tag2], [tag3]...` | String Tags (comma-separated) | Tags the node with the given tags. (unused?) | `@tag KevinNameConvo` |
| **Initial State** |
| `@cutscene` | N/a | Sets the node to be run as a cutscene. | `@cutscene` |
| `@chatter` | N/a | Sets the node to be run as a corner chatter, using the corner dialog box instead of the default one. | `@chatter` |
| **Triggers** |
| `@trigger [triggerId]` | Trigger Id (see trigger table) | Sets this node to be evaluated for execution when the given trigger fires | `@trigger RequestPartnerHelp`, `@trigger SceneStart` |
| `@who [charId]` | Character Id (currently only accepts `kevin`) | Sets the character this node is associated with. If that character is already executing a higher-priority node, this node cannot be executed.<br/>Note that nodes with the trigger `RequestPartnerHelp` will default to using `kevin` so this command is optional. | `@who kevin` |
| `@when [condition1], [condition2], ...` | Conditions (comma-separated) | This node can only be executed once all the given conditions are met.Note that this also sets the "specificity score" of the node based on the number of conditions. Nodes with a higher score are given a chance to execute before nodes with a lower score. | `@when scene:name == "ExperimentPrototype, !player:entered.helm` |
| `@boostScore [value]` | Score Value | Adjusts this node's specificity score. Use this to make nodes more or less likely to be triggered. | `@boostScore 2`, `@boostScore -5` |
| `@once [mode]` | Mode (optional, accepted values are `session` and empty) | This node can only run once. If `session` is specified, this node can be run again after reloading the page. | `@once`, `@once session` |
| `@repeat [duration]` | Duration (node count) | Once executed, this node cannot be run for at least the given number of other nodes executed by the scripting system. Use this to prevent unnecessary repetition of dialog. |`@repeat 5` |
| `@triggerPriority [priority]` | Priority (accepted values are `Low`, `Medium`, `High`, `Cutscene`) | Sets this node's trigger priority. If combined with `who`, If the `@who`-specified character is executing a higher-priority node, this node will be ignored.<br/>Note that `@cutscene` nodes are already considered to have `Cutscene` priority. | `@triggerPriority Medium` |
| `@ignoreDuringCutscene` | N/a | This node cannot be triggered while a cutscene is already running | `@ignoreDuringCutscene` |

### Tables

| Name | Persist | Description | Useage |
| --- | ---- | ----- | ------ |
| **Saved** | | | |
| `global` | Profile | Global table | Use for anything saved but not suitable for other saved tables |
| `jobs` | Profile | Jobs table | Use for anything related to jobs or job progress |
| `player` | Profile | Player table | Use for anything related to the player |
| `kevin` | Profile | Partner table | Use for anything related to the partner character |
| **Session** | | | |
| `session` | Session | Session table | Use for anything that needs to persist for a local session |
| **Temp** | | | |
| `temp` | Scene | Temporary table | Use for anything local to a scene that isn't specific to a certain activity. |
| `experiment` | Scene | Experiment table | Use for anything specific to the experiment scene |
| `portable` | Scene | Portable menu table | Use for anything related to the portable menu state |
| `modeling` | Scene | Modeling table | Use for anything related to the modeling scene |

**Guide to Persistence**  
**Profile**: Persists in save data (once save data is implemented)  
**Session**: Persists between scenes but not between page loads  
**Temp**: Persists only within a scene

### Built-In Variables

| Name | Description | Example
| --- | --- | --- |
| `kevin:help.requests` | Number of times the player has requested help from their partner character | `kevin:help.requests > 15` |
| **Experimentation** |
| `experiment:setup.on` | If the player has the experiment setup panel active | `!experiment:setup.on` |
| `experiment:setup.screen` | Id of the current setup screen | `experiment:setup.screen == "boot"` |
| `experiment:setup.tankType` | Id of the configured tank type | `experiment:setup.tankType == "Stressor"` |
| `experiment:setup.ecoType` | Id of the configured tank ecosystem | `experiment:setup.ecoType == "UrchinBarren"` |
| `experiment:setup.lastActorType` | Id of the last critter type to be added/removed from the tank | `experiment:setup.lastActorType == "Urchin"` |
| `experiment:running` | If the player is currently running an experiment | `experiment:running` |
| `experiment:duration` | Number of seconds the player has been running the current experiment | `experiment:duration >= 30` |
| `experiment:observedBehaviorCount` | Number of new behaviors the player observed during the current experiment | `experiment:observedBehaviorCount == 0` |
| **Modeling** |
| `modeling:hasScenario` | If a scenario could be found for modeling | `modeling:hasScenario` |
| `modeling:modelSync` | Current sync between player model and historical data | `modeling:modelSync > 50` |
| `modeling:predictSync` | Current sync between player model and target prediction | `modeling:predictSync < 25` |
| `modeling:modelPhase` | Current phase in the modeling activity (`model`, `predict`, or `complete`) | `modeling:modelPhase != "predict"` |
| **Misc** |
| `session:nav.shipRoom` | Current room id within the ship | `session:nav.shipRoom == "helm"` |
| `session:nav.diveSite` | Current dive site id | `session:nav.diveSite == "RS-1B"` |
| `temp:camera.region` | Name of the current camera focus region | `temp:camera.region == "WaterProbe"`|

### Read-Only Variables

| Name | Description | Example
| --- | ------ | ----- |
| **Location** |
| `scene:name` | Name of the current scene | `scene:name == "ExperimentPrototype"` |
| **Progress** |
| `global:actNumber` | Index of the current game act (starting from 0) | `global:actNumber == 0` |
| `player:currentJob` | Id of the player's current job | | `player:currentJob == "Job1-6"` |
| `player:currentStation` | Id of the player's current station | `player:currentStation == "Station1"` |
| `jobs:anyAvailable` | Returns the number of jobs are available and unstarted | `jobs:anyAvailable > 1`, `jobs:anyAvailable` |
| `jobs:anyInProgress` | Returns the number of jobs are in progress | `jobs:anyInProgress > 3` |
| `jobs:anyComplete` | Returns the number of jobs have been completed | `jobs:anyComplete <= 2` |
| **Random** |
| `random:common` | Returns a random bool with a **40%** chance of being true | `random:common` |
| `random:uncommon` | Returns a random bool with a **20%** chance of being true | `random:uncommon` |
| `random:rare` | Returns a random bool with a **10%** chance of being true | `random:rare` |

### Functions

| Name | Argument | Description | Example |
| ---- | -------- | ----------- | ------- |
| **Bestiary** |
| `has.entity` | Critter/Ecosystem Id | Returns if the player has a specific critter/ecosystem entry in their bestiary | `has.entity:GiantKelp` |
| `has.fact` | Fact Id | Returns if the player has a specific fact in their bestiary | `has.fact:Urchin.Eats.GiantKelp` |
| **Job** |
| `job.isStartedOrComplete` | Job Id | Returns if the given job has ever been started | `job.isStartedOrComplete:Job1-6` |
| `job.inProgress` | Job Id | Returns if the given job is in progress or active | `job.inProgress:Job1-1a` |
| `job.isComplete` | Job Id | Returns if the given job has been completed | `job.isComplete:Job1-6` |
| `job.isAvailable` | Job Id | Returns if the given job is unstarted and available at a job board. | `job.isAvailable:Job1-3a` |
| `job.taskActive` | Task Id | Returns if the current job has an active task with the given id | `job.taskActive:task1` |
| `job.taskComplete` | Task Id | Returns if the current job has a completed task with the given id | `job.taskComplete:task3.1` |
| **Inventory** |
| `has.item` | Item Id | Returns if the player has a specific item in their inventory | `has.item:SomeArtifact` |
| `item.count` | Item Id | Returns the amount of a specific item in the player's inventory | `item.count:Cash > 50`, `item.count:RareArtifactThingy < 2` |
| **Scripting** |
| `seen` | Script Node Id (Full) | Returns if the player has visited a given node (must provide the full node id, including the basePath) | `seen:partner.help.bestiary.urchins.start` |
| **Misc** |
| `scanned` | Scan Entry Id (Full) | Returns if the player has scanned something with the given entry in the observation scenes (must provide the full entry id, including the base path) | `scanned:RS-1C.probe` |

### Triggers

| Name | Arguments | Description |
| ---- | --------- | ----------- |
| **Basic** |
| `PartnerTalk` | N/a | Triggers when the player asks their partner to talk |
| `RequestPartnerHelp` | N/a | Triggers when the player asks their partner for help |
| `SceneStart` | N/a | Triggers when a scene starts |
| `InspectObject` | `objectId` | Triggers when an object is inspected with a cursor click |
| **Bestiary** |
| `BestiaryEntryAdded` | `entryId` | Triggers when a new entry is added to the bestiary |
| `BestiaryFactAdded` | `factId` | Triggers when a new fact is added to the bestiary |
| `BestiaryFactAddedToModel` | `factId` | Triggers when a fact is added to the universal model |
| **Regions** |
| `PlayerEnterRegion` | `regionId` | Triggers when the player enters a trigger region within a scene |
| `PlayerExitRegion` | `regionId` | Triggers when the player exits a trigger region within a scene |
| `ResearchSiteFound` | `siteId`, `siteHighlighted` | Triggers when the player enters within range of a dive site |
| **Experimentation** |
| `TrySubmitExperiment` | N/a | Triggers when a player attempts to start an experiment |
| `TryEndExperiment` | N/a | Triggers when the player attempts to end an experiment |
| `ExperimentFinished` | N/a | Triggers when the player completes an experiment |
| `NewBehaviorObserved` | `factId` | Triggers when the player observes a new fact during an experiment |
| `BehaviorAlreadyObserved` | `factId` | Triggers when the player attempts to observe a fact that has already been added to the bestiary |
| `ExperimentIdle` | N/a | Triggers during an experiment every 30-ish seconds when the player has not clicked on anything |
| **Modeling** |
| `UniversalModelStarted` | N/a | Triggers when the player views the universal model in modeling |
| `ModelGraphStarted` | N/a | Triggers when the player begins graphing |
| `ModelSyncedImmediate` | N/a | Triggers when the player's sync reaches 100% between their model and historical data |
| `ModelSynced` | N/a | Triggers when the player clicks the button to signal 100% sync between their model and historical data (only if moving on to prediction phase) |
| `ModelPredictImmediate` | N/a | Triggers when the player hits all prediction targets |
| `ModelCompleted` | N/a | Triggers when the player completes the modeling activity |
| **Argumentation** |
| `ArgumentationComplete` | `jobId` | Triggers when the player completes an argumentation activity |
| **Jobs** |
| `JobStarted` | `jobId` | Triggered when a new job is started |
| `JobSwitched` | `jobId` | Triggered when a job becomes the player's active job |
| `JobCompleted` | `jobId` | Triggered when a job is completed |
| `JobTaskCompleted` | `jobId`, `taskId` | Triggered when a job task is completed |
| `JobTasksUpdated` | `jobId` | Triggered after job tasks are updated |

### Formatting Tags

| Name | Arguments | Description | Example |
| ---- | -------- | -------- | --------  |
| **Format** |
| `{n}`, `{newline}` | N/a | Inserts a line break | `Line 1{n}Line 2`|
| `{h} {/h}`, `{highlight} {/highlight}` | N/a | Highlights the contained text | `We should check the {h}Experimentation Room{/h}` |
| `{cash} {/cash}` | N/a | Highlights the contained text as "cash" text, ending with the cash symbol | `{cash}1,000,000{/cash}!` |
| `{gears} {/gears}` | N/a | Highlights the contained text as "gears" text, ending with the gears symbol | `I can offer you this upgrade for {gears}50{/gears}`
| **Text Replace** |
| `{player-name}` | N/a | Inserts the player's name | `Oh, is that {player-name}? Hi {player-name}!` |
| `{loc [key]}` | Localized String Key | Inserts the localized text for the given string key | `{loc player.lose.text}` |
| `{var [id]}`, `{var-f [id]}`, `{var-i [id]}`, `{var-b [id]}`, `{var-s [id]}` | Variable Id | Inserts the value of the variable with the given value.<br/>`-i` variant will treat the variable as an integer.<br/>`-f` will treat it as a float.<br/> `-b` will treat it as a bool.<br/>`-s` will treat it as a localization key and will insert the localized text | `{var-f experiment:duration} seconds` |
| `{icon [id]}` | Icon Id | Inserts the given icon<br/>Available icons are `leftMouse`, `rightMouse` and `mouse` | `Use the {icon leftMouse}Left Mouse to move around` |

### Event Tags

| Name | Arguments | Mode | Description | Example |
| -- | -- | -- | -- | -- |
| **Timing** |
| `{slow} {/slow}` | N/a | Dialog | Reduces text typing speed to 50% for all contained text | `{slow}This text types slower{/slow}` |
| `{reallySlow} {/reallySlow}` | N/a | Dialog | Reduces text typing speed to 25% for all contained text | `{reallySlow}Very very slow text{/reallyslow}` |
| `{fast} {/fast}` | N/a | Dialog | Increases text typing speed to 125% for all contained text | `{fast}Really fast typed text wow{/fast}` |
| `{speed [multiplier]} {/speed}` | Speed Multiplier | Dialog | Adjusts text typing speed for all contained text | `This text runs normal, {speed 0.75}This text runs a little slower.{/speed} And we're back to normal.`
| `{wait [duration]}` | Duration (seconds) | Always | Waits for the given number of seconds before continuing.<br/> Note that when text is being fast forwarded, this type of wait will also be sped up | `This is{wait 0.5} an awkward pause`
| `{wait-abs [duration]}` | Duration (seconds) | Always | Waits for the given number of seconds before continuing.<br/>This does not account for text fast-forward.  | `This is {wait-abs 2}always two seconds of delay` |
| `{continue}` | N/a | Dialog | Waits for player input before continuing the line. | `The first half of the message.{continue} And the second half of the message, after user input.` |
| `{auto}` | N/a | Dialog | Automatically continues to the next line of script without player input. | `Some text that goes away fast {wait 0.5}{auto}` |
| **Dialog** |
| `{*[characterId] #[poseId]}` | Character Id<br/>Pose Id (optional) | Dialog | Sets the current character for the dialog. This will load in their default name and typing sounds. If a pose is specified, that pose portrait will be used as well. | `{@kevin} Some kevin text yeah`, `{@kevin #confused}	Kevin is... confused?` |
| `{#[poseId]` | Pose Id (optional) | Dialog | Sets the current character pose portrait. If no pose is passed in, the default pose will be used. | `{#confused} This is confused text I guess. {#} and back to default pose` |
| `{speaker [speaker text]` | Speaker Text | Dialog | Sets a custom string for the speaker name | `{speaker Kevin, Who Just Stole Some Of Your Blood} Haha I just stole your blood` |
| `{type [soundId]` | Type Sound Id | Dialog | Sets a custom text typing sound | `{type someSquishySoundId} This text types out with squishy sounds` |
| `{hide-dialog}` | N/a | Always | Manually hides the dialog box. Good for separating sections of a conversation. | `And here is some text`<br/>`{hide-dialog}{wait 1}`<br/>`And now here's a separate but connected conversation`
| `{show-dialog}` | N/a | Always | Manually shows the dialog box. | `{show-dialog}` |
| `{style [dialogStyleId]}` | Dialog Style Id | Always | Sets the textbox style. Available styles are `default`, `center`, and `cornerKevin` | `{style center}`<br/>`And this text appears in the center textbox instead.` |
| **Audio** |
| `{bgm [musicId], [crossfade]}` | Music Event Id<br/>Crossfade Duration (seconds) (optional, default value 0.5) | Always | Crossfades to a new piece of background music | `{bgm SomeMusicEvent}`, `{bgm AnotherMusicEvent, 0}` |
| `{bgm-pitch [pitch], [transitionDuration]}` | Pitch Multiplier<br/>Transition Duration (seconds) (optional, default value 0.5) | Always | Fades the current background music to a new pitch | `{bgm-pitch 0.16}Now the music is positively demonic` |
| `{bgm-stop [fadeDuration]}` | Fade Duration (seconds) (optional, default value 0.5) | Always | Stops the current background music with the given fade-out time | `{bgm-stop 2}` |
| `{sfx [soundId], [wait?]}` | Sound Event Id<br/>Wait (optional) | Always | Plays a sound event. If `wait` is provided as second argument, script execution waits for the sound to complete before continuing | `{sfx KevinShocked}`, `{sfx Clatter, wait}` |
| **Screen** |
| `{letterbox} {/letterbox}` | N/a | Always | Activates screen letterbox (cutscene mode) for the duration of this tag. | `And then {letterbox} this part is really cinematic {/letterbox} but we're back to normal now` |
| `{fade-out [color], [duration], [layer], [wait]}` | Color<br/>Duration (seconds)<br/>Layer (optional, accepted values are empty or `above-ui`)<br/>Wait (optional, accepted values are empty or `wait`) | Always | Fades the screen to a given color over a the given period of time. If `above-ui` is specified, this fader will appear above the UI. If `wait` is specified, this will wait for the fader to finish its fade before continuing | `{fade-out black, 0.5}` | `{fade-out red.50, 0.2}` |
| `{fade-in [duration], [wait]}` | Duration (seconds)<br/>Wait (optional, accepted values are empty and `wait`) | Always | Takes the fader established by `fade-out` and hides it over time. If `wait` is specified script execution waits for the fade to complete before continuing | `{fade-out black.50, 0.5, wait} {fade-in 0.5}` |
| `{wipe-out [layer]}` | Layer (optional, accepted values are empty or `above-ui`) | Always | Adds a screen wipe transition and waits for the wipe to be fully covering up the screen. If `above-ui` is set the wipe will also appear over ui elements. Waits for it to complete before proceeding. | `{wipe-out}`, `{wipe-out above-ui}` |
| `{wipe-in}` | N/a | Always | Takes the screen wipe established by a `wipe-out` command and animates it off the screen. Waits for it to complete before proceeding. | `{wipe-out above-ui} {wait 0.5} {wipe-in}` |
| `{screen-flash [color], [duration], [layer], [wait]}` | Color<br/>Duration (seconds)<br/>Layer (optional, accepted values are empty or `above-ui`)<br/>Wait (optional, accepted values are empty or `wait`) | Always | Flashes the given color on screen, gradually fading out over the given duration. If `above-ui` is specified, this will flash over the UI as well. If `wait` is specified, this will wait for the flash to completely fade before continuing with script execution. | `{screen-flash white.25, 0.2, above-ui}`, `{screen-flash black, 0.5}` |
| **Scene** |
| `{enable-object [objectId], [objectId2], [objectId3], ...}` | Script Object Ids (comma separated) | Always | Enables the Script Objects with the given ids | `{enable-object CameraRegion2}`, `{enable-object CameraRegion0, CameraRegion2, ScaryBackgroundThing}` |
| `{disable-object [objectId], [objectId2], [objectId3], ...}` | Script Object Ids (comma separated) | Always | Disables the Script Objects with the given ids | `{disable-object CameraRegion2}`, `{disable-object CameraRegion0, CameraRegion2, ScaryBackgroundThing}` |
| `{broadcast-event [eventId]}` | Event Id | Always | Dispatches the given game event within the scene | `{broadcast-event experiment:badEvent}` |
| `{trigger-response [triggerId], [targetId]}` | Trigger Id (see table)<br/>Target Id (optional) | Always| Sends a Trigger to the scripting system, which can provoke a response. See the table above for a list of in-game triggers. Note you can also create your own trigger ids if necessary simply by using them in script | `{trigger-response RequestPartnerHelp}`, `{trigger-response SomeNewResponse, kevin}` |
| `{load-scene [sceneName], [mode], [context]` | Scene Name<br/>Mode (optional, accepted values are empty and `no-loading-screen`)<br/>Context String (optional) | Always | Loads into another scene. If `no-loading-screen` is provided, the game will assume something else is serving as a loading screen instead (like a fader or screen wipe) | `{load-scene ExperimentPrototype}`, `{load-scene Ship, no-loading-screen}` |
| **Player** |
| `{give-fact [factId]}` | Fact Id | Always | Adds a fact to the player's bestiary | `{give-fact Urchins.Eat.BullKelp}` |
| `{give-entity [entityId]}` | Critter/Ecosystem Id | Always | Adds a critter or ecosystem to the player's bestiary | `{give-entity SeaOtter}`
| `{set-job [jobId]}` | Job Id | Always | Sets the player's current job. Note that this may start that job as well. | `{set-job Job2-1a}` |
| `{complete-job [jobId]}` | Job Id (optional, default to the player's current job) | Always | Completes the given job. | `{complete-job}`, `{complete-job Job2-4a}` |

**Guide to Modes**  
**Always**: Can always be used  
**Dialog**: Can only be used when a dialog box is active or about to be active (i.e. the current line has dialog text)

**Guide to Colors**  
Colors are specified as `color` or `color.alpha`.  
`color` can be either common values (i.e. `black`, `white`, `grey`) or hex codes (i.e. `ffffff`, `0a2b3c`)
`alpha` is specified as a value between 0 and 100 (i.e. `black.50`, `white.25`, `grey.90`). If no `alpha` is specified, a default value of 100 is assumed.