Shader "VRC_MINE/WorldOptimizator"
{
    Properties
    {
        _WorldTex ("Generated Texture", 2D) = "white" {}
        _ChunkX ("Chunk X", Int) = 0
        _ChunkY ("Chunk Y", Int) = 0
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

            texture2D<fixed> _WorldTex;
            int _ChunkX;
            int _ChunkY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * float2(512,384);

                return o;
            }

            
            int block(int3 pos)
            {
                if(pos.y>127) return 0;
                int2 ch = ((pos.xz>>4)&31)*int2(256,128);//31 = ch-1
                pos.xz&=15;
                pos.x+=pos.z*16;
                return round(_WorldTex.Load(int3(pos.xy+ch,0))*255);
            }

            bool show(int3 pos1,int3 pos2)
            {
                int2 b = int2(block(pos1),block(pos2));
                return b.y==0 && b.x!=b.y;
            }

            uint4 frag(v2f i) : SV_Target
            {
                uint2 pos = i.uv;
                bool face = pos.y>192;
                pos.y%=192;
                if(pos.y<64)//64 = mch*4
                {
                    int3 p = int3(pos.x,(pos.y&3)*32,(pos.y>>2)*32);
                    if(((p.x>>4)!=(_ChunkX&31)&&((p.x-1)>>4)!=(_ChunkX&31))||(p.z)>>5!=(_ChunkY&31)/2) discard;
                    uint4 mm = uint4(32,0,32,0);
                    for(int z = 0;z<32;z++)
                    {
                        for(int y = 0;y<32;y++)
                        {
                            int3 p1=p+int3(0,y,z);
                            int3 p2=p1;
                            p2.x--;
                            if(face?show(p1,p2):show(p2,p1))
                            {
                                mm.xz=min(mm.xz,int2(z,y));
                                mm.yw=max(mm.yw,int2(z,y)+1);
                            }
                        }
                    }
                    mm.xz = min(mm.xz,mm.yw);
                    return mm;
                }
                else if(pos.y<128)//128 = mch*8
                {
                    pos.y-=64;
                    int3 p = int3((pos.y>>2)*32,(pos.y&3)*32,pos.x);
                    if(((p.z>>4)!=(_ChunkY&31)&&((p.z-1)>>4)!=(_ChunkY&31))||(p.x)>>5!=(_ChunkX&31)/2) discard;
                    uint4 mm = uint4(32,0,32,0);
                    for(int x = 0;x<32;x++)
                    {
                        for(int y = 0;y<32;y++)
                        {
                            int3 p1=p+int3(x,y,0);
                            int3 p2=p1;
                            p2.z--;
                            if(face?show(p1,p2):show(p2,p1))
                            {
                                mm.xz=min(mm.xz,int2(x,y));
                                mm.yw=max(mm.yw,int2(x,y)+1);
                            }
                        }
                    }
                    mm.xz = min(mm.xz,mm.yw);
                    return mm;
                }
                else
                {
                    uint s=pos.x+(pos.y-128)*512;//128 = mch*8, 512 = mch*32
                    uint2 ch=uint2((s>>7)%16, (s>>7)/16);//16,16 = mch
                    if(ch.x!=(_ChunkX&31)/2||ch.y!=(_ChunkY&31)/2) discard;
                    ch*=32;
                    uint l = s&127;
                    uint4 mm = uint4(32,0,32,0);
                    for(int x = 0;x<32;x++)
                    {
                        for(int z = 0;z<32;z++)
                        {
                            int3 p1=int3(x+ch.x,l,z+ch.y);
                            int3 p2=p1;
                            p2.y++;
                            if(face?show(p1,p2):show(p2,p1))
                            {
                                mm.xz=min(mm.xz,int2(x,z));
                                mm.yw=max(mm.yw,int2(x,z)+1);
                            }
                        }
                    }
                    mm.xz = min(mm.xz,mm.yw);
                    return mm;
                }


                return uint4(0,255,0,1);
            }

            ENDHLSL
        }
    }
}