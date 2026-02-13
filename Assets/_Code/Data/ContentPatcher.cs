using System.Reflection;
using System.Collections.Generic;
using BeauUtil.Debugger;
using BeauData;
using BeauUtil;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {
    static public class ContentPatcher {
        private struct PatchAssetRecord {
            public object Asset;
            public FieldInfo Field;
            public object OriginalValue;
        }

        private class PatchRecord {
            public List<PatchAssetRecord> Assets = new List<PatchAssetRecord>(64);
            public List<IPostPatchCallback> Callbacks = new List<IPostPatchCallback>(8);
        }

        static private readonly PatchRecord s_AppliedPatch = new PatchRecord();

        #region Apply

        static public bool IsPatched() {
            return s_AppliedPatch.Assets.Count > 0;
        }

        static public void Apply(string json) {
            Undo();

            if (!string.IsNullOrEmpty(json)) {
                ApplyChanges(s_AppliedPatch, json);
            }
        }

        static private void ApplyChanges(PatchRecord record, string json) {
            JSON parsed;
            try {
                parsed = JSON.Parse(json);
            } catch(JSON.ParseException e) {
                Log.Error("[ContentPatcher] Error while parsing patch json:\n{0}", e.ToString());
                return;
            }

            if (!parsed.IsObject || parsed.Count <= 0) {
                Log.Warn("[ContentPatcher] Empty patch file");
                return;
            }

            foreach(var kv in parsed.KeyValues) {
                string assetName = kv.Key;
                if (!kv.Value.IsObject || kv.Value.Count <= 0) {
                    Log.Msg("[ContentPatcher] Empty patch set for asset '{0}'", kv.Key);
                    continue;
                }

                object asset = FindAsset(assetName);

                if (asset == null) {
                    continue;
                }

                IPostPatchCallback callback = asset as IPostPatchCallback;
                if (callback != null) {
                    record.Callbacks.Add(callback);
                }

                foreach(var fieldKv in kv.Value.KeyValues) {
                    string fieldName = fieldKv.Key;
                    bool isAppend = false;
                    if (fieldName.StartsWith("+")) {
                        isAppend = true;
                        fieldName = fieldName.Substring(1);
                    }

                    FieldInfo assetField = asset.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (assetField == null) {
                        Log.Error("[ContentPatcher] Unable to find field '{0}' on asset '{1}'", fieldName, assetName);
                        continue;
                    }

                    if (!assetField.IsDefined(typeof(RuntimePatchableAttribute))) {
                        Log.Error("[ContentPatcher] Field '{0}' on asset '{1}' is not marked as RuntimePatchable", fieldName, assetName);
                        continue;
                    }

                    if (!TryConvert(assetField.FieldType, fieldKv.Value, out object patchedValue)) {
                        Log.Error("[ContentPatcher] Unable to resolve '{0}' to valid value for field '{1}' of type {2} on asset '{3}'",
                            fieldKv.Value.ToString(), fieldName, assetField.FieldType.Name, assetName);
                        continue;
                    }

                    PatchAssetRecord assetRec;
                    assetRec.Asset = asset;
                    assetRec.Field = assetField;
                    assetRec.OriginalValue = assetField.GetValue(asset);
                    record.Assets.Add(assetRec);

                    if (isAppend) {
                        Array originalArr = assetRec.OriginalValue as Array;
                        Array patchArr = patchedValue as Array;
                        if (originalArr != null && patchArr != null) {
                            Log.Msg("[ContentPatcher] Patching as append...");
                            Array concatArr = Array.CreateInstance(assetField.FieldType.GetElementType(), originalArr.Length + patchArr.Length);
                            Array.Copy(originalArr, 0, concatArr, 0, originalArr.Length);
                            Array.Copy(patchArr, 0, concatArr, originalArr.Length, patchArr.Length);
                            patchedValue = concatArr;
                        }
                    }

                    assetField.SetValue(asset, patchedValue);
                    MarkChanged(asset);

                    Log.Msg("[ContentPatcher] Patching field '{0}' on asset '{1}'...", fieldName, assetName);
                }
            }

            if (record.Callbacks.Count > 0) {
                Log.Msg("[ContentPatcher] Calling {0} post-patch callbacks...", record.Callbacks.Count);

                foreach (var postPatch in record.Callbacks) {
                    postPatch.OnContentPostPatch();
                }
            }

            Log.Msg("[ContentPatcher] Patch done!");
        }

        static private object FindAsset(string name) {
            bool has = Assets.TryFind(name, out ScriptableObject asset);
            if (!has && Application.isPlaying) {
                asset = Services.Tweaks.GetByName(name);
                has = asset != null;
            }

            if (has) {
                Log.Msg("[ContentPatcher] Located asset with name '{0}'", name);
                return asset;
            } else {
                Log.Error("[ContentPatcher] Unable to locate asset with name '{0}'", name);
                return null;
            }
        }

        #endregion // Apply

        #region Undo

        static public void Undo() {
            UndoChanges(s_AppliedPatch);
        }

        static private void UndoChanges(PatchRecord record) {
            if (record.Assets.Count > 0) {
                Log.Msg("[ContentPatcher] Undoing currently applied patch ({0} changes)...", record.Assets.Count);
                foreach (var patch in record.Assets) {
                    Log.Msg("Reverting '{0}' '{1}'", patch.Asset.ToString(), patch.Field.Name);
                    patch.Field.SetValue(patch.Asset, patch.OriginalValue);

                    MarkChanged(patch.Asset);
                }
                record.Assets.Clear();

                if (record.Callbacks.Count > 0) {
                    Log.Msg("[ContentPatcher] Calling {0} post-patch callbacks...", record.Callbacks.Count);
                    foreach (var callback in record.Callbacks) {
                        callback.OnContentPostPatch();
                    }
                    record.Callbacks.Clear();
                }

                Log.Msg("[ContentPatcher] Patch undone!");
            }
        }

        #endregion // Undo

        #region Type Conversion

        static private bool TryConvert(Type fieldType, JSON json, out object newValue) {
            if (!CanBeConverted(fieldType)) {
                newValue = null;
                return false;
            }

            if (fieldType.IsArray) {
                Type elemType = fieldType.GetElementType();
                if (json.IsArray) {
                    Array val = Array.CreateInstance(elemType, json.Count);
                    for (int i = 0; i < json.Count; i++) {
                        if (!TryConvert(elemType, json[i], out var elem)) {
                            newValue = null;
                            return false;
                        }

                        val.SetValue(elem, i);
                    }
                    newValue = val;
                    return true;
                } else {
                    newValue = null;
                    return false;
                }
            }

            if (fieldType.IsEnum) {
                try {
                    newValue = Enum.Parse(fieldType, json.AsString);
                    return true;
                } catch {
                    newValue = null;
                    return false;
                }
            }

            if (fieldType == typeof(StringHash32)) {
                newValue = new StringHash32(json.AsString);
                return true;
            }

            if (fieldType == typeof(SerializedHash32)) {
                newValue = new SerializedHash32(json.AsString);
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) {
                if (json.IsNull) {
                    newValue = null;
                    return true;
                } else {
                    newValue = FindAsset(json.AsString);
                    return newValue != null;
                }
            }

            TypeCode code = Type.GetTypeCode(fieldType);
            switch (code) {
                case TypeCode.String:
                    newValue = json.AsString;
                    return json.IsString;

                case TypeCode.Boolean:
                    newValue = json.AsBool;
                    return json.IsBool;

                case TypeCode.Byte:
                    newValue = (byte) json.AsInt;
                    return json.IsNumber;

                case TypeCode.SByte:
                    newValue = (sbyte) json.AsInt;
                    return json.IsNumber;

                case TypeCode.UInt16:
                    newValue = (ushort) json.AsInt;
                    return json.IsNumber;

                case TypeCode.Int16:
                    newValue = (short) json.AsInt;
                    return json.IsNumber;

                case TypeCode.UInt32:
                    newValue = (uint) json.AsLong;
                    return json.IsNumber;

                case TypeCode.Int32:
                    newValue = json.AsInt;
                    return json.IsNumber;

                case TypeCode.UInt64:
                    newValue = json.AsULong;
                    return json.IsNumber;

                case TypeCode.Int64:
                    newValue = json.AsLong;
                    return json.IsNumber;

                case TypeCode.Char:
                    newValue = json.AsString?[0] ?? '\0';
                    return json.IsString;

                case TypeCode.Single:
                    newValue = json.AsFloat;
                    return json.IsNumber;

                case TypeCode.Double:
                    newValue = json.AsDouble;
                    return json.IsNumber;

                default:
                    newValue = null;
                    return false;
            }
        }

        static private bool CanBeConverted(Type fieldType) {
            if (fieldType.IsArray) {
                return CanBeConverted(fieldType.GetElementType());
            } else if (fieldType.IsGenericType) {
                return false;
            }

            if (fieldType.IsEnum) {
                return true;
            }

            if (fieldType == typeof(StringHash32) || fieldType == typeof(SerializedHash32)) {
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) {
                return true;
            }

            TypeCode code = Type.GetTypeCode(fieldType);
            switch (code) {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Char:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.String:
                    return true;

                default:
                    return false;
            }
        }

        #endregion // Type Conversion

        #region Editor

        static private void MarkChanged(object obj) {
#if UNITY_EDITOR
            UnityEngine.Object uObj = obj as UnityEngine.Object;
            if (uObj != null) {
                EditorUtility.SetDirty(uObj);
            }
#endif // UNITY_EDITOR
        }

        static private void LockChanges() {
            s_AppliedPatch.Assets.Clear();
            s_AppliedPatch.Callbacks.Clear();
        }

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            EditorApplication.playModeStateChanged += (s) => {
                if (s == PlayModeStateChange.ExitingPlayMode) {
                    Undo();
                }
            };

            EditorApplication.quitting += () => Undo();
            AppDomain.CurrentDomain.DomainUnload += (_, __) => Undo();
        }

        [MenuItem("Aqualab/Apply Patch File")]
        static private void CreatePatcherWindow() {
            ScriptableWizard.DisplayWizard<PatcherWindow>("Apply Patch File", "Apply");
        }

        private sealed class PatcherWindow : ScriptableWizard {
            public TextAsset Text;

            public void OnWizardCreate() {
                if (Text) {
                    Apply(Text.text);
                    LockChanges();
                }
            }
        }

#endif // UNITY_EDITOR

        #endregion // Editor
    }

    public interface IPostPatchCallback {
        void OnContentPostPatch();
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RuntimePatchableAttribute : Attribute {
    }
}