mergeInto(LibraryManager.library, {
    FBStartGameWithUserCode: function(userCode) {
        var userCode = Pointer_stringify(userCode);

        analytics.logEvent("user_code_entered", {
            usercode: userCode
        });
    },

    FBAcceptJob: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("accept_job", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBSwitchJob: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, prevJobId, prevJobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var prevJobName = Pointer_stringify(prevJobName);

        analytics.logEvent("switch_job", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            prev_job_id: prevJobId,
            prev_job_name: prevJobName
        });
    },

    FBReceiveFact: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("receive_fact", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBReceiveEntity: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, entityId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var entityId = Pointer_stringify(entityId);

        analytics.logEvent("receive_entity", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            entity_id: entityId
        });
    },

    FBCompleteJob: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("complete_job", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBCompleteTask: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, taskId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var taskId = Pointer_stringify(taskId);

        analytics.logEvent("complete_task", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            task_id: taskId
        });
        console.log("task with Id "+taskId.toString()+" completed.");
    },

    FBSceneChanged: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, sceneName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var sceneName = Pointer_stringify(sceneName);

        analytics.logEvent("scene_changed", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            scene_name: sceneName
        });
    },

    FBRoomChanged: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, roomName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var roomName = Pointer_stringify(roomName);

        analytics.logEvent("room_changed", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            room_name: roomName
        });
    },

    FBBeginExperiment: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment, critters) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var environment = Pointer_stringify(environment);
        var critters = Pointer_stringify(critters);

        analytics.logEvent("begin_experiment", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critters: critters
        });
    },

    FBBeginDive: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, siteId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var siteId = Pointer_stringify(siteId);

        analytics.logEvent("begin_dive", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            site_id: siteId
        });
    },

    FBBeginModel: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_model", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBeginSimulation: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_simulation", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBAskForHelp: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("ask_for_help", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    },

    FBTalkWithGuide: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("talk_with_guide", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    },

	//Bestiary stuff
    FBOpenBestiary: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_bestiary", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenSpeciesTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_species_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenEnvironmentsTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_environments_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiaryOpenModelsTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_models_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBBestiarySelectSpecies: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, speciesId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var speciesId = Pointer_stringify(speciesId);

        analytics.logEvent("bestiary_select_species", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            species_id: speciesId
        });
    },

    FBBestiarySelectEnvironment: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, environmentId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var environmentId = Pointer_stringify(environmentId);

        analytics.logEvent("bestiary_select_environment", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            environment_id: environmentId
        });
    },
	
	FBBestiarySelectModel: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, modelId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var modelId = Pointer_stringify(modelId);

        analytics.logEvent("bestiary_select_model", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            model_id: modelId
        });
    },

    FBCloseBestiary: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_bestiary", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	
	//Status App 
	FBOpenStatus: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_status", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenJobTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_job_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenItemTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_item_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBStatusOpenTechTab: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_tech_tab", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },
	
	FBCloseStatus: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_status", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBSimulationSyncAchieved: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("simulation_sync_achieved", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBGuideScriptTriggered: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("guide_script_triggered", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    }, 

    FBScriptFired: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("script_fired", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            node_id: nodeId
        });
    }, 

    // Modeling Events

    FBModelingStart: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("model_start", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBModelPhaseChanged: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, phase) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);

        analytics.logEvent("model_phase_changed", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            phase: phase
        });
    }, 

    FBModelEcosystemSelected: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_ecosystem_selected", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptStarted: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_started", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptUpdated: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem, status) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var status = Pointer_stringify(status);

        analytics.logEvent("model_concept_updated", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            status: status
        });
    }, 

    FBModelConceptExported: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_exported", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelSyncError: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem, sync) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_sync_error", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            sync: sync
        });
    }, 

    FBModelPredictCompleted: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_predict_completed", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneUpdate: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem, organism, differenceValue) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var organism = Pointer_stringify(organism);

        analytics.logEvent("model_intervene_update", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem,
            organism: organism,
            difference_value: differenceValue
        });
    }, 

    FBModelInterveneError: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_error", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneCompleted: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_completed", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            ecosystem: ecosystem
        });
    },

    FBModelingEnd: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, phase, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_end", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            phase: phase,
            ecosystem: ecosystem
        });
    },

    FBPurchaseUpgrade: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, itemId, itemName, cost) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var itemId = Pointer_stringify(itemId);
        var itemName = Pointer_stringify(itemName);

        analytics.logEvent("purchase_upgrade", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            item_id: itemId,
            item_name: itemName,
            cost: cost
        });
    },

    FBInsufficientFunds: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, itemId, itemName, cost) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var itemId = Pointer_stringify(itemId);
        var itemName = Pointer_stringify(itemName);

        analytics.logEvent("insufficient_funds", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            item_id: itemId,
            item_name: itemName,
            cost: cost
        });
    },

    FBTalkToShopkeep: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("talk_to_shopkeep", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBAddEnvironment: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);

        analytics.logEvent("add_environment", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment
        });
    },

    FBRemoveEnvironment: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);

        analytics.logEvent("remove_environment", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment
        });
    },

    FBEnvironmentCleared: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);

        analytics.logEvent("environment_cleared", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment
        });
    },

    FBAddCritter: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment, critter) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critter = Pointer_stringify(critter);

        analytics.logEvent("add_critter", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critter: critter
        });
    },

    FBRemoveCritter: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment, critter) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critter = Pointer_stringify(critter);

        analytics.logEvent("remove_critter", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critter: critter
        });
    },

    FBCrittersCleared: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, tankType, environment, critters) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critters = Pointer_stringify(critters);

        analytics.logEvent("remove_critter", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critters: critters
        });
    },

    FBBeginArgument: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_argument", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBFactSubmitted: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("fact_submitted", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBFactRejected: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("fact_rejected", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBLeaveArgument: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("leave_argument", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    },

    FBCompleteArgument: function(userCode, appVersion, appFlavor, logVersion, jobId, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("complete_argument", {
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_id: jobId,
            job_name: jobName
        });
    }

});
