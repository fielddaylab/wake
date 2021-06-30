Shader "Sprites/DayNight/Sea"
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
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVertSea
            #pragma fragment SpriteFragSea
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            fixed4 _SeaColor0;
            fixed4 _SeaColor1;

            struct v2f_sea
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_sea SpriteVertSea(appdata_t IN)
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

            fixed4 SpriteFragSea(v2f_sea IN) : SV_Target
            {
                fixed4 texSample = SampleSpriteTexture (IN.texcoord);
                fixed4 c = lerp(_SeaColor0, _SeaColor1, 1 - texSample.b) * IN.color;
                c.a = texSample.a;

                c.rgb *= c.a;
                return c;
            }

        ENDCG
        }
    }
}