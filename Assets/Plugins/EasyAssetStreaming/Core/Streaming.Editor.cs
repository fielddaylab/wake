#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

[assembly: InternalsVisibleTo("EasyAssetStreaming.Editor")]

namespace EasyAssetStreaming {

    /// <summary>
    /// Asset streaming.
    /// </summary>
#if UNITY_EDITOR
    public partial class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public partial class Streaming {
    #endif // UNITY_EDITOR

        #region Editor Hooks

        #if UNITY_EDITOR

        static private bool s_EditorQuitting;

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            UnityEditor.EditorApplication.quitting += () => s_EditorQuitting = true;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            StreamingHelper.Init();
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode && stateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                return;
            }

            UnloadAll();
            DeregisterTick();
            StreamingHelper.Release();
        }

        static private void OnPrefabStageClosing(UnityEditor.Experimental.SceneManagement.PrefabStage _) {
            UnityEditor.EditorApplication.delayCall += UnloadUnusedSync;
        }

        static private void OnSceneOpened(UnityEngine.SceneManagement.Scene _, UnityEditor.SceneManagement.OpenSceneMode __) {
            if (UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            
            UnloadUnusedSync();
        }

        static private void OnDomainUnload(object sender, EventArgs args) {
            if (s_EditorQuitting) {
                return;
            }

            UnloadAll();
        }

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isPlaying || s_Cache == null)
                return;

            bool manifestUpdated = Manifest.ReloadEditor();

            HashSet<uint> allAddressHashes = new HashSet<uint>();
            foreach(var path in deletedAssets) {
                allAddressHashes.Add(AddressKey(path));
            }
            foreach(var path in movedAssets) {
                allAddressHashes.Add(AddressKey(path));
            }
            foreach(var path in movedFromAssetPaths) {
                allAddressHashes.Add(AddressKey(path));
            }
            
            foreach(var id in s_Cache.ByAddressHash.Values) {
                TryReloadAsset(id, allAddressHashes, manifestUpdated);
            }
        }

        static private void TryReloadAsset(StreamingAssetHandle id, HashSet<uint> modifiedAddresses, bool manifestUpdated) {
            ref AssetEditorInfo editorInfo = ref id.EditorInfo;

            if (string.IsNullOrEmpty(editorInfo.Path)) {
                return;
            }

            if (!manifestUpdated && !modifiedAddresses.Contains(id.MetaInfo.AddressHash)) {
                return;
            }

            bool bDeleted = !File.Exists(editorInfo.Path);
            bool bModified = false;
            if (!bDeleted) {
                try {
                    bModified = File.GetLastWriteTimeUtc(editorInfo.Path).ToFileTimeUtc() != editorInfo.EditTime;
                } catch {
                    bModified = false;
                }
            }

            switch(id.AssetType.Id) {
                case StreamingAssetTypeId.Texture: {
                    if (id.AssetType.Sub == StreamingAssetSubTypeId.Default) {
                        if (bDeleted) {
                            Textures.HandleTextureDeleted(id);
                        } else if (bModified || manifestUpdated) {
                            Textures.HandleTextureModified(id);
                        }
                    }
                    break;
                }
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor Hooks
    }
}