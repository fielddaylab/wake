# Leaf

## Reference

### Triggers
| **Name** | **When** | **Arguments** | **Notes** |
| --- | --- | -- | -- |
| **Basic** |
| `RequestPartnerHelp` | When the player clicks on the guide character help button. |
| `InspectObject` | When the player clicks on an object in the scene. | `objectId`: Id of the `ScriptObject` clicked. |
| **Bestiary** |
| `BestiaryEntryAdded` | When a new entry is added to the bestiary. | `entryId`: Id of the organism/ecosystem unlocked. |
| `BestiaryFactAdded` | When a new fact is added to the bestiary. | `factId`: Id of the fact unlocked. |
| **AQOS** |
| `PortableOpened` | When the player opens AQOS. |
| `PortableAppOpened` | When the player opens a specific tab in AQOS. | `appId`: Id of the opened tab. |
| **Jobs** |
| `JobStarted` | When the player starts a new job. | `jobId`: Id of the started job |
| `JobSwitched` | When the player switches to or starts a job. | `jobId`: Id of the current job. | Will be invoked after loading save file if player has job. Will also be invoked after the `JobStarted` trigger and `JobCompleted` trigger. |
| `JobCompleted` | When the player completes the current job. | `jobId`: Id of the completed job. |
| `JobTaskCompleted` | When the player completes a task. | `jobId`: Id of the current job.<br/> `taskId`: Id of the completed task.
| `JobTasksUpdated` | When the player has completed tasks and the task list has been updated. | `jobId`: Id of the completed job. |
| **Scene** |
| `SceneStart` | When a new scene starts. |
| `TryExitScene` | When the player attempts to exit the current scene. |  `exitId`: Id of the `ScriptObject` representing the exit.<br/> `targetMapId`: Id of the target map.<br/> `targetEntranceId`: Id of the target entrance on the target map. |
| `PlayerEnterRegion` | When the player's ship, sub, or avatar enters into contact with a `ScriptTrigger` region. | `regionId`: Id of the `ScriptObject`. |
| `PlayerExitRegion` | When the player's ship, sub, or avatar exits contact with a `ScriptTrigger` region. | `regionId`: Id of the `ScriptObject`. |
| **Player Ship** |
| `RoomEnter` | When the player enters a room on the ship. | `roomId`: Id of the room. |
| **Station Map** |
| `DiveSiteFound` | When the player drives over an available dive site. | `siteId`: Id of the dive site.<br/> `siteHighlighted`: True if the site is highlighted for the current job. |
| **Dive Sites** |
| `ScannedNewObject` | When the player scans an object in a dive site that they haven't scanned before. | `scanId`: Id of the new scan. |
| `ScannedObject` | When the player scans an object. | `scanId`: Id of the scan. | If a response to `ScannedNewObject` is found, `ScannedObject` will not be triggered. |
| **Shop** |
| `ShopReady` | When the player enters the shop. |
| `ShopViewTable` | When the player views a specific table in the shop. | `tableId`: Id of the shop table. |
| `ShopExit` | When the player chooses to exit the shop. |
| `ShopAttemptBuy` | When the player attempts to purchase an item from the shop. | `itemId`: Id of the item.<br/> `itemName`: Localization key for the item's name.<br/> `canAfford`: True if the player can afford to purchase the item.<br/> `cashCost`: Item's coin cost.<br/> `gearCost`: Item's gear cost. |
| **Experimentation** |
| `ExperimentTankViewed` | When the player selects a specific experiment tank. | `tankType`: Type of tank selected.<br/> `tankId`: Id for the selected. |
| `ExperimentScreenViewed` | When the player views a specific experimentation setup screen. | `tankType`: Type of tank.<br/> `tankId`: Id for the tank.<br/> `screenId`: Id for the viewed screen. |
| `ExperimentStarted` | When the player starts an experiment. | `tankType`: Type of tank selected.<br/> `tankId`: Id for the selected.<br/> `newFactsLeft`: How many new facts the player could theoretically learn (Observation Tank only). |
| `ExperimentFinished` | When the player finishes an experiment. | `tankType`: Type of tank.<br/> `tankId`: Id for the tank. |
| `BehaviorCaptureChance` | When the player has an opportunity to observe a behavior in the Observation Tank. | `factId`: Id of the behavior fact.<br/> `newFact`: True if the fact is one the player has not yet observed. |
| `BehaviorCaptureChanceExpired` | When the player lets an opportunity to observe a behavior in the Observation Tank expire. | `factId`: Id of the behavior fact.<br/> `newFact`: True if the fact is one the player has not yet observed. |
| `NewBehaviorObserved` | When the player observes a new behavior from the Observation Tank. | `factId`: Id for the new fact. |
| `ExperimentIdle` | When the player has gone for 10-30 seconds without observing a new behavior. | `tankType`: Type of tank.<br/> `tankId`: Id for the tank.<br/> `newFactsLeft`: Number of new facts the player could potentially observe.<br/> `missedFacts`: Number of new facts the player could have observed but can no longer do so. |
| **Modeling** |
| `VisualModelStarted` | When the player opens the visual model. |
| `VisualModelUpdated` | When the player has updated the visual model with new facts. |
| `VisualModelExported` | When the player has exported their visual model into a model in AQOS. |
| `SimulationModelStarted` | When the player begins any of the simulation portions of modeling (describe, predict, intervene) |
| `SimulationSyncError` | When the player attempts to line up their describe model but it fails to meet the accuracy requirement. |
| `SimulationSyncSuccess` | When the player successfully lines up their describe model with the required accuracy. |
| `SimulationPredictSuccess` | When the player finishes their predict model.
| `SimulationInterveneError` | When the player attempts an intervention but it fails to meet the requirements. | 
| `SimulationInterveneSuccess` | When the player successfully makes an intervention that meets the requirements. |

