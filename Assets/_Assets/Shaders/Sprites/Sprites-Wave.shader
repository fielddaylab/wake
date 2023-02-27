// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/Wave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _WaveTex("Wave Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1,1,1,1)
        _WaveDistance("Wave Distance", Vector) = (1,1,1,1)
        _TimeScale("Time Scale", Float) = 1
        _WaveFrequency("Wave Frequency", Float) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
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
            #pragma vertex SpriteVertCutout
            #pragma fragment SpriteFragCutout
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            #define PI 3.1415926538

            float4 _MainTex_TexelSize;
            sampler2D _WaveTex;
            float4 _WaveDistance;
            float _TimeScale;
            float _WaveFrequency;

            struct v2f_cutout
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_cutout SpriteVertCutout(appdata_t IN)
            {
                v2f_cutout OUT;

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


            fixed4 SpriteFragCutout(v2f_cutout IN) : SV_Target
            {
                float4 waveData = tex2D(_WaveTex, IN.texcoord);
                float offset = (waveData.y + IN.texcoord.y * _WaveFrequency) * PI;
                float2 finalCoord = IN.texcoord + (_MainTex_TexelSize.xy * _WaveDistance.xy * waveData.z)
                    * float2(sin(offset + _Time.y * _TimeScale * waveData.x * 1.2), sin(offset * 1.3 + _Time.y * _TimeScale * waveData.x));

                fixed4 c = SampleSpriteTexture (finalCoord) * IN.color;
                c.rgb *= c.a;

                return c;
            }
        ENDCG
        }
    }
}
