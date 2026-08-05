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
        _CheckSpawn ("Check Spawn", Int) = 0
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
            bool _CheckSpawn;
            //int _ClimateProbabilities[4];
            static const int _ClimateProbabilities[4] = {10, 25, 45, 20};
            //int _BiomeProbabilities[16];
            static const int _BiomeProbabilities[16] = 
            {
                40,15,30,15,
                30,20,10,40,
                35,30,20,2,
                35,30,25,10
            };
            //int _Weights[22];
            static const int _Weights[22] =
            {
                0,
                12,60,80,45,
                90,55,35,100,
                100,90,75,10,
                100,80,100,35,
                35,80,100,70,
                1
            };
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
                uint h = asuint(p.x);
                h ^= asuint(p.y) * 0x9E3779B9u;
                h ^= asuint(_Seed) * 0x85EBCA6Bu;

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

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

                bool isIsland = !_SetMiniBiomes;
                if(result==0 && _AddIslands)
                {
                    result = ((h%9==0)||(_CheckSpawn&&pos.x==((_Size-1)>>1)&&pos.y==((_Size-1)>>1)));
                    result*=21;
                    isIsland = true;
                }

                if(result==21 && _SetClimateZones)
                {
                    uint hh = h%(_ClimateProbabilities[0]+_ClimateProbabilities[1]+_ClimateProbabilities[2]+_ClimateProbabilities[3]);

                    if(hh<_ClimateProbabilities[0]) result = 17;
                    else hh-=_ClimateProbabilities[0];

                    if(result == 21)
                    {
                        if(hh<_ClimateProbabilities[1]) result = 18;
                        else hh-=_ClimateProbabilities[1];
                    }

                    if(result == 21)
                    {
                        if(hh<_ClimateProbabilities[2]) result = 19;
                        else hh-=_ClimateProbabilities[2];
                    }

                    if(result == 21) result = 20;
                }

                if(result!=0 && result<17 &&_SetMiniBiomes && ((h>>2)&15))
                {
                    result--;
                    result>>=2;
                    result+=17;
                }

                if(result>16 && result < 21 && _SetBiomes)
                {
                    int i = (result-17)<<2;
                    int p0 = _BiomeProbabilities[i];
                    int p1 = _BiomeProbabilities[i+1];
                    int p2 = _BiomeProbabilities[i+2];
                    int p3 = _BiomeProbabilities[i+3];
                    uint r=result;
                    uint hh = h%(p0+p1+p2+p3);

                    if(hh<p0) result = 1+i;
                    else hh-=p0;

                    if(result == r)
                    {
                        if(hh<p1) result = 2+i;
                        else hh-=p1;
                    }

                    if(result == r)
                    {
                        if(hh<p2) result = 3+i;
                        else hh-=p2;
                    }

                    if(result == r) result = 4+i;
                }

                if(isIsland && result== 12) result-=(h%3)+1;

                if(_InitRivers && result>1 && result<64) result+=((h&1)<<6)+64;

                return result;
            }

            ENDHLSL
        }
    }
}