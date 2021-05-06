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
