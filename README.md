# Aqualab
Aqualab is an NSF Funded (DRL #1907384) science practices and life science content learning game produced by Field Day @ University of Wisconsin - Madison, Harvard University and University of Pennslvania.

## Firebase Telemetry Events

### Change Log
1. Initial version
2. Add app version to all events
3. New modeling events (1/12/22)

Firebase automatically adds the following to all events
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

Firebase automatically adds the following meaningful events
* first_visit
* session_start

### Init
* session_Begin(session_id, client_time)
* user_code_entered(usercode)

### Progression
* accept_job (job_id)
* switch_job (job_id)
* receive_fact (fact_id)
* complete_job (job_id)
* complete_task (job_id, task_id)

### Player Actions
* scene_changed (scene_name)
* room_changed (room_name)
* begin_experiment (job_id, (enum)tank_type)
* MORE HERE
* begin_dive (job_id, site_id)
* MORE HERE
* begin_argument (job_id)
* MORE HERE
* begin_model (job_id)
* MORE HERE
* begin_simulation (job_id)
* ask_for_help (node_id) - User clicked the hint button
* talk_with_guide (node_id) - User clicked the button for Kevin

### Portable Device Interactions
* open_bestiry
* bestiary_open_species_tab
* bestiary_open_environments_tab
* bestiary_open_models_tab
* bestiary_select_species (species_id)
* bestiary_select_environment (environment_id)
* bestiary_select_model (model_id)
* close_bestiary
* open_status
* status_open_job_tab
* status_open_item_tab
* status_open_tech_tab
* close_status

### Game Feedback
* simulation_sync_achieved (job_id)
* script_triggered (node_id)

### Modeling

#### model_phase_changed

Player selects a given modeling phase

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| phase | The selected modeling phase - Ecosystem, Concept, Sync, Predict, Intervene |

#### model_ecosystem_selected

Player selects an ecosystem for constructing the model

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| ecosystem | Ecosystem selected for modeling |

#### model_concept_started

Player starts the conceptual modeling phase

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

#### model_concept_updated

Player imports new facts / behaviors into the conceptual model

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| state | Updated state of the concept model - MissingData, PendingImport, ExportReady, UpToDate |

#### model_concept_exported

Player saves the conceptual model to AQOS

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

#### model_sync_error

Player attempts to sync the model but fails

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
| sync | Sync % achieved with the current model |

#### model_sync_completed

Player successfully syncs the model

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

#### model_predict_completed

Player completes the prediction model

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

#### model_intervene_error

Player’s intervention model is unsuccessful

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |

#### model_intervene_completed

Player successfully completes the intervention model

| Parameter | Description |
| --- | --- |
| user_code | The player's unique save code |
| app_version | Current logging version |
| job_id | ID of the current job |
| job_name | String name of the current job |
