var NativePreloadLib = {

    $Cache: {
        /**
         * @type {Map<string, HTMLLinkElement>}
        */
        preloadLinkMap: null
    },

    /**
     * Begins preloading from the given url.
     * @param {string} url 
     */
    NativePreload_Start: function(url) {
        if (!Cache.preloadLinkMap) {
            Cache.preloadLinkMap = new Map();
        }

        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (!Cache.preloadLinkMap.has(urlStr)) {
            var preloadElement = document.createElement("link");
            preloadElement.href = urlStr;
            preloadElement.rel = "preload";
            document.head.appendChild(preloadElement);
            Cache.preloadLinkMap.set(urlStr, preloadElement);

            console.log("[NativePreload] Beginning preload of", urlStr);
        }
    },

    /**
     * Cancels the preload for the given url.
     * @param {string} url
     */
    NativePreload_Cancel: function(url) {
        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (Cache.preloadLinkMap && Cache.preloadLinkMap.has(urlStr)) {
            var preloadElement = Cache.preloadLinkMap.get(urlStr);
            document.head.removeChild(preloadElement);
            Cache.preloadLinkMap.delete(urlStr);

            console.log("[NativePreload] Canceling preload of", urlStr);
        }
    }
}

autoAddDeps(NativePreloadLib, '$Cache');
mergeInto(LibraryManager.library, NativePreloadLib);