var BugReporterLib = {

    BugReporter_DisplayDocument: function(documentSrc) {
        /**
         * @type {String}
         */
        var docSrcStr = Pointer_stringify(documentSrc);
        docStr = docSrcStr.trim();
        if (docSrcStr.startsWith("<html>")) {
            docSrcStr = docSrcStr.substring(6);
        }
        if (docSrcStr.endsWith("</html>")) {
            docSrcStr = docSrcStr.substring(0, docSrcStr.length - 7);
        }

        var newWindow = window.open(null, "_blank");
        if (newWindow != null) {
            newWindow.document.write(docSrcStr);
            newWindow.document.close();
        } else {
            alert("Bug Reporting uses a popup to report context. Please disable your popup blocker and try again.");
        }
    }
    
};

mergeInto(LibraryManager.library, BugReporterLib);