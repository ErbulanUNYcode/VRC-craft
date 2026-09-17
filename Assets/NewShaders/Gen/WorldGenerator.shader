Shader "VRC_MINE/WorldGenerator"
{
    Properties
    {
        _Data ("Data", 2D) = "white" {}
        _ChunkPosX ("ChunkPosX", Int) = 0
        _ChunkPosY ("ChunkPosY", Int) = 0
        _Seed ("Seed", Int) = 0
        _CavesOffset ("CavesOffset", Vector) = (0,0,0,0)
        _Ore1Offset ("Ore1Offset", Vector) = (0,0,0,0)
        _Ore2Offset ("Ore2Offset", Vector) = (0,0,0,0)
        _Ore3Offset ("Ore3Offset", Vector) = (0,0,0,0)
        _DirtOffset ("DirtOffset", Vector) = (0,0,0,0)
        _GravelOffset ("GravelOffset", Vector) = (0,0,0,0)
        _GraniteOffset ("GraniteOffset", Vector) = (0,0,0,0)
        _AndesiteOffset ("AndesiteOffset", Vector) = (0,0,0,0)
        _DioriteOffset ("DioriteOffset", Vector) = (0,0,0,0)
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
            
            Texture2D<uint4> _Data;
            int _ChunkPosX;
            int _ChunkPosY;
            int _Seed;
            float4 _CavesOffset;
            float4 _Ore1Offset;
            float4 _Ore2Offset;
            float4 _Ore3Offset;
            float4 _DirtOffset;
            float4 _GravelOffset;
            float4 _GraniteOffset;
            float4 _AndesiteOffset;
            float4 _DioriteOffset;
            /*
                Ocean
                Tundra
                SnowForest
                SnowTaiga
                SnowPlain
                Taiga
                DarkForest
                Swamp
                DenseForest
                Plain
                Forest
                BrichForest
                SakuraForest
                Desert
                Savanna
                Jungle
                Wasteland
            */
            static const int _BiomeGeology[3][17][7] =
            {
                {
                    {25,25,18,17,17,17,17}, // Ocean
                    {25,25,18,17,17,17,17}, // Tundra

                    {15,18,18,17,17,17,17}, // SnowForest
                    {15,18,18,17,17,17,17}, // SnowTaiga
                    {15,18,18,17,17,17,17}, // SnowPlain
                    {19,18,18,17,17,17,17}, // Taiga
                    {19,18,18,17,17,17,17}, // DarkForest
                    {19,18,18,17,17,17,17}, // Swamp
                    {19,18,18,17,17,17,17}, // DenseForest
                    {19,18,18,17,17,17,17}, // Plain
                    {19,18,18,17,17,17,17}, // Forest
                    {19,18,18,17,17,17,17}, // BirchForest
                    {19,18,18,17,17,17,17}, // SakuraForest

                    {27,27,27,28,28,17,17}, // Desert

                    {19,18,18,17,17,17,17}, // Savanna
                    {19,18,18,17,17,17,17}, // Jungle

                    {29,29,30,30,17,17,17}  // Wasteland
                },

                {
                    {25,25,25,18,17,17,17}, // Ocean
                    {25,25,25,18,17,17,17}, // Tundra

                    {15,18,18,18,17,17,17}, // SnowForest
                    {15,18,18,18,17,17,17}, // SnowTaiga
                    {15,18,18,18,17,17,17}, // SnowPlain
                    {19,18,18,18,17,17,17}, // Taiga
                    {19,18,18,18,17,17,17}, // DarkForest
                    {19,18,18,18,17,17,17}, // Swamp
                    {19,18,18,18,17,17,17}, // DenseForest
                    {19,18,18,18,17,17,17}, // Plain
                    {19,18,18,18,17,17,17}, // Forest
                    {19,18,18,18,17,17,17}, // BirchForest
                    {19,18,18,18,17,17,17}, // SakuraForest

                    {27,27,27,27,28,28,17}, // Desert

                    {19,18,18,18,17,17,17}, // Savanna
                    {19,18,18,18,17,17,17}, // Jungle

                    {29,29,29,30,30,17,17}  // Wasteland
                },

                {
                    {25,25,25,25,17,17,17}, // Ocean
                    {25,25,25,25,17,17,17}, // Tundra

                    {15,18,18,18,18,17,17}, // SnowForest
                    {15,18,18,18,18,17,17}, // SnowTaiga
                    {15,18,18,18,18,17,17}, // SnowPlain
                    {19,18,18,18,18,17,17}, // Taiga
                    {19,18,18,18,18,17,17}, // DarkForest
                    {19,18,18,18,18,17,17}, // Swamp
                    {19,18,18,18,18,17,17}, // DenseForest
                    {19,18,18,18,18,17,17}, // Plain
                    {19,18,18,18,18,17,17}, // Forest
                    {19,18,18,18,18,17,17}, // BirchForest
                    {19,18,18,18,18,17,17}, // SakuraForest

                    {27,27,27,27,27,28,28}, // Desert

                    {19,18,18,18,18,17,17}, // Savanna
                    {19,18,18,18,18,17,17}, // Jungle

                    {29,29,29,29,30,30,17}  // Wasteland
                }
            };
            //3 depth
            //5 blocks
            static const int _ClayGeology[3][5] = 
            {
                {26,26,18,17,17},
                {26,26,26,17,17},
                {26,26,26,18,17}
            };
            //3 biome types
            //2 types
            //3 depth
            //5 blocks
            static const int _ShoreGeology[3][2][3][5] =//
            {
                {//Ocean
                    {//type 1
                        {18,18,17,17,17},
                        {18,18,18,17,17},
                        {18,18,18,18,17}
                    },
                    {//type 2
                        {27,27,28,28,17},
                        {27,27,27,28,17},
                        {27,27,27,28,28}
                    }
                },
                {//Snow
                    {//type 1
                        {15,18,18,17,17},
                        {15,18,18,18,17},
                        {15,18,18,18,18}
                    },
                    {//type 2
                        {15,27,27,28,17},
                        {15,27,27,28,28},
                        {15,27,27,27,28}
                    }
                },
                {//Simple
                    {//type 1
                        {19,18,18,17,17},
                        {19,18,18,18,17},
                        {19,18,18,18,18}
                    },
                    {//type 2
                        {27,27,28,28,17},
                        {27,27,27,28,17},
                        {27,27,27,28,28}
                    }
                }
            };

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
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                o.uv = v.uv * float2(256, 128);
                
                return o;
            }
            
            float hash31(int3 p)
            {
                int n = p.x * 374761393 + p.y * 668265263 + p.z * 1446658333;
                n = (n ^ (n >> 13)) * 1274126177;
                return frac(n * 0.00000000023283064365386963); // 1/2^32
            }
            
            float lerp1(float a, float b, float t){return a + t * (b - a);}
            
            float fade(float t){return t * t * (3.0 - 2.0 * t);}
            
            float noise3D(float3 p)
            {
                int3 i = (int3)floor(p);
                float3 f = frac(p);

                float3 u = float3(fade(f.x), fade(f.y), fade(f.z));

                float4 h4 = lerp(
                    float4(
                        hash31(i),
                        hash31(i + int3(1, 0, 0)),
                        hash31(i + int3(0, 1, 0)),
                        hash31(i + int3(1, 1, 0))
                    ),
                    float4(
                        hash31(i + int3(0, 0, 1)),
                        hash31(i + int3(1, 0, 1)),
                        hash31(i + int3(0, 1, 1)),
                        hash31(i + int3(1, 1, 1))
                    ),
                    u.z
                );

                float2 h2 = lerp(h4.xy, h4.zw, u.y);

                return lerp(h2.x, h2.y, u.x);
            }
            
            float noise3D_opt(float3 p, float act)
            {
                act = act * 0.5 + 0.5;

                int3 i = (int3)floor(p);
                float3 f = frac(p);

                float4 h0 = float4(
                    hash31(i),
                    hash31(i + int3(1, 0, 0)),
                    hash31(i + int3(0, 1, 0)),
                    hash31(i + int3(1, 1, 0))
                );

                float4 h1 = float4(
                    hash31(i + int3(0, 0, 1)),
                    hash31(i + int3(1, 0, 1)),
                    hash31(i + int3(0, 1, 1)),
                    hash31(i + int3(1, 1, 1))
                );

                if (h0.x < act && h0.y < act && h0.z < act && h0.w < act &&
                    h1.x < act && h1.y < act && h1.z < act && h1.w < act)
                    return 0;

                float3 u = float3(fade(f.x), fade(f.y), fade(f.z));

                float4 h4 = lerp(h0, h1, u.z);
                float2 h2 = lerp(h4.xy, h4.zw, u.y);

                return lerp(h2.x, h2.y, u.x) * 0.5 + 0.5;
            }

            bool fbm3D(float3 p)
            {
                float value = noise3D_opt(p, 0.75);

                if (value == 0) return false;

                float v = noise3D_opt(p * 2, 2.5 - value * 2.0) * 0.5;

                if (v == 0) return false;
                value += v;

                value += noise3D_opt(p * 4, 6.0 - value * 4.0) * 0.25;

                return value > 1.5;
            }
            
            float fbmCave(float3 p)
            {
                float value = 0;
                
                value += noise3D(p) * 0.5;
                value += noise3D(p * 2.0) * 0.25;
                
                return value / 0.75;
            }
            
            float hash21(int2 p)
            {
                uint h = asuint(p.x);
                h ^= asuint(p.y) * 0x9E3779B9u;
                h ^= asuint(_Seed) * 0x85EBCA6Bu;

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

                return h * 0.00000000023283064365386963;
            }

            uint hash1D(int p)
            {
                uint h = asuint(p) ^ asuint(_Seed);
                h *= 0x9E3779B9u;
                h ^= h >> 16;
                return h;
            }
            
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                
                float2 u = float2(fade(f.x), fade(f.y));
                
                float x1 = lerp1(a, b, u.x);
                float x2 = lerp1(c, d, u.x);
                
                return lerp1(x1, x2, u.y);
            }

            float fbm2D(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise2D((p+o*4096)*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }
            
            fixed frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int3 pos = int3((uv.x&15)+(_ChunkPosX), uv.y, (uv.x>>4)+(_ChunkPosY));
                if(pos.y<3&&pos.y<hash21(pos.xz)*3) return 16.0/255;//bedrock
                uint4 h = _Data.Load(int3(pos.xz&15,0)); 

                bool m = h.x>>7;
                h.x&=127;
                //if(pos.y==127) return float(h.x)/255;//height map
                //if(pos.y==126) return float(h.y)/255;//biome map
                int b = h.y&31;
                if(h.x<pos.y)
                {
                    if(pos.y==39&&b>0&&b<5) return 13.0/255;//ice
                    return 0;//air
                }

                float3 cavePos = pos+_CavesOffset;
                cavePos.y*=1.5;
                float cave1 = (fbmCave(cavePos/40)-0.5)*30;
                cavePos+=220;
                float cave2 = (fbmCave(cavePos/40)-0.5)*30;
                bool cave = cave1<1&&cave1>-1&&cave2<1&&cave2>-1;
                
                uint delay = h.x-pos.y;

                if(cave&&(h.x>42||(h.x<43&&delay>5))) return 0;/////////
                
                int per = fbm2D(pos.xz*0.07,3)*3;
                float result=0;
                if(m)
                {
                    if(delay<per-1)
                        m=false;
                    else
                        result = 17;
                }
                if(m)
                {
                    if(b>15 && delay<7) result = (hash1D(pos.y)%7)+31;
                }
                else if(h.y>>7&&h.x<39)
                {
                    result = delay<5?_ClayGeology[per][delay]:17;
                }
                else if(h.y>>5&1)
                {
                    int Btype = b;
                    Btype = h.x<39?0:Btype<5?1:2;
                    result = delay<5?_ShoreGeology[Btype][h.y>>6&1][per][delay]:17;
                }
                else
                {
                    if(h.x<39&&delay==0)delay++;
                    result = delay<7?_BiomeGeology[per][min(h.y,16)][delay]:17;
                }


                if(result==17)
                {
                    float sp =hash31(pos+_Ore1Offset);
                    if(pos.y==h.x-1||sp>0.9)
                    if((hash31((pos>>2)+_Ore3Offset)>0.98||hash31(((pos+2)>>2)+_Ore2Offset)>0.99)&&sp>0.25) result=20;//coal ore
                    else if(h.x-3<pos.y||hash21(pos.xz>>1)>0.3)
                    {
                        float sp1 = hash31((pos>>1)+_Ore2Offset);
                        float sp2 = hash31(((pos+1)>>1)+_Ore3Offset);
                        if((sp1>0.995||sp2>0.995)&&sp>0.2) result=21;//iron ore
                        else if(pos.y<15&&(sp1>0.993||sp2>0.993)&&sp>0.5) result=22;//gold ore
                        else if(pos.y<15&&(sp1>0.98||sp2>0.98)&&sp>0.5) result=23;//diamond ore
                    }
                }

                if(result==17)
                {
                    float3 pp = 0.1*float3(pos);
                    if(fbm3D(pp+_DirtOffset)) result=18;//dirt
                    else if(fbm3D(pp+_GravelOffset)) result=25;//gravel
                    else if(fbm3D(pp+_GraniteOffset)) result=59;//granite
                    else if(fbm3D(pp+_DioriteOffset)) result=60;//diorite
                    else if(fbm3D(pp+_AndesiteOffset)) result=61;//andesite
                }

                if(delay==0 && result == 18 && b>1) result = b<5?15:19;//dirt on face to grass
                
                return result/255;
            }
            
            ENDHLSL
        }
    }
}