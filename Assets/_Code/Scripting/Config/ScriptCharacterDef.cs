using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Character Definition")]
    public class ScriptCharacterDef : DBObject, IEditorOnlyData
    {
        [Serializable]
        private struct PortraitDef : IKeyValuePair<StringHash32, Sprite>
        {
            #pragma warning disable CS0649

            public SerializedHash32 Id;
            public Sprite Sprite;

            #pragma warning restore CS0649

            public StringHash32 Key { get { return Id; } }
            public Sprite Value { get { return Sprite; } }
        }

        #region Inspector

        [SerializeField] private TextId m_NameId = default;
        [SerializeField] private TextId m_ShortNameId = default;
        [SerializeField, AutoEnum] private ScriptActorTypeFlags m_Flags = 0;

        [Header("Colors")]
        [SerializeField] private bool m_OverrideNamePalette = false;
        [SerializeField, ShowIfField("m_OverrideNamePalette")] private ColorPalette4 m_NameColor = new ColorPalette4(Color.white, Color.grey);
        [SerializeField] private bool m_OverrideTextPalette = false;
        [SerializeField, ShowIfField("m_OverrideTextPalette")] private ColorPalette4 m_TextColor = new ColorPalette4(Color.white, Color.grey);
        [SerializeField] private bool m_OverrideHistoryColor = false;
        [SerializeField, ShowIfField("m_OverrideHistoryColor")] private Color32 m_HistoryColor = Color.white;

        [Header("Portraits")]
        [SerializeField] private Sprite m_DefaultPortrait = null;
        [SerializeField, KeyValuePair("Id", "Sprite")] private PortraitDef[] m_Portraits = null;
        
        [Header("Sounds")]
        [SerializeField] private SerializedHash32 m_DefaultTypeSFX = null;
        [SerializeField, Range(0.1f, 2f)] private float m_TextToSpeechPitch = 1;

        [Header("Job Giver")]
        [SerializeField] private TextId[] m_JobGiverQuotes = null;

        #endregion // Inspector

        [NonSerialized] private float m_TypeSFXDelay = 0;

        [LeafLookup("Name")] public TextId NameId() { return m_NameId; }
        [LeafLookup("ShortName")] public TextId ShortNameId() { return m_ShortNameId.IsEmpty ? m_NameId : m_ShortNameId; }
        public ScriptActorTypeFlags Flags() { return m_Flags; }
        
        public ColorPalette4? NamePaletteOverride() { return m_OverrideNamePalette ? m_NameColor : new ColorPalette4?(); }
        public ColorPalette4? TextPaletteOverride() { return m_OverrideTextPalette ? m_TextColor : new ColorPalette4?(); }
        public Color32? HistoryColorOverride() { return m_OverrideHistoryColor ? m_HistoryColor : new Color32?(); }

        public Sprite DefaultPortrait() { return m_DefaultPortrait; }
        public Sprite Portrait(StringHash32 inId)
        {
            if (inId.IsEmpty)
                return m_DefaultPortrait;

            Sprite spr;
            if (!m_Portraits.TryGetValue(inId, out spr))
            {
                Log.Error("[ScriptActorDefinition] No portrait with id '{0}' found on actor def {1}", inId, name);
                spr = Services.Assets.Characters.ErrorPortrait();
            }
            
            return spr;
        }
        
        public StringHash32 DefaultTypeSfx() { return m_DefaultTypeSFX; }
        public float TTSPitch() { return m_TextToSpeechPitch; }
        public float AdditionalTypingTextDelay {
            get { return m_TypeSFXDelay; }
            set { m_TypeSFXDelay = value; }
        }

        public ListSlice<TextId> JobCompleteQuotes() {
            return m_JobGiverQuotes;
        }

        public bool HasFlags(ScriptActorTypeFlags inFlags)
        {
            return (m_Flags & inFlags) != 0;
        }

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref m_NameId);
            ValidationUtils.StripDebugInfo(ref m_ShortNameId);
            ValidationUtils.StripDebugInfo(ref m_DefaultTypeSFX);

            for(int i = 0; i < m_Portraits.Length; i++) {
                ValidationUtils.StripDebugInfo(ref m_Portraits[i].Id);
            }
        }

        #endif // UNITY_EDITOR
    }

    [Flags]
    public enum ScriptActorTypeFlags
    {
        IsPlayer = 0x01
    }

    public sealed class ScriptCharacterIdAttribute : DBObjectIdAttribute
    {
        public ScriptCharacterIdAttribute()
            : base(typeof(ScriptCharacterDef)) { }

        public override string Name(DBObject inObject) {
            var def = (ScriptCharacterDef) inObject;
            string found = Loc.Find(def.ShortNameId());
            if (string.IsNullOrEmpty(found))
                return "@" + inObject.name;
            return string.Format("@{0}: {1}", inObject.name, found);
        }
    }
}