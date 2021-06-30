using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    static public class ShaderPalettes
    {
        static private readonly Color DefaultLightColor = ColorBank.White;
        static private readonly Color DefaultShadowColor = ColorBank.DarkGray;

        static public int WorldLightColor;
        static public int WorldShadowColor;

        static public int ActorLightColor;
        static public int ActorShadowColor;

        static public int SeaColor0;
        static public int SeaColor1;

        static public int SkyColor0;
        static public int SkyColor1;

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        #endif // UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static private void BeforeSceneLoad()
        {
            InitializeShaderValues();
        }
        
        static private void InitializeShaderValues()
        {
            WorldLightColor = Shader.PropertyToID("_WorldLightColor");
            WorldShadowColor = Shader.PropertyToID("_WorldShadowColor");

            ActorLightColor = Shader.PropertyToID("_ActorLightColor");
            ActorShadowColor = Shader.PropertyToID("_ActorShadowColor");

            SeaColor0 = Shader.PropertyToID("_SeaColor0");
            SeaColor1 = Shader.PropertyToID("_SeaColor1");

            SkyColor0 = Shader.PropertyToID("_SkyColor0");
            SkyColor1 = Shader.PropertyToID("_SkyColor1");

            Shader.SetGlobalColor(WorldLightColor, DefaultLightColor);
            Shader.SetGlobalColor(WorldShadowColor, DefaultShadowColor);

            Shader.SetGlobalColor(ActorLightColor, DefaultLightColor);
            Shader.SetGlobalColor(ActorShadowColor, DefaultShadowColor);
        }
    }
}