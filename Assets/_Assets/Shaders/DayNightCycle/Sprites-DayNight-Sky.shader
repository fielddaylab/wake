Shader "Sprites/DayNight/Sky"
{
     Properties
     {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
     }
 
     SubShader
     {
        Tags
        { 
            "Queue"="Geometry" 
            "IgnoreProjector"="True" 
            "RenderType"="Geometry" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite On
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVertSky
            #pragma fragment SpriteFragSky
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            fixed4 _SkyColor0;
            fixed4 _SkyColor1;

            struct v2f_sea
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_sea SpriteVertSky(appdata_t IN)
            {
                v2f_sea OUT;

                UNITY_SETUP_INSTANCE_ID (IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                #ifdef UNITY_INSTANCING_ENABLED
                IN.vertex.xy *= _Flip.xy;
                #endif

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SpriteFragSky(v2f_sea IN) : SV_Target
            {
                fixed4 texSample = SampleSpriteTexture (IN.texcoord);
                fixed4 c = lerp(_SkyColor0, _SkyColor1, 1 - texSample.b) * IN.color;
                return c;
            }

        ENDCG
        }
    }
}