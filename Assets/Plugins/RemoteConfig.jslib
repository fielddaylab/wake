mergeInto(LibraryManager.library, {

    FetchSurvey: function (surveyName) {
        var surveyName = Pointer_stringify(surveyName);

        remoteConfig.fetchAndActivate()
            .then(function() {
                var surveyString = remoteConfig.getString(surveyName);
                unityInstance.SendMessage("Survey(Clone)", "ReadSurveyData", surveyString);
            })
            .catch(function(err) {
                console.log(err);
            });
    }

});
