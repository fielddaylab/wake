var NativeFullscreen = {

    /**
     * Sets fullscreen.
     */
    NativeFullscreen_SetFullscreen: function(fullscreen) {
        unityInstance.SetFullscreen(!!fullscreen ? 1 : 0);
    }
};

mergeInto(LibraryManager.library, NativeFullscreen);