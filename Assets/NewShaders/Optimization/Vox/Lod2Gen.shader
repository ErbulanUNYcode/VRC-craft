Shader "VRC_MINE/Lod2Gen"
{
    Properties
    {
        _PrevLodTex ("World Texture", 2D) = "black" {}
        _CtrlTex ("Ctrl Texture", 2D) = "black" {}
    }

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
            
            
            texture2D<fixed> _PrevLodTex;
            texture2D<fixed> _CtrlTex;

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
                o.uv = v.uv * 256;

                return o;
            }

            bool block(int3 pos)
            {
                int2 layer = int2(pos.y&7,pos.y>>3)<<8;
                return _PrevLodTex.Load(int3((pos.xz&255)+layer,0))>0;
            }

            uint frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int3 pos = int3(uv.x&63,(uv.x>>6)+(uv.y>>6<<2),uv.y&63);

                fixed ctrl = _CtrlTex.Load(int3(pos.xz>>2,0));
                if(ctrl == 0) return 0;
                if(ctrl == 1) discard;

                pos<<=2;

                bool b0 = 
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.x+=2;

                bool b1 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.y+=2;

                bool b3 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.x-=2;

                bool b2 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.z+=2;

                bool b6 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.x+=2;

                bool b7 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.y-=2;

                bool b5 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                pos.x-=2;

                bool b4 =
                    block(pos/*+int3(0)*/)||
                    block(pos+int3(1,0,0))||
                    block(pos+int3(0,1,0))||
                    block(pos+int3(1,1,0))||
                    block(pos+int3(0,0,1))||
                    block(pos+int3(1,0,1))||
                    block(pos+int3(0,1,1))||
                    block(pos+int3(1,1,1));

                return uint(b0|b1<<1|b2<<2|b3<<3|b4<<4|b5<<5|b6<<6|b7<<7);
            }

            ENDHLSL
        }
    }
}
