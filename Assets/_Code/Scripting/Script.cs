using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using Leaf;
using Leaf.Runtime;

namespace Aqua {
    static public class Script {
        static public ILeafPlugin Plugin {
            [MethodImpl(256)] get { return Services.Script; }
        }

        static public bool IsLoading {
            [MethodImpl(256)] get { return Services.State.IsLoadingScene(); }
        }

        static public bool IsPaused {
            [MethodImpl(256)] get { return Services.Pause.IsPaused(); }
        }

        static public T ParseArg<T>(StringSlice inArg, object inContext, T inDefault = default(T)) {
            return LeafUtils.ParseArgument<T>(LeafEvalContext.FromObject(inContext, Services.Script), inArg, inDefault);
        }

        static public T ParseArg<T>(StringSlice inArg, T inDefault = default(T)) {
            return LeafUtils.ParseArgument<T>(LeafEvalContext.FromPlugin(Services.Script), inArg, inDefault);
        }

        static public bool ShouldBlock() {
            return !Services.Valid || Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.UI.IsLetterboxed() || Services.State.IsLoadingScene();
        }

        static public bool ShouldBlockIgnoreLetterbox() {
            return Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.State.IsLoadingScene();
        }

        static public Future<StringHash32> PopupNewEntity(BestiaryDesc entity, string descriptionOverride = null, ListSlice<BFBase> extraFacts = default) {
            using(PooledList<BFBase> allFacts = PooledList<BFBase>.Create(entity.AssumedFacts)) {
                allFacts.AddRange(extraFacts);
                allFacts.Sort(BFType.SortByVisualOrder);
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
                    details, fact, flags, PopupFlags.ShowCloseButton | PopupFlags.TallImage, options
                );
            } else {
                return Services.UI.Popup.Present(
                    details.Header, details.Description, details.Image, PopupFlags.ShowCloseButton | PopupFlags.TallImage, options
                );
            }
        }

        static public Future<StringHash32> PopupItemDetails(InvItem item, params NamedOption[] options) {
            return Services.UI.Popup.Present(
                Loc.Find(item.NameTextId()),
                Loc.Find(item.DescriptionTextId()),
                item.ImageSet(),
                PopupFlags.TallImage | PopupFlags.ShowCloseButton,
                options);
        }

        static public void OnSceneLoad(Action action) {
            Services.State.OnLoad(action);
        }

        static public IDisposable DisableInput() {
            Services.Input.PauseAll();
            return new CallOnDispose(() => Services.Input?.ResumeAll());
        }

        static public IDisposable Letterbox() {
            Services.UI.ShowLetterbox();
            return new CallOnDispose(() => Services.UI?.HideLetterbox());
        }

        static public void Tick(this Routine routine) {
            routine.TryManuallyUpdate(0);
        }
    }
}