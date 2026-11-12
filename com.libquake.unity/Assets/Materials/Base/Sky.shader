// Quake's sky, ported from libquake's tools/qviewer/shaders/sky.frag: two layers on a flattened sphere around
// the viewer. The back layer is solid and scrolls at 8 units per second, the front layer (clouds, alpha 0 where
// the back layer shows) at 16. No lightmap. Fog is left to the render pipeline. Unlit pass, so it works in the
// built-in pipeline and in URP.
Shader "LibQuake/Sky"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Back Layer", 2D) = "black" {}
        [NoScaleOffset] _SkyFront ("Front Layer (clouds)", 2D) = "black" {}
        _BackSpeed ("Back Layer Speed", Float) = 8
        _FrontSpeed ("Front Layer Speed", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            sampler2D _SkyFront;
            float _BackSpeed;
            float _FrontSpeed;

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // the layers are 128 texels wide, st is in texels
            float2 LayerUV(float2 st, float speed)
            {
                float2 uv = (st + fmod(_Time.y * speed, 128.0)) / 128.0;
                // the wad importer stores rows bottom up, quake samples them top down
                return float2(uv.x, -uv.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // direction from the eye in quake axes (unity (x, y, z) is quake (-y, z, x)), squashed vertically
                // like the original. the scale of the map does not matter, only the direction
                float3 d = i.worldPos - _WorldSpaceCameraPos;
                float3 dir = float3(d.z, -d.x, d.y * 3.0);
                float2 st = dir.xy * (6.0 * 63.0 / length(dir));

                fixed4 back = tex2D(_MainTex, LayerUV(st, _BackSpeed));
                fixed4 front = tex2D(_SkyFront, LayerUV(st, _FrontSpeed));
                fixed4 col = fixed4(lerp(back.rgb, front.rgb, front.a), 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
