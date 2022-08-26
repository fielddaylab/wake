var NativeWebClickLib = {

    $Cache: {
        /**
         * @type {Function}
         */
        nativeCallback: null,

        /**
         * Invokes the native callback.
         * @param {MouseEvent} m 
         */
        InvokeNativeCallbackFromMouse: function(m) {
            if (m.button == 0 && Cache.nativeCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(Cache.nativeCallback, x, y);
            }
        }
    },

    /**
     * Registers the native callback.
     * @param {Function} callback 
     */
    NativeWebClick_Register: function(callback) {
        Cache.nativeCallback = callback;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("mousedown", Cache.InvokeNativeCallbackFromMouse);
        }
    },

    /**
     * Deregisters the native callback.
     */
    NativeWebClick_Deregister: function() {
        Cache.nativeCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("mousedown", Cache.InvokeNativeCallbackFromMouse);
        }
    }

};

autoAddDeps(NativeWebClickLib, '$Cache');
mergeInto(LibraryManager.library, NativeWebClickLib);