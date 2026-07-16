Shader "Custom/GradientPreview"
{
    Properties
    {
        _Split ("RGB Width", Range(0,1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest On
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float _Split;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.uv.y < _Split)
                {
                    return fixed4(i.color.rgb, 1);
                }

                fixed a = i.color.a;
                return fixed4(a, a, a, 1);
            }

            ENDCG
        }
    }
}