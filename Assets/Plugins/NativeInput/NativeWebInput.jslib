var NativeWebInputLib = {

    $NWICache: {
        /**
         * @type {Function}
         */
        nativeClickCallback: null,

        /**
         * @type {Function}
         */
        nativeKeyboardCallback: null,

        /**
         * @type {[string] => number}
         */
        keyCodeMapping: {
            "Backspace": 8,
            "Tab": 9,
            "Enter": 13,
            "Return": 13,
            "Escape": 27,
            "Space": 32,
        },

        /**
         * Invokes the native click callback.
         * @param {MouseEvent} m 
         */
        InvokeClickCallbackFromMouse: function(m) {
            if (m.button == 0 && NWICache.nativeClickCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(NWICache.nativeClickCallback, x, y);
            }
        },

        /**
         * Invokes the native click callback.
         * @param {TouchEvent} m 
         */
         InvokeClickCallbackFromTouch: function(m) {
            if (NWICache.nativeClickCallback) {
                /** @type {HTMLCanvasElement} */
                var element = m.currentTarget;
                var touch = m.targetTouches[0];
                var x = (touch.clientX - element.clientLeft) / element.clientWidth;
                var y = 1 - ((touch.clientY - element.clientTop) / element.clientHeight);
                dynCall_vff(NWICache.nativeClickCallback, x, y);
            }
        },

        /**
         * Invokes the native keyboard callback.
         * @param {KeyboardEvent} m
         */
        InvokeKeyboardCallback: function(m) {
            if (NWICache.nativeKeyboardCallback) {
                var codeVal = NWICache.keyCodeMapping[m.code];
                if (codeVal !== undefined) {
                    dynCall_vi(NWICache.nativeKeyboardCallback, codeVal | 0);
                }
            }
        }
    },

    /**
     * Registers the native click callback.
     * @param {Function} callback 
     */
    NativeWebInput_RegisterClick: function(callback) {
        NWICache.nativeClickCallback = callback;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("mousedown", NWICache.InvokeClickCallbackFromMouse);
            canvasElement.addEventListener("touchstart", NWICache.InvokeClickCallbackFromTouch);
        }
    },

    /**
     * Deregisters the native click callback.
     */
    NativeWebInput_DeregisterClick: function() {
        NWICache.nativeClickCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("mousedown", NWICache.InvokeClickCallbackFromMouse);
            canvasElement.removeEventListener("touchstart", NWICache.InvokeClickCallbackFromTouch);
        }
    },

    /**
     * Registers the native keyboard callback.
     * @param {Function} callback 
     */
     NativeWebInput_RegisterKeyboard: function(callback) {
        NWICache.nativeKeyboardCallback = callback;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("keypress", NWICache.InvokeKeyboardCallback);
        }
    },

    /**
     * Deregisters the native keyboard callback. 
     */
    NativeWebInput_DeregisterKeyboard: function(callback) {
        NWICache.nativeKeyboardCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("keypress", NWICache.InvokeKeyboardCallback);
        }
    }

};

autoAddDeps(NativeWebInputLib, '$NWICache');
mergeInto(LibraryManager.library, NativeWebInputLib);