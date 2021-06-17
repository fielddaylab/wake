mergeInto(LibraryManager.library, {
    FBStartGameWithUserCode: function(userCode) {
        var userCode = Pointer_stringify(userCode);

        analytics.logEvent("user_code_entered", {
            usercode: userCode
        });
    },

    FBAcceptJob: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("accept_job", {
            job_id: jobId
        });
    },

    FBReceiveFact: function(factId) {
        var factId = Pointer_stringify(factId);

        analytics.logEvent("receive_fact", {
            fact_id: factId
        });
    },

    FBCompleteJob: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("complete_job", {
            job_id: jobId
        });
    },

    FBCompleteTask: function(jobId, taskId) {
        var jobId = Pointer_stringify(jobId);
        var taskId = Pointer_stringify(taskId);

        analytics.logEvent("complete_task", {
            job_id: jobId,
            task_id: taskId
        });
        console.log("task with Id "+taskId.toString()+" completed.");
    },

    FBBeginExperiment: function(jobId, tankType) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("begin_experiment", {
            job_id: jobId,
            tank_type: tankType
        });
    },

    FBBeginDive: function(jobId, siteId) {
        var jobId = Pointer_stringify(jobId);
        var siteId = Pointer_stringify(siteId);

        analytics.logEvent("begin_dive", {
            job_id: jobId,
            site_id: siteId
        });
    },

    FBBeginArgument: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("begin_argument", {
            job_id: jobId
        });
    },

    FBBeginModel: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("begin_model", {
            job_id: jobId
        });
    },

    FBBeginSimulation: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("begin_simulation", {
            job_id: jobId
        });
    },

    FBAskForHelp: function(nodeId) {
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("ask_for_help", {
            node_id: nodeId
        });
    },

    FBTalkWithGuide: function(nodeId) {
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("talk_with_guide", {
            node_id: nodeId
        });
    },

	//Bestiary stuff
    FBOpenBestiary: function(jobId){
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("open_bestiary", {
            job_id: jobId
        });
    },

    FBBestiaryOpenSpeciesTab: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("bestiary_open_species_tab", {
            job_id: jobId
        });
    },

    FBBestiaryOpenEnvironmentsTab: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("bestiary_open_environments_tab", {
            job_id: jobId
        });
    },

    FBBestiaryOpenModelsTab: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("bestiary_open_models_tab", {
            job_id: jobId
        });
    },

    FBBestiaryOpenTasksTab: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("bestiary_open_tasks_tab", {
            job_id: jobId
        });
    },

    FBBestiarySelectSpecies: function(jobId, speciesId) {
        var jobId = Pointer_stringify(jobId);
        var speciesId = Pointer_stringify(speciesId);

        analytics.logEvent("bestiary_select_species",
        {
            job_id: jobId,
            species_id: speciesId
        });
    },

    FBBestiarySelectEnvironment: function(jobId, environmentId) {
        var jobId = Pointer_stringify(jobId);
        var environmentId = Pointer_stringify(environmentId);

        analytics.logEvent("bestiary_select_environment",
        {
            job_id: jobId,
            environment_id: environmentId
        });
    },

    FBCloseBestiary: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("close_bestiary", {
            job_id: jobId
        });
    },


    FBSimulationSyncAchieved: function(jobId) {
        var jobId = Pointer_stringify(jobId);

        analytics.logEvent("simulation_sync_achieved", {
            job_id: jobId
        });
    },

    FBGuideScriptTriggered: function(nodeId) {
        var nodeId = Pointer_stringify(nodeId);

        analytics.logEvent("guide_script_triggered", {
            node_id: nodeId
        });
    }

});
