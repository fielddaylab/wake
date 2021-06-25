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

        static public int LightColor;
        static public int ShadowColor;

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
            LightColor = Shader.PropertyToID("_LightColor");
            ShadowColor = Shader.PropertyToID("_ShadowColor");

            SeaColor0 = Shader.PropertyToID("_SeaColor0");
            SeaColor1 = Shader.PropertyToID("_SeaColor1");

            Shader.SetGlobalColor(LightColor, DefaultLightColor);
            Shader.SetGlobalColor(ShadowColor, DefaultLightColor);
        }
    }
}