using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Leaf;
using BeauUtil;
using BeauData;
using System.Text.RegularExpressions;
using BeauUtil.Debugger;
using System;
using UnityEngine;

namespace Aqua.Editor {
    static public class ProgressionGraph {

        public const int ExpForShop = 35;
        
        [MenuItem("Aqualab/Analysis/Generate Nodes File")]
        static private void GenerateProgressionSource() {
            JSON js = JSON.CreateObject();
            GenerateAssetsList(js["nodes"], js["startWith"]);
            using(var writer = new StreamWriter(File.Open("progression_nodes.json", FileMode.Create))) {
                js.WriteTo(writer, 4);
            }
            EditorUtility.OpenWithDefaultApp("progression_nodes.json");
        }

        static private void GenerateAssetsList(JSON nodes, JSON starting) {
            ValidationUtils.FindAsset<ScienceTweaks>().EditorApply();
            GenerateItemList(nodes, starting);
            GenerateJobsList(nodes, starting);
            GenerateMapList(nodes, starting);
        }

        static private void GenerateMapList(JSON root, JSON starting) {
            MapDB db = ValidationUtils.FindAsset<MapDB>();

            foreach(var room in db.Rooms()) {
                JSON mapJSON = JSON.CreateObject();
                mapJSON["type"].AsString = "map";
                if (db.DefaultUnlockedRooms().Contains(room)) {
                    starting[room.Source()].AsBool = true;
                } else {
                    mapJSON["unlockType"].AsString = "manual";
                }

                mapJSON["disableTraversal"].AsBool = true;

                root.Add(room.Source(), mapJSON);
            }

            foreach(var obj in ValidationUtils.FindAllAssets<MapDesc>(ValidationUtils.IgnoreTemplates)) {
                if (obj.Category() == MapCategory.ShipRoom) {
                    continue;
                }

                JSON mapJSON = JSON.CreateObject();
                mapJSON["type"].AsString = "map";

                bool isDefault = obj.Id() == db.DefaultStationId();
                bool isAtNonDefaultStation = obj.Category() == MapCategory.DiveSite && obj.Parent().Id() != db.DefaultStationId();

                if (obj.Category() == MapCategory.DiveSite) {
                    mapJSON["disableTraversal"].AsBool = true;
                }

                if (isDefault) {
                    starting[obj.name].AsBool = true;
                } else if (isAtNonDefaultStation) {
                    AddRequirement(mapJSON, obj.Parent().name);
                } else if (obj.HasFlags(MapFlags.UnlockedByDefault)) {
                    starting[obj.name].AsBool = true;
                } else {
                    mapJSON["unlockType"].AsString = "manual";
                }

                if (obj.name == "Shop") {
                    mapJSON["unlockType"].AsString = "auto";
                    AddRequirement(mapJSON, "Exp", ExpForShop);
                }

                HandleSpecialMapRequirements(obj.name, mapJSON);

                root.Add(obj.name, mapJSON);
            }
        }

        static private void GenerateItemList(JSON root, JSON starting) {
            InventoryDB db = ValidationUtils.FindAsset<InventoryDB>();

            foreach(var obj in ValidationUtils.FindAllAssets<InvItem>(ValidationUtils.IgnoreTemplates)) {
                JSON itemJSON = JSON.CreateObject();
                if (obj.DefaultAmount() > 0) {
                    starting[obj.name].AsUInt = obj.DefaultAmount();
                }

                if (obj.HasFlags(InvItemFlags.Hidden) && obj.Id() != ItemIds.FlashlightCoordinates) {
                    itemJSON["unlockType"].AsString = "manual";
                }

                if (obj.Category() != InvItemCategory.Currency || obj.CashCost() > 0 || obj.RequiredLevel() > 0) {
                    itemJSON["type"].AsString = "item";
                    AddRequirement(itemJSON, "Shop");
                } else {
                    itemJSON["type"].AsString = "currency";
                    itemJSON["isToken"].AsBool = true;
                }

                if (obj.CashCost() > 0) {
                    AddRequirement(itemJSON, ItemIds.Cash, obj.CashCost(), true);
                }
                if (obj.RequiredLevel() > 0) {
                    AddRequirement(itemJSON, ItemIds.Exp, (int) ScienceUtils.TotalExpForLevel((uint) obj.RequiredLevel()));
                }
                if (obj.Prerequisite()) {
                    AddRequirement(itemJSON, obj.Prerequisite().name);
                }

                HandleSpecialItemUnlocks(obj.Id(), itemJSON);

                root.Add(obj.name, itemJSON);
            }
        }

        static private void GenerateJobsList(JSON root, JSON starting) {
            JobDB db = ValidationUtils.FindAsset<JobDB>();

            foreach(var obj in ValidationUtils.FindAllAssets<JobDesc>(ValidationUtils.IgnoreTemplates)) {
                JSON jobJSON = JSON.CreateObject();

                jobJSON["type"].AsString = "job";

                JSON difficulties = jobJSON["difficulty"];

                int totalDifficulty = obj.Difficulty(ScienceActivityType.Experimentation) + obj.Difficulty(ScienceActivityType.Modeling) + obj.Difficulty(ScienceActivityType.Argumentation);
                difficulties["total"].AsInt = totalDifficulty;
                difficulties["max"].AsInt = Mathf.Max(obj.Difficulty(ScienceActivityType.Experimentation), obj.Difficulty(ScienceActivityType.Modeling), obj.Difficulty(ScienceActivityType.Argumentation));

                if (!obj.StationId().IsEmpty) {
                    AddRequirement(jobJSON, obj.StationId());
                }

                if (obj.Id() == "kelp-shop-welcome") {
                    AddRequirement(jobJSON, "VisualModel");
                    AddRequirement(jobJSON, "ROVTagger");
                }

                foreach(var prereq in obj.RequiredJobs()) {
                    AddRequirement(jobJSON, prereq.name);
                }
                foreach(var upgrade in obj.RequiredUpgrades()) {
                    AddRequirement(jobJSON, upgrade);
                }

                if (obj.RequiredExp() > 0) {
                    AddRequirement(jobJSON, ItemIds.Exp, obj.RequiredExp());
                }

                if (obj.CashReward() > 0) {
                    AddResult(jobJSON, ItemIds.Cash, obj.CashReward());
                }

                if (obj.ExpReward() > 0) {
                    AddResult(jobJSON, ItemIds.Exp, obj.ExpReward());
                }

                foreach(var site in obj.DiveSiteIds()) {
                    AddResult(jobJSON, site);
                }

                if (obj.HasFlags(JobDescFlags.Hidden)) {
                    jobJSON["unlockType"].AsString = "manual";
                }

                AnalyzeJobScript(obj.Scripting(), jobJSON);

                root.Add(obj.name, jobJSON);
            }
        }
    
