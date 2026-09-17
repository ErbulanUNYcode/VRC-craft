Shader "VRC_MINE/WorldOptimizatorIndex"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityCustomRenderTexture.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                noperspective float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * float2(512,384);

                return o;
            }

            uint4 frag(v2f i) : SV_Target
            {
                uint2 uv = floor(i.uv);
                uv.y%=192;
                int2 ch = (uv/int2(32,12));
                uv%=int2(32,12);
                
                if(uv.y<4)
                {
                    return uint4(uv.x,uv.y<<5,0,ch.x+(ch.y<<4));
                }
                else if(uv.y<8)
                {
                    uv.y-=4;
                    return uint4(0,uv.y<<5,uv.x,ch.x+(ch.y<<4));
                }
                else
                {
                    uv.y-=8;
                    return uint4(0,uv.x+(uv.y<<5)+1,0,ch.x+(ch.y<<4));
                }
            }

            ENDHLSL
        }
    }
}