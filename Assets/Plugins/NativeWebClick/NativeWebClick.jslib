var NativeWebClickLib = {

    $NWCCache: {
        /**
         * @type {Function}
         */
        nativeClickCallback: null,

        /**
         * Invokes the native callback.
         * @param {MouseEvent} m 
         */
        InvokeNativeCallbackFromMouse: function(m) {
            if (m.button == 0 && NWCCache.nativeClickCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(NWCCache.nativeClickCallback, x, y);
            }
        },

        /**
         * Invokes the native callback.
         * @param {TouchEvent} m 
         */
         InvokeNativeCallbackFromTouch: function(m) {
            if (NWCCache.nativeClickCallback) {
                /** @type {HTMLCanvasElement} */
                var element = m.currentTarget;
                var touch = m.targetTouches[0];
                var x = (touch.clientX - element.clientLeft) / element.clientWidth;
                var y = 1 - ((touch.clientY - element.clientTop) / element.clientHeight);
                dynCall_vff(NWCCache.nativeClickCallback, x, y);
            }
        }
    },

    /**
     * Registers the native callback.
     * @param {Function} callback 
     */
    NativeWebClick_Register: function(callback) {
        NWCCache.nativeClickCallback = callback;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("mousedown", NWCCache.InvokeNativeCallbackFromMouse);
            canvasElement.addEventListener("touchstart", NWCCache.InvokeNativeCallbackFromTouch);
        }
    },

    /**
     * Deregisters the native callback.
     */
    NativeWebClick_Deregister: function() {
        NWCCache.nativeClickCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("mousedown", NWCCache.InvokeNativeCallbackFromMouse);
            canvasElement.removeEventListener("touchstart", NWCCache.InvokeNativeCallbackFromTouch);
        }
    }

};

autoAddDeps(NativeWebClickLib, '$NWCCache');
mergeInto(LibraryManager.library, NativeWebClickLib);