using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Script Actor Definition")]
    public class ScriptActorDef : DBObject
    {
        [Serializable]
        private struct PortraitDef : IKeyValuePair<StringHash32, Sprite>
        {
            public SerializedHash32 Id;
            public Sprite Sprite;

            public StringHash32 Key { get { return Id; } }
            public Sprite Value { get { return Sprite; } }
        }

        #region Inspector

        [SerializeField] private SerializedHash32 m_NameId = null;
        [SerializeField, AutoEnum] private ScriptActorTypeFlags m_Flags = 0;

        [Header("Colors")]
        [SerializeField] private bool m_OverrideNamePalette = false;
        [SerializeField, ShowIfField("m_OverrideNamePalette")] private ColorPalette4 m_NameColor = new ColorPalette4(Color.white, Color.grey);
        [SerializeField] private bool m_OverrideTextPalette = false;
        [SerializeField, ShowIfField("m_OverrideTextPalette")] private ColorPalette4 m_TextColor = new ColorPalette4(Color.white, Color.grey);
        [SerializeField] private bool m_OverrideHistoryColor = false;
        [SerializeField, ShowIfField("m_OverrideHistoryColor")] private Color32 m_HistoryColor;

        [Header("Portraits")]
        [SerializeField] private Sprite m_DefaultPortrait = null;
        [SerializeField, KeyValuePair("Id", "Sprite")] private PortraitDef[] m_Portraits = null;
        
        [Header("Sounds")]
        [SerializeField] private SerializedHash32 m_DefaultTypeSFX = null;

        #endregion // Inspector

        public StringHash32 NameId() { return m_NameId; }
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
                Debug.LogErrorFormat("[ScriptActorDefinition] No portrait with id '{0}' found on actor def {1}", inId.ToString(), name);
                spr = Services.Assets.Characters.ErrorPortrait();
            }
            
            return spr;
        }
        
        public StringHash32 DefaultTypeSfx() { return m_DefaultTypeSFX; }

        public bool HasFlags(ScriptActorTypeFlags inFlags)
        {
            return (m_Flags & inFlags) != 0;
        }
    }

    [Flags]
    public enum ScriptActorTypeFlags
    {
        IsPlayer = 0x01,
    }
}