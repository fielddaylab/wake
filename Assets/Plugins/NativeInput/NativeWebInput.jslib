var NativeWebInputLib = {

    $NWICache: {
        /**
         * @type {Function}
         */
        nativeMouseDownCallback: null,

        /**
         * @type {Function}
         */
        nativeMouseUpCallback: null,

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
        InvokeMouseDownCallbackFromMouse: function(m) {
            if (m.button == 0 && NWICache.nativeMouseDownCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(NWICache.nativeMouseDownCallback, x, y);
            }
        },

        /**
         * Invokes the native click callback.
         * @param {MouseEvent} m 
         */
        InvokeMouseUpCallbackFromMouse: function(m) {
            if (m.button == 0 && NWICache.nativeMouseUpCallback) {
                var element = m.currentTarget;
                var x = m.offsetX / element.clientWidth;
                var y = 1 - (m.offsetY / element.clientHeight);
                dynCall_vff(NWICache.nativeMouseUpCallback, x, y);
            }
        },

        /**
         * Invokes the native click callback.
         * @param {TouchEvent} m 
         */
         InvokeMouseDownCallbackFromTouch: function(m) {
            if (NWICache.nativeMouseDownCallback) {
                /** @type {HTMLCanvasElement} */
                var element = m.currentTarget;
                var touch = m.targetTouches[0];
                var x = (touch.clientX - element.clientLeft) / element.clientWidth;
                var y = 1 - ((touch.clientY - element.clientTop) / element.clientHeight);
                dynCall_vff(NWICache.nativeMouseDownCallback, x, y);
            }
        },

        /**
         * Invokes the native click callback.
         * @param {TouchEvent} m 
         */
        InvokeMouseUpCallbackFromTouch: function(m) {
            if (NWICache.nativeMouseUpCallback) {
                /** @type {HTMLCanvasElement} */
                var element = m.currentTarget;
                var touch = m.targetTouches[0];
                var x = (touch.clientX - element.clientLeft) / element.clientWidth;
                var y = 1 - ((touch.clientY - element.clientTop) / element.clientHeight);
                dynCall_vff(NWICache.nativeMouseUpCallback, x, y);
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
     * @param {Function} callbackDown
     * @param {Function} callbackUp
     */
    NativeWebInput_RegisterClick: function(callbackDown, callbackUp) {
        NWICache.nativeMouseDownCallback = callbackDown;
        NWICache.nativeMouseUpCallback = callbackUp;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.addEventListener("mousedown", NWICache.InvokeMouseDownCallbackFromMouse);
            canvasElement.addEventListener("mouseup", NWICache.InvokeMouseUpCallbackFromMouse);
            canvasElement.addEventListener("touchstart", NWICache.InvokeMouseDownCallbackFromTouch);
            canvasElement.addEventListener("touchend", NWICache.InvokeMouseUpCallbackFromTouch);
        }
    },

    /**
     * Deregisters the native click callback.
     */
    NativeWebInput_DeregisterClick: function() {
        NWICache.nativeMouseDownCallback = null;
        NWICache.nativeMouseUpCallback = null;

        var canvasElement = document.getElementById("#canvas");
        if (canvasElement) {
            canvasElement.removeEventListener("mousedown", NWICache.InvokeMouseDownCallbackFromMouse);
            canvasElement.removeEventListener("mouseup", NWICache.InvokeMouseUpCallbackFromMouse);
            canvasElement.removeEventListener("touchstart", NWICache.InvokeMouseDownCallbackFromTouch);
            canvasElement.removeEventListener("touchend", NWICache.InvokeMouseUpCallbackFromTouch);
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