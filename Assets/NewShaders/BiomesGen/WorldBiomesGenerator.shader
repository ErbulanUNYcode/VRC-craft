Shader "VRC_MINE/WorldBiomesGenerator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Seed ("Seed", Int) = 0
        _Size ("Size", Int) = 0
        _AddIslands ("Add Islands", Int) = 0
        _SetClimateZones ("Set Climate Zones", Int) = 0
        _SetBiomes ("Set Biomes", Int) = 0
        _SetMiniBiomes ("Set Mini Biomes", Int) = 0
        _InitRivers ("Init Rivers", Int) = 0
        _OffsetX ("Offset X", Int) = 0
        _OffsetY ("Offset Y", Int) = 0
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

            Texture2D<uint> _MainTex;
            int _Seed;
            int _Size;
            bool _AddIslands;
            bool _SetClimateZones;
            bool _SetBiomes;
            bool _SetMiniBiomes;
            bool _InitRivers;
            int _ClimateProbabilities[4];
            int _BiomeProbabilities[16];
            int _Weights[22];
            int _OffsetX;
            int _OffsetY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * _Size + int2(_OffsetX, _OffsetY);

                return o;
            }

            uint hash2D(int2 p)
            {
                uint h = asuint(p.x) * 374761393u;
                h ^= asuint(p.y) * 668265263u;
                h ^= asuint(_Seed) * 1446658333u;

                h ^= (h >> 13);
                h *= 1274126177u;
                h ^= (p.x + p.y);

                return h;
            }

            uint ret(int2 pos)
            {
                if(!(pos.x&1)&&!(pos.y&1)) return _MainTex.Load(int3(pos>>1, 0));
                else if(!(pos.x&1))
                {
                    uint a = _MainTex.Load(int3(pos>>1, 0));
                    uint b = _MainTex.Load(int3((pos>>1)+int2(0,1), 0));
                    if(a==b) return a;
                    uint h = hash2D(pos);
                    if(a*b==0) return h&3>0?max(a,b):0;
                    else return h&1?a:b;
                }
                else  if(!(pos.y&1))
                {
                    uint a = _MainTex.Load(int3(pos>>1, 0));
                    uint b = _MainTex.Load(int3((pos>>1)+int2(1,0), 0));
                    if(a==b) return a;
                    uint h = hash2D(pos);
                    if(a*b==0) return h&3>0?max(a,b):0;
                    else return h&1?a:b;
                }

                return 0;
            }

            uint frag(v2f i) : SV_Target
            {
                int2 pos = floor(i.uv);
                uint result = 0;
                uint h = hash2D(pos);
                if(!(pos.x&1)||!(pos.y&1)) result = ret(pos);
                else {
                    uint a,b,c,d;
                    if((h>>2)&1)
                    {
                        a = ret(pos+int2(0,1));
                        b = ret(pos+int2(0,-1));
                        c = ret(pos+int2(1,0));
                        d = ret(pos+int2(-1,0));
                    }
                    else
                    {
                        a = ret(pos+int2(0,1));
                        b = ret(pos+int2(1,0));
                        c = ret(pos+int2(0,-1));
                        d = ret(pos+int2(-1,0));
                    }
                    /*else if((h>>1)&1)
                    {
                        a = ret(pos+int2(1,1));
                        b = ret(pos+int2(-1,-1));
                        c = ret(pos+int2(1,-1));
                        d = ret(pos+int2(-1,1));
                    }
                    else
                    {
                        a = ret(pos+int2(1,1));
                        b = ret(pos+int2(-1,1));
                        c = ret(pos+int2(1,-1));
                        d = ret(pos+int2(-1,-1));
                    }*/

                    if(a==b&&a==c&&a==d) result = a;
                    else
                    {
                        int sumo = (a==0?1:0)+(b==0?1:0)+(c==0?1:0)+(d==0?1:0);
                        if(h%(12-sumo*2)<sumo)
                        {
                            result = 0;
                        }
                        else
                        {
                            int a_=a&63;
                            int b_=b&63;
                            int c_=c&63;
                            int d_=d&63;
                            a_=_Weights[a_]*(1+(a_==6?sumo*2:0));
                            b_=_Weights[b_]*(1+(b_==6?sumo*2:0));
                            c_=_Weights[c_]*(1+(c_==6?sumo*2:0));
                            d_=_Weights[d_]*(1+(d_==6?sumo*2:0));

                            uint hp=h%(a_+b_+c_+d_);

                            if(hp<a_) result=a;
                            else hp-=a_;

                            if(result==0)
                            {
                                if(hp<b_) result=b;
                                else hp-=b_;
                            }
                            if(result==0)
                            {
                                if(hp<c_) result=c;
                                else hp-=c_;
                            }
                            if(result==0)
                            {
                                result=d;
                            }
                        }
                    }
                }

                bool isIsland = false;
                if(result==0 && _AddIslands)
                {
                    result = h%9==0;
                    isIsland = true;
                }

                if(result==1 && _SetClimateZones)
                {
                    uint hh = h%(_ClimateProbabilities[0]+_ClimateProbabilities[1]+_ClimateProbabilities[2]+_ClimateProbabilities[3]);

                    if(hh<_ClimateProbabilities[0]) result = 2;
                    else hh-=_ClimateProbabilities[0];

                    if(result == 1)
                    {
                        if(hh<_ClimateProbabilities[1]) result = 3;
                        else hh-=_ClimateProbabilities[1];
                    }

                    if(result == 1)
                    {
                        if(hh<_ClimateProbabilities[2]) result = 4;
                        else hh-=_ClimateProbabilities[2];
                    }

                    if(result == 1) result = 5;
                }

                if(result>5 &&_SetMiniBiomes && ((h>>2)&15))
                {
                    result-=6;
                    result>>=2;
                    result+=2;
                }

                if(result>1 && result < 6 && _SetBiomes)
                {
                    int i0 = (result-2)<<2;
                    int i1 = i0+1;
                    int i2 = i0+2;
                    int i3 = i0+3;
                    int p0 = _BiomeProbabilities[i0];
                    int p1 = _BiomeProbabilities[i1];
                    int p2 = _BiomeProbabilities[i2];
                    int p3 = _BiomeProbabilities[i3];
                    uint r=result;
                    uint hh = h%(p0+p1+p2+p3);

                    if(hh<p0) result = 6+i0;
                    else hh-=p0;

                    if(result == r)
                    {
                        if(hh<p1) result = 7+i0;
                        else hh-=p1;
                    }

                    if(result == r)
                    {
                        if(hh<p2) result = 8+i0;
                        else hh-=p2;
                    }

                    if(result == r) result = 9+i0;
                }

                if(isIsland && result== 17) result-=h%3+1;

                if(result!=0 && _InitRivers) result+=((h&1)<<6)+64;

                return result;
            }

            ENDHLSL
        }
    }
}