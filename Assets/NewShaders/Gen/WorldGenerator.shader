Shader "VRC_MINE/WorldGenerator"
{
    Properties
    {
        _Data ("Data", 2D) = "black" {}
        _StructureAtlas ("Structure Atlas", 2D) = "black" {}
        _ChunkPosX ("ChunkPosX", Int) = 0
        _ChunkPosY ("ChunkPosY", Int) = 0
        _Seed ("Seed", Int) = 0
        [IntRange] _DebugBiome ("Debug Biome", Range(0, 17)) = 0
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
            #include "UnityCustomRenderTexture.cginc"

            struct MineCustomRenderTextureV2F
            {
                float4 vertex : SV_POSITION;
                float3 localTexcoord : TEXCOORD0;
                float3 globalTexcoord : TEXCOORD1;
                uint primitiveID : TEXCOORD2;
                float3 direction : TEXCOORD3;
                nointerpolation uint biomeID : TEXCOORD4;
                nointerpolation uint4 structureMetadata : TEXCOORD5;
                nointerpolation int4 structureRaster : TEXCOORD6;
                nointerpolation int3 structureInfo : TEXCOORD7;
            };

            Texture2D<uint4> _Data;
            Texture2D<float4> _StructureAtlas;
            int _ChunkPosX;
            int _ChunkPosY;
            int _Seed;
            int _DebugBiome;
            float4 _CavesOffset;
            float4 _Ore1Offset;
            float4 _Ore2Offset;
            float4 _Ore3Offset;
            float4 _DirtOffset;
            float4 _GravelOffset;
            float4 _GraniteOffset;
            float4 _AndesiteOffset;
            float4 _DioriteOffset;
            static const int TEXTURE_SIZE = 256;
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

            float hash21(int2 p)
            {
                uint h = hash2D(p);

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

            uint4 ReadTerrainData(int2 worldPosition)
            {
                int2 terrainPosition = worldPosition - int2(_ChunkPosX, _ChunkPosY) + 8;
                return _Data.Load(int3(terrainPosition, 0));
            }

            uint4 ReadStructureAtlas(int2 position)
            {
                return (uint4)round(_StructureAtlas.Load(int3(position, 0)) * 255.0);
            }

            // Возвращает сгенерированное байтовое значение в мировой точке.
            // Ноль означает воздух.
            int GenerateTerrainValue(int3 pos, bool surfaceOnly)
            {
                uint4 h = ReadTerrainData(pos.xz);

                if(!surfaceOnly && pos.y==255)
                {
                    uint terrainHeight = h.x&127;
                    if(terrainHeight!=0) terrainHeight += 40;
                    return terrainHeight;
                }
                if(!surfaceOnly && pos.y==254)
                    return h.y&31;

                bool m = h.x>>7;
                h.x&=127;
                h.x+=40;
                int b = h.y&31;
                if(!surfaceOnly && h.x<pos.y)
                {
                    if(pos.y==79&&b>0&&b<5) return 13;//ice
                    return 0;//air
                }

                float3 cavePos = pos+_CavesOffset;
                cavePos.y*=1.5;
                float cave1 = (fbmCave(cavePos/40)-0.5)*30;
                cavePos+=220;
                float cave2 = (fbmCave(cavePos/40)-0.5)*30;
                bool cave = cave1<1&&cave1>-1&&cave2<1&&cave2>-1;

                uint delay = h.x-pos.y;

                if(cave&&(h.x>42||(h.x<43&&delay>5))) return 0;

                int per = fbm2D(pos.xz*0.07,3)*3;
                int result=0;
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

                return result;
            }

            int2 GetZonePosition(uint batchZoneIndex)
            {
                return (int2)CustomRenderTextureCenters[batchZoneIndex].xy;
            }

            int3 GetZoneSize(uint batchZoneIndex)
            {
                return (int3)CustomRenderTextureSizesAndRotations[batchZoneIndex].xyz;
            }

            float4 LocalRectToZone(int4 localRect)
            {
                float2 size = localRect.zw;
                float2 center = float2(
                    localRect.x + size.x * 0.5,
                    TEXTURE_SIZE - localRect.y - size.y * 0.5
                );
                return float4(center, size);
            }

            float2 LocalRectToVertexPosition(
                int4 localRect,
                float2 originalVertexPosition,
                uint batchZoneIndex)
            {
                float4 zone = LocalRectToZone(localRect);
                float2 originalCenter = CustomRenderTextureCenters[batchZoneIndex].xy;
                float2 originalSize = CustomRenderTextureSizesAndRotations[batchZoneIndex].xy;
                float2 originalCenterClip = originalCenter * (2.0 / TEXTURE_SIZE) - 1.0;
                float2 scaledVertexOffset =
                    (originalVertexPosition - originalCenterClip) * (zone.zw / originalSize);
                float2 centerOffset = (zone.xy - originalCenter) * (2.0 / TEXTURE_SIZE);
                return originalCenterClip + scaledVertexOffset + centerOffset;
            }

            MineCustomRenderTextureV2F vert(appdata_customrendertexture v)
            {
                v2f_customrendertexture base = CustomRenderTextureVertexShader(v);
                MineCustomRenderTextureV2F o;

                uint batchZoneIndex = (v.vertexID / 6) % kCustomTextureBatchSize;

                o.vertex = base.vertex;
                o.localTexcoord = base.localTexcoord;
                o.globalTexcoord = base.globalTexcoord;
                o.primitiveID = base.primitiveID;
                o.direction = base.direction;
                o.biomeID = 0;
                o.structureMetadata = 0;
                o.structureRaster = 0;
                o.structureInfo = 0;

                if (base.primitiveID != 0 && base.primitiveID != 409)
                {
                    int2 zoneCenter = GetZonePosition(batchZoneIndex);
                    int2 zoneWorldPosition = int2(_ChunkPosX, _ChunkPosY) + zoneCenter;
                    uint zoneHash = hash2D(zoneWorldPosition);
                    int2 zoneOffset = int2(zoneHash & 3u, (zoneHash >> 2) & 3u);
                    int2 zonePosition = zoneCenter + zoneOffset;
                    uint4 rootTerrainData = ReadTerrainData(
                        int2(_ChunkPosX, _ChunkPosY) + zonePosition
                    );
                    o.biomeID = rootTerrainData.y & 31u;
                    uint structureTicket = (zoneHash >> 4) & 255u;
                    uint4 structureMetadata = ReadStructureAtlas(
                        int2((int)structureTicket, (int)o.biomeID)
                    );
                    if (structureMetadata.g == 0u)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }
                    uint packedSize = structureMetadata.b;
                    int2 structureSize = int2(
                        (packedSize & 7u) + 2u,
                        ((packedSize >> 3) & 7u) + 2u
                    );
                    int2 structureRoot = (structureSize - 1) / 2 + int2(
                        (packedSize >> 6) & 1u,
                        (packedSize >> 7) & 1u
                    );

                    int zoneLayer = GetZoneSize(batchZoneIndex).z;
                    int structureLayer = zoneLayer + structureRoot.y - 4;
                    if (structureLayer < 0 || structureLayer >= structureSize.y)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }

                    int rootHeight = (int)(rootTerrainData.x & 127u);
                    if (rootHeight == 0)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }
                    rootHeight += 40;

                    int minimumTreeHeight = o.biomeID == 7u ? 78 : 79;
                    if (rootHeight < minimumTreeHeight)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }

                    int rootBlock = GenerateTerrainValue(int3(
                        _ChunkPosX + zonePosition.x,
                        rootHeight,
                        _ChunkPosY + zonePosition.y
                    ), true);
                    if (rootBlock != 15 && rootBlock != 18 && rootBlock != 19)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }

                    int structureHeight = (int)(structureMetadata.a & 31u) + 3;
                    int variableHeight = (int)(structureMetadata.a >> 5);
                    int heightGrowth = variableHeight == 0
                        ? 0
                        : (int)((zoneHash >> 20) % (uint)(variableHeight + 1));
                    int renderedHeight = structureHeight + heightGrowth;

                    int layerZ = zonePosition.y + zoneLayer - 4;
                    if (layerZ < 0 || layerZ >= 16)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }

                    int structureLeft = zonePosition.x - structureRoot.x;
                    int clippedLeft = max(structureLeft, 0);
                    int clippedRight = min(structureLeft + structureSize.x, 16);
                    int clippedBottom = max(rootHeight, 0);
                    int clippedTop = min(rootHeight + renderedHeight, TEXTURE_SIZE);
                    if (clippedLeft >= clippedRight || clippedBottom >= clippedTop)
                    {
                        o.vertex.xy = float2(1000.0, 1000.0);
                        return o;
                    }

                    int outputLeft = layerZ * 16 + clippedLeft;
                    o.structureMetadata = structureMetadata;
                    o.structureRaster = int4(
                        outputLeft,
                        clippedBottom,
                        clippedLeft - structureLeft,
                        clippedBottom - rootHeight
                    );
                    o.structureInfo = int3(
                        structureLayer,
                        structureSize.x,
                        heightGrowth
                    );
                    o.vertex.xy = LocalRectToVertexPosition(
                        int4(
                            outputLeft,
                            clippedBottom,
                            clippedRight - clippedLeft,
                            clippedTop - clippedBottom
                        ),
                        base.vertex.xy,
                        batchZoneIndex
                    );
                    o.globalTexcoord.xy = o.vertex.xy * 0.5 + 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                        o.globalTexcoord.y = 1.0 - o.globalTexcoord.y;
                    #endif
                }

                return o;
            }
            
            fixed frag(MineCustomRenderTextureV2F i) : SV_Target
            {
                if (i.primitiveID == 0)
                    return 0;

                if (i.primitiveID != 409)
                {
                    int2 outputPixel = min(
                        (int2)(i.globalTexcoord.xy * TEXTURE_SIZE),
                        TEXTURE_SIZE - 1
                    );
                    int localX = i.structureRaster.z + outputPixel.x - i.structureRaster.x;
                    int grownY = i.structureRaster.w + outputPixel.y - i.structureRaster.y;
                    int atlasY = grownY;
                    if (i.structureInfo.z > 0 && grownY > 1)
                        atlasY = grownY <= i.structureInfo.z + 1
                            ? 1
                            : grownY - i.structureInfo.z;

                    int atlasX = (int)i.structureMetadata.r
                        + i.structureInfo.x * i.structureInfo.y
                        + localX;
                    uint4 structurePixel = ReadStructureAtlas(int2(
                        atlasX,
                        (int)i.structureMetadata.g + atlasY
                    ));

                    if ((structurePixel.r | structurePixel.g | structurePixel.b | structurePixel.a) == 0u)
                        discard;

                    uint settings = structurePixel.a;
                    bool protectedPixel = (settings & 16u) != 0u;
                    bool mandatoryPixel = (settings & 32u) != 0u;
                    bool protectedPass = i.primitiveID >= 265 && i.primitiveID <= 408;
                    bool mandatoryPass = i.primitiveID >= 410;
                    bool optionalPass = i.primitiveID <= 264;
                    if ((optionalPass && (protectedPixel || mandatoryPixel)) ||
                        (protectedPass && (!protectedPixel || mandatoryPixel)) ||
                        (mandatoryPass && !mandatoryPixel))
                        discard;

                    int3 worldPosition = int3(
                        _ChunkPosX + (outputPixel.x & 15),
                        outputPixel.y,
                        _ChunkPosY + (outputPixel.x >> 4)
                    );
                    uint appearanceHash = hash2D(worldPosition.xz + int2(atlasX * 131, atlasY * 197));
                    if ((settings & 4u) != 0u)
                        appearanceHash ^= asuint(worldPosition.y) * 0x9E3779B9u;
                    if ((appearanceHash & 3u) > (settings & 3u))
                        discard;

                    uint variantHash = hash2D(worldPosition.xz + int2(atlasX * 269, atlasY * 353));
                    if ((settings & 8u) != 0u)
                        variantHash ^= asuint(worldPosition.y) * 0x85EBCA6Bu;
                    uint variantCount = (settings & 64u) != 0u ? 3u : 2u;
                    uint variant = variantHash % variantCount;
                    uint block = variant == 0u ? structurePixel.r :
                        (variant == 1u ? structurePixel.g : structurePixel.b);
                    if (block == 0u) discard;

                    return (fixed)block / 255.0;
                }

                int2 uv = (int2)(i.globalTexcoord.xy * TEXTURE_SIZE);
                int3 pos = int3((uv.x&15)+(_ChunkPosX), uv.y, (uv.x>>4)+(_ChunkPosY));
                if(pos.y<3&&pos.y<hash21(pos.xz)*3) return 16.0/255;//bedrock

                int result = GenerateTerrainValue(pos, false);
                if(result==0) discard;
                return float(result)/255;
            }
            
            ENDHLSL
        }
    }
}
