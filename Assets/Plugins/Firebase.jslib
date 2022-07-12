mergeInto(LibraryManager.library, {

    FBAcceptJob: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("accept_job", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBSwitchJob: function(index, userCode, appVersion, appFlavor, logVersion, jobName, prevJobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var prevJobName = Pointer_stringify(prevJobName);

        analytics.logEvent("switch_job", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            prev_job_name: prevJobName
        });
    },

    FBReceiveFact: function(index, userCode, appVersion, appFlavor, logVersion, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("receive_fact", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBReceiveEntity: function(index, userCode, appVersion, appFlavor, logVersion, jobName, entityId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var entityId = Pointer_stringify(entityId);

        analytics.logEvent("receive_entity", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            entity_id: entityId
        });
    },

    FBCompleteJob: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("complete_job", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBCompleteTask: function(index, userCode, appVersion, appFlavor, logVersion, jobName, taskId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var taskId = Pointer_stringify(taskId);

        analytics.logEvent("complete_task", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            task_id: taskId
        });
        console.log("task with Id "+taskId.toString()+" completed.");
    },

    FBSceneChanged: function(index, userCode, appVersion, appFlavor, logVersion, jobName, sceneName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var sceneName = Pointer_stringify(sceneName);

        analytics.logEvent("scene_changed", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            scene_name: sceneName
        });
    },

    FBRoomChanged: function(index, userCode, appVersion, appFlavor, logVersion, jobName, roomName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var roomName = Pointer_stringify(roomName);

        analytics.logEvent("room_changed", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            room_name: roomName
        });
    },

    FBBeginDive: function(index, userCode, appVersion, appFlavor, logVersion, jobName, siteId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var siteId = Pointer_stringify(siteId);

        analytics.logEvent("begin_dive", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            site_id: siteId
        });
    },

    FBBeginModel: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_model", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBBeginSimulation: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_simulation", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBAskForHelp: function(index, userCode, appVersion, appFlavor, logVersion, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("ask_for_help", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            node_id: nodeId
        });
    },

    FBTalkWithGuide: function(index, userCode, appVersion, appFlavor, logVersion, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("talk_with_guide", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            node_id: nodeId
        });
    },

	//Bestiary stuff
    FBOpenBestiary: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_bestiary", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBBestiaryOpenSpeciesTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_species_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBBestiaryOpenEnvironmentsTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_environments_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBBestiaryOpenModelsTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("bestiary_open_models_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBBestiarySelectSpecies: function(index, userCode, appVersion, appFlavor, logVersion, jobName, speciesId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var speciesId = Pointer_stringify(speciesId);

        analytics.logEvent("bestiary_select_species", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            species_id: speciesId
        });
    },

    FBBestiarySelectEnvironment: function(index, userCode, appVersion, appFlavor, logVersion, jobName, environmentId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var environmentId = Pointer_stringify(environmentId);

        analytics.logEvent("bestiary_select_environment", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            environment_id: environmentId
        });
    },
	
	FBBestiarySelectModel: function(index, userCode, appVersion, appFlavor, logVersion, jobName, modelId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var modelId = Pointer_stringify(modelId);

        analytics.logEvent("bestiary_select_model", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            model_id: modelId
        });
    },

    FBCloseBestiary: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_bestiary", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },
	
	
	//Status App 
	FBOpenStatus: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("open_status", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },
	
	FBStatusOpenJobTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_job_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },
	
	FBStatusOpenItemTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_item_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },
	
	FBStatusOpenTechTab: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("status_open_tech_tab", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },
	
	FBCloseStatus: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("close_status", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBSimulationSyncAchieved: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("simulation_sync_achieved", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBGuideScriptTriggered: function(index, userCode, appVersion, appFlavor, logVersion, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("guide_script_triggered", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            node_id: nodeId
        });
    }, 

    FBScriptFired: function(index, userCode, appVersion, appFlavor, logVersion, jobName, nodeId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("script_fired", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            node_id: nodeId
        });
    }, 

    // Modeling Events

    FBModelingStart: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("model_start", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBModelPhaseChanged: function(index, userCode, appVersion, appFlavor, logVersion, jobName, phase) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);

        analytics.logEvent("model_phase_changed", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            phase: phase
        });
    }, 

    FBModelEcosystemSelected: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_ecosystem_selected", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptStarted: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_started", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelConceptUpdated: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem, status) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var status = Pointer_stringify(status);

        analytics.logEvent("model_concept_updated", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem,
            status: status
        });
    }, 

    FBModelConceptExported: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_concept_exported", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelSyncError: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem, sync) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_sync_error", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem,
            sync: sync
        });
    }, 

    FBModelPredictCompleted: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_predict_completed", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneUpdate: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem, organism, differenceValue) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);
        var organism = Pointer_stringify(organism);

        analytics.logEvent("model_intervene_update", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem,
            organism: organism,
            difference_value: differenceValue
        });
    }, 

    FBModelInterveneError: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_error", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    }, 

    FBModelInterveneCompleted: function(index, userCode, appVersion, appFlavor, logVersion, jobName, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_intervene_completed", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            ecosystem: ecosystem
        });
    },

    FBModelingEnd: function(index, userCode, appVersion, appFlavor, logVersion, jobName, phase, ecosystem) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var phase = Pointer_stringify(phase);
        var ecosystem = Pointer_stringify(ecosystem);

        analytics.logEvent("model_end", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            phase: phase,
            ecosystem: ecosystem
        });
    },

    FBPurchaseUpgrade: function(index, userCode, appVersion, appFlavor, logVersion, jobName, itemId, itemName, cost) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var itemId = Pointer_stringify(itemId);
        var itemName = Pointer_stringify(itemName);

        analytics.logEvent("purchase_upgrade", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            item_id: itemId,
            item_name: itemName,
            cost: cost
        });
    },

    FBInsufficientFunds: function(index, userCode, appVersion, appFlavor, logVersion, jobName, itemId, itemName, cost) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var itemId = Pointer_stringify(itemId);
        var itemName = Pointer_stringify(itemName);

        analytics.logEvent("insufficient_funds", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            item_id: itemId,
            item_name: itemName,
            cost: cost
        });
    },

    FBTalkToShopkeep: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("talk_to_shopkeep", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBAddEnvironment: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);

        analytics.logEvent("add_environment", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment
        });
    },

    FBRemoveEnvironment: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);

        analytics.logEvent("remove_environment", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment
        });
    },

    FBAddCritter: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment, critter) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critter = Pointer_stringify(critter);

        analytics.logEvent("add_critter", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critter: critter
        });
    },

    FBRemoveCritter: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment, critter) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critter = Pointer_stringify(critter);

        analytics.logEvent("remove_critter", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critter: critter
        });
    },

    FBBeginExperiment: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment, critters, stabilizerEnabled, autofeederEnabled) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critters = Pointer_stringify(critters);

        analytics.logEvent("begin_experiment", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critters: critters,
            stabilizer_enabled: stabilizerEnabled,
            autofeeder_enabled: autofeederEnabled
        });
    },

    FBEndExperiment: function(index, userCode, appVersion, appFlavor, logVersion, jobName, tankType, environment, critters, stabilizerEnabled, autofeederEnabled) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var tankType = Pointer_stringify(tankType);
        var environment = Pointer_stringify(environment);
        var critters = Pointer_stringify(critters);

        analytics.logEvent("end_experiment", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            tank_type: tankType,
            environment: environment,
            critters: critters,
            stabilizer_enabled: stabilizerEnabled,
            autofeeder_enabled: autofeederEnabled
        });
    },

    FBBeginArgument: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("begin_argument", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBFactSubmitted: function(index, userCode, appVersion, appFlavor, logVersion, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("fact_submitted", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBFactRejected: function(index, userCode, appVersion, appFlavor, logVersion, jobName, factId) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);
        var factId = Pointer_stringify(factId);

        analytics.logEvent("fact_rejected", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName,
            fact_id: factId
        });
    },

    FBLeaveArgument: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("leave_argument", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    },

    FBCompleteArgument: function(index, userCode, appVersion, appFlavor, logVersion, jobName) {
        var userCode = Pointer_stringify(userCode);
        var appVersion = Pointer_stringify(appVersion);
        var appFlavor = Pointer_stringify(appFlavor);
        var jobName = Pointer_stringify(jobName);

        analytics.logEvent("complete_argument", {
            event_sequence_index: index,
            user_code: userCode,
            app_version: appVersion,
            app_flavor: appFlavor,
            log_version: logVersion,
            job_name: jobName
        });
    }

});
