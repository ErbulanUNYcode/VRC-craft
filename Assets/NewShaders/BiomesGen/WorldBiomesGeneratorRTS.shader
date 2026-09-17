Shader "VRC_MINE/WorldBiomesGeneratorRiverToSea"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Int) = 0
        _Orient ("Orient", Vector) = (0,0,0,0)
        _Seed ("Seed", Float) = 0
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

            static const int _BiomeTransitionMap[256] =
            {
              // 0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15
                16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,//0  special Wasteland
                 1,  1,  1,  1,  1,  1,  1,  1,  9,  1,  1,  9,  8,  1,  1,  1,//1  Snow Forest
                 2,  2,  2,  2,  2,  4,  4,  2,  2,  2,  4,  9,  4,  4,  4,  4,//2  Snow Taiga Forest
                 3,  3,  3,  3,  3,  3,  3,  1,  3,  3,  3,  9,  3,  3,  3,  8,//3  Snow Plain
                 2,  2,  4,  4,  4,  4,  4,  4,  4,  4,  4,  9,  4,  4,  4,  4,//4  Taiga Forest
                 1,  5,  1,  1,  5,  5,  5,  5,  9,  5,  5,  9,  5,  9,  5,  5,//5  Dark Forest
                 3,  6,  6,  8,  6,  6,  6,  6,  6,  6,  9,  9,  6,  6,  6,  6,//6  Swamp
                 1,  7,  1,  9,  7,  7,  7,  7,  7,  7,  7,  7,  7,  7,  7,  7,//7  Dense Forest
                 3,  9,  3,  8,  8,  8,  8,  8,  8,  8,  8,  9,  8,  8,  8,  8,//8  Plain
                 3,  9,  1,  1,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,//9  Forest
                 3, 10, 10,  8, 10, 10,  9, 10, 10, 10, 10, 10, 10, 10, 10, 10,//10 Birch Forest
                 9,  9,  9,  9,  9,  9,  9, 11,  9, 11, 11, 11,  9,  9,  9,  9,//11 Sakura Forest
                 3,  8, 12,  8, 12, 12, 12, 12,  8, 12, 12,  9, 12, 12, 12, 12,//12 Desert
                 3, 13, 13,  3, 13, 13, 13, 13,  8, 13, 13,  9, 13, 13, 13, 13,//13 Savanna
                 8, 14, 14,  8, 14, 14, 14, 14, 14, 14, 14,  9, 14, 14, 14, 12,//14 Jungle
                 3, 15, 15,  8, 15, 15, 15, 15,  8, 15, 15,  9, 15, 15, 12, 15 //15 Wasteland
            };


            Texture2D<uint> _MainTex;
            int _Size;
            int4 _Orient;
            int _Seed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * _Size;

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

            uint frag(v2f i) : SV_Target
            {
                int2 pos = floor(i.uv);
                
                uint result = _MainTex.Load(int3(pos, 0));

                uint4 neigh = uint4
                (
                    _MainTex.Load(int3(pos + _Orient.xy, 0)),
                    _MainTex.Load(int3(pos - _Orient.xy, 0)),
                    _MainTex.Load(int3(pos + _Orient.yx, 0)),
                    _MainTex.Load(int3(pos - _Orient.yx, 0))
                );

                uint o = result>>6;
                if(o==0)
                {
                    result&=63;
                    uint
                    a = neigh.x>>6;
                    if(a!=0) return result+(a<<6);
                    a = neigh.y>>6;
                    if(a!=0) return result+(a<<6);
                    a = neigh.z>>6;
                    if(a!=0) return result+(a<<6);
                    a = neigh.w>>6;
                    if(a!=0) return result+(a<<6);
                }

                if((result&63)>1 && !((result&63)>>5))
                {
                    uint key = result>>6;
                    if(
                        ((neigh.x&31)>1 && ((neigh.x&63)>>5)) ||
                        ((neigh.y&31)>1 && ((neigh.y&63)>>5)) ||
                        ((neigh.z&31)>1 && ((neigh.z&63)>>5)) ||
                        ((neigh.w&31)>1 && ((neigh.w&63)>>5))
                    )
                    result += 32;
                    else if(
                        ((neigh.x&31)<2) || (neigh.x>>6)!=(result>>6) ||
                        ((neigh.y&31)<2) || (neigh.y>>6)!=(result>>6) ||
                        ((neigh.z&31)<2) || (neigh.z>>6)!=(result>>6) ||
                        ((neigh.w&31)<2) || (neigh.w>>6)!=(result>>6)
                    )
                    result+=((hash2D(pos)>>1)&7)==0?32:0;
                }

                if((result&31)==13 || (result&31)==16 || (result&31)==17)
                {
                    uint key = result>>6;
                    uint key2 = (result&63)>>5<<5;
                    if(
                        ((neigh.x&31)>1 && key!=(neigh.x>>6)) || 
                        ((neigh.y&31)>1 && key!=(neigh.y>>6)) || 
                        ((neigh.z&31)>1 && key!=(neigh.z>>6)) || 
                        ((neigh.w&31)>1 && key!=(neigh.w>>6)))
                    result = ((hash2D(pos)&15)==0?9:(result&31))+(key<<6) + key2;
                }

                if((result&31)==16)
                {
                    if(
                        ((neigh.x&31)==17) || 
                        ((neigh.y&31)==17) || 
                        ((neigh.z&31)==17) || 
                        ((neigh.w&31)==17)) return result+1;

                    if(
                        ((neigh.x&31)!=16) || 
                        ((neigh.y&31)!=16) || 
                        ((neigh.z&31)!=16) || 
                        ((neigh.w&31)!=16)
                    )
                    {
                        if((hash2D(pos)&7)==0) return result+1;
                    }
                    
                }

                if((result&31)>1 && (result&31)<17)
                {
                    uint key = result>>6;
                    uint key2 = (result&63)>>5<<5;
                    result = (result&31)-1;
                    if(result==0) return 20;
                    if((neigh.x&31)>0) result = _BiomeTransitionMap[(result<<4) + min((neigh.x&31)-1,15)];
                    if((neigh.y&31)>0) result = _BiomeTransitionMap[(result<<4) + min((neigh.y&31)-1,15)];
                    if((neigh.z&31)>0) result = _BiomeTransitionMap[(result<<4) + min((neigh.z&31)-1,15)];
                    if((neigh.w&31)>0) result = _BiomeTransitionMap[(result<<4) + min((neigh.w&31)-1,15)]; 
                    result += (key<<6)+1 + key2;
                }

                return result;
            }

            ENDHLSL
        }
    }
}