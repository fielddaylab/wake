using BeauRoutine;
using BeauUtil;

namespace Aqua {
    static public class Script {
        static public bool ShouldBlock() {
            return Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.UI.IsLetterboxed() || Services.State.IsLoadingScene();
        }

        static public bool ShouldBlockIgnoreLetterbox() {
            return Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.State.IsLoadingScene();
        }

        static public Future<StringHash32> PopupNewEntity(BestiaryDesc entity, string descriptionOverride = null) {
            if (entity.Category() == BestiaryDescCategory.Critter)
            {
                return Services.UI.Popup.PresentFacts(
                    Loc.Format("ui.popup.newBestiary.critter.header", entity.CommonName()),
                    descriptionOverride ?? Loc.Find(entity.Description()),
                    entity.ImageSet(),
                    entity.AssumedFacts);
            }
            else
            {
                return Services.UI.Popup.PresentFacts(
                    Loc.Format("ui.popup.newBestiary.env.header", entity.CommonName()),
                    descriptionOverride ?? Loc.Find(entity.Description()),
                    entity.ImageSet(),
                    entity.AssumedFacts);
            }
        }

        static public Future<StringHash32> PopupNewFact(BFBase fact) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.newFact.header"), null, null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupNewFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), null, null, facts, flags);
        }

        static public Future<StringHash32> PopupUpgradedFact(BFBase fact) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.upgradedFact.header"), null, null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupUpgradedFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), null, null, facts, flags);
        }
    }
}