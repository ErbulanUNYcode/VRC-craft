Shader "VRC_MINE/Editor/TerrainView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        //Blend One One

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D<uint4> _MainTex;
            float4 _MainTex_TexelSize;

            bool _ShowDetails;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _MainTex_TexelSize.zw;
                return o;
            }

            float hash21(int2 p)
			{
				int n = p.x * 374761393 + p.y * 668265263;
				n = (n ^ (n >> 13)) * 1274126177;
				return frac(n * 0.00000000023283064365386963);
			}

            fixed4 frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                float4 r = float4(_MainTex.Load(int3(uv, 0)));
                return r/255;
            }

            ENDCG
        }
    }
}