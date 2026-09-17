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

            static const float4 Terrain[18] =
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
                float4(37,  0, 0, 4), // Swamp
                float4(45, 10, 7, 4), // DenseForest

                float4(43,  6, 3, 1), // Plain
                float4(44,  8, 5, 2), // Forest
                float4(44,  7, 4, 2), // BrichForest
                float4(44,  6, 4, 2), // SakuraForest

                float4(42,  7, 5, 3), // Desert
                float4(44,  8, 5, 2), // Savanna
                float4(45, 11, 8, 5), // Jungle
                float4(46,  8, 5, 2), // Wasteland
                float4(46,  8, 5, 2)  // Wasteland2
            };

            static const float4 Mountain[18] =
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
                float4(75, 30, 0.50, 0.04), // Wasteland
                float4(45,  0, 0.50, 0.04)  // Wasteland2
            };
            
            static const float ShoreType[18] =
            {
                0, // Ocean
                0, // Tundra

                0.4, // SnowForest
                0.3, // SnowTaiga
                0.4, // SnowPlain

                0.3, // Taiga
                0.3, // DarkForest
                0.2, // Swamp
                0.4, // DenseForest

                0.4, // Plain
                0.4, // Forest
                0.3, // BrichForest
                0.2, // SakuraForest

                0.7, // Desert
                0.6, // Savanna
                0.5, // Jungle   
                0, // Wasteland
                0  // Wasteland2
            };
            static const float ShoreScale[18] =
            {
                0.6, // Ocean
                0.6, // Tundra

                0.4, // SnowForest
                0.4, // SnowTaiga
                0.4, // SnowPlain

                0.55, // Taiga
                0.55, // DarkForest
                0.2, // Swamp
                0.55, // DenseForest

                0.55, // Plain
                0.55, // Forest
                0.55, // BrichForest
                0.55, // SakuraForest

                0.6, // Desert
                0.65, // Savanna
                0.6, // Jungle   
                0, // Wasteland
                0  // Wasteland2
            };

            float lerp1(float a, float b, float t){return a + t * (b - a);}

            float fade(float t){return t * t * (3.0 - 2.0 * t);}

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

            float4 biomesClear[19]=
            {
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0),
                float4(0,0,0,0)
            };

            uint4 frag(v2f i) : SV_Target
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
                float4 biomesWeights[19] = biomesClear;
                uint bb;

                for(int x = 0; x < 7; x++)
                {
                    for(int y = 0; y < 7; y++)
                    {
                        bb = b[x+y*8];
                        int i7 = x+y*7;
                        r1 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&31].x += Distance7[i7];
                        if((bb&63)>>5) biomesWeights[18].x+= Distance7[i7];

                        bb = b[x+1+y*8];
                        r2 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&31].y += Distance7[i7];
                        if((bb&63)>>5) biomesWeights[18].y+= Distance7[i7];

                        bb = b[x+8+y*8];
                        r3 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&31].z += Distance7[i7];
                        if((bb&63)>>5) biomesWeights[18].z+= Distance7[i7];
                        
                        bb = b[x+9+y*8];
                        r4 += Sobel7[i7]*(bb>>6);
                        biomesWeights[bb&31].w += Distance7[i7];
                        if((bb&63)>>5) biomesWeights[18].w+= Distance7[i7];
                    }
                }

                r1 = lerp(lerp(r1,r2,float(pos.x&3)/4),lerp(r3,r4 ,float(pos.x&3)/4),float(pos.y&3)/4);

                for(int i = 0; i < 19; i++)
                {
                    biomesWeights[i].x = lerp(lerp(biomesWeights[i].x,biomesWeights[i].y,float(pos.x&3)/4),lerp(biomesWeights[i].z,biomesWeights[i].w ,float(pos.x&3)/4),float(pos.y&3)/4);
                }

                float river = length(r1)/2.5;
                
                float4 terrain = 0; //terrain
                float4 mountain = 0; //mountain
                
                for(int i = 0; i < 18; i++)
                {
                    terrain+= biomesWeights[i].x*Terrain[i];
                    mountain+= biomesWeights[i].x*Mountain[i];
                }

                terrain/=256;
                mountain/=256;
                terrain.x = lerp(terrain.x,37,biomesWeights[18].x/256);
                terrain.y = lerp(terrain.y,0,biomesWeights[18].x/256);

                float height = terrain.x+fbm(0.0078125*pos,1)*terrain.y+fbm(0.015625*pos,2)*terrain.z+fbm(0.0625*pos,3)*terrain.w;
                float tHeight = height;
                float m = lerp(height,mountain.x+fbm(0.0078125*pos+651,5)*mountain.y,smoothstep(mountain.z,mountain.z+mountain.w,fbm(0.00390625*pos+372,5)));
                height=max(height,m);
                float r = lerp(height,31+fbm(0.0078125*pos+815,4)*5,river/256);
                height=min(height,r);


                float oceanW = biomesWeights[0].x+biomesWeights[1].x;
                float shore = abs(saturate(oceanW/256+(river/256*(1-oceanW/256)))*2-1);
                float h = hash21(pos)*(height<39&&oceanW>0?oceanW:(256.0-oceanW));
                int resultB;
                if(height<39&&(oceanW>0||(oceanW<0.9&&river>0))) resultB = h<biomesWeights[1].x;
                else
                {
                    for(int i = 2; i < 18; i++)
                    {
                        if(h<biomesWeights[i].x)
                        {
                            resultB=i;
                            break;
                        }
                        
                        h-=biomesWeights[i].x;
                    }
                }
                float type = 0;
                float scale = 0;
                for(int i = 2; i < 18; i++)
                {
                    type+=ShoreType[i]*biomesWeights[i].x;
                    scale+=ShoreScale[i]*biomesWeights[i].x;
                }
                scale/=256-oceanW;

                if(scale+fbm(0.0625*pos+6846,3)*0.5>shore)
                {
                    type/=256-oceanW;
                    resultB+=32+((fbm(0.125*pos+686,3)<type)*64)+((noise01(0.13*pos,0.9)>0.8)*128);
                }

                if(height>tHeight-2+fbm(0.0625*pos+954,3)*10)
                {
                    height+=128;
                    if(biomesWeights[17].x>0.2) resultB = 17+(resultB>>5<<5);
                }

                return uint4(height,resultB,0,0);
            }

            ENDHLSL
        }
    }
}