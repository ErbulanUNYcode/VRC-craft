Shader "Hidden/DepthToR_Cutout"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Cutoff ("Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }

        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i, float4 svPos : SV_Position) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.uv).a;

                // alpha clipping
                clip(alpha - _Cutoff);

                // глубина как в Z-buffer
                float depth = svPos.z;

                return depth;
            }

            ENDCG
        }
    }
}