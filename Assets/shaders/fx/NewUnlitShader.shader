Shader "Custom/FakeNormalLighting"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Intensity ("Light Intensity", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);

                // Фейковое освещение с 3 осей
                float r = saturate(dot(n, float3(1,0,0)));
                float g = saturate(dot(n, float3(0,1,0)));
                float b = saturate(dot(n, float3(0,0,1)));

                float3 lightColor = float3(r, g, b) * _Intensity;
                float3 baseColor = tex2D(_MainTex, i.uv).rgb;

                return float4(baseColor * lightColor, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
