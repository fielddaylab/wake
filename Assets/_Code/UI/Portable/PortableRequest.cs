using System;
using BeauRoutine;
using BeauUtil;

namespace Aqua.Portable
{
    public struct PortableRequest : IDisposable {
        public PortableRequestType Type;
        public PortableAppId App;
        public PortableRequestFlags Flags;
        public StringHash32 TargetId;
        public Future<StringHash32> Response;
        public BestiaryApp.CanSelectFactDelegate CanSelect;
        public BestiaryApp.SelectForSetDelegate OnSelect;

        public void Dispose() {
            Ref.Dispose(ref Response);
            Type = PortableRequestType.None;
            Flags = 0;
            TargetId = null;
        }

        static public PortableRequest OpenApp(PortableAppId inAppId) {
            return new PortableRequest() {
                Type = PortableRequestType.OpenApp,
                App = inAppId
            };
        }

        static public PortableRequest SelectFact() {
            return new PortableRequest() {
                Type = PortableRequestType.SelectFact,
                App = PortableAppId.Organisms,
                Flags = PortableRequestFlags.ForceInputEnabled,
                Response = Future.Create<StringHash32>()
            };
        }

        static public PortableRequest SelectFactSet(BestiaryApp.SelectForSetDelegate inSelectDelegate) {
            return new PortableRequest() {
                Type = PortableRequestType.SelectFactSet,
                App = PortableAppId.Organisms,
                Flags = PortableRequestFlags.ForceInputEnabled,
                Response = Future.Create<StringHash32>(),
                OnSelect = inSelectDelegate
            };
        }

        static public PortableRequest ShowFact(StringHash32 inFactId) {
            return new PortableRequest() {
                Type = PortableRequestType.ShowFact,
                App = FindAppForFact(inFactId),
                TargetId = inFactId
            };
        }

        static public PortableRequest ShowEntry(StringHash32 inEntryId) {
            return new PortableRequest() {
                Type = PortableRequestType.ShowBestiary,
                App = FindAppForEntry(inEntryId),
                TargetId = inEntryId
            };
        }

        // static public PortableRequest FromUpdate(BestiaryUpdateParams inUpdateParams) {
        //     switch(inUpdateParams.Type) {
        //         case BestiaryUpdateParams.UpdateType.Entity: {
        //             return ShowEntry(inUpdateParams.Id);
        //         }
        //         case BestiaryUpdateParams.UpdateType.Fact:
        //         case BestiaryUpdateParams.UpdateType.UpgradeFact: {
        //             return ShowFact(inUpdateParams.Id);
        //         }
        //         default: {
        //             return default;
        //         }
        //     }
        // }

        static private PortableAppId FindAppForFact(StringHash32 inFactId) {
            var fact = Assets.Fact(inFactId);
            return fact.Parent.Category() == BestiaryDescCategory.Critter ? PortableAppId.Organisms : PortableAppId.Environments;
        }

        static private PortableAppId FindAppForEntry(StringHash32 inEntryId) {
            var bestiary = Assets.Bestiary(inEntryId);
            return bestiary.Category() == BestiaryDescCategory.Critter ? PortableAppId.Organisms : PortableAppId.Environments;
        }
    }

    public enum PortableRequestType : byte {
        None = 0,

        OpenApp,
        ShowBestiary,
        ShowFact,
        SelectFact,
        SelectFactSet
    }

    public enum PortableRequestFlags : byte {
        DisableNavigation = 0x01,
        DisableClose = 0x02,
        ForceInputEnabled = 0x04
    }

    [LabeledEnum]
    public enum PortableAppId : byte {
        Organisms,
        Environments,
        Job,
        Tech,
        Options,
        Journal,

        [Hidden]
        NULL = 255
    }
}