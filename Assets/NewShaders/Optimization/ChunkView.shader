Shader "VRC_MINE/Debug/ChunkView"
{
    Properties
    {
        _MainTex ("Chunk Data", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed value = tex2D(_MainTex, i.uv).r;
                float layerUV = frac(i.uv.x * 16.0);
                float borderDistance = min(layerUV, 1.0 - layerUV);
                fixed separatorStrength = borderDistance < (1.0 / 128.0) ? 0.35 : 0.0;
                separatorStrength *= i.uv.x > 0.0 && i.uv.x < 1.0;
                fixed color = value + (1.0 - value) * separatorStrength;
                return fixed4(color, color, color, 1.0);
            }
            ENDHLSL
        }
    }
}
