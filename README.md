# Aqualab
Aqualab is an NSF Funded (DRL #1907384) science practices and life science content learning game produced by Field Day @ University of Wisconsin - Madison, Harvard University and University of Pennslvania.

## Firebase Telementry Events

Progression
* Accept_Job (user_id, session_id, client_time, job_id)
* Receive_Fact (user_id, session_id, client_time, fact_id)
* Complete_Job (user_id, session_id, client_time, job_id)

Player Actions
* Begin_Experiment (user_id, session_id, client_time, job_id, (enum)tank_type)
* Begin_Dive (user_id, session_id, client_time, job_id, site_id)
* Begin_Argument (user_id, session_id, client_time, job_id)
* Begin_Model (user_id, session_id, client_time, job_id)
* Begin_Simulation (user_id, session_id, client_time, job_id)
* Ask_For_Help (user_id, session_id, client_time, node_id)
* Talk_With_Guide (user_id, session_id, client_time, node_id)

Game Feedback
* Simulation_Sync_Achieved (user_id, session_id, client_time, job_id)
* Guide_Script_Triggered (user_id, session_id, client_time, node_id)