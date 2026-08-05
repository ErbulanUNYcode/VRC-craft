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

            Texture2D<uint2> _MainTex;
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
                float2 r = _MainTex.Load(int3(uv, 0));
                /*uv.x++;
                float2 r2 = _MainTex.Load(int3(uv, 0));
                uv.y++;
                float2 r4 = _MainTex.Load(int3(uv, 0));
                uv.x--;
                float2 r3 = _MainTex.Load(int3(uv, 0));
                
                r1 = lerp(r1, r2, i.uv.x%1);
                r3 = lerp(r3, r4, i.uv.x%1);
                r1 = lerp(r1, r3, i.uv.y%1);*/

                //if(r1.x==255) discard;
                return fixed4((r-15)/80,0,1);
            }

            ENDCG
        }
    }
}