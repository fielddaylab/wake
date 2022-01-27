#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using BeauUtil;
using UnityEngine;

namespace Aqua.Testing {
    static public class JobValidation {
        #if DEVELOPMENT

        #region Steps

        static private IEnumerator LoadScene(StringSlice sceneName, StringHash32 entrance) {
            yield return StateUtil.LoadSceneWithWipe(sceneName.ToString(), entrance);
        }

        static private IEnumerator ExitScene(StringHash32 entrance) {
            yield return StateUtil.LoadPreviousSceneWithWipe(entrance);
        }

        static private void Give(StringHash32 assetId, StringSlice args) {
            var asset = Assets.Find(assetId);
            Type type = asset.GetType();
            if (type == typeof(BestiaryDesc)) {
                Save.Bestiary.RegisterEntity(assetId);
            } else if (typeof(BFBase).IsAssignableFrom(type)) {
                Save.Bestiary.RegisterFact(assetId);
                if (!args.IsEmpty) {
                    Save.Bestiary.AddDiscoveredFlags(assetId, StringParser.ConvertTo<BFDiscoveredFlags>(args));
                }
            } else if (type == typeof(InvItem)) {
                InvItem item = (InvItem) asset;
                switch(item.Category()) {
                    case InvItemCategory.Currency: {
                        Save.Inventory.AdjustItem(assetId, StringParser.ParseInt(args, 1));
                        break;
                    }
                    case InvItemCategory.Artifact: {
                        Save.Inventory.AdjustItem(assetId, StringParser.ParseInt(args, 1));
                        break;
                    }
                    case InvItemCategory.Upgrade: {
                        Save.Inventory.AddUpgrade(assetId);
                        break;
                    }
                }
            } else if (type == typeof(JobDesc)) {
                Save.Jobs.DebugMarkComplete(assetId);
            }
        }

        static private void FinishJob(StringHash32 jobId) {
            var job = Assets.Job(jobId);
            Save.Jobs.DebugMarkComplete(jobId);
        }

        static private void UnlockMap(StringHash32 mapId) {
            if (Assets.Has(mapId)) {
                MapDesc map = Assets.Map(mapId);
                switch(map.Category()) {
                    case MapCategory.Station: {
                        Save.Map.UnlockStation(mapId);
                        break;
                    }
                    case MapCategory.DiveSite: {
                        Save.Map.UnlockSite(mapId);
                        break;
                    }
                    default: {
                        Save.Map.UnlockRoom(mapId);
                        break;
                    }
                }
            } else {
                Save.Map.UnlockRoom(mapId);
            }
        }

        #endregion // Steps

        #endif // DEVELOPMENT
    }
}