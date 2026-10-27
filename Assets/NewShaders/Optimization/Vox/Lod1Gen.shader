Shader "VRC_MINE/Lod1Gen"
{
    Properties
    {
        _WorldTex ("World Texture", 2D) = "black" {}
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
            
            
            texture2D<fixed> _WorldTex;
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
                o.uv = v.uv * 2048;

                return o;
            }

            bool block(int3 pos)
            {
                int2 ch = ((pos.xz>>4)&63)<<8;
                pos.xz&=15;
                pos.x+=pos.z<<4;
                return _WorldTex.Load(int3(pos.xy+ch,0))>0;
            }

            uint frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int3 pos = int3(uv.x&255,(uv.x>>8)+(uv.y>>8<<3),uv.y&255);

                fixed ctrl = _CtrlTex.Load(int3(pos.xz,0));
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
