using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aqua.Character;
using AquaAudio;
using BeauUtil;
using BeauUtil.Services;
using UnityEngine;

namespace Aqua {

    [ServiceDependency(typeof(InputService), typeof(StateMgr))]
    public sealed class SecretService : ServiceBehaviour {
        private const string AudioPath = "Secret/SecretAudio";

        #region Types

        private enum KeyKind {
            Code,
            Directional
        }

        public enum CheatType {
            Single,
            Repeat,
            Toggle
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Keystroke {
            [FieldOffset(0)] public KeyKind Kind;
            [FieldOffset(4)] public KeyCode Code;
            [FieldOffset(4)] public FacingId Direction;

            public Keystroke(KeyCode code) {
                Kind = KeyKind.Code;
                Direction = 0;
                Code = code;
            }

            public Keystroke(FacingId direction) {
                Kind = KeyKind.Directional;
                Code = 0;
                Direction = direction;
            }
        }

        private struct CheatEntry {
            public StringHash32 Id;
            public CheatType Type;
            public StringHash32 Context;

            public Keystroke[] Pattern;

            public Action Activate;
            public Action Deactivate;
            
            public Func<bool> Validate;
        }
    
        #endregion // Types

        private readonly RingBuffer<CheatEntry> m_Cheats = new RingBuffer<CheatEntry>(32, RingBufferMode.Expand);
        private readonly RingBuffer<KeyCode> m_LastKeyEntries = new RingBuffer<KeyCode>(16, RingBufferMode.Overwrite);

        [NonSerialized] private AudioPackage m_AudioPackage;

        [NonSerialized] private HashSet<StringHash32> m_CheatContexts = Collections.NewSet<StringHash32>(4);
        [NonSerialized] private int m_CheatBlockCounter = 0;
        [NonSerialized] private bool m_CachedCheatsAllowed = true;
        private HashSet<StringHash32> m_ActiveCheats = Collections.NewSet<StringHash32>(16);

        #region Cheats

        public void RegisterCheat(StringHash32 cheatId, CheatType type, StringHash32 context, string pattern, Action activate, Func<bool> validate = null, Action deactivate = null) {
            CheatEntry entry = default;
            entry.Id = cheatId;
            entry.Type = type;
            entry.Context = context;
            entry.Pattern = GeneratePattern(pattern);
            entry.Activate = activate;
            entry.Validate = validate;
            entry.Deactivate = deactivate;

            m_Cheats.PushBack(entry);
        }

        public void DeregisterCheat(StringHash32 cheatId) {
            m_Cheats.RemoveWhere((e, id) => e.Id == id, cheatId);
        }

        public bool IsCheatActive(StringHash32 cheatId) {
            return m_ActiveCheats.Contains(cheatId);
        }

        private void HandleKeyPressed(KeyCode keycode) {
            if (!m_CachedCheatsAllowed || Script.IsPausedOrLoading) {
                return;
            }

            m_LastKeyEntries.PushBack(keycode);
            CheckForLatestCheat();
        }

        #region State

        public void AllowCheats(StringHash32 context) {
            m_CheatContexts.Add(context);
        }

        public void DisallowCheats(StringHash32 context) {
            m_CheatContexts.Remove(context);
        }

        public void BlockCheats() {
            m_CheatBlockCounter++;
            CheckCheatsEnabled();
        }

        public void UnblockCheats() {
            m_CheatBlockCounter--;
            CheckCheatsEnabled();
        }

        private void CheckCheatsEnabled() {
            bool allowed = m_CheatBlockCounter <= 0;
            if (allowed != m_CachedCheatsAllowed) {
                m_CachedCheatsAllowed = allowed;
                m_LastKeyEntries.Clear();
            }
        }

        private void CheckForLatestCheat() {
            if (m_LastKeyEntries.Count < 2) {
                return;
            }

            for(int i = 0; i < m_Cheats.Count; i++) {
                ref CheatEntry entry = ref m_Cheats[i];

                if (!ShouldCheckCheat(ref entry)) {
                    continue;
                }

                if (!CheckPattern(m_LastKeyEntries, entry.Pattern)) {
                    continue;
                }

                if (entry.Validate != null && !entry.Validate()) {
                    continue;
                }

                m_LastKeyEntries.Clear();
                LoadSecretAudio();

                switch(entry.Type) {
                    case CheatType.Single:
                    case CheatType.Repeat: {
                        m_ActiveCheats.Add(entry.Id);
                        entry.Activate();
                        Services.Audio.PostEvent("UI.CheatActivated");
                        break;
                    }

                    case CheatType.Toggle: {
                        if (m_ActiveCheats.Remove(entry.Id)) {
                            entry.Deactivate();
                            Services.Audio.PostEvent("UI.CheatDeactivated");
                        } else {
                            m_ActiveCheats.Add(entry.Id);
                            entry.Activate();
                            Services.Audio.PostEvent("UI.CheatActivated");
                        }
                        break;
                    }
                }
            }
        }

