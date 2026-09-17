Shader "VRC_MINE/WorldOptimizator"
{
    Properties
    {
        _WorldTex ("World Texture", 2D) = "white" {}
        _CtrlTex ("Ctrl Texture", 2D) = "white" {}
        _IndexTex ("Index Texture", 2D) = "white" {}
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
            texture2D<fixed> _CtrlTex;
            texture2D<uint4> _IndexTex;
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
                int2 ch = ((pos.xz>>4)&31)*int2(256,128);
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
                uint2 uv = floor(i.uv);
                fixed ctrl = _CtrlTex.Load(int3(uv.x,uv.y%192,0));

                if(ctrl==0) return 0;
                if(ctrl==1) discard;
                
                uint4 index = _IndexTex.Load(int3(i.uv,0));
                uint3 pos = uint3(index.x+((index.w&15)<<5),index.y,index.z+(index.w>>4<<5));
                uint4 mm = uint4(32,0,32,0);
                if(uv.y>191)
                {
                    uv.y=((uv.y-192)>>2)%3;
                    if(uv.y==0)
                    {
                        for(int y=0;y<32;y++)
                        {
                            for(int z=0;z<32;z++)
                            {
                                int3 p1=pos+int3(0,y,z);
                                int3 p2=p1-int3(1,0,0);
                                if(show(p1,p2))
                                {
                                    mm.xz = min(mm.xz,int2(y,z));
                                    mm.yw = max(mm.yw,int2(y,z)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                    else if(uv.y==1)
                    {
                        for(int y=0;y<32;y++)
                        {
                            for(int x=0;x<32;x++)
                            {
                                int3 p1=pos+int3(x,y,0);
                                int3 p2=p1-int3(0,0,1);
                                if(show(p1,p2))
                                {
                                    mm.xz = min(mm.xz,int2(y,x));
                                    mm.yw = max(mm.yw,int2(y,x)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                    else
                    {
                        for(int z=0;z<32;z++)
                        {
                            for(int x=0;x<32;x++)
                            {
                                int3 p1=pos+int3(x,0,z);
                                int3 p2=p1-int3(0,1,0);
                                if(show(p1,p2))
                                {
                                    mm.xz = min(mm.xz,int2(z,x));
                                    mm.yw = max(mm.yw,int2(z,x)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                }
                else
                {
                    uv.y=(uv.y>>2)%3;
                    if(uv.y==0)
                    {
                        for(int y=0;y<32;y++)
                        {
                            for(int z=0;z<32;z++)
                            {
                                int3 p1=pos+int3(0,y,z);
                                int3 p2=p1-int3(1,0,0);
                                if(show(p2,p1))
                                {
                                    mm.xz = min(mm.xz,int2(y,z));
                                    mm.yw = max(mm.yw,int2(y,z)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                    else if(uv.y==1)
                    {
                        for(int y=0;y<32;y++)
                        {
                            for(int x=0;x<32;x++)
                            {
                                int3 p1=pos+int3(x,y,0);
                                int3 p2=p1-int3(0,0,1);
                                if(show(p2,p1))
                                {
                                    mm.xz = min(mm.xz,int2(y,x));
                                    mm.yw = max(mm.yw,int2(y,x)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                    else
                    {
                        for(int z=0;z<32;z++)
                        {
                            for(int x=0;x<32;x++)
                            {
                                int3 p1=pos+int3(x,0,z);
                                int3 p2=p1-int3(0,1,0);
                                if(show(p2,p1))
                                {
                                    mm.xz = min(mm.xz,int2(z,x));
                                    mm.yw = max(mm.yw,int2(z,x)+1);
                                }
                            }
                        }
                        mm.xz=min(mm.xz,mm.yw);
                        return mm;
                    }
                }
            }

            ENDHLSL
        }
    }
}