### Built-in Variables
| **Id** | **Usage** | **Notes** |
| -- | -- | -- |
| **Scene** |
| `scene:name` | Name of the current scene. |
| `scene:mapId` | Id of the map entry for the current scene. |
| `scene:lastEntrance` | Id of the entrance used to enter the current scene. |
| `temp:camera.region` | Id of the last entered camera region. |
| `temp:interact.object` | Id of the last object with a visible interaction icon. |
| **Progression** |
| `global:actNumber` | Current story act number. |
| `player:currentJob` | Id of the current player job, or `null` if no job. |
| `player:currentStation` | Id of the current station |
| **Player Ship** |
| `global:nav.shipRoom` | Id of the last ship room the player was in. |
| **Help System** |
| `kevin:help.requests` | Number of times the player has requested help. |
| **AQOS** |
| `portable:app` | Id of the currently open tab in AQOS. |
| `portable:open` | True if AQOS is currently open. |
| `portable:bestiary.currentEntry` | Currently selected organism/ecosystem in AQOS. |
| `portable:lastSelectedFactId` | Id of the last selected fact in AQOS. |
| **Shop** |
| `world:shopUnlocked` | True if the shop has been unlocked. |
| `shop:table` | Id of the currently selected shop table. |
| **Modeling** |
| `modeling:ecosystemSelected` | Id of the currently selected ecosystem. |
| `modeling:hasJob` | True if the current job has a modeling activity for the selected ecosystem. |
| `modeling:hasPendingFacts` | True if the player has facts they need to import to the current visual model. |
| `modeling:hasMissingFacts` | True if the player is missing facts for the current visual model. |
| `modeling:hasPendingExport` | True if the player has enough facts in the current visual model to export a model to AQOS. |
| `modeling:simSync` | Accuracy of the current describe model, from 0-100 |
| `modeling:phase` | Id of the current modeling phase. |

### Methods
| **Name** | **Return Type** | **Usage** | **Arguments** | **Script Component** | **Notes** |
| -- | -- | -- | -- | -- | -- |
| **Conditions** |
