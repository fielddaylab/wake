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
		ZTest Equal
        Blend Off

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVertCutout
            #pragma fragment SpriteFragCutout
            #pragma target 2.0
            #include "UnityCG.cginc"

			struct vertex_t
			{
			};

            struct v2f_cutout
            {
            };

            v2f_cutout SpriteVertCutout(vertex_t IN)
            {
                v2f_cutout OUT;
                return OUT;
            }


            fixed4 SpriteFragCutout(v2f_cutout IN) : SV_Target
            {
				return fixed4(0, 0, 0, 0);
            }
        ENDCG
        }
    }
}
