mergeInto(LibraryManager.library, {
    FBStartGameWithUserCode: function(userCode) {
        var userCode = Pointer_stringify(userCode);

        analytics.logEvent("user_code_entered", {
            usercode: userCode
        });
    },

    FBAcceptJob: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("accept_job", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBSwitchJob: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("switch_job", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBReceiveFact: function(userCode, appVersion, jobId, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("receive_fact", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBCompleteJob: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("complete_job", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBCompleteTask: function(userCode, appVersion, jobId, jobName, taskId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var taskId = Pointer_stringify(taskId);

        analytics.logEvent("complete_task", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            task_id: taskId
        });
        console.log("task with Id "+taskId.toString()+" completed.");
    },

    FBSceneChanged: function(userCode, appVersion, jobId, jobName, sceneName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var sceneName = Pointer_stringify(sceneName);

        analytics.logEvent("scene_changed", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            scene_name: sceneName
        });
    },

    FBRoomChanged: function(userCode, appVersion, jobId, jobName, roomName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var roomName = Pointer_stringify(roomName);

        analytics.logEvent("room_changed", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            room_name: roomName
        });
    },

    FBBeginExperiment: function(userCode, appVersion, jobId, jobName, tankType) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_experiment", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType
        });
    },

    FBBeginDive: function(userCode, appVersion, jobId, jobName, siteId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var siteId = Pointer_stringify(siteId);

        analytics.logEvent("begin_dive", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            site_id: siteId
        });
    },

    FBBeginArgument: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_argument", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBeginModel: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_model", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBeginSimulation: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_simulation", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBAskForHelp: function(userCode, appVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("ask_for_help", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    },

    FBTalkWithGuide: function(userCode, appVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("talk_with_guide", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    },

	//Bestiary stuff
    FBOpenBestiary: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_bestiary", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenSpeciesTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_species_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenEnvironmentsTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_environments_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenModelsTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_models_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiarySelectSpecies: function(userCode, appVersion, jobId, jobName, speciesId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var speciesId = Pointer_stringify(speciesId);

        analytics.logEvent("bestiary_select_species", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            species_id: speciesId
        });
    },

    FBBestiarySelectEnvironment: function(userCode, appVersion, jobId, jobName, environmentId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var environmentId = Pointer_stringify(environmentId);

        analytics.logEvent("bestiary_select_environment", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            environment_id: environmentId
        });
    },
	
	FBBestiarySelectModel: function(userCode, appVersion, jobId, jobName, modelId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var modelId = Pointer_stringify(modelId);

        analytics.logEvent("bestiary_select_model", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            model_id: modelId
        });
    },

    FBCloseBestiary: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_bestiary", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	
	//Status App 
	FBOpenStatus: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_status", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenJobTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_job_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenItemTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_item_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenTechTab: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_tech_tab", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBCloseStatus: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_status", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBSimulationSyncAchieved: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("simulation_sync_achieved", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBGuideScriptTriggered: function(userCode, appVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("guide_script_triggered", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    }, 

    // Modeling Events

    FBModelingStart: function(userCode, appVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("model_start", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBModelPhaseChanged: function(userCode, appVersion, jobId, jobName, phase) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);

        analytics.logEvent("model_phase_changed", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            phase: phase
        });
    }, 

    FBModelEcosystemSelected: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_ecosystem_selected", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptStarted: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_started", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptUpdated: function(userCode, appVersion, jobId, jobName, ecosystem, status) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var status = Pointer_stringify(status);

        analytics.logEvent("model_concept_updated", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            status: status
        });
    }, 

    FBModelConceptExported: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_exported", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelSyncError: function(userCode, appVersion, jobId, jobName, ecosystem, sync) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_sync_error", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            sync: sync
        });
    }, 

    FBModelPredictCompleted: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_predict_completed", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneUpdate: function(userCode, appVersion, jobId, jobName, ecosystem, organism, differenceValue) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var organism = Pointer_stringify(organism);

        analytics.logEvent("model_intervene_update", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            organism: organism,
            difference_value: differenceValue
        });
    }, 

    FBModelInterveneError: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_error", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneCompleted: function(userCode, appVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_completed", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    },

    FBModelingEnd: function(userCode, appVersion, jobId, jobName, phase, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_end", {
            user_code: userCode,
            app_version: appVersion,
            job_id: jobId,
            job_name: jobName,
            phase: phase,
            ecosystem: ecosystem
        });
    }

});
