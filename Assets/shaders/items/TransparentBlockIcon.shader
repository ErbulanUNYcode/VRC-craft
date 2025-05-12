Shader "Custom/UnlitBlockIcon"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _BlockID ("Block ID", Range(1,68)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 100
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cutoff;
            float _BlockID;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 objNormal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.objNormal = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.objNormal);

                float2 uv = i.uv;

                float id = _BlockID*2;
                
                if (n.y > 0.5)
                {
                    uv = (uv+float2(id%32,floor(id/32)*3))/32;
                }
                else if (n.y < -0.5)
                {
                    uv = 1-uv;
                    uv = (uv+float2(id%32+1,floor(id/32)*3))/32;
                }
                else if (n.x < -0.5)
                {
                    uv = (uv+float2(id%32+1,floor(id/32)*3+2))/32;
                }
                else if (n.x > 0.5)
                {
                    uv = uv;
                    uv = (uv+float2(id%32+1,floor(id/32)*3+2))/32;
                }
                else if (n.z > 0.5)
                {
                    uv = uv;
                    uv = (uv+float2(id%32+1,floor(id/32)*3+1))/32;
                }
                else if (n.z < -0.5)
                {
                    uv = 1-uv;
                    uv = (uv+float2(id%32+1,floor(id/32)*3+1))/32;
                }
                
                fixed4 col = tex2D(_MainTex, frac(uv));

                clip(col.a-_Cutoff);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent Cutout"
}
