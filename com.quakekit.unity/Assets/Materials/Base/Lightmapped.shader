// Quake style surface: albedo * lightmap * 2, computed in gamma space like the original renderer.
// The lightmap comes from uv2 and is bound per renderer by QKitLightmap. Fullbright pixels (_EmissionMap,
// black elsewhere) keep their texture color regardless of the light, like in Quake. Unlit pass, so it works
// in the built-in pipeline and in URP.
Shader "QuakeKit/Lightmapped"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [NoScaleOffset] _QKitLightmap ("Lightmap", 2D) = "gray" {}
        _QKitOverbright ("Overbright", Float) = 2
        [NoScaleOffset] _EmissionMap ("Emission (fullbright pixels)", 2D) = "black" {}
        _EmissionStrength ("Emission Strength", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Alpha Test", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _ALPHATEST_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _QKitLightmap;
            sampler2D _EmissionMap;
            half _EmissionStrength;
            fixed4 _Color;
            half _QKitOverbright;
            half _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = v.uv2;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                fixed4 albedo = texel * _Color;
#ifdef _ALPHATEST_ON
                clip(albedo.a - _Cutoff);
#endif
                half3 light = tex2D(_QKitLightmap, i.uv2).rgb * _QKitOverbright;
                half3 emission = tex2D(_EmissionMap, i.uv).rgb;
                // share of fullbright pixels in this (filtered) texel, 1 on a fullbright pixel
                half fullbright = saturate(max(emission.r, max(emission.g, emission.b)) /
                                           max(max(texel.r, max(texel.g, texel.b)), 0.001));
#ifdef UNITY_COLORSPACE_GAMMA
                half3 col = lerp(albedo.rgb * light, emission * _EmissionStrength, fullbright);
#else
                half3 lit = saturate(LinearToGammaSpace(albedo.rgb) * light);
                half3 col = GammaToLinearSpace(lerp(lit, LinearToGammaSpace(emission), fullbright)) *
                            lerp(1, _EmissionStrength, fullbright);
#endif
                fixed4 result = fixed4(col, albedo.a);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
