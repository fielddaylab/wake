var NativePreloadLib = {

    $NPCache: {
        /**
         * @type {Map<string, HTMLLinkElement | HTMLAudioElement>}
        */
        preloadLinkMap: null,

        /**
         * @type {string[]}
         */
        resourceTypeStrings: [ "fetch",
            "audio",
            "image",
            "video"
        ],

        /**
         * @type {Set<string>}
         */
        preloadLinksLoaded: null,

        /**
         * @type {string}
         */
        preloadCrossOriginSetting: "anonymous",

        /**
         * 
         */
        Initialize: function() {
            if (!NPCache.preloadLinkMap) {
                NPCache.preloadLinkMap = new Map();
            }
            if (!NPCache.preloadLinksLoaded) {
                NPCache.preloadLinksLoaded = new Set();
            }
        },

        /**
         * 
         * @param {string} url 
         */
        NativePreloadOnLoad: function(url) {
            if (!NPCache.preloadLinkMap.has(url)) {
                return;
            }

            NPCache.preloadLinksLoaded.add(url);
        },

        /**
         * 
         * @param {string} url 
         */
        NativePreloadOnError: function(url) {
            if (!NPCache.preloadLinkMap.has(url)) {
                return;
            }

            NPCache.preloadLinksLoaded.add(url);
            console.error("[NativePreload] Error when loading", url);
        },

        /**
         * @param {string} path 
         * @param {string} ext
         * @return {string}
         */
        ChangeExtension: function(path, ext) {
            const idx = path.lastIndexOf(".");
            if (idx >= 0) {
                return path.substring(0, idx) + ext;
            } else {
                return path + ext;
            }
        }
    },

    /**
     * Begins preloading from the given url.
     * @param {string} url 
     * @param {number} resourceType
     */
    NativePreload_Start: function(url, resourceType) {
        NPCache.Initialize();

        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (!NPCache.preloadLinkMap.has(urlStr)) {
            var preloadElement;
            if (resourceType == 1) { // audio loads via audio
                preloadElement = new Audio();

                var oggSource = document.createElement("source");
                oggSource.src = NPCache.ChangeExtension(urlStr, ".ogg");
                oggSource.type = "audio/ogg";

                var mp3Source = document.createElement("source");
                mp3Source.src = NPCache.ChangeExtension(urlStr, ".mp3");
                mp3Source.type = "audio/mpeg";
                
                preloadElement.appendChild(oggSource);
                preloadElement.appendChild(mp3Source);

                preloadElement.autoplay = false;
                preloadElement.crossOrigin = NPCache.preloadCrossOriginSetting;
                preloadElement.load();
            } else { // everything else loads via link
                preloadElement = document.createElement("link");
                preloadElement.href = urlStr;
                preloadElement.rel = "preload";
                preloadElement.as = NPCache.resourceTypeStrings[resourceType | 0];
                preloadElement.crossOrigin = NPCache.preloadCrossOriginSetting;
            }

            NPCache.preloadLinkMap.set(urlStr, preloadElement);
            document.body.appendChild(preloadElement);

            if (resourceType != 1) {
                preloadElement.onload = function() {
                    NPCache.NativePreloadOnLoad(urlStr);
                };
                preloadElement.onerror = function() {
                    NPCache.NativePreloadOnError(urlStr);
                }
            }

            console.log("[NativePreload] Beginning preload of", urlStr);
        }
    },

    /**
     * Returns if the url is preloaded.
     * @param {string} url 
     */
    NativePreload_IsLoaded: function(url) {
        /** @type {string} */
        var urlStr = Pointer_stringify(url);
        
        if (NPCache.preloadLinkMap && NPCache.preloadLinkMap.has(urlStr)) {
            var preloadElement = NPCache.preloadLinkMap.get(urlStr);
            if (preloadElement instanceof HTMLAudioElement) {
                return preloadElement.readyState == 4;
            } else if (preloadElement instanceof HTMLLinkElement) {
                return NPCache.preloadLinkMap.has(urlStr);
            } else {
                return false;
            }
        } else {
            return false;
        }
    },

    /**
     * Cancels the preload for the given url.
     * @param {string} url
     */
    NativePreload_Cancel: function(url) {
        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (NPCache.preloadLinkMap && NPCache.preloadLinkMap.has(urlStr)) {
            var preloadElement = NPCache.preloadLinkMap.get(urlStr);
            preloadElement.onload = null;
            preloadElement.onerror = null;
            preloadElement.parentElement.removeChild(preloadElement);
            NPCache.preloadLinkMap.delete(urlStr);
            NPCache.preloadLinksLoaded.delete(urlStr);

            console.log("[NativePreload] Canceling preload of", urlStr);
        }
    }
}

autoAddDeps(NativePreloadLib, '$NPCache');
mergeInto(LibraryManager.library, NativePreloadLib);