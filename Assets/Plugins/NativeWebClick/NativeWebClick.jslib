var NativeWebClickLib = {

    $Cache: {
        /**
         * @type {Function}
         */
        nativeClickCallback: null,

        /**
         * Invokes the native callback.
         * @param {MouseEvent} m 
         */
        InvokeNativeCallbackFromMouse: function(m) {
            if (m.button == 0 && Cache.nativeClickCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(Cache.nativeClickCallback, x, y);
            }
        },

        /**
         * Invokes the native callback.
         * @param {TouchEvent} m 
         */
         InvokeNativeCallbackFromTouch: function(m) {
            if (Cache.nativeClickCallback) {
                /** @type {HTMLCanvasElement} */
                var element = m.currentTarget;
                var touch = m.targetTouches[0];
                var x = (touch.clientX - element.clientLeft) / element.clientWidth;
                var y = 1 - ((touch.clientY - element.clientTop) / element.clientHeight);
                dynCall_vff(Cache.nativeClickCallback, x, y);
            }
        }
    },

    /**
     * Registers the native callback.
     * @param {Function} callback 
     */
    NativeWebClick_Register: function(callback) {
        Cache.nativeClickCallback = callback;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("mousedown", Cache.InvokeNativeCallbackFromMouse);
            canvasElement.addEventListener("touchstart", Cache.InvokeNativeCallbackFromTouch);
        }
    },

    /**
     * Deregisters the native callback.
     */
    NativeWebClick_Deregister: function() {
        Cache.nativeClickCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("mousedown", Cache.InvokeNativeCallbackFromMouse);
            canvasElement.removeEventListener("touchstart", Cache.InvokeNativeCallbackFromTouch);
        }
    }

};

autoAddDeps(NativeWebClickLib, '$Cache');
mergeInto(LibraryManager.library, NativeWebClickLib);