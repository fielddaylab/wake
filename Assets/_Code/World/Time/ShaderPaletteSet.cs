using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class ShaderPaletteSet : MonoBehaviour
    {
        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor WorldLightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor WorldShadowPalette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor ActorLightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor ActorShadowPalette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor SeaColor0Palette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor SeaColor1Palette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor SkyColor0Palette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public ShaderColor SkyColor1Palette;

        #endregion // Inspector

        private void OnEnable()
        {
            Shader.SetGlobalColor(ShaderPalettes.WorldLightColor, WorldLightPalette.Color);
            Shader.SetGlobalColor(ShaderPalettes.WorldShadowColor, WorldShadowPalette.Color);
            Shader.SetGlobalColor(ShaderPalettes.ActorLightColor, ActorLightPalette.Color);
            Shader.SetGlobalColor(ShaderPalettes.ActorShadowColor, ActorShadowPalette.Color);
            Shader.SetGlobalColor(ShaderPalettes.SeaColor0, SeaColor0Palette.Color);
            Shader.SetGlobalColor(ShaderPalettes.SeaColor1, SeaColor1Palette.Color);
            Shader.SetGlobalColor(ShaderPalettes.SkyColor0, SkyColor0Palette.Color);
            Shader.SetGlobalColor(ShaderPalettes.SkyColor1, SkyColor1Palette.Color);
        }

        #if UNITY_EDITOR

        [ContextMenu("Generate Sea and Sky alt colors")]
        private void AutoGenerateSeaAndSkyAlts()
        {
            SeaColor1Palette = ShaderColor.Darken(SeaColor0Palette, 0.5f);
            SkyColor1Palette = ShaderColor.Darken(SkyColor0Palette, 0.5f);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR
    }
}