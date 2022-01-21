# Aqualab
Aqualab is an NSF Funded (DRL #1907384) science practices and life science content learning game produced by Field Day @ University of Wisconsin - Madison, Harvard University and University of Pennslvania.

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
1. Initial version
2. Add app version to all events
3. New modeling events (1/19/22)

### Event Categories
1. [Init](#Init)
2. [Progression](#Progression)
3. [Player Actions](#PlayerActions)
4. [Game Feedback](#GameFeedback)
5. [Portable Device Interactions](#PortableDeviceInteractions)
6. [Modeling](#Modeling)

<a name="Init"/>

### Init 

#### user_code_entered

Player starts the game by entering their user code.

| Parameter | Description |
| --- | --- |
| usercode | The player's unique save code |

<a name="Progression"/>

### Progression

1. [accept_job](#accept_job)
2. [switch_job](#switch_job)
3. [receive_fact](#receive_fact)
4. [complete_job](#complete_job)
5. [complete_task](#complete_task)

<a name="accept_job"/>

#### accept_job

Player accepts a job with a given id.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the accepted job |
| job_name | String name of the accepted job |

<a name="switch_job"/>

#### switch_job

Player switches jobs by starting a different one.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the new job |
| job_name | String name of the new job |

<a name="receive_fact"/>

#### receieve_fact

A fact is added to the player's bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| fact_id | Unique ID for the given fact |

<a name="complete_job"/>

#### complete_job

Player completes a given job.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the completed job |
| job_name | String name of the completed job |

<a name="complete_task"/>

#### complete_task

Player completes a task for a given job.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the new job |
| job_name | String name of the new job |
| task_id | ID of the completed task |

<a name="PlayerActions"/>

### Player Actions

1. [scene_changed](#scene_changed)
2. [room_changed](#room_changed)
3. [begin_experiment](#begin_experiment)
4. [begin_dive](#begin_dive)
5. [begin_argument](#begin_argument)
6. [ask_for_help](#ask_for_help)

<a name="scene_changed"/>

#### scene_changed

Player loads into a new scene (ex. "Ship").

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| scene_name | Name of the loaded scene |

<a name="room_changed"/>

#### room_changed

Player enters a new room on the ship.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| scene_name | Name of the room being entered|

<a name="begin_experiment"/>

#### begin_experiment

Player starts an experiment with a given tank type.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| tank_type | Selected tank type for the experiment |

| tank_type |
| --- |
| Observation |
| Stress |
| Measurement | 

<a name="begin_dive"/>

#### begin_dive

Player enters a given dive site.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| site_id | ID of the dive site |

<a name="begin_argument"/>

#### begin_argument

Player begins the argumentation process.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="ask_for_help"/>

#### ask_for_help

Player clicks the hint button.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| node_id | Scripting ID for the hint response |

<a name="GameFeedback"/>

### Game Feedback

#### guide_script_triggered

Player triggers conversation with the guide (Kevin).

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| node_id | Scripting ID for guide's response |

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
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="bestiary_open_species_tab"/>

#### bestiary_open_species_tab

Player opens the species tab in the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="bestiary_open_environments_tab"/>

#### bestiary_open_environments_tab

Player opens the environments tab in the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="bestiary_open_models_tab"/>

#### bestiary_open_models_tab

Player opens the models tab in the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="bestiary_select_species"/>

#### bestiary_select_species

Player selects a species from the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| species_id | ID of the selected species |

<a name="bestiary_select_environment"/>

#### bestiary_select_environment

Player selects an environment from the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| environment_id | ID of the selected environment |

<a name="bestiary_select_model"/>

#### bestiary_select_model

Player selects a model from the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| model_id | ID of the selected model |

<a name="close_bestiary"/>

#### close_bestiary

Player closes the bestiary.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="open_status"/>

#### open_status

Player opens the portable status app, which defaults to the job tab.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="status_open_job_tab"/>

#### status_open_job_tab

Player opens the job tab in the portable status app.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="status_open_item_tab"/>

#### status_open_item_tab

Player opens the item tab in the portable status app.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="status_open_tech_tab"/>

#### status_open_tech_tab

Player opens the tech tab in the portable status app.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="close_status"/>

#### close_status

Player closes the portable status app.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
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
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

<a name="model_phase_changed"/>

#### model_phase_changed

Player selects a given modeling phase.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
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
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_concept_started"/>

#### model_concept_started

Player starts the conceptual modeling phase.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_concept_updated"/>

#### model_concept_updated

Player imports new facts / behaviors into the conceptual model.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
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
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="begin_simulation"/>

#### begin_simulation

Player enters the sync phase of modeling.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_sync_error"/>

#### model_sync_error

Player attempts to sync the model but fails.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |
| sync | Sync % achieved with the current model |

<a name="simulation_sync_achieved"/>

#### simulation_sync_achieved

Player successfully syncs the model.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_predict_completed"/>

#### model_predict_completed

Player completes the prediction model.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_intervene_update"/>

#### model_intervene_update

Player introduces a new organism or updates an existing organism's population count in the intervention model.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |
| organism | The organism having its population modified by the player |
| difference_value | The population change for the selected organism |

<a name="model_intervene_error"/>

#### model_intervene_error

Player’s intervention model is unsuccessful.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="model_intervene_completed"/>

#### model_intervene_completed

Player successfully completes the intervention model.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

<a name="end_model"/>

#### end_model

Player exits the modeling room.

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| phase | The selected modeling phase upon leaving |
| ecosystem | The selected ecosystem upon leaving |
