using Aqua.Profile;
using Aqua.Scripting;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Leaf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        static public class ColorTags
        {
            public const string PropertyColorString = "#bb8fce";
            public const string CritterColorString = "#fff27e";
            public const string AlertColorString = "#db3553";
            public const string EnvColorString = "#f57c18";
            public const string ItemColorString = "#00ffc8";
            public const string MapColorString = "#ffccf9";
            public const string CashColorString = "#e5cf12";
            public const string ExpColorString = "#a1ff29";
        }

        #region Parser

        private void InitParsers()
        {
            m_TagEventParser = new CustomTagParserConfig();

            LeafUtils.ConfigureDefaultParsers(m_TagEventParser, this, (k, o) => Loc.Find(k, o));

            // Replace Tags
            m_TagEventParser.AddReplace("n", "\n").WithAliases("newline");
            m_TagEventParser.AddReplace("highlight", "<color=yellow>").WithAliases("h").CloseWith("</color>");
            m_TagEventParser.AddReplace("property-name", "<" + ColorTags.PropertyColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("critter-name", "<" + ColorTags.CritterColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("!", "<" + ColorTags.AlertColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("env-name", "<" + ColorTags.EnvColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("item", "<" + ColorTags.ItemColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("item-name", "<" + ColorTags.ItemColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("map-name", "<" + ColorTags.MapColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("m", "<" + ColorTags.MapColorString + ">").CloseWith("</color>");
            m_TagEventParser.AddReplace("player-name", () => Save.Name);
            m_TagEventParser.AddReplace("cash", "<" + ColorTags.CashColorString + ">").CloseWith("</color><sprite name=\"cash\">");
            m_TagEventParser.AddReplace("exp", "<" + ColorTags.ExpColorString + ">").CloseWith("</color><sprite name=\"exp\">");
            m_TagEventParser.AddReplace("pg", ReplacePlayerGender);
            m_TagEventParser.AddReplace("icon", ReplaceIcon);
            m_TagEventParser.AddReplace("nameof", ReplaceNameOf);
            m_TagEventParser.AddReplace("pluralnameof", ReplacePluralNameOf);
            m_TagEventParser.AddReplace("fullnameof", ReplaceFullNameOf);
            m_TagEventParser.AddReplace("item-count", ReplaceItemCount);
            m_TagEventParser.AddReplace('|', "{wait 0.25}");

            // Extra Replace Tags (with embedded events)

            m_TagEventParser.AddReplace("slow", "{wait 0.05}{speed 0.5}").CloseWith("{/speed}{wait 0.05}");
            m_TagEventParser.AddReplace("reallySlow", "{wait 0.05}{speed 0.25}").CloseWith("{/speed}{wait 0.05}");
            m_TagEventParser.AddReplace("fast", "{wait 0.05}{speed 1.25}").CloseWith("{/speed}{wait 0.05}");

            // Global Events
            m_TagEventParser.AddEvent("bgm-pitch", ScriptEvents.Global.PitchBGM).WithStringData();
            m_TagEventParser.AddEvent("bgm-volume", ScriptEvents.Global.VolumeBGM).WithStringData();
            m_TagEventParser.AddEvent("bgm-stop", ScriptEvents.Global.StopBGM).WithFloatData(0.5f);
            m_TagEventParser.AddEvent("bgm", ScriptEvents.Global.PlayBGM).WithStringData();
            m_TagEventParser.AddEvent("hide-dialog", ScriptEvents.Global.HideDialog);
            m_TagEventParser.AddEvent("release-dialog", ScriptEvents.Global.ReleaseDialog);
            m_TagEventParser.AddEvent("sfx", ScriptEvents.Global.PlaySound).WithStringData();
            m_TagEventParser.AddEvent("show-dialog", ScriptEvents.Global.ShowDialog);
            m_TagEventParser.AddEvent("wait-abs", ScriptEvents.Global.WaitAbsolute).WithFloatData(0.25f);
            m_TagEventParser.AddEvent("cutscene", ScriptEvents.Global.CutsceneOn).CloseWith(ScriptEvents.Global.CutsceneOff);
            m_TagEventParser.AddEvent("enable-object", ScriptEvents.Global.EnableObject).WithStringData();
            m_TagEventParser.AddEvent("disable-object", ScriptEvents.Global.DisableObject).WithStringData();
            m_TagEventParser.AddEvent("broadcast-event", ScriptEvents.Global.BroadcastEvent).WithStringData();
            m_TagEventParser.AddEvent("fade-out", ScriptEvents.Global.FadeOut).WithStringData();
            m_TagEventParser.AddEvent("fade-in", ScriptEvents.Global.FadeIn).WithStringData();
            m_TagEventParser.AddEvent("wipe-out", ScriptEvents.Global.ScreenWipeOut).WithStringData();
            m_TagEventParser.AddEvent("wipe-in", ScriptEvents.Global.ScreenWipeIn).WithStringData();
            m_TagEventParser.AddEvent("screen-flash", ScriptEvents.Global.ScreenFlash).WithStringData();
            m_TagEventParser.AddEvent("trigger-response", ScriptEvents.Global.TriggerResponse).WithStringData();
            m_TagEventParser.AddEvent("style", ScriptEvents.Global.BoxStyle).WithStringHashData();

            // Dialog-Specific Events
            m_TagEventParser.AddEvent("auto", ScriptEvents.Dialog.Auto);
            m_TagEventParser.AddEvent("clear", ScriptEvents.Dialog.Clear);
            m_TagEventParser.AddEvent("continue", ScriptEvents.Dialog.InputContinue);
            m_TagEventParser.AddEvent("speaker", ScriptEvents.Dialog.Speaker).WithStringData();
            m_TagEventParser.AddEvent("speed", ScriptEvents.Dialog.Speed).WithFloatData(1);
            m_TagEventParser.AddEvent("sticky", ScriptEvents.Dialog.DoNotClose);
            m_TagEventParser.AddEvent("type", ScriptEvents.Dialog.SetTypeSFX).WithStringHashData();
            m_TagEventParser.AddEvent("voice", ScriptEvents.Dialog.SetVoiceType).WithStringHashData("default");
        }

        #endregion // Parser

        #region Replace Callbacks

        static private string ReplacePlayerGender(TagData inTag, object inContext)
        {
            TempList16<StringSlice> slices = new TempList16<StringSlice>();
            int sliceCount = inTag.Data.Split(ChoiceSplitChars, StringSplitOptions.None, ref slices);
            Pronouns playerPronouns = Save.Pronouns;
            
            if (sliceCount < 3)
            {
                Log.Warn("[ScriptingService] Expected 3 arguments to '{0}' tag", inTag.Id);
                if (sliceCount == 0)
                {
                    return playerPronouns.ToString();
                }
                else if (sliceCount == 1)
                {
                    return slices[0].ToString();
                }
                else
                {
                    switch(playerPronouns)
                    {
                        case Pronouns.Masculine:
                        default:
                            return slices[0].ToString();
                        case Pronouns.Feminine:
                            return slices[1].ToString();
                    }
                }
            }
            else
            {
                switch(playerPronouns)
                {
                    case Pronouns.Masculine:
                        return slices[0].ToString();
                    case Pronouns.Feminine:
                        return slices[1].ToString();
                    default:
                        return slices[2].ToString();
                }
            }
        }

        static private string ReplaceIcon(TagData inTag, object inContext)
        {
            return string.Format("<sprite name=\"{0}\">", inTag.Data.ToString());
        }

        static private string ReplaceItemCount(TagData inTag, object inContext)
        {
            StringHash32 itemId = Script.ParseArg<StringHash32>(inTag.Data);
            InvItem itemDesc = Assets.Item(itemId);
            int itemCount = (int) Save.Inventory.ItemCount(itemId);
            
            if (itemId == ItemIds.Cash)
            {
                return string.Format("<" + ColorTags.CashColorString + ">" + "{0}</color><sprite name=\"cash\">", itemCount);
            }
            else if (itemId == ItemIds.Exp)
            {
                return string.Format("<" + ColorTags.ExpColorString + ">" + "{0}</color><sprite name=\"exp\">", itemCount);
            }
            else if (itemDesc.Category() == InvItemCategory.Upgrade)
            {
                return string.Format("<" + ColorTags.ItemColorString + ">" + "{0}</color>", Loc.Find(itemDesc.NameTextId()));
            }
            else
            {
                return string.Format("<" + ColorTags.ItemColorString + ">" + "{0} {1}</color>", itemCount, Loc.Find(itemCount == 1 ? itemDesc.NameTextId() : itemDesc.PluralNameTextId()));
            }
        }

        static private string ReplaceNameOf(TagData inTag, object inContext)
        {
            if (inTag.Data.StartsWith('@'))
            {
                StringHash32 characterId = inTag.Data.Substring(1);
                return Loc.Find(Assets.Character(characterId).ShortNameId());
            }

            ScriptableObject obj = Assets.Find(Script.ParseArg<StringHash32>(inTag.Data, inContext));
            
            BestiaryDesc bestiary = obj as BestiaryDesc;
            if (!bestiary.IsReferenceNull())
            {
                switch(bestiary.Category())
                {
                    case BestiaryDescCategory.Critter:
                        return Loc.FormatFromString("<" + ColorTags.CritterColorString + ">" + "{0}</color>", bestiary.CommonName());
                    case BestiaryDescCategory.Environment:
                        return Loc.FormatFromString("<" + ColorTags.EnvColorString + ">" + "{0}</color>", bestiary.CommonName());
                    default:
                        return Loc.Find(bestiary.CommonName());
                }
            }

            InvItem item = obj as InvItem;
            if (!item.IsReferenceNull())
            {
                if (item.Id() == ItemIds.Cash)
                    return Loc.FormatFromString("<" + ColorTags.CashColorString + ">" + "{0}</color><sprite name=\"cash\">", item.NameTextId());
                else if (item.Id() == ItemIds.Exp)
                    return Loc.FormatFromString("<" + ColorTags.ExpColorString + ">" + "{0}</color><sprite name=\"exp\">", item.NameTextId());
                else
                    return Loc.FormatFromString("<" + ColorTags.ItemColorString + ">" + "{0}</color>", item.NameTextId());
            }

            WaterPropertyDesc property = obj as WaterPropertyDesc;
            if (!property.IsReferenceNull())
            {
                return Loc.FormatFromString("<" + ColorTags.PropertyColorString + ">" + "{0}</color", property.LabelId());
            }

            MapDesc map = obj as MapDesc;
            if (!map.IsReferenceNull())
            {
                return Loc.FormatFromString("<" + ColorTags.MapColorString + ">" + "{0}</color>", map.LabelId());
            }

            ScriptCharacterDef charDef = obj as ScriptCharacterDef;
            if (!charDef.IsReferenceNull())
            {
                return Loc.Find(charDef.ShortNameId());
            }

            Log.Error("[ScriptingService] Unknown symbol to get name of: '{0}'", inTag.Data);
            return "[ERROR]";
        }

        static private string ReplacePluralNameOf(TagData inTag, object inContext)
        {
            ScriptableObject obj = Assets.Find(Script.ParseArg<StringHash32>(inTag.Data, inContext));
            
            BestiaryDesc bestiary = obj as BestiaryDesc;
            if (!bestiary.IsReferenceNull())
            {
                switch(bestiary.Category())
                {
                    case BestiaryDescCategory.Critter:
                        return Loc.FormatFromString("<" + ColorTags.CritterColorString + ">" + "{0}</color>", bestiary.PluralCommonName());
                    case BestiaryDescCategory.Environment:
                        return Loc.FormatFromString("<" + ColorTags.EnvColorString + ">" + "{0}</color>", bestiary.PluralCommonName());
                    default:
                        return Loc.Find(bestiary.PluralCommonName());
                }
            }

            InvItem item = obj as InvItem;
            if (!item.IsReferenceNull())
            {
                if (item.Id() == ItemIds.Cash)
                    return Loc.FormatFromString("<" + ColorTags.CashColorString + ">" + "{0}</color><sprite name=\"cash\">", item.PluralNameTextId());
                else if (item.Id() == ItemIds.Exp)
                    return Loc.FormatFromString("<" + ColorTags.ExpColorString + ">" + "{0}</color><sprite name=\"exp\">", item.PluralNameTextId());
                else
                    return Loc.FormatFromString("<" + ColorTags.ItemColorString + ">" + "{0}</color>", item.PluralNameTextId());
            }

            return ReplaceNameOf(inTag, inContext);
        }

        static private string ReplaceFullNameOf(TagData inTag, object inContext)
        {
            if (inTag.Data.StartsWith('@'))
            {
                StringHash32 characterId = inTag.Data.Substring(1);
                return Loc.Find(Assets.Character(characterId).NameId());
            }

            ScriptableObject obj = Assets.Find(Script.ParseArg<StringHash32>(inTag.Data, inContext));

            ScriptCharacterDef charDef = obj as ScriptCharacterDef;
            if (!charDef.IsReferenceNull())
            {
                return Loc.Find(charDef.NameId());
            }

            return ReplaceNameOf(inTag, inContext);
        }

        static private readonly char[] ChoiceSplitChars = new char[] { '|' };

        #endregion // Replace Callbacks

        #region Event Setup

        private void InitHandlers()
        {
            m_TagEventHandler = new TagStringEventHandler();

            LeafUtils.ConfigureDefaultHandlers(m_TagEventHandler, this);

            m_TagEventHandler
                .Register(ScriptEvents.Global.HideDialog, EventHideDialog)
                .Register(ScriptEvents.Global.ReleaseDialog, EventReleaseDialog)
                .Register(ScriptEvents.Global.ShowDialog, EventShowDialog)
                .Register(ScriptEvents.Global.CutsceneOff, (e, o) => Thread(o).PopCutscene() )
                .Register(ScriptEvents.Global.CutsceneOn, (e, o) => Thread(o).PushCutscene() )
                .Register(ScriptEvents.Global.PitchBGM, EventPitchBGM)
                .Register(ScriptEvents.Global.VolumeBGM, EventVolumeBGM)
                .Register(ScriptEvents.Global.PlayBGM, EventPlayBGM)
                .Register(ScriptEvents.Global.PlaySound, EventPlaySound)
                .Register(ScriptEvents.Global.StopBGM, (e, o) => { Services.Audio.StopMusic(e.Argument0.AsFloat()); })
                .Register(ScriptEvents.Global.WaitAbsolute, (e, o) => { return Routine.WaitRealSeconds(e.Argument0.AsFloat()); })
                .Register(ScriptEvents.Global.BroadcastEvent, EventBroadcastEvent)
                .Register(ScriptEvents.Global.TriggerResponse, EventTriggerResponse)
                .Register(ScriptEvents.Global.BoxStyle, EventSetBoxStyle)
                .Register(ScriptEvents.Global.ScreenWipeOut, EventScreenWipeOut)
                .Register(ScriptEvents.Global.ScreenWipeIn, EventScreenWipeIn)
                .Register(ScriptEvents.Global.ScreenFlash, EventScreenFlash)
                .Register(ScriptEvents.Global.FadeOut, EventFadeOut)
                .Register(ScriptEvents.Global.FadeIn, EventFadeIn)
                .Register(ScriptEvents.Global.EnableObject, EventEnableObject)
                .Register(ScriptEvents.Global.DisableObject, EventDisableObject);

            m_SkippedEvents = new HashSet<StringHash32>();
            m_SkippedEvents.Add(ScriptEvents.Global.CutsceneOn);
            m_SkippedEvents.Add(ScriptEvents.Global.CutsceneOff);
            m_SkippedEvents.Add(ScriptEvents.Global.PlaySound);
            m_SkippedEvents.Add(LeafUtils.Events.Wait);
            m_SkippedEvents.Add(ScriptEvents.Global.WaitAbsolute);
            m_SkippedEvents.Add(ScriptEvents.Global.BoxStyle);
            m_SkippedEvents.Add(ScriptEvents.Global.ScreenFlash);
            m_SkippedEvents.Add(ScriptEvents.Global.HideDialog);
            m_SkippedEvents.Add(ScriptEvents.Global.ShowDialog);
            m_SkippedEvents.Add(ScriptEvents.Dialog.Auto);
            m_SkippedEvents.Add(ScriptEvents.Dialog.Clear);
            m_SkippedEvents.Add(ScriptEvents.Dialog.InputContinue);
            m_SkippedEvents.Add(ScriptEvents.Dialog.SetTypeSFX);
            m_SkippedEvents.Add(ScriptEvents.Dialog.SetVoiceType);
            m_SkippedEvents.Add(ScriptEvents.Dialog.Speaker);
            m_SkippedEvents.Add(ScriptEvents.Dialog.Speed);
            m_SkippedEvents.Add(LeafUtils.Events.Character);
            m_SkippedEvents.Add(LeafUtils.Events.Pose);

            m_DialogOnlyEvents = new HashSet<StringHash32>();
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.Auto);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.Clear);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.InputContinue);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.SetTypeSFX);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.DoNotClose);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.SetVoiceType);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.Speaker);
            m_DialogOnlyEvents.Add(ScriptEvents.Dialog.Speed);
            m_DialogOnlyEvents.Add(LeafUtils.Events.Character);
            m_DialogOnlyEvents.Add(LeafUtils.Events.Pose);
        }

        #endregion // Event Setup
    
        #region Event Callbacks

        static private ScriptThread Thread(object inObject)
        {
            return inObject as ScriptThread;
        }

        static private IVariantResolver Resolver(object inObject)
        {
            IVariantResolver resolver = inObject as IVariantResolver;
            return resolver ?? Thread(inObject)?.Resolver;
        }


        private void EventHideDialog(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog?.Hide();
        }

        private void EventReleaseDialog(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog = null;
        }

        private void EventShowDialog(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog?.Show();
        }

        private IEnumerator EventPlaySound(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();

            StringHash32 id = args[0];
            AudioHandle playback = Services.Audio.PostEvent(id);
            if (args.Count > 1 && args[1] == "wait")
            {
                return playback.Wait();
            }

            return null;
        }

        private void EventPlayBGM(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();
            if (args.Count > 0)
            {
                float crossFadeTime = 0.5f;
                string name = args[0].ToString();
                if (args.Count >= 2)
                {
                    crossFadeTime = StringParser.ParseFloat(args[1], 0.5f);
                }
                Services.Audio.SetMusic(name, crossFadeTime);
            }
        }

        private void EventPitchBGM(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();
            float pitch = 1;
            float fadeTime = 0.5f;
            if (args.Count >= 1)
            {
                pitch = StringParser.ParseFloat(args[0], 1);
            }
            if (args.Count >= 2)
            {
                fadeTime = StringParser.ParseFloat(args[1], 0.5f);
            }
            Services.Audio.CurrentMusic().SetPitch(pitch, fadeTime);
        }

        private void EventVolumeBGM(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();
            float volume = 1;
            float fadeTime = 0.5f;
            if (args.Count >= 1)
            {
                volume = StringParser.ParseFloat(args[0], 1);
            }
            if (args.Count >= 2)
            {
                fadeTime = StringParser.ParseFloat(args[1], 0.5f);
            }
            Services.Audio.CurrentMusic().SetVolume(volume, fadeTime);
        }

        private void EventBroadcastEvent(TagEventData inEvent, object inContext)
        {
            Services.Events.Dispatch(inEvent.StringArgument);
        }

        private IEnumerator EventTriggerResponse(TagEventData inEvent, object inContext)
        {
            ScriptThread thread = Thread(inContext);
            var args = inEvent.ExtractStringArgs();

            StringHash32 trigger = args[0];
            StringHash32 who = args.Count > 1 ? args[1] : (thread != null ? thread.Target() : StringHash32.Null);
            bool bWait = args.Count > 2 && args[2] == "wait";

            var response = Services.Script.TriggerResponse(trigger, who, thread?.Actor, thread?.Locals);
            if (response.IsRunning() && bWait)
                return response.Wait();
            
            return null;
        }

        private void EventSetBoxStyle(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog = Services.UI.GetDialog(inEvent.Argument0.AsStringHash());
        }

        private IEnumerator EventScreenWipeOut(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();

            var faderSource = Services.UI.WorldFaders;
            if (args.Count > 0 && args[0] == "above-ui")
            {
                faderSource = Services.UI.ScreenFaders;
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenWipe == null)
            {
                var wipe = faderSource.AllocWipe().Object;
                thread.ScreenWipe = wipe;
                return wipe.Show();
            }
            return null;
        }

        private IEnumerator EventScreenWipeIn(TagEventData inEvent, object inContext)
        {
            var thread = Thread(inContext);
            if (thread.ScreenWipe != null)
            {
                IEnumerator hide = thread.ScreenWipe.Hide(true);
                thread.ClearWipeWithoutHide();
                return hide;
            }
            return null;
        }

        private IEnumerator EventScreenFlash(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();

            Color color = Parsing.ParseColor(args[0]);
            float duration = StringParser.ParseFloat(args[1], 0.2f);

            var faders = Services.UI.WorldFaders;
            bool bWait = false;
            for(int i = 2; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "above-ui")
                {
                    faders = Services.UI.ScreenFaders;
                }
                else if (arg == "wait")
                {
                    bWait = true;
                }
            }

            IEnumerator flash = faders.Flash(color, duration);
            if (bWait)
                return flash;
            return null;
        }

        private IEnumerator EventFadeOut(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();

            Color color = Parsing.ParseColor(args[0]);
            float duration = StringParser.ParseFloat(args[1], 0.2f);

            var faders = Services.UI.WorldFaders;
            bool bWait = false;
            for(int i = 2; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "above-ui")
                {
                    faders = Services.UI.ScreenFaders;
                }
                else if (arg == "wait")
                {
                    bWait = true;
                }
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenFader == null)
            {
                var fader = faders.AllocFader().Object;
                thread.ScreenFader = fader;
            }

            IEnumerator fade = thread.ScreenFader.Show(color, duration);
            if (bWait)
                return fade;
            return null;
        }

        private IEnumerator EventFadeIn(TagEventData inEvent, object inContext)
        {
            var args = inEvent.ExtractStringArgs();

            float duration = StringParser.ParseFloat(args[0], 0.2f);

            bool bWait = false;
            for(int i = 1; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "wait")
                {
                    bWait = true;
                }
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenFader != null)
            {
                var fader = thread.ScreenFader;
                thread.ClearFaderWithoutHide();
                IEnumerator fadeRoutine = fader.Hide(duration, true);
                if (bWait)
                    return fadeRoutine;
            }

            return null;
        }

        private void EventEnableObject(TagEventData inEvent, object inContext)
        {
            EventEnableDisableObjectImpl(inEvent, inContext, true);
        }

        private void EventDisableObject(TagEventData inEvent, object inContext)
        {
            EventEnableDisableObjectImpl(inEvent, inContext, false);
        }

        private void EventEnableDisableObjectImpl(TagEventData inEvent, object inContext, bool inbActive)
        {
            var ids = inEvent.ExtractStringArgs();
            foreach(var id in ids)
            {
                foreach(var scriptObject in GetScriptObjects(id))
                {
                    scriptObject.gameObject.SetActive(inbActive);
                }
            }
        }

        #endregion // Event Callbacks
    }
}