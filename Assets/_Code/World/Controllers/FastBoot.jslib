var FastBootLib = {
    FastBoot_Initialize: function() {
        if (WEBAudio.audioContext.state === "suspended") {
            WEBAudio.audioContext.resume();
        }
    }
};

mergeInto(LibraryManager.library, FastBootLib);