var TTSLibraryImpl = {

    /**
     * Checks if text-to-speech is available.
     * @returns {boolean}
     */
    TTSCheckAvailable: function() {
        const speechSynthesis = window.speechSynthesis;
        if (!speechSynthesis)
            return false;

        const voices = speechSynthesis.getVoices();
        return voices.length > 0;
    },

    /**
     * Cancels the current tts request.
     * @returns {boolean}
     */
    TTSCancel: function() {
        const speechSynthesis = window.speechSynthesis;
        if (!speechSynthesis)
            return false;

        speechSynthesis.cancel();
        return true;
    },

    /**
     * Speaks a string.
     * @param {string} text 
     * @param {string} lang
     * @param {number} rate 
     * @param {number} volume
     * @param {number} pitch
     * @returns {boolean}
     */
    TTSSpeak: function(text, lang, rate, volume, pitch) {
        const speechSynthesis = window.speechSynthesis;
        if (!speechSynthesis)
            return false;

        speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(Pointer_stringify(text));
        utterance.volume = volume;
        utterance.rate = rate;
        utterance.pitch = pitch;
        utterance.lang = Pointer_stringify(lang);
        speechSynthesis.speak(utterance);
        return true;
    },

    /**
     * Returns if text-to-speech is currently speaking.
     * @returns {boolean}
     */
    TTSIsSpeaking: function() {
        const speechSynthesis = window.speechSynthesis;
        if (!speechSynthesis)
            return false;

        return speechSynthesis.speaking;
    }
};

mergeInto(LibraryManager.library, TTSLibraryImpl);