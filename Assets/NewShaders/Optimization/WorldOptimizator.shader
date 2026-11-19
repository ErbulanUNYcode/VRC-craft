Shader "VRC_MINE/WorldOptimizator"
{
    Properties
    {
        _WorldTex ("World Texture", 3D) = "black" {}
        _CtrlTex ("Ctrl Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityCustomRenderTexture.cginc"
            #include "SurfaceVisibility.cginc"

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
            
            Texture3D<uint> _WorldTex;
            texture2D<fixed> _CtrlTex;
            v2f vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                v2f o;
                o.pos = base.vertex;
                o.uv = base.globalTexcoord.xy * float2(512,384);

                return o;
            }

            
            int block(int3 pos)
            {
                if (pos.y < 0 || pos.y >= 128) return 0;
                return _WorldTex.Load(int4(pos.x & 511, 511 - (pos.z & 511), pos.y, 0));
            }

            bool show(int3 pos1,int3 pos2)
            {
                int2 b = int2(block(pos1),block(pos2));
                return ShouldDrawFaceOneWay(b.y, b.x);
            }

            uint4 frag(v2f i) : SV_Target
            {
                uint2 uv = floor(i.uv);
                fixed ctrl = _CtrlTex.Load(int3(uv.x,uv.y%192,0));

                if (ctrl == 0) return 0;
                if (ctrl == 1) discard;
                
                uint sourceY = uv.y;
                uint4 mm = uint4(32,0,32,0);
                if(uv.y>191)
                {
                    sourceY-=192;
                    uv.y=(sourceY>>2)%3;
                    if(uv.y==0)
                    {
                        uint3 pos = uint3(uv.x,(sourceY%12&3)<<5,(sourceY/12)<<5);
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
                        uint3 pos = uint3((uv.x>>5)<<5,(sourceY%12&3)<<5,(uv.x&31)+((sourceY/12)<<5));
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
                        uint3 pos = uint3((uv.x>>5)<<5,(uv.x&31)+((sourceY%12&3)<<5)+1,(sourceY/12)<<5);
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
                        uint3 pos = uint3(uv.x,(sourceY%12&3)<<5,(sourceY/12)<<5);
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
                        uint3 pos = uint3((uv.x>>5)<<5,(sourceY%12&3)<<5,(uv.x&31)+((sourceY/12)<<5));
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
                        uint3 pos = uint3((uv.x>>5)<<5,(uv.x&31)+((sourceY%12&3)<<5)+1,(sourceY/12)<<5);
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
