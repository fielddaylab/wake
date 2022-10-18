# Aqualab
Aqualab is an NSF Funded (DRL #1907384) science practices and life science content learning game produced by Field Day @ University of Wisconsin - Madison, Harvard University and University of Pennslvania.

## Debugging Tools
[Job Pricing / Sequence Explorer](https://beauprime.github.io/ProgressionGraph/)

## Firebase Telemetry Events

Firebase automatically adds the following parameters to all events, documented [here](https://support.google.com/firebase/answer/7061705?hl=en). Event data is then dumped to BigQuery daily - the BigQuery schema can be found [here](https://support.google.com/firebase/answer/7029846?hl=en&ref_topic=7029512).
* event_timestamp
* user_id (We need to manually set this if we have it)
* device.category
* device.mobile_brand_name (i.e Apple)
* device.mobile_model_name (i.e Safari)
* device.operating_system
* device.language
* geo.country
* geo.region (i.e Wisconsin)
* geo.city (i.e Madison)
* ga_session_id

Firebase automatically logs the following meaningful events, documented [here](https://support.google.com/firebase/answer/9234069?hl=en&ref_topic=6317484).
* first_visit
* page_view
* session_start
* user_engagement

### Change Log
1. Initial version (3/14/22)
2. Update experimentation events (3/22/22)
3. Add event sequence index (5/17/22)

### Event Categories
1. [Progression](#Progression)
2. [Player Actions](#PlayerActions)
3. [Game Feedback](#GameFeedback)
4. [Portable Device Interactions](#PortableDeviceInteractions)
5. [Modeling](#Modeling)
6. [Shop](#Shop)
7. [Experimentation](#Experimentation)
8. [Argumentation](#Argumentation)

<a name="Progression"/>

### Progression

1. [accept_job](#accept_job)
2. [switch_job](#switch_job)
3. [receive_fact](#receive_fact)
4. [receive_entity](#receive_entity)
5. [complete_job](#complete_job)
6. [complete_task](#complete_task)

<a name="accept_job"/>

#### accept_job

Player accepts a job with a given id.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the accepted job |

<a name="switch_job"/>

#### switch_job

Player switches jobs by starting a different one.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the new job |
| prev_job_name | String name of the previous job |

<a name="receive_fact"/>

#### receive_fact

A fact is added to the player's bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| fact_id | Unique ID for the given fact |
| fact_entity | Unique ID for the fact's owning entity |
| fact_type | String name of the fact type |
| fact_stressed | Boolean indicating if the fact represents a behavior that only executes when its owning organism is stressed 
| fact_rate | Boolean indicating if the fact represents a behavior that has a rate attached |
| has_rate | Boolean indicating if the player has the rate for the given fact |

#### upgrade_fact

A fact is upgraded in the player's bestiary to add a rate.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| fact_id | Unique ID for the given fact |
| fact_entity | Unique ID for the fact's owning entity |
| fact_type | String name of the fact type |
| fact_stressed | Boolean indicating if the fact represents a behavior that only executes when its owning organism is stressed 
| fact_rate | Boolean indicating if the fact represents a behavior that has a rate attached |
| has_rate | Boolean indicating if the player has the rate for the given fact |

<a name="receive_entity"/>

#### receieve_entity

An entity is added to the player's bestiary (ex. scanning a critter in a dive site).

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| entity_id | Unique ID for the given entity |

<a name="complete_job"/>

#### complete_job

Player completes a given job.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the completed job |

<a name="complete_task"/>

#### complete_task

Player completes a task for a given job.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the new job |
| task_id | ID of the completed task |

<a name="PlayerActions"/>

### Player Actions

1. [scene_changed](#scene_changed)
2. [room_changed](#room_changed)
3. [begin_dive](#begin_dive)
4. [ask_for_help](#ask_for_help)

<a name="scene_changed"/>

#### scene_changed

Player loads into a new scene (ex. "Ship").

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| scene_name | Name of the loaded scene |

<a name="room_changed"/>

#### room_changed

Player enters a new room on the ship.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| room_name | Name of the room being entered|

<a name="begin_dive"/>

#### begin_dive

Player enters a given dive site.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| site_id | ID of the dive site |

<a name="ask_for_help"/>

#### ask_for_help

Player clicks the hint button.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| node_id | Scripting ID for the hint response |

<a name="GameFeedback"/>

### Game Feedback

#### guide_script_triggered

Player triggers conversation with the guide (V1ctor).

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| node_id | Scripting ID for guide's response |

#### script_fired

Player triggers a given script node through dialogue or interactions.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| node_id | ID of a given script node |

#### script_line_displayed

Player sees a line of dialog.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| node_id | ID of a given script node |
| text_string | String displayed to the player |

<a name="PortableDeviceInteractions"/>

### Portable Device Interactions

1. [open_bestiary](#open_bestiary)
2. [bestiary_open_species_tab](#bestiary_open_species_tab)
3. [bestiary_open_environments_tab](#bestiary_open_environments_tab)
4. [bestiary_open_models_tab](#bestiary_open_models_tab)
5. [bestiary_select_species](#bestiary_select_species)
6. [bestiary_select_environment](#bestiary_select_environment)
7. [bestiary_select_model](#bestiary_select_model)
8. [close_bestiary](#close_bestiary)
9. [open_status](#open_status)
10. [status_open_job_tab](#status_open_job_tab)
11. [status_open_item_tab](#status_open_item_tab)
12. [status_open_tech_tab](#status_open_tech_tab)
13. [close_status](#close_status)

<a name="open_bestiary"/>

#### open_bestiary

Player opens the bestiary, which defaults to the species tab.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="bestiary_open_species_tab"/>

#### bestiary_open_species_tab

Player opens the species tab in the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="bestiary_open_environments_tab"/>

#### bestiary_open_environments_tab

Player opens the environments tab in the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="bestiary_open_models_tab"/>

#### bestiary_open_models_tab

Player opens the models tab in the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="bestiary_select_species"/>

#### bestiary_select_species

Player selects a species from the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| species_id | ID of the selected species |

<a name="bestiary_select_environment"/>

#### bestiary_select_environment

Player selects an environment from the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| environment_id | ID of the selected environment |

<a name="bestiary_select_model"/>

#### bestiary_select_model

Player selects a model from the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| model_id | ID of the selected model |

<a name="close_bestiary"/>

#### close_bestiary

Player closes the bestiary.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="open_status"/>

#### open_status

Player opens the portable status app, which defaults to the job tab.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="status_open_job_tab"/>

#### status_open_job_tab

Player opens the job tab in the portable status app.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="status_open_item_tab"/>

#### status_open_item_tab

Player opens the item tab in the portable status app.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="status_open_tech_tab"/>

#### status_open_tech_tab

Player opens the tech tab in the portable status app.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="close_status"/>

#### close_status

Player closes the portable status app.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="Modeling"/>

### Modeling

1. [begin_model](#begin_model)
2. [model_phase_changed](#model_phase_changed)
3. [model_ecosystem_selected](#model_ecosystem_selected)
4. [model_concept_started](#model_concept_started)
5. [model_concept_updated](#model_concept_updated)
6. [model_concept_exported](#model_concept_exported)
7. [begin_simulation](#begin_simulation)
8. [model_sync_error](#model_sync_error)
9. [simulation_sync_achieved](#simulation_sync_achieved)
10. [model_predict_completed](#model_predict_completed)
11. [model_intervene_update](#model_intervene_update)
12. [model_intervene_error](#model_intervene_error)
13. [model_intervene_completed](#model_intervene_completed)
14. [end_model](#end_model)

<a name="begin_model"/>

#### begin_model

Player enters the modeling room.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="model_phase_changed"/>

#### model_phase_changed

Player selects a given modeling phase.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| phase | The selected modeling phase |

| phase |
| --- |
| Ecosystem |
| Concept |
| Sync | 
| Predict |
| Intervene |

<a name="model_ecosystem_selected"/>

#### model_ecosystem_selected

Player selects an ecosystem for constructing the model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_concept_started"/>

#### model_concept_started

Player starts the conceptual modeling phase.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_concept_updated"/>

#### model_concept_updated

Player imports new facts / behaviors into the conceptual model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |
| status | Updated status of the concept model |

| status |
| --- |
| MissingData |
| PendingImport |
| ExportReady | 
| UpToDate |

<a name="model_concept_exported"/>

#### model_concept_exported

Player saves the conceptual model to AQOS.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="begin_simulation"/>

#### begin_simulation

Player enters the sync phase of modeling.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_sync_error"/>

#### model_sync_error

Player attempts to sync the model but fails.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |
| sync | Sync % achieved with the current model |

<a name="simulation_sync_achieved"/>

#### simulation_sync_achieved

Player successfully syncs the model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_predict_completed"/>

#### model_predict_completed

Player completes the prediction model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_intervene_update"/>

#### model_intervene_update

Player introduces a new organism or updates an existing organism's population count in the intervention model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |
| organism | The organism having its population modified by the player |
| difference_value | The population change for the selected organism |

<a name="model_intervene_error"/>

#### model_intervene_error

Player’s intervention model is unsuccessful.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_intervene_completed"/>

#### model_intervene_completed

Player successfully completes the intervention model.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="end_model"/>

#### end_model

Player exits the modeling room.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| phase | The selected modeling phase upon leaving |
| ecosystem | The selected ecosystem upon leaving |

<a name="Shop"/>

### Shop

1. [purchase_upgrade](#purchase_upgrade)
2. [insufficient_funds](#insufficient_funds)
3. [talk_to_shopkeep](#talk_to_shopkeep)

<a name="purchase_upgrade"/>

#### purchase_upgrade

Player purchases an upgrade from the shop.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| item_id | ID of the purchased item |
| item_name | String name of the purchased item |
| cost | Cost of the purchased item |

<a name="insufficient_funds"/>

#### insufficient_funds

Player attempts to purchase an item but doesn't have enough currency.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| item_id | ID of the item |
| item_name | String name of the item |
| cost | Cost of the item |

<a name="talk_to_shopkeep"/>

#### talk_to_shopkeep

Player talks to the shopkeeper.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="Experimentation"/>

### Experimentation

1. [add_environment](#add_environment)
2. [remove_environment](#remove_environment)
3. [add_critter](#add_critter)
4. [remove_critter](#remove_critter)
5. [begin_experiment](#begin_experiment)
6. [end_experiment](#end_experiment)

| tank_type |
| --- |
| Observation |
| Stress |
| Measurement |

<a name="add_environment"/>

#### add_environment

Player selects an environment for running the experiment.

| Parameter | Description |
| --- | --- |.
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Name of the added environment |

<a name="remove_environment"/>

#### remove_environment

Player deselects an environment.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Name of the removed environment |

<a name="add_critter"/>

#### add_critter

Player adds a critter to the tank.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Selected environment for the experiment |
| critter | Name of the critter added to the tank |

<a name="remove_critter"/>

#### remove_critter

Player removes a critter from the tank.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Selected environment for the experiment |
| critter | Name of the critter removed from the tank |

<a name="begin_experiment"/>

#### begin_experiment

Player starts an experiment with a given tank type.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Selected environment for the experiment |
| critters | Comma separated list of all critters added to the tank |
| stabilizer_enabled | Bit value for stabilizer enabled in measurement tank (0 = false, 1 = true, default to 1) |
| stabilizer_enabled | Bit value for auto feeder enabled in measurement tank (0 = false, 1 = true, default to 0) |

<a name="end_experiment"/>

#### end_experiment

Player ends the current experiment.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |
| environment | Selected environment for the experiment |
| critters | Comma separated list of all critters added to the tank |
| stabilizer_enabled | Bit value for stabilizer enabled in measurement tank (0 = false, 1 = true, default to 1) |
| stabilizer_enabled | Bit value for auto feeder enabled in measurement tank (0 = false, 1 = true, default to 0) |

<a name="Argumentation"/>

### Argumentation

1. [begin_argument](#begin_argument)
2. [fact_submitted](#fact_submitted)
3. [fact_rejected](#fact_rejected)
4. [complete_argument](#complete_argument)

<a name="begin_argument"/>

#### begin_argument

Player begins argumentation for a job.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="fact_submitted"/>

#### fact_submitted

Player submits a fact to argumentation.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| fact_id | ID of the submitted fact |

<a name="fact_rejected"/>

#### fact_rejected

Submitted fact is incorrect / rejected by the argumentation script.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |
| fact_id | ID of the rejected fact |

<a name="leave_argument"/>

#### leave_argument

Player clicks "Let me get back to you" during argumentation.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

<a name="complete_argument"/>

#### complete_argument

Player completes argumentation for a job.

| Parameter | Description |
| --- | --- |
| event_sequence_index | Sequence index of the current event |
| user_code | The player's unique save code |
| app_version | Current game build version |
| app_flavor | Git branch origin for current build |
| log_version | Current logging version |
| job_name | String name of the current job |

## DBExport.json Schema

### Root

| Parameter | Type | Description |
| --- | --- | --- |
| `jobs` | `JobData[]` | List of all jobs. | 

### JobData

| Parameter | Type | Description |
| --- | --- | --- |
| `id` | `String` | Identifier for job. |
| `date.added` | `Int64` | UTC timestamp for when job was added |
| `date.deprecated` | `Int64` | UTC timestamp for when job was no longer in game. This field is excluded if the job is still in the game. |
| `tasks` | `TaskData[]` | List of tasks for the job. |

### TaskData

| Parameter | Type | Description |
| --- | --- | --- |
| `id` | `String` | Identifier for the task. |
| `date.added` | `Int64` | UTC timestamp for when the task was added |
| `date.deprecated` | `Int64` | UTC timestamp for when the task was no longer in game. This field is excluded if the task is still part of the job. |

### Example

```json
{
    "jobs": [
      {
         "id": "arctic-missing-whale",
         "date": {
            "added": 1.32914936623526E+17
         },
         "tasks": [
            {
               "id": "findWhale",
               "date": {
                  "added": 1.32914936623526E+17
               }
            },
            {
               "id": "reportBack",
               "date": {
                  "added": 1.32914936623526E+17
               }
            }
         ]
      },
      {
         "id": "arctic-time-of-death",
         "date": {
            "added": 1.32914936623526E+17
         },
         "tasks": [
            {
               "id": "getPopulations",
               "date": {
                  "added": 1.32914936623526E+17
               }
            },
            {
               "id": "reportBack",
               "date": {
                  "added": 1.32914936623526E+17
               }
            }
         ]
      }
    ]
}
```
