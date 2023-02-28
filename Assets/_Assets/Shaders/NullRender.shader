// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Null Render"
{
    Properties
    {
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
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            struct v2f_cutout
            {
            };

            v2f_cutout SpriteVertCutout(appdata_t IN)
            {
                v2f_cutout OUT;
                return OUT;
            }


            fixed4 SpriteFragCutout(v2f_cutout IN) : SV_Target
            {
                clip(-1);
                return fixed4(0, 0, 0, 0);
            }
        ENDCG
        }
    }
}
