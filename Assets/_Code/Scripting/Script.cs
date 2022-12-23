using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Aqua.Character;
using Aqua.Scripting;
using Aqua.View;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using Leaf;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua {
    static public class Script {
        static public readonly StringHash32 WorldTableId = "world";

        static public ILeafPlugin Plugin {
            [MethodImpl(256)]
            get { return Services.Script; }
        }

        static public bool IsLoading {
            [MethodImpl(256)]
            get { return StateUtil.IsLoading; }
        }

        static public bool IsPaused {
            [MethodImpl(256)]
            get { return Services.Pause.IsPaused(); }
        }

        static public bool IsPausedOrLoading {
            [MethodImpl(256)]
            get { return StateUtil.IsLoading || Services.Pause.IsPaused(); }
        }

        [LeafMember("ScriptBlocking"), Preserve]
        static public bool ShouldBlock() {
            return !Services.Valid || Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || Services.UI.IsLetterboxed() || StateUtil.IsLoading;
        }

        [LeafMember("ScriptBlockingIgnoreLetterbox"), Preserve]
        static public bool ShouldBlockIgnoreLetterbox() {
            return Services.Script.IsCutscene() || Services.UI.Popup.IsDisplaying() || StateUtil.IsLoading;
        }

        [MethodImpl(256)]
        static public void OnSceneLoad(Action action, int priority = 0) {
            Services.State.OnLoad(action, priority);
        }

        static public PlayerBody CurrentPlayer {
            [MethodImpl(256)]
            get { return Services.State.Player; }
        }

        #region Argument Parsing

        static public T ParseArg<T>(StringSlice inArg, object inContext, T inDefault = default(T)) {
            return LeafUtils.ParseArgument<T>(LeafEvalContext.FromObject(inContext, Services.Script), inArg, inDefault);
        }

        static public T ParseArg<T>(StringSlice inArg, T inDefault = default(T)) {
            return LeafUtils.ParseArgument<T>(LeafEvalContext.FromPlugin(Services.Script), inArg, inDefault);
        }

        #endregion // Argument Parsing

        #region Popups

        static public Future<StringHash32> PopupNewEntity(BestiaryDesc entity, string descriptionOverride = null, ListSlice<BFBase> extraFacts = default) {
            if (entity.HasFlags(BestiaryDescFlags.IsSpecter)) {
                return PopupNewSpecter(entity, descriptionOverride, extraFacts);
            }

            using (PooledList<BFBase> allFacts = PooledList<BFBase>.Create(entity.AssumedFacts)) {
                allFacts.AddRange(extraFacts);
                allFacts.Sort(BFType.SortByVisualOrder);
                if (entity.Category() == BestiaryDescCategory.Critter) {
                    return Services.UI.Popup.PresentFacts(
                        Loc.Format("ui.popup.newBestiary.critter.header", entity.CommonName()),
                        descriptionOverride ?? Loc.Find(entity.Description()),
                        entity.ImageSet(),
                        new PopupFacts(allFacts));
                } else {
                    return Services.UI.Popup.PresentFacts(
                        Loc.Format("ui.popup.newBestiary.env.header", entity.CommonName()),
                        descriptionOverride ?? Loc.Find(entity.Description()),
                        entity.ImageSet(),
                        new PopupFacts(allFacts));
                }
            }
        }

        static public Future<StringHash32> PopupNewSpecter(BestiaryDesc entity, string descriptionOverride = null, ListSlice<BFBase> extraFacts = default) {
            string header = Loc.Format("ui.popup.newBestiary.specter.header", Formatting.ScrambleLoc(entity.CommonName()));
            string text = Loc.Format("ui.popup.newBestiary.specter.description", Formatting.ScrambleLoc(entity.EncodedMessage()));
            return Services.UI.Popup.Present(header, text, entity.EncodedIcon(), PopupFlags.TallImage);
        }

        static public Future<StringHash32> PopupNewFact(BFBase fact, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.newFact.header"), textOverride, entity ? entity.ImageSet() : null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupNewFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), textOverride, entity ? entity.ImageSet() : null, new PopupFacts(facts, flags));
        }

        static public Future<StringHash32> PopupUpgradedFact(BFBase fact, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.upgradedFact.header"), textOverride, entity ? entity.ImageSet() : null, fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
        }

        static public Future<StringHash32> PopupUpgradedFacts(ListSlice<BFBase> facts, ListSlice<BFDiscoveredFlags> flags, BestiaryDesc entity = null, string textOverride = null) {
            return Services.UI.Popup.PresentFacts(Loc.Find("ui.popup.factsUpdated.header"), textOverride, entity ? entity.ImageSet() : null, new PopupFacts(facts, flags));
        }

        static public Future<StringHash32> PopupFactDetails(BFBase fact, BFDiscoveredFlags flags, BestiaryDesc reference, params NamedOption[] options) {
            BFDetails details = BFType.GenerateDetails(fact, flags, reference);
            bool showFact = (BFType.Flags(fact) & BFFlags.HideFactInDetails) == 0;

            if ((flags & BFDiscoveredFlags.IsEncrypted) != 0) {
                details.Header = Loc.Format("fact.encrypted.header", Formatting.Scramble(details.Header));
                details.Description = Loc.Format("fact.encrypted.description", Formatting.Scramble(details.Description));
                details.Image = default;

                return Services.UI.Popup.Present(
                    details.Header, details.Description, details.Image, PopupFlags.ShowCloseButton | PopupFlags.TallImage, options
                );
            }

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
                PopupFlags.TallImage | PopupFlags.ShowCloseButton | PopupFlags.ImageTextBG,
                options);
        }

        #endregion // Popups

        #region Regions

        static public IDisposable DisableInput() {
            Services.Input.PauseAll();
            return new CallOnDispose(() => Services.Input?.ResumeAll());
        }

        static public IDisposable Letterbox() {
            Services.UI.ShowLetterbox();
            return new CallOnDispose(() => Services.UI?.HideLetterbox());
        }

        #endregion // Regions

        #region Variables

        [MethodImpl(256)]
        static public Variant ReadVariable(TableKeyPair id, Variant defaultVal = default) {
            return Services.Data.GetVariable(id, null, defaultVal);
        }

        [MethodImpl(256)]
        static public Variant ReadVariable(StringSlice id, Variant defaultVal = default) {
            return Services.Data.GetVariable(id, null, defaultVal);
        }

        [MethodImpl(256)]
        static public void WriteVariable(TableKeyPair id, Variant value) {
            Services.Data?.SetVariable(id, value, null);
        }

        [MethodImpl(256)]
        static public void WriteVariable(StringSlice id, Variant value) {
            Services.Data?.SetVariable(id, value, null);
        }

        [MethodImpl(256)]
        static public Variant ReadWorldVariable(StringHash32 id, Variant defaultVal = default) {
            return Services.Data.GetVariable(new TableKeyPair(WorldTableId, id), null, defaultVal);
        }

        [MethodImpl(256)]
        static public Variant ReadWorldVariable(StringSlice id, Variant defaultVal = default) {
            return Services.Data.GetVariable(new TableKeyPair(WorldTableId, id), null, defaultVal);
        }

        [MethodImpl(256)]
        static public void WriteWorldVariable(StringHash32 id, Variant value) {
            Services.Data?.SetVariable(new TableKeyPair(WorldTableId, id), value, null);
        }

        [MethodImpl(256)]
        static public void WriteWorldVariable(StringSlice id, Variant value) {
            Services.Data?.SetVariable(new TableKeyPair(WorldTableId, id), value, null);
        }

        #endregion // Variables

        #region Inspection

        /// <summary>
        /// Interacts with an object.
        /// </summary>
        static public Routine Interact(ScriptInteractParams inParams, MonoBehaviour inHost = null) {
            Routine r = Routine.Start(inHost, InteractRoutine(inParams));
            r.Tick();
            return r;
        }

        /// <summary>
        /// Routine for interacting with an object.
        /// </summary>
        static public IEnumerator InteractRoutine(ScriptInteractParams inParams) {
            Script.PopCancel();

            inParams.Config.PreTrigger?.Invoke(ref inParams);

            ScriptThreadHandle thread;

            thread = ScriptObject.Interact(inParams.Source.Object.Parent, !inParams.Available, inParams.Config.TargetId);

            if (!inParams.Available) {
                IEnumerator locked = inParams.Config.OnLocked?.Invoke(inParams, thread);
                if (locked != null)
                    yield return null;

                if (thread.IsRunning())
                    yield return thread.Wait();
                yield break;
            }

            IEnumerator execute = inParams.Config.OnPerform?.Invoke(inParams, thread);
            if (execute != null)
                yield return execute;
            if (thread.IsRunning())
                yield return thread.Wait();

            if (Script.PopCancel()) {
                yield break;
            }

            switch (inParams.Config.Action) {
                case ScriptInteractAction.Inspect: {
                        thread = ScriptObject.Inspect(inParams.Source.Object.Parent);
                        yield return thread.Wait();
                        break;
                    }

                case ScriptInteractAction.Talk: {
                        thread = ScriptObject.Talk(inParams.Source.Object.Parent, inParams.Config.TargetId);
                        yield return thread.Wait();
                        break;
                    }

                case ScriptInteractAction.GoToPreviousScene: {
                        StateUtil.LoadPreviousSceneWithWipe(inParams.Config.TargetEntranceId, null, inParams.Config.LoadFlags);
                        break;
                    }

                case ScriptInteractAction.GoToMap: {
                        StateUtil.LoadMapWithWipe(inParams.Config.TargetId, inParams.Config.TargetEntranceId, null, inParams.Config.LoadFlags);
                        break;
                    }

                case ScriptInteractAction.GoToView: {
                        ViewManager.Find<ViewManager>().GoToNode(inParams.Source.Object.GetComponent<ViewLink>());
                        break;
                    }
            }
        }

        #endregion // Inspection

        #region Cancelling

        static private bool s_CancelInteraction = false;

        /// <summary>
        /// Retrieves the current cancel flag and resets it.
        /// </summary>
        static public bool PopCancel() {
            bool cancel = s_CancelInteraction;
            s_CancelInteraction = false;
            return cancel;
        }

        [LeafMember("CancelInteract"), Preserve]
        static public void QueueCancel() {
            s_CancelInteraction = true;
        }

        #endregion // Cancelling

        static public void Tick(this Routine routine) {
            routine.TryManuallyUpdate(0);
        }

        // Added by Xander 06/03/22
        [LeafMember, Preserve]
        static public bool IsPlayerOnShip() {
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            return ((currentMapId == MapIds.Helm) || (currentMapId == MapIds.Modeling) || (currentMapId == MapIds.Experimentation)
            || (currentMapId == MapIds.WorldMap)) || (currentMapId == MapIds.ModelingFoyer)
            || (currentMapId == MapIds.Cabin);
        }

        [LeafMember, Preserve]
        static public bool IsPlayerOnStation() {
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            return ((currentMapId == MapIds.RS_Kelp) || (currentMapId == MapIds.RS_Coral) ||
             (currentMapId == MapIds.RS_Bayou) || (currentMapId == MapIds.RS_Arctic));
        }
    }
}