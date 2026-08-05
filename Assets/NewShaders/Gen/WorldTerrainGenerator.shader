Shader "VRC_MINE/WorldTerrainGenerator"
{
    Properties
    {
        _BiomesData ("BiomesData", 2D) = "white" {} 
        _ChunkPosX ("ChunkPosX", Int) = 0
        _ChunkPosY ("ChunkPosY", Int) = 0
        _ReadOffsetX ("ReadOffsetX", Int) = 0
        _ReadOffsetY ("ReadOffsetY", Int) = 0
        _Seed ("Seed", Int) = 0
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

            Texture2D<uint> _BiomesData;
            int _ReadOffsetX;
            int _ReadOffsetY;
            int _ChunkPosX;
            int _ChunkPosY;
            int _TestViewIndex;
            int _Seed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv * 16;

                return o;
            }

            static const int2 Sobel7[49] =
            {
                 int2( 1,  1), int2( 6,  4), int2(15,  5), int2(20,  0), int2(15, -5), int2(6, -4), int2( 1, -1),
                 int2( 4,  6), int2(24, 24), int2(60, 30), int2(80,  0), int2(60,-30), int2(24, -24), int2( 4, -6),
                 int2( 5, 15), int2(30, 60), int2(75, 75), int2(100, 0), int2(75,-75), int2(30,-60), int2( 5,-15),
                 int2( 0, 20), int2( 0, 80), int2( 0,100), int2( 0,  0), int2( 0,-100), int2( 0, -80), int2( 0, -20),
                 int2(-5, 15), int2(-30, 60), int2(-75, 75), int2(-100, 0), int2(-75,-75), int2(-30,-60), int2(-5,-15),
                 int2(-4,  6), int2(-24, 24), int2(-60, 30), int2(-80, 0), int2(-60,-30), int2(-24, -24), int2(-4, -6),
                 int2(-1,  1), int2(-6,  4), int2(-15,  5), int2(-20,  0), int2(-15, -5), int2(-6, -4), int2(-1, -1)
            };

            static const uint Distance7[49] =
            {
                 1,  2,  3,  4,  3,  2,  1,
                 2,  4,  6,  8,  6,  4,  2,
                 3,  6,  9, 12,  9,  6,  3,
                 4,  8, 12, 16, 12,  8,  4,
                 3,  6,  9, 12,  9,  6,  3,
                 2,  4,  6,  8,  6,  4,  2,
                 1,  2,  3,  4,  3,  2,  1
            };
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

            static const float4 Terrain[17] =
            // x = average terrain height
            // y = octave 1 amplitude          (scale 128)
            // z = octaves 2-3 amplitude       (scales 64, 32)
            // w = octaves 4-6 amplitude       (scales 16, 8, 4)
            {
                float4(15, 10, 7, 5), // Ocean
                float4(15, 10, 7, 5), // Tundra

                float4(45,  8, 5, 2), // SnowForest
                float4(46, 10, 6, 3), // SnowTaiga
                float4(43,  6, 3, 1), // SnowPlain

                float4(46, 10, 6, 3), // Taiga
                float4(45,  9, 6, 3), // DarkForest
                float4(39,  0, 0, 3), // Swamp
                float4(45, 10, 7, 4), // DenseForest

                float4(43,  6, 3, 1), // Plain
                float4(44,  8, 5, 2), // Forest
                float4(44,  7, 4, 2), // BrichForest
                float4(44,  6, 4, 2), // SakuraForest

                float4(42,  7, 5, 3), // Desert
                float4(44,  8, 5, 2), // Savanna
                float4(45, 11, 8, 5), // Jungle
                float4(46,  8, 5, 2)  // Wasteland
            };

            static const float4 Mountain[17] =
            // x = average mountain height
            // y = mountain surface noise amplitude
            // z = mountain appearance threshold
            //     lower value = mountains occupy more territory
            // w = smoothstep transition width
            //     lower value = harder and steeper mountain rise
            {
                float4(15,  0, 1.00, 0.00), // Ocean
                float4(15,  0, 1.00, 0.00), // Tundra

                float4(68, 14, 0.72, 0.18), // SnowForest
                float4(76, 16, 0.62, 0.16), // SnowTaiga
                float4(60, 10, 0.82, 0.22), // SnowPlain

                float4(74, 16, 0.64, 0.17), // Taiga
                float4(69, 14, 0.70, 0.18), // DarkForest
                float4(40,  0, 1.00, 0.00), // Swamp
                float4(72, 15, 0.67, 0.17), // DenseForest

                float4(58, 10, 0.84, 0.23), // Plain
                float4(65, 13, 0.75, 0.20), // Forest
                float4(62, 11, 0.78, 0.21), // BrichForest
                float4(60, 10, 0.80, 0.22), // SakuraForest

                float4(61,  9, 0.80, 0.13), // Desert
                float4(65, 11, 0.74, 0.16), // Savanna
                float4(75, 17, 0.63, 0.18), // Jungle
                float4(86, 10, 0.40, 0.07)  // Wasteland
            };

            float lerp1(float a, float b, float t){return a + t * (b - a);}

            float fade(float t){return t * t * (3.0 - 2.0 * t);}

            float hash21(int2 p)
            {
                /*int n = p.x * 374761393 + p.y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                return frac(n * 0.00000000023283064365386963); // 1/2^32*/

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

            float noise01(float2 p, float ch)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i)>ch;
                float b = hash21(i + float2(1, 0))>ch;
                float c = hash21(i + float2(0, 1))>ch;
                float d = hash21(i + float2(1, 1))>ch;

                float2 u = float2(fade(f.x), fade(f.y));

                float x1 = lerp1(a, b, u.x);
                float x2 = lerp1(c, d, u.x);

                return lerp1(x1, x2, u.y);
            }
            
            float fbm(float2 p,int o)
            {
                float value = 0;
                for(int i = 0; i < o; i++)
                {
                    value+=noise2D((p+o*4096)*(1<<i))/(1<<(i+1));
                }

                return value/(1.0-1.0/(1<<o));
            }

            int4 biomesClear[17]=
            {
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0),
                int4(0,0,0,0)
            };

            uint2 frag(v2f i) : SV_Target
            {
                int2 uv = i.uv;
                int2 pos = int2(uv.x+(_ChunkPosX), uv.y+(_ChunkPosY));
                int2 rPos = int2(uv.x+(_ReadOffsetX), uv.y+(_ReadOffsetY))>>2;
                uint b[64];//read 8x8
                for(int x = 0; x < 8; x++)
                {
                    for(int y = 0; y < 8; y++)
                    {
                        b[x+y*8] = _BiomesData.Load(int3(int2(rPos.x+x, rPos.y+y)+64, 0));
                    }
                }
                
                int2 r1 = 0;
                int2 r2 = 0;
                int2 r3 = 0;
                int2 r4 = 0;
                int4 biomesWeights[17] = biomesClear;
                uint bb;

                for(int x = 0; x < 7; x++)
                {
                    for(int y = 0; y < 7; y++)
                    {
                        bb = b[x+y*8];
                        int i7 = x+y*7;
                        r1 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&63].x += Distance7[i7];

                        bb = b[x+1+y*8];
                        r2 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&63].y += Distance7[i7];

                        bb = b[x+8+y*8];
                        r3 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&63].z += Distance7[i7];

                        bb = b[x+9+y*8];
                        r4 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&63].w += Distance7[i7];
                    }
                }

                r1 = lerp(lerp(r1,r2,float(pos.x&3)/4),lerp(r3,r4 ,float(pos.x&3)/4),float(pos.y&3)/4);

                for(int i = 0; i < 17; i++)
                {
                    biomesWeights[i].x = lerp(lerp(biomesWeights[i].x,biomesWeights[i].y,float(pos.x&3)/4),lerp(biomesWeights[i].z,biomesWeights[i].w ,float(pos.x&3)/4),float(pos.y&3)/4);
                }

                float river = length(r1)/2.5;
                
                float4 terrain = 0; //terrain
                float4 mountain = 0; //mountain

                for(int i = 0; i < 17; i++)
                {
                    terrain+= biomesWeights[i].x*Terrain[i];
                    mountain+= biomesWeights[i].x*Mountain[i];
                }

                terrain/=256;
                mountain/=256;

                float height = terrain.x+fbm(float2(pos.x,pos.y)/128,1)*terrain.y+fbm(float2(pos.x,pos.y)/64,2)*terrain.z+fbm(float2(pos.x,pos.y)/16,3)*terrain.w;
                float m = lerp(height,mountain.x+fbm(float2(pos.x,pos.y)/128+651,5)*mountain.y,smoothstep(mountain.z,mountain.z+mountain.w,fbm(float2(pos.x,pos.y)/256+372,5)));
                height=max(height,m);
                float r = lerp(height,31+fbm(float2(pos.x,pos.y)/128+815,4)*5,river/255);   
                height=min(height,r);
                return uint2(round(height),0);
            }

            ENDHLSL
        }
    }
}