var NativePreloadLib = {

    $Cache: {
        /**
         * @type {Map<string, HTMLLinkElement | HTMLAudioElement>}
        */
        preloadLinkMap: null,

        /**
         * @type {string[]}
         */
        resourceTypeStrings: null,

        /**
         * @type {string}
         */
        preloadCrossOriginSetting: null
    },

    /**
     * Begins preloading from the given url.
     * @param {string} url 
     * @param {number} resourceType
     */
    NativePreload_Start: function(url, resourceType) {
        if (!Cache.preloadLinkMap) {
            Cache.preloadLinkMap = new Map();
        }
        if (!Cache.resourceTypeStrings) {
            Cache.resourceTypeStrings = [
                "fetch",
                "audio",
                "image",
                "video"
            ];
        }
        if (!Cache.preloadCrossOriginSetting) {
            Cache.preloadCrossOriginSetting = "anonymous";
        }

        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (!Cache.preloadLinkMap.has(urlStr)) {
            var preloadElement;
            if (resourceType == 1) { // audio loads via audio
                preloadElement = new Audio();
                preloadElement.src = urlStr;
                preloadElement.autoplay = false;
                preloadElement.crossOrigin = Cache.preloadCrossOriginSetting;
            } else { // everything else loads via link
                preloadElement = document.createElement("link");
                preloadElement.href = urlStr;
                preloadElement.rel = "preload";
                preloadElement.as = Cache.resourceTypeStrings[resourceType | 0];
                preloadElement.crossOrigin = Cache.preloadCrossOriginSetting;
            }

            document.body.appendChild(preloadElement);
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
            preloadElement.parentElement.removeChild(preloadElement);
            Cache.preloadLinkMap.delete(urlStr);

            console.log("[NativePreload] Canceling preload of", urlStr);
        }
    }
}

autoAddDeps(NativePreloadLib, '$Cache');
mergeInto(LibraryManager.library, NativePreloadLib);