        private bool ShouldCheckCheat(ref CheatEntry entry) {
            if (entry.Context.IsEmpty) {
                if (!Save.IsLoaded) {
                    return false;
                }
            }
            else if (!m_CheatContexts.Contains(entry.Context)) {
                return false;
            }

            switch(entry.Type) {
                case CheatType.Single: {
                    return !m_ActiveCheats.Contains(entry.Id);
                }
                default: {
                    return true;
                }
            }
        }

        private void LoadSecretAudio() {
            if (m_AudioPackage != null) {
                return;
            }

            m_AudioPackage = Resources.Load<AudioPackage>(AudioPath);
            Services.Audio.Load(m_AudioPackage);
        }

        #endregion // State

        #region Patterns

        static private readonly KeyCode[] FacingToKeyCodeMap = new KeyCode[] {
            KeyCode.LeftArrow, KeyCode.A, // left
            KeyCode.RightArrow, KeyCode.D, // right
            KeyCode.UpArrow, KeyCode.W, // up
            KeyCode.DownArrow, KeyCode.S, // down,

            KeyCode.None, KeyCode.None,
            KeyCode.None, KeyCode.None,
        };

        static private bool CheckPattern(RingBuffer<KeyCode> lastKeys, Keystroke[] pattern) {
            if (lastKeys.Count < pattern.Length) {
                return false;
            }

            int endPattern = pattern.Length - 1;
            int endBuffer = lastKeys.Count - 1;

            for(int i = 0; i < pattern.Length; i++) {
                if (!CheckKeystroke(lastKeys[endBuffer - i], pattern[endPattern - i])) {
                    return false;
                }
            }

            return true;
        }

        static private bool CheckKeystroke(KeyCode key, Keystroke stroke) {
            if (stroke.Kind == KeyKind.Code) {
                return key == stroke.Code;
            }

            return key == FacingToKeyCodeMap[(int) stroke.Direction * 2] || key == FacingToKeyCodeMap[(int) stroke.Direction * 2 + 1];
        }

        static private Keystroke[] GeneratePattern(string pattern) {
            Keystroke[] strokes = new Keystroke[pattern.Length];
            for(int i = 0; i < pattern.Length; i++) {
                strokes[i] = GenerateStroke(pattern[i]);
            }
            return strokes;
        }

        static private Keystroke GenerateStroke(char patternCode) {
            switch(patternCode) {
                case 'U': {
                    return new Keystroke(FacingId.Up);
                }

                case 'D': {
                    return new Keystroke(FacingId.Down);
                }

                case 'L': {
                    return new Keystroke(FacingId.Left);
                }

                case 'R': {
                    return new Keystroke(FacingId.Right);
                }

                case '_': {
                    return new Keystroke(KeyCode.Space);
                }

                case '=': {
                    return new Keystroke(KeyCode.Return);
                }

                default: {
                    if (patternCode >= 'a' && patternCode <= 'z') {
                        return new Keystroke(KeyCode.A + (patternCode - 'a'));
                    } else if (patternCode >= '0' && patternCode <= '9') {
                        return new Keystroke(KeyCode.Alpha0 + (patternCode - '0'));
                    } else {
                        throw new ArgumentException(string.Format("Cannot map character '{0}' to cheat keystroke", patternCode), "patternCode");
                    }
                }
            }
        }
    
        #endregion // Patterns
    
        #endregion // Cheats

        #region Service

        protected override void Initialize() {
            Services.Input.OnKeyPressed += HandleKeyPressed;

            GameCheats.RegisterCheats(this);
        }

        protected override void Shutdown() {
            if (Services.Valid && Services.Input) {
                Services.Input.OnKeyPressed -= HandleKeyPressed;
            }
        }

        #endregion // Service
    }

    static internal class GameCheats {
        static internal void RegisterCheats(SecretService service) {

            // swap to mal's voice
            service.RegisterCheat("victor_malvoice", SecretService.CheatType.Toggle, null, "forevermine", () => {
                Services.Audio.RemapEvent("text_type_guide", "text_type_guide_MAL");
                Assets.Character(GameConsts.Target_V1ctor).AdditionalTypingTextDelay = 0.05f;
            }, null, () => {
                Services.Audio.ClearRemap("text_type_guide");
                Assets.Character(GameConsts.Target_V1ctor).AdditionalTypingTextDelay = 0;
            });
        }
    }
}