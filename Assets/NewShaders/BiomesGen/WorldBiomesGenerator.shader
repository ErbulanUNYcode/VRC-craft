Shader "VRC_MINE/WorldBiomesGenerator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Seed ("Seed", Int) = 0
        _ClimateSeed ("Climate Seed", Int) = 0
        _ClimateScale ("Climate Scale", Float) = 4
        _Size ("Size", Int) = 0
        _AddIslands ("Island Chance (out of 64)", Range(0, 64)) = 0
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
                float2 climateUV : TEXCOORD1;
            };

            Texture2D<uint> _MainTex;
            int _Seed;
            int _ClimateSeed;
            float _ClimateScale;
            int _Size;
            int _AddIslands;
            bool _SetClimateZones;
            bool _SetBiomes;
            bool _SetMiniBiomes;
            bool _InitRivers;
            bool _CheckSpawn;
            //int _ClimateProbabilities[4];
            static const int _ClimateProbabilities[4] = {30, 18, 25, 40};
            //int _BiomeProbabilities[16];
            static const int _BiomeProbabilities[17] = 
            {
                40,15,30,15,
                30,20,10,40,
                35,30,20,2,
                30,25,30,10,10
            };
            //int _Weights[22];
            static const int _Weights[22] =
            {
                0,
                12,60,80,45,
                90,55,35,100,
                100,90,75,50,
                100,80,100,80,
                80,80,100,70,
                1
            };

            int _OffsetX;
            int _OffsetY;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * _Size + int2(_OffsetX, _OffsetY);
                o.climateUV = v.uv;

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

            float climateHash21(int2 p)
            {
                uint h = asuint(p.x);
                h ^= asuint(p.y) * 0x9E3779B9u;
                h ^= asuint(_ClimateSeed) * 0x85EBCA6Bu;

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

                return h * 0.00000000023283064365386963;
            }

            float climateNoise2D(float2 p)
            {
                float2 grid = floor(p);
                float2 fraction = frac(p);
                float2 blend = fraction * fraction * (3.0 - 2.0 * fraction);

                float a = climateHash21(grid);
                float b = climateHash21(grid + float2(1, 0));
                float c = climateHash21(grid + float2(0, 1));
                float d = climateHash21(grid + float2(1, 1));

                return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
            }

            float climateFbm2D(float2 p)
            {
                float value = climateNoise2D((p + 8192) * 1.0) * 0.5;
                value += climateNoise2D((p + 8192) * 2.0) * 0.25;
                return value / 0.75;
            }

            uint climateZone(float2 normalizedUV)
            {
                uint value = (uint)(climateFbm2D(normalizedUV * _ClimateScale) *
                    (_ClimateProbabilities[0]+_ClimateProbabilities[1]+_ClimateProbabilities[2]+_ClimateProbabilities[3]));

                if(value<_ClimateProbabilities[0]) return 17;
                value-=_ClimateProbabilities[0];
                if(value<_ClimateProbabilities[1]) return 18;
                value-=_ClimateProbabilities[1];
                if(value<_ClimateProbabilities[2]) return 19;
                return 20;
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
                            int a_=a&31;
                            int b_=b&31;
                            int c_=c&31;
                            int d_=d&31;
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
                if(result==0 && _AddIslands>0)
                {
                    bool isSpawnCenter =
                        pos.x == ((_Size - 1) >> 1) &&
                        pos.y == ((_Size - 1) >> 1);
                    result = (h&63) < _AddIslands*(isSpawnCenter?16:1);
                    result = result || (_CheckSpawn && isSpawnCenter);
                    result*=21;
                    isIsland = true;
                }

                if(result==21 && _SetClimateZones)
                {
                    result = climateZone(i.climateUV);
                }

                uint biome = result&31;
                bool isMiniBiome = false;
                if(biome!=0 && biome<17 && !(biome>=13 && biome<=16) && _SetMiniBiomes && ((h>>2)&63)<30)
                {
                    biome = climateZone(i.climateUV);
                    result = (result&~31u) + biome;
                    isMiniBiome = true;
                }

                biome = result&31;
                if(biome>16 && biome<21 && _SetBiomes)
                {
                    int i = (biome-17)<<2;
                    int p0 = _BiomeProbabilities[i];
                    int p1 = _BiomeProbabilities[i+1];
                    int p2 = _BiomeProbabilities[i+2];
                    int p3 = _BiomeProbabilities[i+3];
                    // Сакура — только мини-биом; пустошь — только обычный биом.
                    if((!isMiniBiome || isIsland) && i==8) p3 = 0;
                    if(isIsland && i==12) p2 = 0;
                    if((isMiniBiome || isIsland) && i==12) p3 = 0;
                    uint r=biome;
                    uint hh = h%(p0+p1+p2+p3);

                    if(hh<p0) biome = 1+i;
                    else hh-=p0;

                    if(biome == r)
                    {
                        if(hh<p1) biome = 2+i;
                        else hh-=p1;
                    }

                    if(biome == r)
                    {
                        if(hh<p2) biome = 3+i;
                        else hh-=p2;
                    }

                    if(biome == r) biome = 4+i;
                    result = (result&~31u)+biome;
                }

                if(_InitRivers && result>1 && result<64) result+=((h&1)<<6)+64;

                return result;
            }

            ENDHLSL
        }
    }
}
