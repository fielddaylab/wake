#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // DEVELOPMENT

using Aqua.Debugging;
using Aqua.Profile;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    public partial class DataService : ServiceBehaviour, IDebuggable, ILoadable
    {
        #if DEVELOPMENT

        #if UNITY_EDITOR
        [NonSerialized] private DMInfo m_BookmarksMenu;
        #endif // UNITY_EDITOR

        internal void UseDebugProfile()
        {
            ClearOldProfile();

            SaveData saveData = CreateNewProfile(DebugSaveId);
            DebugService.Log(LogMask.DataService, "[DataService] Created debug profile");
            DeclareProfile(saveData, false, false);
        }

        private void LoadBookmark(string inBookmarkName)
        {
            TextAsset bookmarkAsset = Resources.Load<TextAsset>("Bookmarks/" + inBookmarkName);
            if (!bookmarkAsset)
                return;

            SaveData bookmark;
            if (TryLoadProfileFromBytes(bookmarkAsset.bytes, out bookmark))
            {
                ClearOldProfile();

                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile from bookmark '{0}'", inBookmarkName);

                DeclareProfile(bookmark, false, false);
                m_LastBookmarkName = inBookmarkName;
                StartPlaying(null, true);
            }

            Resources.UnloadAsset(bookmarkAsset);
        }

        private void ForceReloadSave()
        {
            if (m_LastBookmarkName != null)
            {
                LoadBookmark(m_LastBookmarkName);
            }
            else
            {
                LoadProfile(m_ProfileName).OnComplete((success) => {
                    if (success)
                        StartPlaying(null, true);
                });
            }
        }

        private void ForceRestart()
        {
            DeleteLocalSave(m_ProfileName);
            NewProfile(m_ProfileName).OnComplete((success) => {
                if (success)
                    StartPlaying(null, true);
            });
        }

        private void DebugSaveData()
        {
            SyncProfile();

            #if UNITY_EDITOR

            Directory.CreateDirectory("Saves");
            string saveName = string.Format("Saves/save_{0}.json", m_CurrentSaveData.LastUpdated);
            string binarySaveName = string.Format("Saves/save_{0}.bbin", m_CurrentSaveData.LastUpdated);
            Serializer.WriteFile(m_CurrentSaveData, saveName, OutputOptions.PrettyPrint, Serializer.Format.JSON);
            Serializer.WriteFile(m_CurrentSaveData, binarySaveName, OutputOptions.None, Serializer.Format.Binary);
            Debug.LogFormat("[DataService] Saved Profile to {0} and {1}", saveName, binarySaveName);
            EditorUtility.OpenWithDefaultApp(saveName);

            #elif DEVELOPMENT_BUILD

            string json = Serializer.Write(m_CurrentSaveData, OutputOptions.None, Serializer.Format.JSON);
            Debug.LogFormat("[DataService] Current Profile: {0}", json);

            #endif // UNITY_EDITOR
        }

        #if UNITY_EDITOR

        private void BookmarkSaveData()
        {
            SyncProfile();

            Cursor.visible = true;
            
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Bookmark", string.Empty, "json", "Choose a location to save your bookmark", "Assets/Resources/Bookmarks/");
            if (!string.IsNullOrEmpty(path))
            {
                Serializer.WriteFile(m_CurrentSaveData, path, OutputOptions.PrettyPrint, Serializer.Format.JSON);
                Debug.LogFormat("[DataService] Saved bookmark at {0}", path);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                RegenerateBookmarks(m_BookmarksMenu);
            }

            Cursor.visible = false;
        }

        #endif // UNITY_EDITOR

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            // jobs menu

            DMInfo jobsMenu = new DMInfo("Jobs");

            DMInfo startJobMenu = new DMInfo("Start Job");
            foreach(var job in Services.Assets.Jobs.Objects)
                RegisterJobStart(startJobMenu, job.Id());

            jobsMenu.AddSubmenu(startJobMenu);
            jobsMenu.AddDivider();
            jobsMenu.AddButton("Complete Current Job", () => Save.Jobs.MarkComplete(Save.CurrentJobId), () => Save.CurrentJob.IsValid);
            jobsMenu.AddButton("Clear All Job Progress", () => Save.Jobs.ClearAll());

            yield return jobsMenu;

            // bestiary menu

            DMInfo bestiaryMenu = new DMInfo("Bestiary");

            DMInfo critterMenu = new DMInfo("Critters");
            foreach(var critter in Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Critter))
                RegisterEntityToggle(critterMenu, critter.Id());

            DMInfo envMenu = new DMInfo("Environments");
            foreach(var env in Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Environment))
                RegisterEntityToggle(envMenu, env.Id());

            DMInfo factMenu = new DMInfo("Facts");
            Dictionary<StringHash32, DMInfo> factSubmenus = new Dictionary<StringHash32, DMInfo>();
            foreach(var fact in Services.Assets.Bestiary.AllFacts())
            {
                if (Services.Assets.Bestiary.IsAutoFact(fact.Id))
                    continue;

                DMInfo submenu;
                StringHash32 submenuKey = fact.Parent.Id();
                if (!factSubmenus.TryGetValue(submenuKey, out submenu))
                {
                    submenu = new DMInfo(submenuKey.ToDebugString());
                    factSubmenus.Add(submenuKey, submenu);
                    factMenu.AddSubmenu(submenu);
                }

                RegisterFactToggle(submenu, fact.Id, BFType.DefaultDiscoveredFlags(fact));
            }

            bestiaryMenu.AddSubmenu(critterMenu);
            bestiaryMenu.AddSubmenu(envMenu);
            bestiaryMenu.AddSubmenu(factMenu);

            bestiaryMenu.AddDivider();

            bestiaryMenu.AddButton("Unlock All Entries", () => UnlockAllBestiaryEntries(false));
            bestiaryMenu.AddButton("Unlock All Facts", () => UnlockAllBestiaryEntries(true));
            bestiaryMenu.AddButton("Clear Bestiary", () => ClearBestiary());

            yield return bestiaryMenu;

            // map menu

            DMInfo mapMenu = new DMInfo("World Map");

            mapMenu.AddButton("Unlock All Stations", () => UnlockAllStations());
            mapMenu.AddDivider();

            foreach(var map in Services.Assets.Map.Stations())
            {
                RegisterStationToggle(mapMenu, map.Id());
            }

            yield return mapMenu;

            // ship rooms

            DMInfo roomMenu = new DMInfo("Rooms");

            roomMenu.AddButton("Unlock All Rooms", () => UnlockAllRooms());
            roomMenu.AddDivider();

            foreach(var room in Services.Assets.Map.Rooms())
            {
                RegisterRoomToggle(roomMenu, room);
            }

            yield return roomMenu;

            // dive sites

            DMInfo diveSiteMenu  = new DMInfo("Dive Sites");

            diveSiteMenu.AddButton("Unlock All Dive Sites", () => UnlockAllSites());
            diveSiteMenu.AddDivider();

            foreach(var diveSite in Services.Assets.Map.DiveSites())
            {
                RegisterSiteToggle(diveSiteMenu, diveSite.Id());
            }

            yield return diveSiteMenu;

            // inventory menu

            DMInfo invMenu = new DMInfo("Inventory");

            invMenu.AddButton("Add 100 Cash", () => Save.Inventory.AdjustItem(ItemIds.Cash, 100));
            invMenu.AddButton("Add 10 Exp", () => Save.Inventory.AdjustItem(ItemIds.Exp, 10));
            invMenu.AddButton("Unlock All Upgrades", () => UnlockAllUpgrades());

            invMenu.AddDivider();

            foreach(var upgrade in Services.Assets.Inventory.Upgrades)
            {
                RegisterUpgradeToggle(invMenu, upgrade.Id());
            }

            yield return invMenu;

            // save data menu

            DMInfo saveMenu = new DMInfo("Player Profile");

            DMInfo bookmarkMenu = new DMInfo("Bookmarks");
            #if UNITY_EDITOR
            m_BookmarksMenu = bookmarkMenu;
            #endif // UNITY_EDITOR

            RegenerateBookmarks(bookmarkMenu);
            saveMenu.AddSubmenu(bookmarkMenu);
            saveMenu.AddDivider();

            saveMenu.AddButton("Save", () => SaveProfile("DEBUG"), IsProfileLoaded);
            saveMenu.AddButton("Save (Debug)", () => DebugSaveData(), IsProfileLoaded);
            #if UNITY_EDITOR
            saveMenu.AddButton("Save as Bookmark", () => BookmarkSaveData(), IsProfileLoaded);
            #else 
            saveMenu.AddButton("Save as Bookmark", null, () => false);
            #endif // UNITY_EDITOR
            saveMenu.AddToggle("Autosave Enabled", AutosaveEnabled, SetAutosaveEnabled);

            saveMenu.AddDivider();

            #if DEVELOPMENT
            saveMenu.AddButton("Reload Save", () => ForceReloadSave(), IsProfileLoaded);
            saveMenu.AddButton("Restart from Beginning", () => ForceRestart());
            #endif // DEVELOPMENT

            saveMenu.AddDivider();

            saveMenu.AddButton("Clear Local Saves", () => ClearLocalSaves());

            yield return saveMenu;

            // defaults

            DMInfo defaultsMenu = new DMInfo("Defaults");

            defaultsMenu.AddButton("Unlock Full Map", () => {
                UnlockAllRooms();
                UnlockAllSites();
                UnlockAllStations();
            });

            defaultsMenu.AddButton("Unlock Full Inventory", () => {
                UnlockAllUpgrades();
            });

            defaultsMenu.AddButton("Unlock Bestiary Entries", () => {
                UnlockAllBestiaryEntries(false);
            });

            defaultsMenu.AddDivider();

            defaultsMenu.AddButton("Unlock Map, Inventory, Bestiary", () => {
                UnlockAllRooms();
                UnlockAllSites();
                UnlockAllStations();
                UnlockAllUpgrades();
                UnlockAllBestiaryEntries(false);
            });

            yield return defaultsMenu;
        }

        static private void RegenerateBookmarks(DMInfo inMenu)
        {
            var allBookmarks = Resources.LoadAll<TextAsset>("Bookmarks");

            inMenu.Clear();

            if (allBookmarks.Length == 0)
            {
                inMenu.AddText("No bookmarks :(", () => string.Empty);
            }
            else
            {
                foreach(var bookmark in allBookmarks)
                {
                    RegisterBookmark(inMenu, bookmark);
                }
            }
        }

        static private void RegisterBookmark(DMInfo inMenu, TextAsset inAsset)
        {
            #if DEVELOPMENT
            string name = inAsset.name;
            inMenu.AddButton(name, () => Services.Data.LoadBookmark(name), () => Services.Data.m_SaveResult == null);
            #endif // DEVELOPMENT
            
            Resources.UnloadAsset(inAsset);
        }

        static private void RegisterJobStart(DMInfo inMenu, StringHash32 inJobId)
        {
            inMenu.AddButton(inJobId.ToDebugString(), () => 
            {
                Save.Jobs.ForgetJob(inJobId);
                Save.Jobs.SetCurrentJob(inJobId); 
            }, () => Save.CurrentJobId != inJobId);
        }

        static private void RegisterEntityToggle(DMInfo inMenu, StringHash32 inEntityId)
        {
            inMenu.AddToggle(inEntityId.ToDebugString(),
                () => { return Save.Bestiary.HasEntity(inEntityId); },
                (b) =>
                {
                    if (b)
                        Save.Bestiary.RegisterEntity(inEntityId);
                    else
                        Save.Bestiary.DeregisterEntity(inEntityId);
                }
            );
        }

        static private void RegisterFactToggle(DMInfo inMenu, StringHash32 inFactId, BFDiscoveredFlags inDefaultFlags)
        {
            inMenu.AddToggle(inFactId.ToDebugString(),
                () => { return Save.Bestiary.HasFact(inFactId); },
                (b) =>
                {
                    if (b)
                        Save.Bestiary.RegisterFact(inFactId, true);
                    else
                        Save.Bestiary.DeregisterFact(inFactId);
                }
            );
            if (inDefaultFlags != BFDiscoveredFlags.All) {
                inMenu.AddToggle(inFactId.ToDebugString() + " (with Rate)",
                    () => { return Save.Bestiary.IsFactFullyUpgraded(inFactId); },
                    (b) =>
                    {
                        if (b) {
                            Save.Bestiary.RegisterFact(inFactId, true);
                            Save.Bestiary.AddDiscoveredFlags(inFactId, BFDiscoveredFlags.Rate);
                        } else {
                            Save.Bestiary.RemoveDiscoveredFlags(inFactId, BFDiscoveredFlags.Rate);
                        }
                    }
                );
            }
        }

        static private void UnlockAllBestiaryEntries(bool inbIncludeFacts)
        {
            bool bChanged = false;
            foreach(var entry in Services.Assets.Bestiary.Objects)
            {
                bChanged |= Save.Bestiary.DebugRegisterEntityNoEvent(entry.Id());
                if (inbIncludeFacts)
                {
                    foreach(var fact in entry.Facts)
                    {
                        bChanged |= Save.Bestiary.DebugRegisterFactNoEvent(fact.Id);
                        bChanged |= Save.Bestiary.DebugRegisterFactFlagsNoEvent(fact.Id, BFDiscoveredFlags.All);
                    }
                }
            }
            if (bChanged)
            {
                Services.Events.QueueForDispatch(GameEvents.BestiaryUpdated, new BestiaryUpdateParams(BestiaryUpdateParams.UpdateType.Unknown, StringHash32.Null));
            }
        }

        static private void ClearBestiary()
        {
            foreach(var entry in Services.Assets.Bestiary.Objects)
            {
                Save.Bestiary.DeregisterEntity(entry.Id());
                foreach(var fact in entry.Facts)
                    Save.Bestiary.DeregisterFact(fact.Id);
            }
        }

        static private void RegisterUpgradeToggle(DMInfo inMenu, StringHash32 inItem)
        {
            inMenu.AddToggle(inItem.ToDebugString(),
                () => { return Save.Inventory.HasUpgrade(inItem); },
                (b) =>
                {
                    if (b)
                        Save.Inventory.AddUpgrade(inItem);
                    else
                        Save.Inventory.RemoveUpgrade(inItem);
                });
        }

        static private void UnlockAllUpgrades()
        {
            foreach(var entry in Services.Assets.Inventory.Upgrades)
            {
                Save.Inventory.AddUpgrade(entry.Id());
            }
        }

        static private void RegisterStationToggle(DMInfo inMenu, StringHash32 inStationId)
        {
            inMenu.AddToggle(inStationId.ToDebugString(),
                () => { return Save.Map.IsStationUnlocked(inStationId); },
                (b) =>
                {
                    if (b)
                        Save.Map.UnlockStation(inStationId);
                    else
                        Save.Map.LockStation(inStationId);
                }
            );
        }

        static private void RegisterSiteToggle(DMInfo inMenu, StringHash32 inSiteId)
        {
            inMenu.AddToggle(inSiteId.ToDebugString(),
                () => { return Save.Map.IsSiteUnlocked(inSiteId); },
                (b) =>
                {
                    if (b)
                        Save.Map.UnlockSite(inSiteId);
                    else
                        Save.Map.LockSite(inSiteId);
                }
            );
        }

        static private void RegisterRoomToggle(DMInfo inMenu, StringHash32 inRoomId)
        {
            inMenu.AddToggle(inRoomId.ToDebugString(),
                () => { return Save.Map.IsRoomUnlocked(inRoomId); },
                (b) =>
                {
                    if (b)
                        Save.Map.UnlockRoom(inRoomId);
                    else
                        Save.Map.LockRoom(inRoomId);
                }
            );
        }

        static private void UnlockAllRooms()
        {
            foreach(var roomId in Services.Assets.Map.Rooms())
            {
                Save.Map.UnlockRoom(roomId);
            }
        }

        static private void UnlockAllStations()
        {
            foreach(var map in Services.Assets.Map.Stations())
            {
                Save.Map.UnlockStation(map.Id());
            }
        }

        static private void UnlockAllSites()
        {
            foreach(var map in Services.Assets.Map.DiveSites())
            {
                Save.Map.UnlockSite(map.Id());
            }
        }

        static private void ClearLocalSaves()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Log.Warn("[DataService] All local save data has been cleared");
        }

        #endregion // IDebuggable

        #endif // DEVELOPMENT
    }
}