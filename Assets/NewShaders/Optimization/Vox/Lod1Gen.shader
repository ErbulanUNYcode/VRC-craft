Shader "VRC_MINE/Lod1Gen"
{
    Properties
    {
        _WorldTex0 ("World Texture 0", 2D) = "black" {}
        _WorldTex1 ("World Texture 1", 2D) = "black" {}
        _WorldTex2 ("World Texture 2", 2D) = "black" {}
        _WorldTex3 ("World Texture 3", 2D) = "black" {}
        _WorldTex4 ("World Texture 4", 2D) = "black" {}
        _WorldTex5 ("World Texture 5", 2D) = "black" {}
        _WorldTex6 ("World Texture 6", 2D) = "black" {}
        _WorldTex7 ("World Texture 7", 2D) = "black" {}
        _WorldTex8 ("World Texture 8", 2D) = "black" {}
        _WorldTex9 ("World Texture 9", 2D) = "black" {}
        _WorldTex10 ("World Texture 10", 2D) = "black" {}
        _WorldTex11 ("World Texture 11", 2D) = "black" {}
        _WorldTex12 ("World Texture 12", 2D) = "black" {}
        _WorldTex13 ("World Texture 13", 2D) = "black" {}
        _WorldTex14 ("World Texture 14", 2D) = "black" {}
        _WorldTex15 ("World Texture 15", 2D) = "black" {}
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
            
            
            texture2D<fixed> _WorldTex0;
            texture2D<fixed> _WorldTex1;
            texture2D<fixed> _WorldTex2;
            texture2D<fixed> _WorldTex3;
            texture2D<fixed> _WorldTex4;
            texture2D<fixed> _WorldTex5;
            texture2D<fixed> _WorldTex6;
            texture2D<fixed> _WorldTex7;
            texture2D<fixed> _WorldTex8;
            texture2D<fixed> _WorldTex9;
            texture2D<fixed> _WorldTex10;
            texture2D<fixed> _WorldTex11;
            texture2D<fixed> _WorldTex12;
            texture2D<fixed> _WorldTex13;
            texture2D<fixed> _WorldTex14;
            texture2D<fixed> _WorldTex15;
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
                int chunkX = pos.x >> 4;
                int chunkZ = pos.z >> 4;
                int page = ((chunkX >> 4) & 3) | (((chunkZ >> 4) & 3) << 2);
                int2 texPos = int2(
                    ((chunkX & 15) << 8) + (pos.x & 15) + ((pos.z & 15) << 4),
                    ((chunkZ & 15) << 8) + pos.y
                );

                if (page == 0) return _WorldTex0.Load(int3(texPos, 0)) > 0;
                if (page == 1) return _WorldTex1.Load(int3(texPos, 0)) > 0;
                if (page == 2) return _WorldTex2.Load(int3(texPos, 0)) > 0;
                if (page == 3) return _WorldTex3.Load(int3(texPos, 0)) > 0;
                if (page == 4) return _WorldTex4.Load(int3(texPos, 0)) > 0;
                if (page == 5) return _WorldTex5.Load(int3(texPos, 0)) > 0;
                if (page == 6) return _WorldTex6.Load(int3(texPos, 0)) > 0;
                if (page == 7) return _WorldTex7.Load(int3(texPos, 0)) > 0;
                if (page == 8) return _WorldTex8.Load(int3(texPos, 0)) > 0;
                if (page == 9) return _WorldTex9.Load(int3(texPos, 0)) > 0;
                if (page == 10) return _WorldTex10.Load(int3(texPos, 0)) > 0;
                if (page == 11) return _WorldTex11.Load(int3(texPos, 0)) > 0;
                if (page == 12) return _WorldTex12.Load(int3(texPos, 0)) > 0;
                if (page == 13) return _WorldTex13.Load(int3(texPos, 0)) > 0;
                if (page == 14) return _WorldTex14.Load(int3(texPos, 0)) > 0;
                return _WorldTex15.Load(int3(texPos, 0)) > 0;
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