        static private void AnalyzeJobScript(LeafAsset asset, JSON jobJSON) {
            if (asset == null) {
                Log.Error("No job script found for job '{0}'", jobJSON["id"].AsString);
                return;
            }

            string contents = asset.Source();
            if (contents.Length == 0) {
                Log.Error("Job script for job '{0}' was empty", jobJSON["id"].AsString);
                return;
            }

            HashSet<StringHash32> assets = new HashSet<StringHash32>();
            foreach(Match match in GiveUpgradeRegex.Matches(contents)) {
                if (assets.Add(match.Groups[1].Value)) {
                    AddResult(jobJSON, match.Groups[1].Value);
                }
            }

            foreach(Match match in UnlockMapRegex.Matches(contents)) {
                if (assets.Add(match.Groups[1].Value)) {
                    AddResult(jobJSON, match.Groups[1].Value);
                }
            }
        }

        static private void HandleSpecialItemUnlocks(StringHash32 id, JSON itemJSON) {
            if (id == ItemIds.MeasurementTank) {
                AddResult(itemJSON, ItemIds.WaterStabilizer);
                AddResult(itemJSON, ItemIds.AutoFeeder);
            } else if (id == ItemIds.PredictionModel) {
                AddResult(itemJSON, ItemIds.SyncModel);
                AddResult(itemJSON, ItemIds.WaterModeling);
            } else if (id == ItemIds.WaterModeling || id == ItemIds.SyncModel || id == ItemIds.WaterStabilizer || id == ItemIds.AutoFeeder) {
                itemJSON["unlockType"].AsString = "manual";
            } else if (id == ItemIds.FlashlightCoordinates) {
                AddResult(itemJSON, "FinalStation");
            }
        }

        static private void HandleSpecialMapRequirements(StringHash32 id, JSON mapJSON) {
            if (id == MapIds.RS_2R) {
                AddRequirement(mapJSON, ItemIds.PropGuard);
            } else if (id == MapIds.RS_3N) {
                AddRequirement(mapJSON, ItemIds.Hull);
                AddRequirement(mapJSON, ItemIds.Flashlight);
            } else if (id == MapIds.RS_4X) {
                AddRequirement(mapJSON, ItemIds.Flashlight);
                AddRequirement(mapJSON, ItemIds.Engine);
            }
        }

        #region Generic

        static private JSON GenerateAssetRef(string name) {
            JSON obj = JSON.CreateObject();
            obj["id"].AsString = name;
            return obj;
        }

        static private JSON GenerateAssetRef(string name, int amount, bool spend = false) {
            JSON obj = JSON.CreateObject();
            obj["id"].AsString = name;
            obj["amount"].AsInt = amount;
            if (spend) {
                obj["consume"].AsBool = true;
            }
            return obj;
        }

        static private void AddRequirement(JSON nodeJSON, string name) {
            nodeJSON["requires"].Add(GenerateAssetRef(name));
        }

        static private void AddRequirement(JSON nodeJSON, StringHash32 id) {
            nodeJSON["requires"].Add(GenerateAssetRef(id.ToDebugString()));
        }

        static private void AddRequirement(JSON nodeJSON, string name, int amount, bool spend = false) {
            nodeJSON["requires"].Add(GenerateAssetRef(name, amount, spend));
        }

        static private void AddRequirement(JSON nodeJSON, StringHash32 id, int amount, bool spend = false) {
            nodeJSON["requires"].Add(GenerateAssetRef(id.ToDebugString(), amount, spend));
        }

        static private void AddResult(JSON nodeJSON, string name) {
            nodeJSON["results"].Add(GenerateAssetRef(name));
        }

        static private void AddResult(JSON nodeJSON, StringHash32 id) {
            nodeJSON["results"].Add(GenerateAssetRef(id.ToDebugString()));
        }

        static private void AddResult(JSON nodeJSON, string name, int amount, bool spend = false) {
            nodeJSON["results"].Add(GenerateAssetRef(name, amount, spend));
        }

        static private void AddResult(JSON nodeJSON, StringHash32 id, int amount, bool spend = false) {
            nodeJSON["results"].Add(GenerateAssetRef(id.ToDebugString(), amount, spend));
        }

        #endregion // Generic

        static private Regex GiveUpgradeRegex = new Regex("(?<!\\/\\/\\s?)\\$call\\s+GiveUpgrade\\(\"?(.*?)\"?(?:,.*?)?\\)");
        static private Regex UnlockMapRegex = new Regex("(?<!\\/\\/\\s?)\\$call\\s+Unlock(?:Site|Room|Station)\\(\"?(.*?)\"?(?:,.*?)?\\)");
    }
}