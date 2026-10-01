Shader "VRC_MINE/UIOverLayerLocalizer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EyeOffset ("Eye Offset", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
            "RenderType"="Transparent"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _EyeOffset;

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 eyeOffset = lerp(-_EyeOffset.xyz, _EyeOffset.xyz, unity_StereoEyeIndex);

                float3 viewPos = mul((float3x3)UNITY_MATRIX_V, worldPos - eyeOffset);

                o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                return texColor * i.color;
            }

            ENDCG
        }
    }
}