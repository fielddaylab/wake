namespace Aqua {
    static public class Script {
        static public bool ShouldBlock() {
            return Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.UI.IsLetterboxed() || Services.State.IsLoadingScene();
        }
    }
}