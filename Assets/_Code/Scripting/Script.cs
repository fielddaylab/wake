using BeauPools;
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

        static public Future<StringHash32> PopupNewEntity(BestiaryDesc entity, string descriptionOverride = null, ListSlice<BFBase> extraFacts = default) {
            using(PooledList<BFBase> allFacts = PooledList<BFBase>.Create(entity.AssumedFacts)) {
                allFacts.AddRange(extraFacts);
                if (entity.Category() == BestiaryDescCategory.Critter) {
                    return Services.UI.Popup.PresentFacts(
                        Loc.Format("ui.popup.newBestiary.critter.header", entity.CommonName()),
                        descriptionOverride ?? Loc.Find(entity.Description()),
                        entity.ImageSet(),
                        allFacts);
                } else {
                    return Services.UI.Popup.PresentFacts(
                        Loc.Format("ui.popup.newBestiary.env.header", entity.CommonName()),
                        descriptionOverride ?? Loc.Find(entity.Description()),
                        entity.ImageSet(),
                        allFacts);
                }
            }
        }

        static public Future<StringHash32> PopupNewFact(BFBase fact, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.newFact.header"), textOverride, entity ? entity.ImageSet() : null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupNewFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), textOverride, entity ? entity.ImageSet() : null, facts, flags);
        }

        static public Future<StringHash32> PopupUpgradedFact(BFBase fact, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.upgradedFact.header"), textOverride, entity ? entity.ImageSet() : null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupUpgradedFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), textOverride, entity ? entity.ImageSet() : null, facts, flags);
        }

        static public Future<StringHash32> PopupFactDetails(BFBase fact, BFDiscoveredFlags flags, params NamedOption[] options) {
            BFDetails details = BFType.GenerateDetails(fact, flags);
            bool showFact = (BFType.Flags(fact) & BFFlags.HideFactInDetails) == 0;

            if (showFact) {
                return Services.UI.Popup.PresentFactDetails(
                    details, fact, flags, PopupFlags.ShowCloseButton, options
                );
            } else {
                return Services.UI.Popup.Present(
                    details.Header, details.Description, details.Image, PopupFlags.ShowCloseButton, options
                );
            }
        }
    }
}