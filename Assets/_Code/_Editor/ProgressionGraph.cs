using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Leaf;
using BeauUtil;
using BeauData;
using System.Text.RegularExpressions;

namespace Aqua.Editor {
    static public class ProgressionGraph {
        private enum UnlockType {
            Manual,
            Visible
        }

        [MenuItem("Aqualab/Analysis/Generate Nodes File")]
        static private void GenerateProgressionSource() {
            JSON js = JSON.CreateObject();
            GenerateAssetsList(js["nodes"]);
            using(var writer = new StreamWriter(File.Open("progression_nodes.json", FileMode.Create))) {
                js.WriteTo(writer, 4);
            }
            EditorUtility.OpenWithDefaultApp("progression_nodes.json");
        }

        static private void GenerateAssetsList(JSON root) {
            GenerateItemList(root);
            GenerateJobsList(root);
            GenerateMapList(root);
        }

        static private void GenerateMapList(JSON root) {
            MapDB db = ValidationUtils.FindAsset<MapDB>();

            foreach(var room in db.Rooms()) {
                JSON mapJSON = JSON.CreateObject();
                mapJSON["type"].AsString = "map";
                if (db.DefaultUnlockedRooms().Contains(room)) {
                    mapJSON["startWith"].AsBool = true;
                } else {
                    mapJSON["unlockType"].AsString = "manual";
                }

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

                if (isDefault) {
                    mapJSON["startWith"].AsBool = true;
                } else if (isAtNonDefaultStation) {
                    mapJSON["requires"].Add(GenerateAssetRef(obj.Parent().name));
                } else if (obj.HasFlags(MapFlags.UnlockedByDefault)) {
                    mapJSON["startWith"].AsBool = true;
                } else {
                    mapJSON["unlockType"].AsString = "manual";
                }

                root.Add(obj.name, mapJSON);
            }
        }

        static private void GenerateItemList(JSON root) {
            InventoryDB db = ValidationUtils.FindAsset<InventoryDB>();

            foreach(var obj in ValidationUtils.FindAllAssets<InvItem>(ValidationUtils.IgnoreTemplates)) {
                JSON itemJSON = JSON.CreateObject();
                if (obj.DefaultAmount() > 0) {
                    itemJSON["startWith"].AsUInt = obj.DefaultAmount();;
                }

                if (obj.Category() != InvItemCategory.Currency || obj.CashCost() > 0 || obj.RequiredExp() > 0) {
                    itemJSON["type"].AsString = "item";
                    itemJSON["requires"].Add(GenerateAssetRef("Shop"));
                } else {
                    itemJSON["type"].AsString = "currency";
                    itemJSON["isToken"].AsBool = true;
                }

                if (obj.CashCost() > 0) {
                    itemJSON["requires"].Add(GenerateAssetRef(ItemIds.Cash.ToDebugString(), obj.CashCost(), true));
                }
                if (obj.RequiredExp() > 0) {
                    itemJSON["requires"].Add(GenerateAssetRef(ItemIds.Exp.ToDebugString(), obj.RequiredExp()));
                }
                if (obj.Prerequisite()) {
                    itemJSON["requires"].Add(GenerateAssetRef(obj.Prerequisite().name));
                }

                HandleSpecialItemUnlocks(obj.Id(), itemJSON);

                root.Add(obj.name, itemJSON);
            }
        }

        static private void GenerateJobsList(JSON root) {
            JobDB db = ValidationUtils.FindAsset<JobDB>();

            foreach(var obj in ValidationUtils.FindAllAssets<JobDesc>(ValidationUtils.IgnoreTemplates)) {
                JSON jobJSON = JSON.CreateObject();

                jobJSON["type"].AsString = "job";

                if (!obj.StationId().IsEmpty) {
                    jobJSON["requires"].Add(GenerateAssetRef(obj.StationId().ToDebugString()));
                }

                foreach(var prereq in obj.RequiredJobs()) {
                    jobJSON["requires"].Add(GenerateAssetRef(prereq.name));
                }
                foreach(var upgrade in obj.RequiredUpgrades()) {
                    jobJSON["requires"].Add(GenerateAssetRef(upgrade.ToDebugString()));
                }

                if (obj.CashReward() > 0) {
                    jobJSON["result"].Add(GenerateAssetRef(ItemIds.Cash.ToDebugString(), obj.CashReward()));
                }

                if (obj.ExpReward() > 0) {
                    jobJSON["result"].Add(GenerateAssetRef(ItemIds.Exp.ToDebugString(), obj.ExpReward()));
                }

                AnalyzeJobScript(obj.Scripting(), jobJSON);

                root.Add(obj.name, jobJSON);
            }
        }

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
    
        static private void AnalyzeJobScript(LeafAsset asset, JSON jobJSON) {
            if (asset == null) {
                return;
            }

            string contents = asset.Source();

            HashSet<StringHash32> assets = new HashSet<StringHash32>();
            foreach(Match match in GiveUpgradeRegex.Matches(contents)) {
                if (assets.Add(match.Groups[1].Value)) {
                    jobJSON["result"].Add(GenerateAssetRef(match.Groups[1].Value));
                }
            }

            foreach(Match match in UnlockMapRegex.Matches(contents)) {
                if (assets.Add(match.Groups[1].Value)) {
                    jobJSON["result"].Add(GenerateAssetRef(match.Groups[1].Value));
                }
            }
        }

        static private void HandleSpecialItemUnlocks(StringHash32 id, JSON itemJSON) {
            if (id == ItemIds.MeasurementTank) {
                itemJSON["results"].Add(GenerateAssetRef(ItemIds.WaterStabilizer.ToDebugString()));
                itemJSON["results"].Add(GenerateAssetRef(ItemIds.AutoFeeder.ToDebugString()));
            } else if (id == ItemIds.PredictionModel) {
                itemJSON["results"].Add(GenerateAssetRef(ItemIds.SyncModel.ToDebugString()));
                itemJSON["results"].Add(GenerateAssetRef(ItemIds.WaterModeling.ToDebugString()));
            }
        }

        static private Regex GiveUpgradeRegex = new Regex("(?<!\\/\\/\\s?)\\$call GiveUpgrade\\(\"?(.*?)\"?(?:,.*?)?\\)");
        static private Regex UnlockMapRegex = new Regex("(?<!\\/\\/\\s?)\\$call Unlock(?:Site|Room|Station)\\(\"?(.*?)\"?(?:,.*?)?\\)");
    }
}