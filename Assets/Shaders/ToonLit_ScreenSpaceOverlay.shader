Shader "Toon/ScreenSpaceOverlay" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_OverlayColor ("Overlay Color", Color) = (0.5,0.5,0.5,1)
		_OverlayTex ("Overlay", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
CGPROGRAM
#pragma surface surf ToonRamp

sampler2D _Ramp;

// custom lighting function that uses a texture ramp based
// on angle between light direction and normal
#pragma lighting ToonRamp exclude_path:prepass
inline half4 LightingToonRamp (SurfaceOutput s, half3 lightDir, half atten)
{
	#ifndef USING_DIRECTIONAL_LIGHT
	lightDir = normalize(lightDir);
	#endif
	
	half d = dot (s.Normal, lightDir)*0.5 + 0.5;
	half3 ramp = tex2D (_Ramp, float2(d,d)).rgb;
	
	half4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2);
	c.a = 0;
	return c;
}


sampler2D _MainTex;
sampler2D _OverlayTex;
float4 _Color;
float4 _OverlayColor;

struct Input {
	float2 uv_MainTex : TEXCOORD0;
	float4 screenPos;

};

void surf (Input IN, inout SurfaceOutput o) {

	float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
	half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	half4 n = tex2D(_OverlayTex, screenUV) * _OverlayColor;
	o.Albedo = c.rgb + n.rgb;
	o.Alpha = c.a;
}
ENDCG

	} 

	Fallback "Diffuse"
}
